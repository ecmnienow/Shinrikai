using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Api.Auth;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Grammar;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;

namespace Nienow.Shinrikai.Api.Controllers
{
    /// <summary>Port de GrammarController (mapa adaptativo + cadastro).</summary>
    [Route("api/grammar")]
    public sealed class GrammarController(GrammarService grammar, UserService users, IMonitoringService monitoring)
        : ShinrikaiController(monitoring, "Estudo de gramatica")
    {
        /// <summary>Index: o mapa, agrupado por nível, com cada conteúdo liberado ou não para o aluno.</summary>
        [HttpGet]
        public async Task<IReadOnlyList<GrammarMapGroup>> Map(CancellationToken ct)
        {
            await Log("Pagina inicial", null, ct);
            return await grammar.GetMapAsync(User.UserId(), await CurrentLevelAsync(users, ct), ct);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TGrammar>> Get(int id, CancellationToken ct)
        {
            var item = await grammar.GetAsync(id, ct);
            if (item is null)
                return NotFound();

            await Log("Selecao", id.ToString(), ct);
            return item;
        }

        [HttpGet("{id:int}/requisites")]
        public async Task<ActionResult<GrammarRequisites>> Requisites(int id, CancellationToken ct)
        {
            await Log("Ver requisitos", id.ToString(), ct);
            return await grammar.GetRequisitesAsync(id, await CurrentLevelAsync(users, ct), ct) is { } r ? r : NotFound();
        }

        /// <summary>Chamado pelo front após 20 s de leitura (AdaptiveMap.ReadingTimeToDeclareKnowledge).</summary>
        [HttpPost("{id:int}/knowledge")]
        public async Task<IActionResult> DeclareKnowledge(int id, CancellationToken ct)
        {
            await grammar.DeclareKnowledgeAsync(User.UserId(), id, ct);
            return NoContent();
        }

        // ---- Cadastro (Administrator, Teacher) ----

        /// <summary>ListAllGrammars: conteúdos de um nível, para escolher pré-requisitos.</summary>
        [HttpGet("all")]
        [Authorize(Roles = Roles.ContentEditors)]
        public Task<IReadOnlyList<GrammarSummary>> ListAll([FromQuery] EJLPT? jlpt, CancellationToken ct) =>
            grammar.ListByLevelAsync(jlpt, ct);

        [HttpGet("{id:int}/edit")]
        [Authorize(Roles = Roles.ContentEditors)]
        public async Task<ActionResult<TGrammar>> GetForEdit(int id, CancellationToken ct) =>
            await grammar.GetAsync(id, ct) is { } item ? item : NotFound();

        [HttpPost]
        [Authorize(Roles = Roles.ContentEditors)]
        public async Task<ActionResult<Response>> Save(GrammarForm form, CancellationToken ct) =>
            Result(await grammar.SaveAsync(form, ct));
    }
}
