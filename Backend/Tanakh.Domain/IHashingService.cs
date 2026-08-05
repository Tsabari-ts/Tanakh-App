namespace Tanakh.Domain
{
    // Keyed (HMAC) hashing for anything that must be looked up without ever
    // storing the plaintext - e.g. consent_records.ip_hash (D-09/D-10).
    public interface IHashingService
    {
        // Generic pepper-keyed hash. Callers normalize their own input first
        // if normalization matters for their lookup.
        string Hash(string value);
    }
}
