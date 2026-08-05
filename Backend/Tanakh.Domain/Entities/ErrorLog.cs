using System;
using Tanakh.Domain.Auditing;

namespace Tanakh.Domain.Entities
{
    // Written automatically by GlobalExceptionHandler for every unhandled
    // exception (Level = Error). Info/Warn/Fatal exist as values an admin
    // can filter by, but nothing writes them yet - no call site fabricated
    // just to populate them.
    public class ErrorLog : IHasCreatedAt
    {
        public Guid Id { get; set; }

        public ErrorLevel Level { get; set; }

        public required string Message { get; set; }

        public string? StackTrace { get; set; }

        public string? Endpoint { get; set; }

        public int? StatusCode { get; set; }

        public bool Resolved { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
