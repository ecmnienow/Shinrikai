using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Auth;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.Vocabulary;
using Nienow.Shinrikai.Vocabulary.Model;

namespace Nienow.Shinrikai.Controllers
{
    public sealed record GenerateTranscriptionRequest(int SubjectId, bool Mistakes);

    public sealed record CheckTranscriptionRequest(IReadOnlyList<VocabularyAnswer> Answers);

    /// <summary>Port de TranscriptionController (Transcrição para hiragana).</summary>
    [Route("api/transcription")]
    public sealed class TranscriptionController(TranscriptionService transcription, IMonitoringService monitoring)
        : StudyController(monitoring, "Transcricao de vocabulario")
    {
        [HttpGet("subjects")]
        public Task<IReadOnlyList<Subject>> Subjects(CancellationToken ct) => transcription.GetSubjectsAsync(ct);

        [HttpPost("sessions")]
        public async Task<ActionResult<TranscriptionSession>> Generate(GenerateTranscriptionRequest request, CancellationToken ct)
        {
            var session = await transcription.GenerateAsync(User.UserId(), request.SubjectId, request.Mistakes, ct: ct);
            if (session is null)
                return NotFound(Core.Response.Fail("Assunto não encontrado."));

            await Log("Gerar lista", session.Subject + (request.Mistakes ? " mistakes" : ""), ct);
            return session;
        }

        [HttpPost("check")]
        public async Task<ActionResult<IReadOnlyList<VocabularyResult>>> Check(CheckTranscriptionRequest request, CancellationToken ct)
        {
            if (!IsValidPage(request.Answers?.Count ?? 0))
                return PageSizeError();

            var results = await transcription.CheckAnswersAsync(User.UserId(), request.Answers!, ct);
            await Log("Verificar respostas", results.Count(r => !r.Wrong) + " Acertos", ct);
            return Ok(results);
        }

        /// <summary>TranscriptionController.SearchKanjiWord — até 10 palavras com kanji (usado pelo editor de textos).</summary>
        [HttpGet("kanji-words")]
        public Task<IReadOnlyList<TWord>> SearchKanjiWord([FromQuery] string? term, CancellationToken ct) =>
            transcription.SearchKanjiWordAsync(term ?? string.Empty, ct);
    }
}
