<#
    Freito single-host deployment workflow for the Plesk host.
    Credentials are supplied by the local, ignored deploy-single-host.ps1.
#>
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Server,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Username,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Password,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$RemotePath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$DbConnStr,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$JwtSigningKey,

    [string]$CorsOrigin = "",
    [ValidateRange(0, 600)]
    [int]$OfflineDrainSeconds = 45,
    [ValidateRange(1, 100)]
    [int]$HealthCheckAttempts = 12,
    [ValidateRange(1, 300)]
    [int]$HealthCheckDelaySeconds = 5
)

$ErrorActionPreference = "Stop"
$repoRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
$rootPrefix = "$repoRoot\"
$deployRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "deploy"))
$wwwRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "src\Freito.Api\wwwroot"))
$apiProject = Join-Path $repoRoot "src\Freito.Api\Freito.Api.csproj"
$infrastructureProject = Join-Path $repoRoot "src\Freito.Infrastructure\Freito.Infrastructure.csproj"
$webRoot = Join-Path $repoRoot "web"
$webDist = Join-Path $webRoot "dist"
$uploaderPath = Join-Path $repoRoot "upload-ftp.ps1"
$backendPath = Join-Path $deployRoot "backend"
$appOfflinePath = Join-Path $deployRoot "app_offline.htm"

