using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Api.Auth;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;

namespace Nienow.Shinrikai.Api.Controllers
{
    public sealed record LoginRequest(string Username, string Password);

    /// <summary>Port de LoginController (Forms Auth + Session → JWT).</summary>
    [Route("api/auth")]
    public sealed class AuthController(UserService users, JwtTokenService tokens, IMonitoringService monitoring)
        : ShinrikaiController(monitoring, "Logout")
    {
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthenticatedUser>> Login(LoginRequest request, CancellationToken ct)
        {
            var user = await users.LoginAsync(request.Username, request.Password, ct);
            if (user is null)
                return Unauthorized(Core.Response.Fail("Usuário ou senha inválidos."));

            await Monitoring.AddAsync(user.ID, "Login", "Autenticacao", "Autenticado", null, ct);
            return tokens.Issue(user);
        }

        [HttpPost("signup")]
        [AllowAnonymous]
        public async Task<ActionResult<Response>> SignUp(TUser user, CancellationToken ct)
        {
            user.Type = null; // nunca aceitar o papel vindo do cliente
            var response = await users.SignUpAsync(user, ct);
            if (response.OK && await users.GetUserAsync(user.Login!, ct) is { } created)
                await Monitoring.AddAsync(created.ID, "SignUp", "Cadastro de usuario", user.Name, null, ct);
            return Result(response);
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserInfo>> Me(CancellationToken ct) =>
            await users.GetUserAsync(User.Identity!.Name!, ct) is { } user ? UserInfo.From(user) : Unauthorized();

        /// <summary>JWT é stateless: o cliente descarta o token. Mantém o registro de interação do original.</summary>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            await Log("Logout", null, ct);
            return NoContent();
        }
    }
}
