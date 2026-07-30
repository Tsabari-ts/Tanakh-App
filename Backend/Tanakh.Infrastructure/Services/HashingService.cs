using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography;
using System.Text;
using Tanakh.Domain;
using Tanakh.Infrastructure.Options;

namespace Tanakh.Infrastructure.Services
{
    public class HashingService : IHashingService
    {
        private readonly HashingOptions options;

        public HashingService(IOptions<HashingOptions> options)
        {
            this.options = options.Value;
        }

        public string HashEmail(string email)
        {
            // Validated lazily, not in the constructor: HashEmail is only
            // ever called from within ISuppressionService.IsSuppressedAsync,
            // itself only called from inside EmailSender's try/catch - so a
            // missing pepper surfaces as "don't send" (fail closed), not an
            // unhandled exception during DI activation.
            if (string.IsNullOrEmpty(options.Pepper))
            {
                throw new InvalidOperationException(
                    "Hashing:Pepper must be configured - suppression checks cannot run without it.");
            }

            // Normalized so the same address always hashes the same way
            // regardless of how it was typed/stored.
            string normalized = email.Trim().ToLowerInvariant();
            byte[] pepper = Encoding.UTF8.GetBytes(options.Pepper);
            byte[] hash = HMACSHA256.HashData(pepper, Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexStringLower(hash);
        }
    }
}
