using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.User;
using Xunit;

namespace Nienow.Shinrikai.Tests
{
    public sealed class UserProfileTests
    {
        private static TProfileUpdate Valid() => new() { Name = "Ana", Email = "ana@b.com", Sex = ESex.Female, JLPT = EJLPT.N4 };

        [Fact]
        public void Validate_profile_without_password_change()
        {
            Assert.True(UserService.ValidateProfile(Valid()).OK);

            var noName = Valid();
            noName.Name = "  ";
            Assert.False(UserService.ValidateProfile(noName).OK);

            var badEmail = Valid();
            badEmail.Email = "invalido";
            Assert.False(UserService.ValidateProfile(badEmail).OK);
        }

        [Theory]
        [InlineData(null, "12345", "12345", false)]  // sem senha atual
        [InlineData("old", "1234", "1234", false)]    // menos de 5 dígitos
        [InlineData("old", "12345", "54321", false)]  // repetição diferente
        [InlineData("old", "", "12345", false)]       // só a repetição preenchida
        [InlineData("old", "12345", "12345", true)]
        public void Validate_profile_password_change(string? current, string next, string repeat, bool ok)
        {
            var profile = Valid();
            profile.CurrentPassword = current;
            profile.NewPassword = next;
            profile.RepeatPassword = repeat;
            Assert.Equal(ok, UserService.ValidateProfile(profile).OK);
        }

        [Fact]
        public async Task Update_saves_for_the_logged_user()
        {
            var repo = new FakeUsers();
            var response = await new UserService(repo).UpdateProfileAsync("ana", Valid());

            Assert.True(response.OK);
            var (id, saved, hash) = Assert.Single(repo.Updates);
            Assert.Equal(1, id);
            Assert.False(saved.ChangesPassword);
            Assert.Null(hash);
        }

        [Fact]
        public async Task Update_rejects_email_of_another_user()
        {
            var repo = new FakeUsers();
            var profile = Valid();
            profile.Email = "bia@b.com";

            Assert.False((await new UserService(repo).UpdateProfileAsync("ana", profile)).OK);
            Assert.Empty(repo.Updates);
        }

        [Fact]
        public async Task Update_keeps_own_email()
        {
            var repo = new FakeUsers();
            Assert.True((await new UserService(repo).UpdateProfileAsync("ana", Valid())).OK);
        }

        [Theory]
        [InlineData("errada", false)]
        [InlineData("senha1", true)]
        public async Task Password_change_requires_the_current_password(string current, bool ok)
        {
            var repo = new FakeUsers();
            var profile = Valid();
            profile.CurrentPassword = current;
            profile.NewPassword = profile.RepeatPassword = "nova123";

            Assert.Equal(ok, (await new UserService(repo).UpdateProfileAsync("ana", profile)).OK);
            Assert.Equal(ok ? 1 : 0, repo.Updates.Count);
            if (ok)
                Assert.True(PasswordHasher.Verify("nova123", repo.Updates[0].Hash, out _));
        }

        [Fact]
        public async Task Login_with_plain_password_replaces_it_with_a_hash()
        {
            var repo = new FakeUsers();
            var service = new UserService(repo);

            Assert.Null(await service.LoginAsync("ana", "errada"));
            Assert.Equal("senha1", repo.Password(1));

            Assert.NotNull(await service.LoginAsync("ana", "senha1"));
            Assert.True(PasswordHasher.IsHash(repo.Password(1)));
            Assert.NotNull(await service.LoginAsync("ana", "senha1"));
            Assert.Null(await service.LoginAsync("ana", "errada"));
        }

        [Fact]
        public async Task Sign_up_stores_only_the_hash()
        {
            var repo = new FakeUsers();
            var user = new TUser { Name = "Caio", Email = "caio@b.com", Login = "caio", Password = "segredo", RepeatPassword = "segredo" };

            Assert.True((await new UserService(repo).SignUpAsync(user)).OK);
            var stored = repo.Password(3);
            Assert.True(PasswordHasher.IsHash(stored));
            Assert.DoesNotContain("segredo", stored);
            Assert.NotNull(await new UserService(repo).LoginAsync("caio", "segredo"));
        }

        [Fact]
        public async Task Migration_hashes_every_plain_password_once()
        {
            var repo = new FakeUsers();
            var service = new UserService(repo);

            Assert.Equal(2, await service.HashPlainPasswordsAsync());
            Assert.True(repo.ColumnEnsured);
            Assert.True(PasswordHasher.IsHash(repo.Password(1)));
            Assert.True(PasswordHasher.IsHash(repo.Password(2)));
            Assert.NotNull(await service.LoginAsync("bia", "senha2"));

            Assert.Equal(0, await service.HashPlainPasswordsAsync());
        }

        /// <summary>Usuários em memória; as senhas começam em texto puro, como na base original.</summary>
        private sealed class FakeUsers : IUserRepository
        {
            private readonly List<(TUser User, string Password)> users =
            [
                (new TUser { ID = 1, Login = "ana", Name = "Ana", Email = "ana@b.com", Type = "T" }, "senha1"),
                (new TUser { ID = 2, Login = "bia", Name = "Bia", Email = "bia@b.com" }, "senha2"),
            ];

            public List<(int Id, TProfileUpdate Profile, string? Hash)> Updates { get; } = [];
            public bool ColumnEnsured { get; private set; }

            public string Password(int id) => users.Single(u => u.User.ID == id).Password;

            public Task<TUser?> GetByLoginAsync(string login, CancellationToken ct = default) =>
                Task.FromResult(users.Select(u => u.User).FirstOrDefault(u => u.Login == login));

            public Task<TUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
                Task.FromResult(users.Select(u => u.User).FirstOrDefault(u => u.Email == email));

            public Task<string?> GetStoredPasswordAsync(string login, CancellationToken ct = default) =>
                Task.FromResult<string?>(users.FirstOrDefault(u => u.User.Login == login).Password);

            public Task UpdateProfileAsync(int userId, TProfileUpdate profile, string? newPasswordHash, CancellationToken ct = default)
            {
                Updates.Add((userId, profile, newPasswordHash));
                return Task.CompletedTask;
            }

            public Task SignUpAsync(TUser user, string passwordHash, CancellationToken ct = default)
            {
                user.ID = users.Max(u => u.User.ID) + 1;
                users.Add((user, passwordHash));
                return Task.CompletedTask;
            }

            public Task ReplacePasswordAsync(int userId, string expected, string passwordHash, CancellationToken ct = default)
            {
                var i = users.FindIndex(u => u.User.ID == userId && u.Password == expected);
                if (i >= 0)
                    users[i] = (users[i].User, passwordHash);
                return Task.CompletedTask;
            }

            public Task<IReadOnlyList<(int UserId, string Stored)>> GetPlainPasswordsAsync(CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<(int UserId, string Stored)>>(
                    [.. users.Where(u => !PasswordHasher.IsHash(u.Password)).Select(u => (u.User.ID, u.Password))]);

            public Task EnsurePasswordColumnAsync(CancellationToken ct = default)
            {
                ColumnEnsured = true;
                return Task.CompletedTask;
            }

            public Task<string> GetNotificationAsync(int userId, int notificationId, bool force, CancellationToken ct = default) => Task.FromResult("");

            public Task DeclareUnderstandingAsync(int userId, int notificationId, CancellationToken ct = default) => Task.CompletedTask;
        }
    }
}
