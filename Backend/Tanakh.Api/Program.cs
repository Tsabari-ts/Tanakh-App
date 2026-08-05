using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
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
builder.Services.AddSingleton<IAdminPasswordHasher, AdminPasswordHasher>();
builder.Services.AddScoped<ISubscriberAnonymizationService, SubscriberAnonymizationService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddSingleton<IUnsubscribeTokenService, UnsubscribeTokenService>();
builder.Services.AddScoped<INextChapterResolver, NextChapterResolver>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
builder.Services.AddOptions<TanakhDataOptions>()
    .Bind(builder.Configuration.GetSection(TanakhDataOptions.SectionName));
builder.Services.AddOptions<SmsOptions>()
    .Bind(builder.Configuration.GetSection(SmsOptions.SectionName));
builder.Services.AddOptions<HashingOptions>()
    .Bind(builder.Configuration.GetSection(HashingOptions.SectionName));
builder.Services.AddOptions<RetentionOptions>()
    .Bind(builder.Configuration.GetSection(RetentionOptions.SectionName));
builder.Services.AddOptions<RemindersOptions>()
    .Bind(builder.Configuration.GetSection(RemindersOptions.SectionName));
builder.Services.AddOptions<AdminOptions>()
    .Bind(builder.Configuration.GetSection(AdminOptions.SectionName));
builder.Services.AddAuthentication(AdminCookieAuthDefaults.SchemeName)
    .AddCookie(AdminCookieAuthDefaults.SchemeName, options =>
    {
        options.Cookie.Name = "tanakh_admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;

        // This is an API, not a browser login-page flow - return 401/403
        // JSON instead of the default redirect-to-login-page behavior.
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization(options =>
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim(ClaimTypes.Role, "admin")));
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        return ValueTask.CompletedTask;
    };

    // 5 attempts / 15 minutes per IP, then hard-rejected until the window
    // rolls - this is also the "30-minute lockout" from the spec in
    // practice (two consecutive rolled windows without a successful login).
    options.AddPolicy(RateLimiterPolicyNames.AdminLogin, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));
});
builder.Services.AddHostedService<RetentionHostedService>();
builder.Services.AddHostedService<ReminderPlannerService>();
builder.Services.AddHostedService<ReminderDispatcherService>();
builder.Services.AddHttpClient<ISmsSender, Sms4FreeSmsSender>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        int timeoutSeconds = serviceProvider.GetRequiredService<IOptions<SmsOptions>>().Value.TimeoutSeconds;
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    });
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

// --hash-admin-password <pw>: one-off local utility to produce the value
// for Admin__PasswordHash - never derived any other way, and safe to run
// in any environment (it touches no database/service state).
int hashPasswordFlagIndex = Array.IndexOf(args, "--hash-admin-password");
if (hashPasswordFlagIndex >= 0)
{
    if (hashPasswordFlagIndex + 1 >= args.Length)
    {
        throw new InvalidOperationException("--hash-admin-password requires a password argument.");
    }

    IAdminPasswordHasher hasher = app.Services.GetRequiredService<IAdminPasswordHasher>();
    Console.WriteLine(hasher.Hash(args[hashPasswordFlagIndex + 1]));
    return;
}

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

// Cookie auth requires credentialed CORS requests, which browsers reject
// under AllowAnyOrigin() ("Access-Control-Allow-Origin: *" is incompatible
// with "credentials: include") - so this is now an explicit allowlist
// instead of wide-open, for every endpoint, not just the admin ones.
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
app.UseCors(x => x.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials());

// Hidden-route hardening (spec section 1): every admin response is marked
// noindex/nofollow, regardless of whether the request succeeds or fails.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/v1/admin"))
    {
        context.Response.Headers.Append("X-Robots-Tag", "noindex, nofollow");
    }
    await next();
});

app.UseRateLimiter();

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
// this app's core function genuinely can't run without. SMS4FREE is
// deliberately excluded: reminder sends happen asynchronously via the
// dispatcher background service, not on the request path, so a down
// provider doesn't make this instance unable to serve traffic.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

public partial class Program;
