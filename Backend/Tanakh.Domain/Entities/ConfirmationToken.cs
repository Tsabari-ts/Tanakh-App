using System;
using System.Security.Cryptography;
using System.Text;
using Tanakh.Domain.Auditing;

namespace Tanakh.Domain.Entities
{
    // The raw token exists only in the outbound email - this row stores
    // just its SHA-256 hash (hex), never the plaintext value. Single use,
    // enforced via UsedAt; TTL enforced via ExpiresAt at lookup time.
    public class ConfirmationToken : IHasCreatedAt
    {
        public required string TokenHash { get; set; }

        public Guid SubscriberId { get; set; }

        public required string Purpose { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }

        public DateTimeOffset? UsedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public static string ComputeTokenHash(string rawToken)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexStringLower(hash);
        }
    }
}
