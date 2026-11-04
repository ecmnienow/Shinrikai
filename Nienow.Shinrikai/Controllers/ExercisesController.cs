using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Auth;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Exercises;
using Nienow.Shinrikai.Grammar;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.Text;
using Nienow.Shinrikai.User;

namespace Nienow.Shinrikai.Controllers
{
    public sealed record CheckExerciseRequest(int Answer);

    /// <summary>Port de ExercisesController (resolução + cadastro).</summary>
    [ApiController]
    [Route("api/exercises")]
    [Authorize]
    public sealed class ExercisesController(
        ExercisesService exercises,
        GrammarService grammar,
        TextService texts,
        UserService users,
        IMonitoringService monitoring) : ControllerBase
    {
        private const string Screen = "Resolução de exercícios";

        /// <summary>Index(grammarId, textId): IDs na ordem adaptativa (técnica 4.4).</summary>
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<int>>> List([FromQuery] int grammarId, [FromQuery] int textId, CancellationToken ct)
        {
            if (grammarId > 0 && textId > 0)
                return BadRequest(Core.Response.Fail("Informe gramática ou texto, não os dois."));

            // O original registrava "Texto " + grammarId (bug); aqui vai o ID do texto.
            var option = grammarId > 0 ? "Gramática " + grammarId : textId > 0 ? "Texto " + textId : "Geral";
            await Log("Pagina inicial", option, ct);

            var level = (await users.GetUserAsync(User.Identity!.Name!, ct))?.JLPT ?? User.Jlpt();
            return Ok(await exercises.ListAsync(level, grammarId, textId, ct: ct));
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
        public async Task<ActionResult<Core.Response>> Check(int id, CheckExerciseRequest request, CancellationToken ct)
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
                return BadRequest(Core.Response.Fail("Informe gramática ou texto, não os dois."));

            var exercise = new TExercise { GrammarId = grammarId, TextId = textId };
            if (grammarId > 0)
                exercise.Level = (await grammar.GetAsync(grammarId, ct))?.Level ?? EJLPT.None;
            if (textId > 0)
                exercise.Level = (await texts.GetForEditingAsync(textId, ct))?.Level ?? EJLPT.None;
            return exercise;
        }

        [HttpPost]
        [Authorize(Roles = Roles.ContentEditors)]
        public async Task<ActionResult<Core.Response>> Save(TExercise exercise, CancellationToken ct)
        {
            var response = await exercises.SaveAsync(exercise, ct);
            return response.OK ? response : BadRequest(response);
        }

        private Task Log(string action, string? option, CancellationToken ct) =>
            monitoring.AddAsync(User.UserId(), Screen, action, option, User.SessionId(), ct);
    }
}
