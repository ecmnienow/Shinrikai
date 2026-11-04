using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Auth;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;

namespace Nienow.Shinrikai.Controllers
{
    public sealed record InteractionRequest(string Action, string? Option);

    /// <summary>
    /// Base dos controllers: exige login e registra cada ação em INTERACTIONS com o nome da tela
    /// (<paramref name="screen"/>), como o MonitoringManager do original.
    /// </summary>
    [ApiController]
    [Authorize]
    public abstract class ShinrikaiController(IMonitoringService monitoring, string screen) : ControllerBase
    {
        public const int MaxUploadBytes = 20 * 1024 * 1024;

        protected IMonitoringService Monitoring { get; } = monitoring;

        protected Task Log(string action, string? option, CancellationToken ct) =>
            Monitoring.AddAsync(User.UserId(), screen, action, option, User.SessionId(), ct);

        /// <summary>O nível vem do banco (não do token), para refletir alterações sem novo login.</summary>
        protected async Task<EJLPT> CurrentLevelAsync(UserService users, CancellationToken ct) =>
            (await users.GetUserAsync(User.Identity!.Name!, ct))?.JLPT ?? User.Jlpt();

        /// <summary>Regra de negócio recusada → 400 com a mensagem.</summary>
        protected ActionResult<Response> Result(Response response) => response.OK ? response : BadRequest(response);

        protected static ActionResult PageSizeError() => new BadRequestObjectResult(Core.Response.Fail($"Envie de 1 a {Paging.PageSize} respostas."));

        protected static bool IsValidPage(int count) => count is > 0 and <= Paging.PageSize;
    }

    /// <summary>Telas de estudo em lista: a navegação feita no cliente (páginas, repetir) é registrada por aqui.</summary>
    public abstract class StudyController(IMonitoringService monitoring, string screen) : ShinrikaiController(monitoring, screen)
    {
        [HttpPost("interactions")]
        public async Task<IActionResult> Interaction(InteractionRequest request, CancellationToken ct)
        {
            await Log(request.Action, request.Option, ct);
            return NoContent();
        }
    }

    public static class FormFileExtensions
    {
        public static byte[]? ReadBytes(this IFormFile? file)
        {
            if (file is not { Length: > 0 })
                return null;
            using var ms = new MemoryStream();
            file.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
