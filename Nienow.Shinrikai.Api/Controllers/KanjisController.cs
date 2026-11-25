using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Api.Auth;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Core.Adaptive;
using Nienow.Shinrikai.Kanji;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;

namespace Nienow.Shinrikai.Api.Controllers
{
    public sealed record GenerateKanjiRequest(string Level, bool Mistakes);

    public sealed record CheckKanjiRequest(IReadOnlyList<KanjiAnswer> Answers, string? Level = null);

    public sealed class KanjiForm
    {
        public int ID { get; set; }
        public string? KanjiLetter { get; set; }
        public string? Kana { get; set; }
        public string? Romaji { get; set; }
        public string? JLPT { get; set; }
        public string? Info { get; set; }
        public IFormFile? Image { get; set; }

        public TKanji ToKanji() => new()
        {
            ID = ID, KanjiLetter = KanjiLetter?.Trim(), Kana = Kana, Romaji = Romaji, JLPT = JLPT, Info = Info,
            Image = Image.ReadBytes(),
        };
    }

    /// <summary>Port de KanjisController (leitura de kanjis + cadastro).</summary>
    [Route("api/kanjis")]
    public sealed class KanjisController(KanjiService kanjis, IMonitoringService monitoring)
        : StudyController(monitoring, "Leitura de kanjis")
    {
        [HttpPost("sessions")]
        public async Task<ActionResult<KanjiSession>> Generate(GenerateKanjiRequest request, CancellationToken ct)
        {
            if (JlptLevel.Normalize(request.Level) is not { } level || !KanjiLevels.All.Contains(level))
                return BadRequest(Core.Response.Fail("Nível inválido."));

            await Log("Gerar lista", level + (request.Mistakes ? " mistakes" : ""), ct);
            return await kanjis.GenerateAsync(User.UserId(), level, request.Mistakes, ct: ct);
        }

        [HttpPost("check")]
        public async Task<ActionResult<IReadOnlyList<KanjiResult>>> Check(CheckKanjiRequest request, CancellationToken ct)
        {
            if (!IsValidPage(request.Answers?.Count ?? 0))
                return PageSizeError();

            var results = await kanjis.CheckAnswersAsync(User.UserId(), request.Answers!, request.Level, ct);
            await Log("Verificar respostas", results.Count(r => !r.Wrong) + " Acertos", ct);
            return Ok(results);
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

        [HttpGet("readings")]
        [Authorize(Roles = Roles.ContentEditors)]
        public async Task<ActionResult<KanjiReadingReport>> Readings([FromQuery] string letter, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(letter))
                return BadRequest();
            return await kanjis.GetReadingReportAsync(letter, ct) is { } report ? report : NotFound(Core.Response.Fail("Kanji não encontrado!"));
        }

        [HttpPost]
        [Authorize(Roles = Roles.ContentEditors)]
        [RequestSizeLimit(MaxUploadBytes)]
        public async Task<ActionResult<Response>> Save([FromForm] KanjiForm form, CancellationToken ct) =>
            Result(await kanjis.SaveAsync(form.ToKanji(), ct));
    }
}
