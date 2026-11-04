using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Api.Auth;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Exercises;
using Nienow.Shinrikai.Grammar;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.Text;
using Nienow.Shinrikai.User;

namespace Nienow.Shinrikai.Api.Controllers
{
    public sealed record CheckExerciseRequest(int Answer);

    /// <summary>Port de ExercisesController (resolução + cadastro).</summary>
    [Route("api/exercises")]
    public sealed class ExercisesController(
        ExercisesService exercises,
        GrammarService grammar,
        TextService texts,
        UserService users,
        IMonitoringService monitoring) : ShinrikaiController(monitoring, "Resolução de exercícios")
    {
        private const string BothContents = "Informe gramática ou texto, não os dois.";

        /// <summary>Index(grammarId, textId): IDs na ordem adaptativa (técnica 4.4).</summary>
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<int>>> List([FromQuery] int grammarId, [FromQuery] int textId, CancellationToken ct)
        {
            if (grammarId > 0 && textId > 0)
                return BadRequest(Core.Response.Fail(BothContents));

            // O original registrava "Texto " + grammarId (bug); aqui vai o ID do texto.
            await Log("Pagina inicial", grammarId > 0 ? "Gramática " + grammarId : textId > 0 ? "Texto " + textId : "Geral", ct);
            return Ok(await exercises.ListAsync(await CurrentLevelAsync(users, ct), grammarId, textId, ct: ct));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ExerciseQuestion>> Get(int id, CancellationToken ct)
        {
            var question = await exercises.GetQuestionAsync(id, ct);
            if (question is null)
                return NotFound();

            await Log("Selecao", id.ToString(), ct);
            return question;
        }

        [HttpPost("{id:int}/check")]
        public async Task<ActionResult<Response>> Check(int id, CheckExerciseRequest request, CancellationToken ct)
        {
            var result = await exercises.CheckAnswerAsync(User.UserId(), id, request.Answer, ct);
            if (result is null)
                return NotFound();

            await Log("Verifica resposta", "Questao " + id + ": " + result.OK, ct);
            return result;
        }

        // ---- Cadastro (Administrator, Teacher) ----

        /// <summary>Register(id, grammarId, textId): exercício existente ou um novo já ligado ao conteúdo, com o nível dele.</summary>
        [HttpGet("edit")]
        [Authorize(Roles = Roles.ContentEditors)]
        public async Task<ActionResult<TExercise>> GetForEdit([FromQuery] int id, [FromQuery] int grammarId, [FromQuery] int textId, CancellationToken ct)
        {
            if (id != 0)
                return await exercises.GetAsync(id, ct) is { } existing ? existing : NotFound();

            if (grammarId > 0 && textId > 0)
                return BadRequest(Core.Response.Fail(BothContents));

            var level = grammarId > 0 ? (await grammar.GetAsync(grammarId, ct))?.Level
                : textId > 0 ? (await texts.GetForEditingAsync(textId, ct))?.Level
                : null;
            return new TExercise { GrammarId = grammarId, TextId = textId, Level = level ?? EJLPT.None };
        }

        [HttpPost]
        [Authorize(Roles = Roles.ContentEditors)]
        public async Task<ActionResult<Response>> Save(TExercise exercise, CancellationToken ct) =>
            Result(await exercises.SaveAsync(exercise, ct));
    }
}
