using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Tanakh.Infrastructure.Options;

namespace Tanakh.Api.Auth
{
    // Minimal HTTP Basic authentication for the admin dashboard (T-23) -
    // "Basic Auth minimum" per spec. Credentials come from the Admin config
    // section, never hardcoded; comparisons are fixed-time to avoid a
    // timing side-channel.
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "AdminBasic";

        private readonly AdminOptions adminOptions;

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IOptions<AdminOptions> adminOptions)
            : base(options, logger, encoder)
        {
            this.adminOptions = adminOptions.Value;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Fail closed if Admin:Username/Password were never configured -
            // otherwise an unconfigured deployment would accept an empty
            // username/password as valid credentials.
            if (string.IsNullOrEmpty(adminOptions.Username) || string.IsNullOrEmpty(adminOptions.Password))
            {
                return Task.FromResult(AuthenticateResult.Fail("Admin:Username/Password are not configured."));
            }

            if (!Request.Headers.TryGetValue("Authorization", out Microsoft.Extensions.Primitives.StringValues headerValue)
                || !AuthenticationHeaderValue.TryParse(headerValue.ToString(), out AuthenticationHeaderValue? header)
                || !string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
                || header.Parameter is null)
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing Basic authentication header."));
            }

            string credentials;
            try
            {
                credentials = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter));
            }
            catch (FormatException)
            {
                return Task.FromResult(AuthenticateResult.Fail("Malformed Basic authentication header."));
            }

            int separatorIndex = credentials.IndexOf(':');
            if (separatorIndex < 0)
            {
                return Task.FromResult(AuthenticateResult.Fail("Malformed Basic authentication header."));
            }

            string username = credentials[..separatorIndex];
            string password = credentials[(separatorIndex + 1)..];

            if (!FixedTimeEquals(username, adminOptions.Username) || !FixedTimeEquals(password, adminOptions.Password))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid credentials."));
            }

            ClaimsIdentity identity = new(new[] { new Claim(ClaimTypes.Name, username) }, Scheme.Name);
            AuthenticationTicket ticket = new(new ClaimsPrincipal(identity), Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Headers.WWWAuthenticate = "Basic realm=\"Admin\"";
            return base.HandleChallengeAsync(properties);
        }

        private static bool FixedTimeEquals(string a, string b) =>
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }
}
