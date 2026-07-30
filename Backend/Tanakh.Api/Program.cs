using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using System;
using System.Diagnostics;
using System.Linq;
using Tanakh.Api;
using Tanakh.Api.Services;
using Tanakh.Domain;
using Tanakh.Domain.Caching;
using Tanakh.Infrastructure;
using Tanakh.Infrastructure.Caching;
using Tanakh.Infrastructure.Data;
using Tanakh.Infrastructure.HealthChecks;
using Tanakh.Infrastructure.Options;
using Tanakh.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextPool<AppDbContext>(options =>
{
    string connectionString = builder.Configuration.GetConnectionString("AppDb")
        ?? throw new InvalidOperationException("Missing required connection string 'ConnectionStrings:AppDb'.");

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure();
        npgsqlOptions.CommandTimeout(30);
    });
    options.UseSnakeCaseNamingConvention();

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
    }
});

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 100;
});
builder.Services.AddSingleton<ITanakhCache, MemoryTanakhCache>();
builder.Services.AddScoped<CacheProvider>();
builder.Services.AddScoped<ITanakhStructureService, TanakhStructureService>();
builder.Services.AddScoped<ITanakhTextService, TanakhTextService>();
builder.Services.AddScoped<IJewishCalendarService, JewishCalendarService>();
builder.Services.AddOptions<TanakhDataOptions>()
    .Bind(builder.Configuration.GetSection(TanakhDataOptions.SectionName));
builder.Services.AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddHealthChecks()
    .AddCheck<TanakhDataHealthCheck>("tanakh-data", tags: new[] { "ready" });
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthorization();

app.MapControllers();

// Liveness: is the process up and able to route requests at all - no
// dependency checks (Predicate excludes every registered check).
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness: can this instance actually serve traffic correctly. Only
// checks TanakhData/TanakhStructure file presence - the one dependency
// this app's core function genuinely can't run without. SMTP/email is
// deliberately excluded: it's an optional, gracefully-degraded feature
// (see EmailOptions), not a readiness-gating dependency, and Subscribe
// already reports its own failure per-request when email delivery fails.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

public partial class Program;
