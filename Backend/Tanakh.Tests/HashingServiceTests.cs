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
        public void Hash_Is_Deterministic()
        {
            HashingService service = CreateService("test-pepper");

            string hash1 = service.Hash("some-value");
            string hash2 = service.Hash("some-value");

            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void Hash_Differs_By_Pepper()
        {
            string hashWithPepperA = CreateService("pepper-a").Hash("some-value");
            string hashWithPepperB = CreateService("pepper-b").Hash("some-value");

            Assert.NotEqual(hashWithPepperA, hashWithPepperB);
        }

        [Fact]
        public void Hash_Throws_When_Pepper_Not_Configured()
        {
            HashingService service = CreateService(string.Empty);

            Assert.Throws<InvalidOperationException>(() => service.Hash("some-value"));
        }
    }
}
