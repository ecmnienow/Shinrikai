using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nienow.DataBaseConn;
using Nienow.Helper;
using Nienow.Shinrikai.Core.Adaptive;
using Nienow.Shinrikai.Vocabulary.Model;
using Monitor = Nienow.Helper.Monitor;

namespace Nienow.Shinrikai.Vocabulary
{
    /// <summary>Item da transcrição: o aluno vê a palavra em kanji e escreve em hiragana.</summary>
    public record TranscriptionAnswer(int WordId, string Prompt, string? Answer);

    public record TranscriptionSession(string Subject, bool Mistakes, IReadOnlyList<IReadOnlyList<VocabularyItem>> Pages)
    {
        public int TotalItems => Pages.Sum(p => p.Count);
    }

    public interface ITranscriptionRepository
    {
        Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken ct = default);
        Task<IReadOnlyList<TWord>> GetVocabularyBySubjectAsync(int subjectId, CancellationToken ct = default);
        Task<IReadOnlyList<TWord>> GetMistakesAsync(int userId, int subjectId, CancellationToken ct = default);
        Task<IReadOnlyList<TWord>> GetWordsAsync(IReadOnlyList<int> ids, CancellationToken ct = default);
        Task<IReadOnlyList<TWord>> SearchKanjiWordAsync(string term, CancellationToken ct = default);
        Task<Monitor?> GetMonitorAsync(int userId, int wordId, CancellationToken ct = default);
        Task SaveMonitorAsync(Monitor monitor, CancellationToken ct = default);
    }

    /// <summary>Port de DBTranscription.cs (SQL parametrizado; monitor em TRL_MONITOR).</summary>
    public sealed class TranscriptionRepository(IShinrikaiDb db, IMonitorStore monitors) : ITranscriptionRepository
    {
        private const string HasKanji = "(VCB_KANJI_1 IS NOT NULL OR VCB_KANJI_2 IS NOT NULL OR VCB_KANJI_3 IS NOT NULL)";
        private const string SubjectFilter = "(VCB_SBJ_ID_1 = @SBJ OR VCB_SBJ_ID_2 = @SBJ OR VCB_SBJ_ID_3 = @SBJ)";

        /// <summary>Só assuntos que têm alguma palavra com kanji na possibilidade daquele assunto.</summary>
        public async Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken ct = default)
        {
            await using var conn = await db.OpenAsync(ct);
            return await conn.QueryAsync(
                """
                SELECT SBJ_ID, SBJ_NAME
                FROM SUBJECTS
                WHERE
                 (SELECT COUNT(*)
                    FROM VOCABULARY
                    WHERE ((VCB_SBJ_ID_1 = SBJ_ID AND VCB_KANJI_1 IS NOT NULL) OR
                           (VCB_SBJ_ID_2 = SBJ_ID AND VCB_KANJI_2 IS NOT NULL) OR
                           (VCB_SBJ_ID_3 = SBJ_ID AND VCB_KANJI_3 IS NOT NULL))) > 0
                ORDER BY SBJ_NAME
                """,
                r => new Subject(r.GetInt("SBJ_ID"), r.GetNullableString("SBJ_NAME") ?? ""), ct);
        }

        public async Task<IReadOnlyList<TWord>> GetVocabularyBySubjectAsync(int subjectId, CancellationToken ct = default)
        {
            await using var conn = await db.OpenAsync(ct);
            return await conn.QueryAsync(
                $"{VocabularyRepository.SelectWord} WHERE {HasKanji} AND {SubjectFilter}",
                VocabularyRepository.Map, ct, ("@SBJ", subjectId));
        }

        public async Task<IReadOnlyList<TWord>> GetMistakesAsync(int userId, int subjectId, CancellationToken ct = default)
        {
            await using var conn = await db.OpenAsync(ct);
            return await conn.QueryAsync(
                $"""
                {VocabularyRepository.SelectWord}
                  LEFT OUTER JOIN TRL_MONITOR ON TMN_VCB_ID = VCB_ID
                WHERE {HasKanji}
                  AND TMN_COUNTER > 0 AND TMN_USR_ID = @USR AND {SubjectFilter}
                """,
                VocabularyRepository.Map, ct, ("@USR", userId), ("@SBJ", subjectId));
        }

        public async Task<IReadOnlyList<TWord>> GetWordsAsync(IReadOnlyList<int> ids, CancellationToken ct = default)
        {
            if (ids.Count == 0)
                return [];
            var (inSql, parameters) = DbExtensions.InClause(ids.Distinct().ToList());
            await using var conn = await db.OpenAsync(ct);
            return await conn.QueryAsync($"{VocabularyRepository.SelectWord} WHERE VCB_ID IN ({inSql})", VocabularyRepository.Map, ct, parameters);
        }

        /// <summary>TranscriptionController.SearchKanjiWord — busca por kanji ou tradução (com kanji).</summary>
        public async Task<IReadOnlyList<TWord>> SearchKanjiWordAsync(string term, CancellationToken ct = default)
        {
            await using var conn = await db.OpenAsync(ct);
            return await conn.QueryAsync(
                $"""
                {VocabularyRepository.SelectWord}
                WHERE VCB_KANJI_1 LIKE @TERM OR
                  VCB_KANJI_2 LIKE @TERM OR
                  VCB_KANJI_3 LIKE @TERM OR
                  (VCB_TRANSLATION_1 LIKE @TERM AND VCB_KANJI_1 IS NOT NULL) OR
                  (VCB_TRANSLATION_2 LIKE @TERM AND VCB_KANJI_2 IS NOT NULL) OR
                  (VCB_TRANSLATION_3 LIKE @TERM AND VCB_KANJI_3 IS NOT NULL)
                """,
                VocabularyRepository.Map, ct, ("@TERM", "%" + term + "%"));
        }

        public Task<Monitor?> GetMonitorAsync(int userId, int wordId, CancellationToken ct = default) =>
            monitors.GetAsync(MonitorTables.Transcription, userId, wordId, ct);

        public Task SaveMonitorAsync(Monitor monitor, CancellationToken ct = default) =>
            monitors.SaveAsync(MonitorTables.Transcription, monitor, ct);
    }

    /// <summary>
    /// Port de TranscriptionManager.cs (Transcrição para hiragana). Mesma mecânica do vocabulário,
    /// mas o aluno vê a forma em kanji do assunto e escreve o kana; o contador usa a variante
    /// <see cref="FragmentVariation.ApplyTranscription"/>.
    /// </summary>
    public sealed class TranscriptionService(ITranscriptionRepository repository, TimeProvider? clock = null)
    {
        public const int SearchLimit = 10;

        private readonly TimeProvider _clock = clock ?? TimeProvider.System;

        public Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken ct = default) => repository.GetSubjectsAsync(ct);

        public async Task<TranscriptionSession?> GenerateAsync(int userId, int subjectId, bool mistakes, Random? rng = null, CancellationToken ct = default)
        {
            var subject = (await repository.GetSubjectsAsync(ct)).FirstOrDefault(s => s.Id == subjectId);
            if (subject is null)
                return null;

            var words = mistakes
                ? await repository.GetMistakesAsync(userId, subjectId, ct)
                : await repository.GetVocabularyBySubjectAsync(subjectId, ct);

            return BuildSession(subject.Name, words, mistakes, rng ?? Random.Shared);
        }

        /// <summary>
        /// Embaralha e pagina de 10 em 10. Palavras sem kanji na possibilidade do assunto ficam de
        /// fora: o original as pulava só na exibição (NextPage) e a correção, feita por posição,
        /// desalinhava as respostas. Aqui a correção é por WordId.
        /// </summary>
        public static TranscriptionSession BuildSession(string subject, IEnumerable<TWord> words, bool mistakes, Random rng)
        {
            var items = new List<VocabularyItem>();
            foreach (var word in words)
            {
                var kanji = word.GetKanjiWords(subject);
                if (kanji.Length == 0)
                    continue;
                items.Add(new VocabularyItem(word.ID, kanji[rng.Next(kanji.Length)]));
            }

            items.ShuffleList(rng);
            var pages = items.Chunk(VocabularyService.PageSize).Select(c => (IReadOnlyList<VocabularyItem>)c).ToList();
            return new TranscriptionSession(subject, mistakes, pages);
        }

        public static bool Check(TWord word, string? answer) =>
            !string.IsNullOrWhiteSpace(answer) && word.CheckTranscriptedWord(answer);

        public async Task<IReadOnlyList<VocabularyResult>> CheckAnswersAsync(int userId, IReadOnlyList<TranscriptionAnswer> answers, CancellationToken ct = default)
        {
            var words = (await repository.GetWordsAsync(answers.Select(a => a.WordId).ToList(), ct)).ToDictionary(w => w.ID);
            var results = new List<VocabularyResult>(answers.Count);

            foreach (var a in answers)
            {
                if (!words.TryGetValue(a.WordId, out var word))
                {
                    results.Add(new VocabularyResult(a.WordId, a.Prompt, a.Answer, Wrong: true, Expected: []));
                    continue;
                }

                var correct = Check(word, a.Answer);
                var monitor = await repository.GetMonitorAsync(userId, word.ID, ct) ?? new Monitor { UsrID = userId, VcbID = word.ID };
                FragmentVariation.ApplyTranscription(monitor, correct, _clock.GetLocalNow().DateTime);
                await repository.SaveMonitorAsync(monitor, ct);

                results.Add(new VocabularyResult(word.ID, a.Prompt, a.Answer, !correct, correct ? [] : [word.Kana ?? string.Empty]));
            }

            return results;
        }

        public async Task<IReadOnlyList<TWord>> SearchKanjiWordAsync(string term, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(term))
                return [];
            return (await repository.SearchKanjiWordAsync(term.Trim(), ct)).Take(SearchLimit).ToList();
        }
    }
}
