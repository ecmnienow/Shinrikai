using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Api.Auth;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.Text;
using Nienow.Shinrikai.User;

namespace Nienow.Shinrikai.Api.Controllers
{
    /// <summary>Port de TextController (leitura com stretchtext + cadastro).</summary>
    [Route("api/texts")]
    public sealed class TextController(TextService texts, UserService users, IMonitoringService monitoring)
        : ShinrikaiController(monitoring, "Leitura de textos")
    {
        [HttpGet]
        public async Task<IReadOnlyList<TextListGroup>> List(CancellationToken ct)
        {
            await Log("Pagina inicial", null, ct);
            return await texts.ListAsync(await CurrentLevelAsync(users, ct), ct);
        }

        /// <summary>GetText: corpo com furigana escondida conforme o conhecimento do aluno.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<TText>> Get(int id, CancellationToken ct)
        {
            var text = await texts.GetForReadingAsync(id, User.UserId(), ct);
            if (text is null)
                return NotFound();

            await Log("Selecao", id.ToString(), ct);
            return text;
        }

        /// <summary>Chamado pelo front após 20 s de leitura.</summary>
        [HttpPost("{id:int}/knowledge")]
        public async Task<IActionResult> DeclareKnowledge(int id, CancellationToken ct)
        {
            await texts.DeclareKnowledgeAsync(User.UserId(), id, ct);
            return NoContent();
        }

        /// <summary>O aluno clicou numa palavra para ver a furigana (não a conhecia).</summary>
        [HttpPost("words/{wordId:int}/lack-of-knowledge")]
        public async Task<IActionResult> RegisterLackOfKnowledge(int wordId, CancellationToken ct)
        {
            await Log("Desconhecimento de palavra", wordId.ToString(), ct);
            await texts.RegisterLackOfKnowledgeAsync(User.UserId(), wordId, ct);
            return NoContent();
        }

        // ---- Cadastro (Administrator, Teacher) ----

        [HttpGet("{id:int}/edit")]
        [Authorize(Roles = Roles.ContentEditors)]
        public async Task<ActionResult<TText>> GetForEdit(int id, CancellationToken ct) =>
            await texts.GetForEditingAsync(id, ct) is { } text ? text : NotFound();

        [HttpPost]
        [Authorize(Roles = Roles.ContentEditors)]
        public async Task<ActionResult<Response>> Save(TextForm form, CancellationToken ct) =>
            Result(await texts.SaveAsync(form, ct));
    }
}
