using Nienow.Shinrikai.User;
using Xunit;

namespace Nienow.Shinrikai.Tests
{
    public sealed class PasswordHasherTests
    {
        [Fact]
        public void Hash_verifies_and_fits_the_column()
        {
            var hash = PasswordHasher.Hash("minha senha ção");

            Assert.True(PasswordHasher.IsHash(hash));
            Assert.True(hash.Length <= PasswordHasher.ColumnLength);
            Assert.DoesNotContain("minha", hash);
            Assert.True(PasswordHasher.Verify("minha senha ção", hash, out var rehash));
            Assert.False(rehash);
            Assert.False(PasswordHasher.Verify("minha senha cao", hash, out _));
        }

        [Fact]
        public void Same_password_gets_a_different_salt()
        {
            Assert.NotEqual(PasswordHasher.Hash("12345"), PasswordHasher.Hash("12345"));
        }

        [Theory]
        [InlineData("senha1", "senha1", true)]
        [InlineData("senha1   ", "senha1", true)]  // CHAR da base antiga, com espaços à direita
        [InlineData("senha1", "Senha1", false)]
        [InlineData("senha1", "", false)]
        [InlineData("", "", false)]
        public void Plain_legacy_password_is_accepted_and_flagged_for_rehash(string stored, string typed, bool ok)
        {
            Assert.Equal(ok, PasswordHasher.Verify(typed, stored, out var rehash));
            Assert.Equal(ok, rehash);
        }

        [Fact]
        public void Fewer_iterations_than_current_asks_for_rehash()
        {
            var weak = PasswordHasher.Hash("12345", 1_000);

            Assert.True(PasswordHasher.Verify("12345", weak, out var rehash));
            Assert.True(rehash);
        }

        [Theory]
        [InlineData("PBKDF2$SHA256$")]
        [InlineData("PBKDF2$SHA256$abc$AAAA$AAAA")]
        [InlineData("PBKDF2$SHA256$1000$não-é-base64$AAAA")]
        [InlineData("PBKDF2$SHA256$1000$AAAA")]
        public void Malformed_hash_never_verifies(string stored)
        {
            Assert.False(PasswordHasher.Verify("PBKDF2$SHA256$", stored, out _));
        }
    }
}
