using Freito.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();
builder.Services.AddScoped<Freito.Api.Services.AuditLogWriter>();
builder.Services.AddScoped<Freito.Api.Services.FreightRateService>();
builder.Services.AddScoped<Freito.Api.Services.LocalChargeService>();
builder.Services.AddScoped<Freito.Api.Services.FreightRateCsvImporter>();
builder.Services.AddScoped<Freito.Api.Services.LocalChargeCsvImporter>();
builder.Services.AddScoped<Freito.Api.Services.QuotationService>();

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

app.UseAuthorization();
app.MapControllers();

// Single-domain deploy: API under /api/*, React build served as static files/SPA
// fallback for everything else. wwwroot is populated by `npm run build` in web/
// during publish — see technical-plan.md §1.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
