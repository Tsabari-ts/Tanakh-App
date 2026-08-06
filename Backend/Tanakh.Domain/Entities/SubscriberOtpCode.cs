using System;
using Tanakh.Domain.Auditing;

namespace Tanakh.Domain.Entities
{
    // Phone-verification OTP for the public subscribe flow - distinct from
    // OtpCode (single-admin-identity login OTP). Keyed by phone number
    // rather than a subscriber id, since the subscriber row may not exist
    // yet (first-time signup) - see SubscriptionService.SubscribeAsync.
    public class SubscriberOtpCode : IHasCreatedAt
    {
        public Guid Id { get; set; }

        public required string PhoneNumber { get; set; }

        public required string CodeHash { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }

        public int Attempts { get; set; }

        public bool Used { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
