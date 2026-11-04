using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Auth;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;
using Nienow.Shinrikai.Vocabulary;
using Nienow.Shinrikai.Vocabulary.Model;

namespace Nienow.Shinrikai.Controllers
{
    public sealed record GenerateVocabularyRequest(int SubjectId, bool ToJapanese, bool Mistakes);

    public sealed record CheckVocabularyRequest(string Subject, bool ToJapanese, IReadOnlyList<VocabularyAnswer> Answers);

    /// <summary>Formulário de cadastro: campos do TWord + até 3 imagens e 3 áudios.</summary>
    public sealed class WordForm
    {
        public int ID { get; set; }
        public string? Kana { get; set; }
        public string? Romaji { get; set; }
        public int SubjectID1 { get; set; }
        public int SubjectID2 { get; set; }
        public int SubjectID3 { get; set; }
        public string? KanjiWord1 { get; set; }
        public string? KanjiWord2 { get; set; }
        public string? KanjiWord3 { get; set; }
        public string? Word1 { get; set; }
        public string? Word2 { get; set; }
        public string? Word3 { get; set; }
        public IFormFile? Image1 { get; set; }
        public IFormFile? Image2 { get; set; }
        public IFormFile? Image3 { get; set; }
        public IFormFile? Audio1 { get; set; }
        public IFormFile? Audio2 { get; set; }
        public IFormFile? Audio3 { get; set; }

        public TWord ToWord() => new()
        {
            ID = ID, Kana = Kana, Romaji = Romaji,
            SubjectID1 = SubjectID1, SubjectID2 = SubjectID2, SubjectID3 = SubjectID3,
            KanjiWord1 = KanjiWord1, KanjiWord2 = KanjiWord2, KanjiWord3 = KanjiWord3,
            Word1 = Word1, Word2 = Word2, Word3 = Word3,
            Image1 = Image1.ReadBytes(), Image2 = Image2.ReadBytes(), Image3 = Image3.ReadBytes(),
            Audio1 = Audio1.ReadBytes(), Audio2 = Audio2.ReadBytes(), Audio3 = Audio3.ReadBytes(),
        };
    }

    /// <summary>Port de VocabularyController (estudo + cadastro).</summary>
    [Route("api/vocabulary")]
    public sealed class VocabularyController(VocabularyService vocabulary, IMonitoringService monitoring)
        : StudyController(monitoring, "Estudo de vocabulario")
    {
        /// <summary>
        /// Assuntos (usado no estudo e no cadastro). O registro de "Pagina inicial" é feito pela tela
        /// de estudo via /interactions, para o cadastro não contar como acesso ao estudo.
        /// </summary>
        [HttpGet("subjects")]
        public Task<IReadOnlyList<Subject>> Subjects(CancellationToken ct) => vocabulary.GetSubjectsAsync(ct);

        /// <summary>GenerateVocabulary: devolve todas as páginas; o cliente faz Anterior/Próximo/Repetir.</summary>
        [HttpPost("sessions")]
        public async Task<ActionResult<VocabularySession>> Generate(GenerateVocabularyRequest request, CancellationToken ct)
        {
            var session = await vocabulary.GenerateAsync(User.UserId(), request.SubjectId, request.ToJapanese, request.Mistakes, ct: ct);
            if (session is null)
                return NotFound(Core.Response.Fail("Assunto não encontrado."));

            await Log("Gerar lista", session.Subject + (request.Mistakes ? " mistakes" : ""), ct);
            return session;
        }

        [HttpPost("check")]
        public async Task<ActionResult<IReadOnlyList<VocabularyResult>>> Check(CheckVocabularyRequest request, CancellationToken ct)
        {
            if (!IsValidPage(request.Answers?.Count ?? 0))
                return PageSizeError();

            var results = await vocabulary.CheckAnswersAsync(User.UserId(), request.Subject, request.ToJapanese, request.Answers!, ct);
            await Log("Verificar respostas", results.Count(r => !r.Wrong) + " Acertos", ct);
            return Ok(results);
        }

        [HttpGet("words/{id:int}/{media:regex(^(image|audio)$)}/{slot:int:range(1,3)}")]
        public async Task<IActionResult> Media(int id, string media, int slot, CancellationToken ct)
        {
            var kind = media == "image" ? VocabularyMedia.Image : VocabularyMedia.Audio;
            return await vocabulary.GetMediaAsync(id, kind, slot, ct) is { } bytes
                ? File(bytes, kind == VocabularyMedia.Image ? "image/jpeg" : "audio/mpeg")
                : NotFound();
        }

        // ---- Cadastro (Administrator, Teacher) ----

        [HttpGet("words/{id:int}")]
        [Authorize(Roles = Roles.ContentEditors)]
        public async Task<ActionResult<TWord>> GetWord(int id, CancellationToken ct) =>
            await vocabulary.GetWordAsync(id, ct) is { } word ? word : NotFound();

        [HttpGet("words")]
        [Authorize(Roles = Roles.ContentEditors)]
        public async Task<ActionResult<TWord>> SearchWord([FromQuery] string kana, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(kana))
                return BadRequest();
            return await vocabulary.GetWordByKanaAsync(kana, ct) is { } word ? word : NotFound();
        }

        [HttpPost("words")]
        [Authorize(Roles = Roles.ContentEditors)]
        [RequestSizeLimit(MaxUploadBytes)]
        public async Task<ActionResult<Response>> SaveWord([FromForm] WordForm form, CancellationToken ct) =>
            Result(await vocabulary.SaveWordAsync(form.ToWord(), ct));
    }
}
