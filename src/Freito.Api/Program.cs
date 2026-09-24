using System.Text;
using Freito.Api.Auth;
using Freito.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using System.Text.Json.Serialization;
using Freito.Api.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
    options.Filters.Add(new MissingExchangeRateExceptionFilter()))
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT set as an httpOnly cookie on login (technical-plan.md §1) — read back from the cookie
// here since browsers won't attach it as an Authorization header automatically.
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.Configure<JwtOptions>(jwtSection);
var jwtKey = jwtSection["Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Missing Jwt:Key. Set it in appsettings.json or an environment variable — never ship a default in production.");

builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("access_token", out var token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<Freito.Api.Services.AuditLogWriter>();
builder.Services.AddScoped<Freito.Api.Services.FreightRateService>();
builder.Services.AddScoped<Freito.Api.Services.LocalChargeService>();
builder.Services.AddScoped<Freito.Api.Services.FreightRateCsvImporter>();
builder.Services.AddScoped<Freito.Api.Services.LocalChargeCsvImporter>();
builder.Services.AddScoped<Freito.Api.Services.QuotationService>();
builder.Services.AddScoped<Freito.Api.Services.QuotationPdfService>();

// QuestPDF Community license — free for organizations with <$1M USD annual gross revenue or
// open-source projects (see https://www.questpdf.com/license/). Re-check this before a
// production deploy if that stops being true for whoever runs Freito.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Connection string comes from appsettings / environment / Plesk app settings —
// never hardcoded. See appsettings.json for the expected key.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Default. Set it in appsettings.json or an environment variable.");

// A pinned ServerVersion (not ServerVersion.AutoDetect) so the app can start
// even if MySQL is briefly unreachable — AutoDetect connects synchronously
// during startup and would crash-loop the whole app on a transient DB blip.
// Bump this once the actual MySQL version on the Plesk host is confirmed (T0).
var mySqlVersion = new MySqlServerVersion(new Version(8, 0, 36));

builder.Services.AddDbContext<FreitoDbContext>(options =>
    options.UseMySql(connectionString, mySqlVersion));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Single-domain deploy: API under /api/*, React build served as static files/SPA
// fallback for everything else. wwwroot is populated by `npm run build` in web/
// during publish — see technical-plan.md §1.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
