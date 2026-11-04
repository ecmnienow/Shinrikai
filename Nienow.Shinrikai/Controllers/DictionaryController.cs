using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Auth;
using Nienow.Shinrikai.Dictionary;
using Nienow.Shinrikai.Monitoring;

namespace Nienow.Shinrikai.Controllers
{
    /// <summary>
    /// Port de DictionaryController. Imagem e áudio usam /api/vocabulary/words/{id}/{image|audio}/{slot}
    /// (o original só servia a possibilidade 1, mesmo quando a imagem estava na 2 ou na 3).
    /// </summary>
    [ApiController]
    [Route("api/dictionary")]
    [Authorize]
    public sealed class DictionaryController(DictionaryService dictionary, IMonitoringService monitoring) : ControllerBase
    {
        private const string Screen = "Dicionario";

        [HttpPost("interactions")]
        public async Task<IActionResult> Opened(CancellationToken ct)
        {
            await monitoring.AddAsync(User.UserId(), Screen, "Pagina inicial", null, User.SessionId(), ct);
            return NoContent();
        }

        [HttpGet("search")]
        public async Task<SearchResult> Search([FromQuery] string? term, [FromQuery] ESearchType type = ESearchType.General, CancellationToken ct = default)
        {
            await monitoring.AddAsync(User.UserId(), Screen, "Pesquisa", term, User.SessionId(), ct);
            return await dictionary.SearchAsync(term, type, ct);
        }
    }
}
