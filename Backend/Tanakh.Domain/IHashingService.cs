namespace Tanakh.Domain
{
    // Keyed (HMAC) hashing for anything that must be looked up without ever
    // storing the plaintext - suppression_list.email_hash, and any future
    // ip_hash/consent-record hashing (D-09/D-10).
    public interface IHashingService
    {
        // Generic pepper-keyed hash. Callers normalize their own input first
        // if normalization matters for their lookup (see HashEmail).
        string Hash(string value);

        // Normalizes (trim + lowercase) before hashing, so the same address
        // always hashes the same way regardless of how it was typed/stored.
        string HashEmail(string email);
    }
}
