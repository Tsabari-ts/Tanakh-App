using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography;
using System.Text;
using Tanakh.Domain;
using Tanakh.Infrastructure.Options;

namespace Tanakh.Infrastructure.Services
{
    public class UnsubscribeTokenService : IUnsubscribeTokenService
    {
        // Bump when rotating the signing key: add a case in GetKey for the
        // new id, keep the old one so previously issued links still verify,
        // and switch CurrentKeyId to the new value.
        private const string CurrentKeyId = "v1";

        private readonly HashingOptions hashingOptions;

        public UnsubscribeTokenService(IOptions<HashingOptions> hashingOptions)
        {
            this.hashingOptions = hashingOptions.Value;
        }

        public string Issue(Guid subscriberId)
        {
            string payload = $"{subscriberId}|{DateTimeOffset.UtcNow:O}";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
            byte[] signature = Sign(CurrentKeyId, payloadBytes);

            return $"{CurrentKeyId}.{Base64UrlEncode(payloadBytes)}.{Base64UrlEncode(signature)}";
        }

        public bool TryValidate(string token, out Guid subscriberId)
        {
            subscriberId = Guid.Empty;

            string[] parts = token.Split('.');
            if (parts.Length != 3)
            {
                return false;
            }

            byte[]? key = GetKey(parts[0]);
            if (key is null)
            {
                return false;
            }

            byte[] payloadBytes;
            byte[] signatureBytes;
            try
            {
                payloadBytes = Base64UrlDecode(parts[1]);
                signatureBytes = Base64UrlDecode(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] expectedSignature = HMACSHA256.HashData(key, payloadBytes);
            if (!CryptographicOperations.FixedTimeEquals(signatureBytes, expectedSignature))
            {
                return false;
            }

            string payload = Encoding.UTF8.GetString(payloadBytes);
            string[] payloadParts = payload.Split('|', 2);

            return payloadParts.Length == 2 && Guid.TryParse(payloadParts[0], out subscriberId);
        }

        private byte[] Sign(string keyId, byte[] payloadBytes)
        {
            byte[] key = GetKey(keyId)
                ?? throw new InvalidOperationException($"Unknown unsubscribe token key id '{keyId}'.");

            return HMACSHA256.HashData(key, payloadBytes);
        }

        private byte[]? GetKey(string keyId) => keyId switch
        {
            "v1" => Encoding.UTF8.GetBytes(hashingOptions.Pepper),
            _ => null
        };

        private static string Base64UrlEncode(byte[] bytes) =>
            Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        private static byte[] Base64UrlDecode(string value)
        {
            string padded = value.Replace('-', '+').Replace('_', '/');
            padded += (padded.Length % 4) switch
            {
                2 => "==",
                3 => "=",
                _ => ""
            };

            return Convert.FromBase64String(padded);
        }
    }
}
