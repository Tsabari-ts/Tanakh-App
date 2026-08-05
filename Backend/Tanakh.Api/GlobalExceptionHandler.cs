using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain.Entities;
using Tanakh.Infrastructure.Data;

namespace Tanakh.Api
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> logger;
        private readonly IProblemDetailsService problemDetailsService;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetailsService)
        {
            this.logger = logger;
            this.problemDetailsService = problemDetailsService;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            string traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

            logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await TryWriteErrorLogAsync(httpContext, exception, cancellationToken);

            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred.",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
                }
            });
        }

        // This handler is registered as a singleton (AddExceptionHandler<T>),
        // so it can't take a scoped AppDbContext in its constructor -
        // resolved per-request from RequestServices instead. A DB write
        // failure here must never be why the actual error response fails,
        // so it's caught and only logged.
        private async Task TryWriteErrorLogAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            try
            {
                AppDbContext dbContext = httpContext.RequestServices.GetRequiredService<AppDbContext>();

                await dbContext.ErrorLogs.AddAsync(new ErrorLog
                {
                    Id = Guid.CreateVersion7(),
                    Level = ErrorLevel.Error,
                    Message = exception.Message,
                    StackTrace = exception.ToString(),
                    Endpoint = httpContext.Request.Path,
                    StatusCode = StatusCodes.Status500InternalServerError,
                }, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception loggingException)
            {
                logger.LogError(loggingException, "Failed to write error_log row for the unhandled exception above.");
            }
        }
    }
}
