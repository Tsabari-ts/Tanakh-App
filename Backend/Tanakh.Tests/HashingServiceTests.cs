using Microsoft.Extensions.Options;
using Tanakh.Infrastructure.Options;
using Tanakh.Infrastructure.Services;

namespace Tanakh.Tests
{
    public class HashingServiceTests
    {
        private static HashingService CreateService(string pepper) =>
            new(Options.Create(new HashingOptions { Pepper = pepper }));

        [Fact]
        public void HashEmail_Is_Case_And_Whitespace_Insensitive()
        {
            HashingService service = CreateService("test-pepper");

            string hash1 = service.HashEmail("Someone@Example.com");
            string hash2 = service.HashEmail("  someone@example.com  ");

            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void HashEmail_Differs_By_Pepper()
        {
            string hashWithPepperA = CreateService("pepper-a").HashEmail("someone@example.com");
            string hashWithPepperB = CreateService("pepper-b").HashEmail("someone@example.com");

            Assert.NotEqual(hashWithPepperA, hashWithPepperB);
        }

        [Fact]
        public void HashEmail_Throws_When_Pepper_Not_Configured()
        {
            HashingService service = CreateService(string.Empty);

            Assert.Throws<InvalidOperationException>(() => service.HashEmail("someone@example.com"));
        }
    }
}
