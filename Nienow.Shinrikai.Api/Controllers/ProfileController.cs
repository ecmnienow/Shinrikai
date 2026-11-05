using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Api.Auth;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;

namespace Nienow.Shinrikai.Api.Controllers
{
    /// <summary>Dados do usuário logado, sem a senha. Login e papel vão só para exibição.</summary>
    public sealed record UserProfile(
        string? Login, string? Name, string? Email, ESex Sex, DateTime? Birthday,
        string? Profession, string? OtherLanguages, EJLPT Jlpt, string Role)
    {
        public static UserProfile From(TUser user) => new(
            user.Login, user.Name, user.Email, user.Sex,
            // Cadastro sem data grava 0001-01-01 (ou NULL): os dois viram "sem data".
            user.Birthday.Year <= 1 ? null : user.Birthday,
            user.Profession, user.OtherLanguages, user.JLPT, user.Role);
    }

    public sealed record ProfileUpdateResult(string? Message, UserInfo User);

    /// <summary>Edição dos próprios dados. O usuário é sempre o do token; papel e login não são alteráveis aqui.</summary>
    [Route("api/profile")]
    public sealed class ProfileController(UserService users, IMonitoringService monitoring)
        : ShinrikaiController(monitoring, "Perfil")
    {
        [HttpGet]
        public async Task<ActionResult<UserProfile>> Get(CancellationToken ct) =>
            await users.GetUserAsync(User.Identity!.Name!, ct) is { } user ? UserProfile.From(user) : Unauthorized();

        [HttpPut]
        public async Task<ActionResult<ProfileUpdateResult>> Update(TProfileUpdate profile, CancellationToken ct)
        {
            var login = User.Identity!.Name!;
            var response = await users.UpdateProfileAsync(login, profile, ct);
            if (!response.OK)
                return BadRequest(response);

            await Log("Alterar dados", profile.ChangesPassword ? "com senha" : null, ct);
            return await users.GetUserAsync(login, ct) is { } user
                ? new ProfileUpdateResult(response.Message, UserInfo.From(user))
                : Unauthorized();
        }
    }
}
