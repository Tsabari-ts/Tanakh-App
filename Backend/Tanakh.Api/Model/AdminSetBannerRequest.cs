using System;

namespace Tanakh.Api.Model
{
    public class AdminSetBannerRequest
    {
        public required string Text { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }
    }
}
