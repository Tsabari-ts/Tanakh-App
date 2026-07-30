using System.Collections.Generic;

namespace Tanakh.Domain
{
    public class EmailMessage
    {
        public required string To { get; set; }
        public required string Subject { get; set; }
        public required string Body { get; set; }

        // e.g. List-Unsubscribe / List-Unsubscribe-Post (RFC 8058) - see
        // ISubscriptionService unsubscribe endpoints.
        public IReadOnlyDictionary<string, string>? Headers { get; set; }
    }
}