foreach ($targetPath in @($deployRoot, $wwwRoot)) {
    if (-not $targetPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean a path outside the repository."
    }
    $existing = Get-Item -LiteralPath $targetPath -Force -ErrorAction SilentlyContinue
    if ($existing -and ($existing.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
        throw "Refusing to clean a reparse point at $targetPath."
    }
}

foreach ($requiredPath in @($apiProject, $infrastructureProject, $uploaderPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required deployment file not found: $requiredPath"
    }
}

if ([string]::IsNullOrWhiteSpace($CorsOrigin)) {
    $CorsOrigin = "https://$RemotePath"
}

$remoteDbPattern = '(?i)(Server|Host)=localhost(?=;|$)'
if ($DbConnStr -notmatch $remoteDbPattern) {
    throw "The database connection string must use localhost for the hosted app."
}
$remoteDbEvaluator = [System.Text.RegularExpressions.MatchEvaluator]{
    param($match)
    return $match.Groups[1].Value + "=" + $Server
}
$remoteDbConnStr = [System.Text.RegularExpressions.Regex]::Replace(
    $DbConnStr,
    $remoteDbPattern,
    $remoteDbEvaluator,
    1
)

if (Test-Path -LiteralPath $deployRoot) {
    Remove-Item -LiteralPath $deployRoot -Recurse -Force
}
New-Item -Path $deployRoot -ItemType Directory | Out-Null

if (Test-Path -LiteralPath $wwwRoot) {
    Remove-Item -LiteralPath $wwwRoot -Recurse -Force
}
New-Item -Path $wwwRoot -ItemType Directory | Out-Null

$deploymentSucceeded = $false
try {
    Write-Host "1. Applying pending database migrations..." -ForegroundColor Yellow
    $previousConnection = [Environment]::GetEnvironmentVariable("ConnectionStrings__Default", "Process")
    $env:ConnectionStrings__Default = $remoteDbConnStr
    try {
        Push-Location $repoRoot
        & dotnet ef database update --configuration Release --no-build --project $infrastructureProject --startup-project $apiProject
        if ($LASTEXITCODE -ne 0) { throw "Database migration failed; deployment stopped before uploading files." }
    }
    finally {
        Pop-Location
        if ($null -eq $previousConnection) {
            Remove-Item Env:ConnectionStrings__Default -ErrorAction SilentlyContinue
        }
        else {
            $env:ConnectionStrings__Default = $previousConnection
        }
    }

    Write-Host "2. Building React assets before publish evaluation..." -ForegroundColor Yellow
    Push-Location $webRoot
    try {
        & npm install
        if ($LASTEXITCODE -ne 0) { throw "Web dependency installation failed; remote files were not changed." }
        & npm run build
        if ($LASTEXITCODE -ne 0) { throw "Web build failed; remote files were not changed." }
    }
    finally {
        Pop-Location
    }
    if (-not (Test-Path -LiteralPath (Join-Path $webDist "index.html") -PathType Leaf)) {
        throw "React build did not produce web/dist/index.html."
    }
    Copy-Item -Path (Join-Path $webDist "*") -Destination $wwwRoot -Recurse -Force

    Write-Host "3. Publishing Freito for the Plesk IIS host (win-x86)..." -ForegroundColor Yellow
    & dotnet publish $apiProject -c Release -r win-x86 --self-contained true -o $backendPath
    if ($LASTEXITCODE -ne 0) { throw "Freito publish failed; remote files were not changed." }

    Remove-Item -LiteralPath (Join-Path $backendPath "appsettings.Development.json") -Force -ErrorAction SilentlyContinue

    $webConfigPath = Join-Path $backendPath "web.config"
    if (-not (Test-Path -LiteralPath $webConfigPath -PathType Leaf)) {
        throw "web.config not found in the publish output."
    }

    [xml]$webConfig = Get-Content -LiteralPath $webConfigPath -Raw
    $aspNetCoreNode = $webConfig.configuration.location.'system.webServer'.aspNetCore
    if (-not $aspNetCoreNode) { throw "ASP.NET Core IIS configuration was not found in web.config." }
    $envVarsNode = $aspNetCoreNode.environmentVariables
    if (-not $envVarsNode) {
        $envVarsNode = $webConfig.CreateElement("environmentVariables")
        [void]$aspNetCoreNode.AppendChild($envVarsNode)
    }

    function Set-WebConfigEnvVar {
        param([string]$Name, [string]$Value)
        $existingVar = $envVarsNode.environmentVariable | Where-Object { $_.name -eq $Name }
        if ($existingVar) {
            $existingVar.value = $Value
        }
        else {
            $newVar = $webConfig.CreateElement("environmentVariable")
            $newVar.SetAttribute("name", $Name)
            $newVar.SetAttribute("value", $Value)
            [void]$envVarsNode.AppendChild($newVar)
        }
    }

    Set-WebConfigEnvVar -Name "ASPNETCORE_ENVIRONMENT" -Value "Production"
    Set-WebConfigEnvVar -Name "ConnectionStrings__Default" -Value $DbConnStr
    Set-WebConfigEnvVar -Name "Jwt__Key" -Value $JwtSigningKey
    Set-WebConfigEnvVar -Name "Cors__AllowedOrigins__0" -Value $CorsOrigin
    $aspNetCoreNode.SetAttribute("stdoutLogEnabled", "false")
    $aspNetCoreNode.SetAttribute("hostingModel", "outofprocess")
    $webConfig.Save($webConfigPath)

    $maintenanceHtml = @"
<!doctype html>
<html lang="th">
<head>
<meta charset="utf-8">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8">
<meta http-equiv="Cache-Control" content="no-cache, no-store, must-revalidate">
<meta http-equiv="Pragma" content="no-cache">
<meta http-equiv="Expires" content="0">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta http-equiv="refresh" content="10">
<title>Freito - &#3585;&#3635;&#3621;&#3633;&#3591;&#3629;&#3633;&#3611;&#3648;&#3604;&#3605;&#3619;&#3632;&#3610;&#3610; (System Updating)</title>
<style>
body{font-family:Segoe UI,-apple-system,sans-serif;background:#08101d;color:#eaf3f7;display:grid;place-items:center;min-height:100vh;margin:0;padding:1rem;box-sizing:border-box}
.card{max-width:38rem;width:100%;padding:2.5rem 2rem;border:1px solid #24415a;border-radius:1rem;background:#101b2b;text-align:center;box-shadow:0 10px 30px rgba(0,0,0,0.5)}
.spinner{width:36px;height:36px;border:3px solid #24415a;border-top-color:#38b6c3;border-radius:50%;margin:0 auto 1.25rem;animation:spin 1s linear infinite}
@keyframes spin{to{transform:rotate(360deg)}}
h1{color:#38b6c3;margin:0 0 0.5rem;font-size:1.5rem}
.th-sub{font-size:1rem;color:#d0e0ea;margin:0 0 0.75rem;line-height:1.6}
.en-sub{font-size:0.875rem;color:#8fa6b5;margin:0;line-height:1.5}
.auto-refresh{font-size:0.75rem;color:#5a7587;margin-top:1.5rem}
</style>
</head>
<body>
<main class="card">
  <div class="spinner"></div>
  <h1>Freito &#3585;&#3635;&#3621;&#3633;&#3591;&#3629;&#3633;&#3611;&#3648;&#3604;&#3605;&#3619;&#3632;&#3610;&#3610;</h1>
  <p class="th-sub">&#3619;&#3632;&#3610;&#3610;&#3585;&#3635;&#3621;&#3633;&#3591;&#3629;&#3618;&#3641;&#3656;&#3619;&#3632;&#3627;&#3623;&#3656;&#3634;&#3591;&#3585;&#3634;&#3619;&#3611;&#3619;&#3633;&#3610;&#3611;&#3619;&#3640;&#3591; &#3585;&#3619;&#3640;&#3603;&#3634;&#3619;&#3629;&#3626;&#3633;&#3585;&#3588;&#3619;&#3641;&#3656;&#3649;&#3621;&#3657;&#3623;&#3621;&#3629;&#3591;&#3619;&#3637;&#3648;&#3615;&#3619;&#3594;&#3627;&#3609;&#3657;&#3634;&#3648;&#3623;&#3655;&#3610;&#3651;&#3627;&#3617;&#3656;&#3629;&#3637;&#3585;&#3588;&#3619;&#3633;&#3657;&#3591;</p>
  <p class="en-sub">System is updating. Please wait a moment, this page will refresh automatically.</p>
  <p class="auto-refresh">&#8635; Auto-refreshing every 10 seconds...</p>
</main>
</body>
</html>
"@
    [System.IO.File]::WriteAllText($appOfflinePath, $maintenanceHtml, [System.Text.UTF8Encoding]::new($true))

    $pwsh = (Get-Command powershell.exe -ErrorAction SilentlyContinue).Source
    if (-not $pwsh) { $pwsh = (Get-Command pwsh.exe -ErrorAction Stop).Source }

    Write-Host "3. Taking the application offline..." -ForegroundColor Yellow
    & $pwsh -NoProfile -ExecutionPolicy Bypass -File $uploaderPath `
        -Server $Server -Username $Username -Password $Password `
        -LocalPath $deployRoot -RemotePath $RemotePath -ExcludePaths @("backend")
    if ($LASTEXITCODE -ne 0) { throw "Could not upload the maintenance page; application files were not changed." }

    if ($OfflineDrainSeconds -gt 0) {
        Write-Host "Waiting $OfflineDrainSeconds seconds for the IIS worker to release deployed files..."
        Start-Sleep -Seconds $OfflineDrainSeconds
    }

    Write-Host "4. Uploading the published application..." -ForegroundColor Yellow
    # Do not prune the remote root; Plesk keeps its own files such as .user.ini there.
    & $pwsh -NoProfile -ExecutionPolicy Bypass -File $uploaderPath `
        -Server $Server -Username $Username -Password $Password `
        -LocalPath $backendPath -RemotePath $RemotePath
    if ($LASTEXITCODE -ne 0) { throw "Application upload failed; maintenance mode remains enabled." }

    Write-Host "5. Bringing Freito online and checking database readiness..." -ForegroundColor Yellow
    $deleteRequest = [System.Net.FtpWebRequest]::Create("ftp://$Server/$RemotePath/app_offline.htm")
    $deleteRequest.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
    $deleteRequest.Credentials = New-Object System.Net.NetworkCredential($Username, $Password)
    $deleteRequest.UsePassive = $true
    $deleteRequest.UseBinary = $true
    $deleteResponse = $deleteRequest.GetResponse()
    $deleteResponse.Close()

    $healthUrl = "https://$RemotePath/api/health/db"
    $healthOk = $false
    $lastHealthResult = "no response"
    for ($attempt = 1; $attempt -le $HealthCheckAttempts; $attempt++) {
        try {
            $healthResponse = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 20
            $lastHealthResult = "HTTP $([int]$healthResponse.StatusCode)"
            if ([int]$healthResponse.StatusCode -eq 200) {
                $healthOk = $true
                break
            }
        }
        catch {
            if ($_.Exception.Response) {
                $lastHealthResult = "HTTP $([int]$_.Exception.Response.StatusCode)"
            }
            else {
                $lastHealthResult = $_.Exception.Message
            }
        }

        if ($attempt -lt $HealthCheckAttempts) {
            Write-Host "Readiness check $attempt/${HealthCheckAttempts}: $lastHealthResult"
            Start-Sleep -Seconds $HealthCheckDelaySeconds
        }
    }

    if (-not $healthOk) {
        Write-Warning "Database readiness check failed ($lastHealthResult); restoring maintenance mode."
        & $pwsh -NoProfile -ExecutionPolicy Bypass -File $uploaderPath `
            -Server $Server -Username $Username -Password $Password `
            -LocalPath $deployRoot -RemotePath $RemotePath -ExcludePaths @("backend")
        if ($LASTEXITCODE -ne 0) { throw "Readiness failed and restoring maintenance mode also failed." }
        throw "Deployment uploaded, but $healthUrl did not return HTTP 200 after $HealthCheckAttempts attempts. Maintenance mode was restored."
    }

    $deploymentSucceeded = $true
    Write-Host "Database readiness check passed. Freito is live." -ForegroundColor Green
}
finally {
    Remove-Item -LiteralPath $appOfflinePath -Force -ErrorAction SilentlyContinue
    if ($deploymentSucceeded -and (Test-Path -LiteralPath $deployRoot)) {
        Remove-Item -LiteralPath $deployRoot -Recurse -Force
    }
    Set-Location $repoRoot
}
