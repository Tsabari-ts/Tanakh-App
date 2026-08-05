using System;
using System.Security.Cryptography;
using System.Text;
using Tanakh.Domain;

namespace Tanakh.Infrastructure.Services
{
    public class AdminPasswordHasher : IAdminPasswordHasher
    {
        // OWASP-recommended minimum for PBKDF2-HMACSHA256 as of 2024.
        private const int DefaultIterations = 210_000;
        private const int SaltSizeBytes = 16;
        private const int HashSizeBytes = 32;

        public string Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt, DefaultIterations, HashAlgorithmName.SHA256, HashSizeBytes);

            return $"{DefaultIterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public bool Verify(string password, string encodedHash)
        {
            string[] parts = encodedHash.Split('.');
            if (parts.Length != 3 || !int.TryParse(parts[0], out int iterations))
            {
                return false;
            }

            byte[] salt;
            byte[] expectedHash;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expectedHash = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}
