using System;

namespace Tanakh.Domain
{
    // Stateless HMAC-signed token, deliberately with no expiry - unsubscribe
    // links in old emails must keep working forever (see T-15/T-16). Bears
    // a key-id prefix so the signing key can be rotated without breaking
    // links signed under the previous key.
    public interface IUnsubscribeTokenService
    {
        string Issue(Guid subscriberId);

        bool TryValidate(string token, out Guid subscriberId);
    }
}
