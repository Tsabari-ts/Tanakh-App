namespace Tanakh.Api.Model
{
    // Returned once, at subscribe time - the client stores ManageToken
    // (localStorage) and sends it back with every later preferences/
    // unsubscribe call. There is no login, so this token *is* the only way
    // the app recognizes "this browser's subscription" afterwards.
    public class SubscriptionResponse
    {
        public required string ManageToken { get; set; }
    }
}
