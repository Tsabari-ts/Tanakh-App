namespace Tanakh.Domain
{
    // PBKDF2-HMACSHA256 (Rfc2898DeriveBytes), not bcrypt/argon2 - avoids
    // pulling in a new NuGet dependency for a single-admin-account use case.
    // See Tanakh.Infrastructure.Services.AdminPasswordHasher.
    public interface IAdminPasswordHasher
    {
        // Encoded as "{iterations}.{saltBase64}.{hashBase64}" - self-describing
        // so the iteration count can be raised later without invalidating
        // hashes generated under the old one.
        string Hash(string password);

        bool Verify(string password, string encodedHash);
    }
}
