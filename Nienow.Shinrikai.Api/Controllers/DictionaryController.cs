using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Dictionary;
using Nienow.Shinrikai.Monitoring;

namespace Nienow.Shinrikai.Api.Controllers
{
    /// <summary>
    /// Port de DictionaryController. Imagem e áudio usam /api/vocabulary/words/{id}/{image|audio}/{slot}
    /// (o original só servia a possibilidade 1, mesmo quando a imagem estava na 2 ou na 3).
    /// </summary>
    [Route("api/dictionary")]
    public sealed class DictionaryController(DictionaryService dictionary, IMonitoringService monitoring)
        : ShinrikaiController(monitoring, "Dicionario")
    {
        [HttpPost("interactions")]
        public async Task<IActionResult> Opened(CancellationToken ct)
        {
            await Log("Pagina inicial", null, ct);
            return NoContent();
        }

        [HttpGet("search")]
        public async Task<SearchResult> Search([FromQuery] string? term, [FromQuery] ESearchType type = ESearchType.General, CancellationToken ct = default)
        {
            await Log("Pesquisa", term, ct);
            return await dictionary.SearchAsync(term, type, ct);
        }
    }
}
