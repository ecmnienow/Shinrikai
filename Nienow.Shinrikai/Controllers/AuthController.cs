using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Auth;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;

namespace Nienow.Shinrikai.Controllers
{
    public sealed record LoginRequest(string Username, string Password);

    /// <summary>Port de LoginController (Forms Auth + Session → JWT).</summary>
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController(UserService users, JwtTokenService tokens, IMonitoringService monitoring) : ControllerBase
    {
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthenticatedUser>> Login(LoginRequest request, CancellationToken ct)
        {
            var user = await users.LoginAsync(request.Username, request.Password, ct);
            if (user is null)
                return Unauthorized(new Response { OK = false, Message = "Usuário ou senha inválidos." });

            var auth = tokens.Issue(user);
            await monitoring.AddAsync(user.ID, "Login", "Autenticacao", "Autenticado", null, ct);
            return auth;
        }

        [HttpPost("signup")]
        [AllowAnonymous]
        public async Task<ActionResult<Response>> SignUp(TUser user, CancellationToken ct)
        {
            user.Type = null; // nunca aceitar o papel vindo do cliente
            var response = await users.SignUpAsync(user, ct);
            if (!response.OK)
                return BadRequest(response);

            var created = await users.GetUserAsync(user.Login!, ct);
            if (created is not null)
                await monitoring.AddAsync(created.ID, "SignUp", "Cadastro de usuario", user.Name, null, ct);

            return response;
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserInfo>> Me(CancellationToken ct)
        {
            var user = await users.GetUserAsync(User.Identity!.Name!, ct);
            if (user is null)
                return Unauthorized();

            return new UserInfo(user.ID, user.Login, user.Name, user.JLPT, user.Role);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            // JWT é stateless: o cliente descarta o token. Mantém o registro de interação do original.
            await monitoring.AddAsync(User.UserId(), "Logout", "Logout", null, User.SessionId(), ct);
            return NoContent();
        }
    }
}
