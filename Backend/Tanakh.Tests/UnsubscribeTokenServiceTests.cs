using Microsoft.Extensions.Options;
using System;
using Tanakh.Domain;
using Tanakh.Infrastructure.Options;
using Tanakh.Infrastructure.Services;
using Xunit;

namespace Tanakh.Tests
{
    public class UnsubscribeTokenServiceTests
    {
        private static IUnsubscribeTokenService CreateService(string pepper = "test-pepper") =>
            new UnsubscribeTokenService(Options.Create(new HashingOptions { Pepper = pepper }));

        [Fact]
        public void Issue_Then_TryValidate_RoundTrips_The_Same_SubscriberId()
        {
            IUnsubscribeTokenService service = CreateService();
            Guid subscriberId = Guid.CreateVersion7();

            string token = service.Issue(subscriberId);
            bool valid = service.TryValidate(token, out Guid resolvedId);

            Assert.True(valid);
            Assert.Equal(subscriberId, resolvedId);
        }

        [Fact]
        public void TryValidate_Rejects_A_Tampered_Signature()
        {
            IUnsubscribeTokenService service = CreateService();
            string token = service.Issue(Guid.CreateVersion7());
            string[] parts = token.Split('.');
            string tampered = $"{parts[0]}.{parts[1]}.{new string('A', parts[2].Length)}";

            Assert.False(service.TryValidate(tampered, out _));
        }

        [Fact]
        public void TryValidate_Rejects_A_Token_Signed_With_A_Different_Pepper()
        {
            IUnsubscribeTokenService issuer = CreateService("pepper-one");
            IUnsubscribeTokenService verifier = CreateService("pepper-two");

            string token = issuer.Issue(Guid.CreateVersion7());

            Assert.False(verifier.TryValidate(token, out _));
        }

        [Fact]
        public void TryValidate_Rejects_An_Unknown_Key_Id()
        {
            IUnsubscribeTokenService service = CreateService();

            Assert.False(service.TryValidate("v99.abc.def", out _));
        }

        [Fact]
        public void TryValidate_Rejects_A_Malformed_Token()
        {
            IUnsubscribeTokenService service = CreateService();

            Assert.False(service.TryValidate("not-a-real-token", out _));
        }
    }
}
