using Tanakh.Infrastructure.Services;

namespace Tanakh.Tests
{
    public class AdminPasswordHasherTests
    {
        private readonly AdminPasswordHasher hasher = new();

        [Fact]
        public void Verify_Succeeds_For_Correct_Password()
        {
            string hash = hasher.Hash("correct horse battery staple");

            Assert.True(hasher.Verify("correct horse battery staple", hash));
        }

        [Fact]
        public void Verify_Fails_For_Incorrect_Password()
        {
            string hash = hasher.Hash("correct horse battery staple");

            Assert.False(hasher.Verify("wrong password", hash));
        }

        [Fact]
        public void Hash_Is_Salted_Differently_Each_Time()
        {
            string hash1 = hasher.Hash("same-password");
            string hash2 = hasher.Hash("same-password");

            Assert.NotEqual(hash1, hash2);
            Assert.True(hasher.Verify("same-password", hash1));
            Assert.True(hasher.Verify("same-password", hash2));
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-three-parts")]
        [InlineData("abc.def.ghi")]
        [InlineData("100000.not-base64!.not-base64!")]
        public void Verify_Fails_For_Malformed_Hash(string malformedHash)
        {
            Assert.False(hasher.Verify("any-password", malformedHash));
        }
    }
}
