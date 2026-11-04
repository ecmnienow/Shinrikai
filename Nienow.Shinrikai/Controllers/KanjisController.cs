using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nienow.Helper;
using Nienow.Shinrikai.Auth;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Kanji;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;

namespace Nienow.Shinrikai.Controllers
{
    public sealed record GenerateKanjiRequest(string Level, bool Mistakes);

    public sealed record CheckKanjiRequest(IReadOnlyList<KanjiAnswer> Answers);

    public sealed class KanjiForm
    {
        public int ID { get; set; }
        public string? KanjiLetter { get; set; }
        public string? Kana { get; set; }
        public string? Romaji { get; set; }
        public string? JLPT { get; set; }
        public string? Info { get; set; }
        public IFormFile? Image { get; set; }

        public TKanji ToKanji()
        {
            byte[]? image = null;
            if (Image is { Length: > 0 })
            {
                using var stream = Image.OpenReadStream();
                image = stream.ToByteArray();
            }

            return new TKanji
            {
                ID = ID,
                KanjiLetter = KanjiLetter?.Trim(),
                Kana = Kana,
                Romaji = Romaji,
                JLPT = JLPT,
                Info = Info,
                Image = image,
            };
        }
    }

    /// <summary>Port de KanjisController (leitura de kanjis + cadastro).</summary>
    [ApiController]
    [Route("api/kanjis")]
    [Authorize]
    public sealed class KanjisController(KanjiService kanjis, IMonitoringService monitoring) : ControllerBase
    {
        private const string Screen = "Leitura de kanjis";

        [HttpPost("sessions")]
        public async Task<ActionResult<KanjiSession>> Generate(GenerateKanjiRequest request, CancellationToken ct)
        {
            if (KanjiLevels.Normalize(request.Level) is not { } level || !KanjiLevels.All.Contains(level))
                return BadRequest(Core.Response.Fail("Nível inválido."));

            await Log("Gerar lista", level + (request.Mistakes ? " mistakes" : ""), ct);
            return await kanjis.GenerateAsync(User.UserId(), level, request.Mistakes, ct: ct);
        }

        [HttpPost("check")]
        public async Task<ActionResult<IReadOnlyList<KanjiResult>>> Check(CheckKanjiRequest request, CancellationToken ct)
        {
            if (request.Answers is not { Count: > 0 } || request.Answers.Count > KanjiService.PageSize)
                return BadRequest(Core.Response.Fail($"Envie de 1 a {KanjiService.PageSize} respostas."));

            var results = await kanjis.CheckAnswersAsync(User.UserId(), request.Answers, ct);
            await Log("Verificar respostas", results.Count(r => !r.Wrong) + " Acertos", ct);
            return Ok(results);
        }

        [HttpPost("interactions")]
        public async Task<IActionResult> Interaction(InteractionRequest request, CancellationToken ct)
        {
            await Log(request.Action, request.Option, ct);
            return NoContent();
        }

        [HttpGet("{id:int}/image")]
        public async Task<IActionResult> Image(int id, CancellationToken ct) =>
            await kanjis.GetImageAsync(id, ct) is { } bytes ? File(bytes, "image/jpeg") : NotFound();

        // ---- Cadastro (Administrator, Teacher) ----

        [HttpGet]
        [Authorize(Roles = Roles.ContentEditors)]
        public async Task<ActionResult<TKanji>> Get([FromQuery] string letter, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(letter))
                return BadRequest();
            return await kanjis.GetByLetterAsync(letter, ct) is { } kanji ? kanji : NotFound(Core.Response.Fail("Kanji não encontrado!"));
        }

        [HttpPost]
        [Authorize(Roles = Roles.ContentEditors)]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<ActionResult<Core.Response>> Save([FromForm] KanjiForm form, CancellationToken ct)
        {
            var response = await kanjis.SaveAsync(form.ToKanji(), ct);
            return response.OK ? response : BadRequest(response);
        }

        private Task Log(string action, string? option, CancellationToken ct) =>
            monitoring.AddAsync(User.UserId(), Screen, action, option, User.SessionId(), ct);
    }
}
