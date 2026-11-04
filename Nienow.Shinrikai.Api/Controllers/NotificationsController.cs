using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Api.Auth;
using Nienow.Shinrikai.User;

namespace Nienow.Shinrikai.Api.Controllers
{
    public sealed record NotificationMessage(int Id, string Message);

    /// <summary>
    /// Port de HomeController.GetNotification / DeclareUnderstanding: as janelas de "Ajuda" de cada
    /// tela (1 Transcrição, 3 Gramática, 4 Texto, 5 Exercícios, 6 Vocabulário, 7 Kanjis).
    /// Sem <c>force</c>, só retorna mensagem se o aluno ainda não clicou em "Entendi".
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/notifications")]
    public sealed class NotificationsController(IUserRepository users) : ControllerBase
    {
        [HttpGet("{id:int}")]
        public async Task<NotificationMessage> Get(int id, [FromQuery] bool force, CancellationToken ct) =>
            new(id, await users.GetNotificationAsync(User.UserId(), id, force, ct));

        [HttpPost("{id:int}/understood")]
        public async Task<IActionResult> Understood(int id, CancellationToken ct)
        {
            await users.DeclareUnderstandingAsync(User.UserId(), id, ct);
            return NoContent();
        }
    }
}
