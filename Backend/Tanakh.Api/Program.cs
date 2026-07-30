using Microsoft.AspNetCore.Authentication;
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
using Tanakh.Api.Auth;
using Tanakh.Api.Services;
using Tanakh.Domain;
using Tanakh.Domain.Caching;
using Tanakh.Infrastructure;
using Tanakh.Infrastructure.Caching;
using Tanakh.Infrastructure.Data;
using Tanakh.Infrastructure.HealthChecks;
using Tanakh.Infrastructure.Options;
using Tanakh.Infrastructure.Reminders;
using Tanakh.Infrastructure.Retention;
using Tanakh.Infrastructure.Seeding;
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
builder.Services.AddScoped<IReadingProgressService, ReadingProgressService>();
builder.Services.AddSingleton<IHashingService, HashingService>();
builder.Services.AddScoped<ISuppressionService, SuppressionService>();
builder.Services.AddScoped<ISubscriberAnonymizationService, SubscriberAnonymizationService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddSingleton<IUnsubscribeTokenService, UnsubscribeTokenService>();
builder.Services.AddScoped<INextChapterResolver, NextChapterResolver>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
builder.Services.AddOptions<TanakhDataOptions>()
    .Bind(builder.Configuration.GetSection(TanakhDataOptions.SectionName));
builder.Services.AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddOptions<HashingOptions>()
    .Bind(builder.Configuration.GetSection(HashingOptions.SectionName));
builder.Services.AddOptions<RetentionOptions>()
    .Bind(builder.Configuration.GetSection(RetentionOptions.SectionName));
builder.Services.AddOptions<RemindersOptions>()
    .Bind(builder.Configuration.GetSection(RemindersOptions.SectionName));
builder.Services.AddOptions<AdminOptions>()
    .Bind(builder.Configuration.GetSection(AdminOptions.SectionName));
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
        BasicAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization();
builder.Services.AddHostedService<RetentionHostedService>();
builder.Services.AddHostedService<ReminderPlannerService>();
builder.Services.AddHostedService<ReminderDispatcherService>();
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

// --seed / --reset-db: one-shot dev convenience commands, hard-blocked
// outside Development so they can never run against staging/prod.
if (args.Contains("--seed") || args.Contains("--reset-db"))
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("--seed and --reset-db are only allowed in the Development environment.");
    }

    using (IServiceScope scope = app.Services.CreateScope())
    {
        IDatabaseSeeder seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();

        if (args.Contains("--reset-db"))
        {
            await seeder.ResetSchemaAsync();
        }

        await seeder.SeedAsync();
    }

    return;
}

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

app.UseAuthentication();
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
