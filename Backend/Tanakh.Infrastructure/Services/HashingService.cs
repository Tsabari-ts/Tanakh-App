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

        public string Hash(string value)
        {
            if (string.IsNullOrEmpty(options.Pepper))
            {
                throw new InvalidOperationException(
                    "Hashing:Pepper must be configured - hashing cannot run without it.");
            }

            byte[] pepper = Encoding.UTF8.GetBytes(options.Pepper);
            byte[] hash = HMACSHA256.HashData(pepper, Encoding.UTF8.GetBytes(value));
            return Convert.ToHexStringLower(hash);
        }
    }
}
