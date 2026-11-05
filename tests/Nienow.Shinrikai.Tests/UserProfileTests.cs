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
            var (id, saved) = Assert.Single(repo.Updates);
            Assert.Equal(1, id);
            Assert.False(saved.ChangesPassword);
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
        }

        private sealed class FakeUsers : IUserRepository
        {
            private readonly List<(TUser User, string Password)> users =
            [
                (new TUser { ID = 1, Login = "ana", Name = "Ana", Email = "ana@b.com", Type = "T" }, "senha1"),
                (new TUser { ID = 2, Login = "bia", Name = "Bia", Email = "bia@b.com" }, "senha2"),
            ];

            public List<(int Id, TProfileUpdate Profile)> Updates { get; } = [];

            public Task<TUser?> GetByLoginAsync(string login, CancellationToken ct = default) =>
                Task.FromResult(users.Select(u => u.User).FirstOrDefault(u => u.Login == login));

            public Task<TUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
                Task.FromResult(users.Select(u => u.User).FirstOrDefault(u => u.Email == email));

            public Task<string?> GetStoredPasswordAsync(string login, CancellationToken ct = default) =>
                Task.FromResult<string?>(users.FirstOrDefault(u => u.User.Login == login).Password);

            public Task UpdateProfileAsync(int userId, TProfileUpdate profile, CancellationToken ct = default)
            {
                Updates.Add((userId, profile));
                return Task.CompletedTask;
            }

            public Task SignUpAsync(TUser user, CancellationToken ct = default) => Task.CompletedTask;

            public Task<string> GetNotificationAsync(int userId, int notificationId, bool force, CancellationToken ct = default) => Task.FromResult("");

            public Task DeclareUnderstandingAsync(int userId, int notificationId, CancellationToken ct = default) => Task.CompletedTask;
        }
    }
}
