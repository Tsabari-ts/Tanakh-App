using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using System.Diagnostics;
using Tanakh.Api;
using Tanakh.Api.Services;
using Tanakh.Domain;
using Tanakh.Domain.Caching;
using Tanakh.Infrastructure;
using Tanakh.Infrastructure.Caching;
using Tanakh.Infrastructure.Options;
using Tanakh.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();

public partial class Program;
