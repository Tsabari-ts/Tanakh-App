namespace Tanakh.Domain
{
    // Keyed (HMAC) hashing for anything that must be looked up without ever
    // storing the plaintext - currently just suppression_list.email_hash.
    public interface IHashingService
    {
        string HashEmail(string email);
    }
}
