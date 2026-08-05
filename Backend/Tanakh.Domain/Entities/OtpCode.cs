using System;
using Tanakh.Domain.Auditing;

namespace Tanakh.Domain.Entities
{
    // Single-admin-identity OTP: there's exactly one admin account (from
    // config), so there's no AdminId FK - a new login attempt just
    // invalidates any prior unused row (see AdminAuthController).
    public class OtpCode : IHasCreatedAt
    {
        public Guid Id { get; set; }

        public required string CodeHash { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }

        public int Attempts { get; set; }

        public bool Used { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
