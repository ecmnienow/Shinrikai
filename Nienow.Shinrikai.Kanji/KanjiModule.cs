using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Nienow.DataBaseConn;
using Nienow.Helper;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Core.Adaptive;

namespace Nienow.Shinrikai.Kanji
{
    /// <summary>Port de TKanji.cs.</summary>
    public class TKanji
    {
        public int ID { get; set; }
        public string? KanjiLetter { get; set; }
        /// <summary>Leituras em kana separadas por vírgula.</summary>
        public string? Kana { get; set; }
        public string? Romaji { get; set; }
        public string? Info { get; set; }
        /// <summary>Nível JLPT ("N5"…"N1") ou null.</summary>
        public string? JLPT { get; set; }
        public bool HasImage { get; set; }

        [JsonIgnore] public byte[]? Image { get; set; }

        public string[] KanaReadingForms => (Kana ?? string.Empty).Split(',');

        /// <summary>
        /// TKanji.CheckReading — copiado 1:1. Cada posição certa soma 1, 2 ou 4 ao resultado.
        /// Respostas vazias ou repetidas entre si não contam.
        /// </summary>
        public int CheckReading(params string?[] readingForms)
        {
            var resultsArray = new[] { 1, 2, 4 };
            var results = 0;

            for (var i = 0; i < readingForms.Length && i < resultsArray.Length; i++)
            {
                var text = readingForms[i];
                if (string.IsNullOrEmpty(text))
                    continue;
                if (readingForms.Count(t => t == text) > 1)
                    continue;
                if (KanaReadingForms.FirstOrDefault(o => o == text) != null)
                    results += resultsArray[i];
            }

            return results;
        }
    }

    /// <summary>Kanji a ser lido: o aluno escreve até 3 leituras (GKanjiAnswer).</summary>
    public record KanjiItem(int KanjiId, string Kanji, int Inputs);

    public record KanjiSession(string Level, bool Mistakes, IReadOnlyList<IReadOnlyList<KanjiItem>> Pages);

    public record KanjiAnswer(int KanjiId, string Kanji, IReadOnlyList<string?> Answers);

    /// <summary>Resultado por kanji; <see cref="WrongInputs"/> indica cada campo errado.</summary>
    public record KanjiResult(int KanjiId, string Kanji, IReadOnlyList<string?> Answers, IReadOnlyList<bool> WrongInputs, bool Wrong, string[] Expected);

    public static class KanjiLevels
    {
        public static readonly string[] All = ["N5", "N4", "N3", "N2", "N1"];

        /// <summary>
        /// KanjiManager.StartLoadingKanjis: níveis cumulativos (N4 = N5 + N4 …). N1 = todos os kanjis,
        /// inclusive sem nível (retorna null = sem filtro).
        /// </summary>
        public static IReadOnlyList<string>? Cumulative(string level)
        {
            var index = Array.IndexOf(All, JlptLevel.Normalize(level));
            if (index < 0)
                throw new ArgumentException($"Nível inválido: {level}", nameof(level));
            return index == All.Length - 1 ? null : All[..(index + 1)];
        }
    }

    public interface IKanjiRepository
    {
        Task<IReadOnlyList<TKanji>> GetKanjisAsync(IReadOnlyList<string>? levels, CancellationToken ct = default);
        Task<IReadOnlyList<TKanji>> GetMistakesAsync(int userId, IReadOnlyList<string>? levels, CancellationToken ct = default);
        Task<IReadOnlyList<TKanji>> GetByIdsAsync(IReadOnlyList<int> ids, CancellationToken ct = default);
        Task<TKanji?> GetByLetterAsync(string letter, CancellationToken ct = default);
        Task<byte[]?> GetImageAsync(int id, CancellationToken ct = default);
        Task AddAsync(TKanji kanji, CancellationToken ct = default);
        Task UpdateAsync(TKanji kanji, CancellationToken ct = default);
    }

    /// <summary>Port de DBKanji.cs (SQL parametrizado).</summary>
    public sealed class KanjiRepository(IShinrikaiDb db) : IKanjiRepository
    {
        private const string Select =
            """
            SELECT KNJ_ID, KNJ_KANJI, KNJ_KANA, KNJ_ROMAJI, KNJ_JLPT, KNJ_INFO,
              (CASE WHEN KNJ_IMAGE IS NOT NULL THEN 'T' ELSE 'F' END) HAS_IMAGE
            FROM KANJI
            """;

        public Task<IReadOnlyList<TKanji>> GetKanjisAsync(IReadOnlyList<string>? levels, CancellationToken ct = default)
        {
            var (where, parameters) = LevelFilter(levels, "WHERE");
            return db.QueryAsync($"{Select} {where}", Map, ct, parameters);
        }

        public Task<IReadOnlyList<TKanji>> GetMistakesAsync(int userId, IReadOnlyList<string>? levels, CancellationToken ct = default)
        {
            var (where, parameters) = LevelFilter(levels, "AND");
            return db.QueryAsync(
                $"""
                {Select}
                  LEFT OUTER JOIN KAN_MONITOR ON KMN_KNJ_ID = KNJ_ID
                WHERE KMN_COUNTER > 0 AND KMN_USR_ID = @USR {where}
                """,
                Map, ct, [("@USR", userId), .. parameters]);
        }

        public Task<IReadOnlyList<TKanji>> GetByIdsAsync(IReadOnlyList<int> ids, CancellationToken ct = default)
        {
            if (ids.Count == 0)
                return Task.FromResult<IReadOnlyList<TKanji>>([]);
            var (inSql, parameters) = DbExtensions.InClause(ids);
            return db.QueryAsync($"{Select} WHERE KNJ_ID IN ({inSql})", Map, ct, parameters);
        }

        public Task<TKanji?> GetByLetterAsync(string letter, CancellationToken ct = default) =>
            db.QuerySingleOrDefaultAsync($"{Select} WHERE KNJ_KANJI = @KNJ", Map, ct, ("@KNJ", letter));

        public async Task<byte[]?> GetImageAsync(int id, CancellationToken ct = default) =>
            await db.ScalarAsync("SELECT KNJ_IMAGE FROM KANJI WHERE KNJ_ID = @ID", ct, ("@ID", id)) as byte[];

        public Task AddAsync(TKanji kanji, CancellationToken ct = default) =>
            db.ExecuteAsync(
                """
                INSERT INTO KANJI (KNJ_ID, KNJ_KANJI, KNJ_KANA, KNJ_ROMAJI, KNJ_JLPT, KNJ_INFO, KNJ_IMAGE)
                VALUES (GEN_ID(KANJI_ID_GEN, 1), @KNJ, @KAN, @RMJ, @LVL, @INF, @IMG)
                """,
                ct, [.. Parameters(kanji), ("@IMG", kanji.Image)]);

        /// <summary>A imagem só é sobrescrita quando uma nova foi enviada.</summary>
        public Task UpdateAsync(TKanji kanji, CancellationToken ct = default)
        {
            var hasImage = kanji.Image is { Length: > 0 };
            return db.ExecuteAsync(
                $"""
                UPDATE KANJI SET
                  KNJ_KANJI = @KNJ, KNJ_KANA = @KAN, KNJ_ROMAJI = @RMJ,
                  KNJ_JLPT = @LVL, KNJ_INFO = @INF{(hasImage ? ", KNJ_IMAGE = @IMG" : "")}
                WHERE KNJ_ID = @ID
                """,
                ct, [.. Parameters(kanji), ("@ID", kanji.ID), .. hasImage ? [("@IMG", (object?)kanji.Image)] : Array.Empty<(string, object?)>()]);
        }

        private static (string Sql, (string, object?)[] Parameters) LevelFilter(IReadOnlyList<string>? levels, string keyword)
        {
            if (levels is null)
                return (string.Empty, []);
            var (inSql, parameters) = DbExtensions.InClause(levels, "lvl");
            return ($"{keyword} UPPER(KNJ_JLPT) IN ({inSql})", parameters);
        }

        private static (string, object?)[] Parameters(TKanji kanji) =>
        [
            ("@KNJ", kanji.KanjiLetter),
            ("@KAN", kanji.Kana),
            ("@RMJ", kanji.Romaji),
            ("@LVL", JlptLevel.Normalize(kanji.JLPT)),
            ("@INF", kanji.Info == "none" ? null : kanji.Info),
        ];

        private static TKanji Map(DbDataReader r) => new()
        {
            ID = r.GetInt("KNJ_ID"),
            KanjiLetter = r.GetNullableString("KNJ_KANJI"),
            Kana = r.GetNullableString("KNJ_KANA"),
            Romaji = r.GetNullableString("KNJ_ROMAJI"),
            JLPT = JlptLevel.Normalize(r.GetNullableString("KNJ_JLPT")),
            Info = r.GetNullableString("KNJ_INFO"),
            HasImage = r.GetFlag("HAS_IMAGE"),
        };
    }

    /// <summary>Port de KanjiManager.cs (leitura de kanjis + cadastro).</summary>
    public sealed class KanjiService(IKanjiRepository repository, IMonitorStore monitors, TimeProvider? clock = null)
    {
        public const int MaxInputs = 3;

        private DateTime Now => (clock ?? TimeProvider.System).GetLocalNow().DateTime;

        public async Task<KanjiSession> GenerateAsync(int userId, string level, bool mistakes, Random? rng = null, CancellationToken ct = default)
        {
            var levels = KanjiLevels.Cumulative(level);
            var kanjis = mistakes
                ? await repository.GetMistakesAsync(userId, levels, ct)
                : await repository.GetKanjisAsync(levels, ct);

            return BuildSession(JlptLevel.Normalize(level)!, kanjis, mistakes, rng ?? Random.Shared);
        }

        public static KanjiSession BuildSession(string level, IEnumerable<TKanji> kanjis, bool mistakes, Random rng)
        {
            var items = kanjis
                .Where(k => !string.IsNullOrEmpty(k.KanjiLetter))
                .Select(k => new KanjiItem(k.ID, k.KanjiLetter!, Math.Min(k.KanaReadingForms.Length, MaxInputs)))
                .ToList();
            items.ShuffleList(rng);
            return new KanjiSession(level, mistakes, items.Paginate());
        }

        /// <summary>KanjiManager.CheckAnswers: o kanji está certo só se todas as leituras pedidas estão certas.</summary>
        public static KanjiResult Check(TKanji kanji, KanjiAnswer answer)
        {
            var answers = answer.Answers.Take(MaxInputs).ToArray();
            var bits = kanji.CheckReading(answers);
            var wrongInputs = answers.Select((_, i) => (bits & (1 << i)) == 0).ToArray();
            var wrong = wrongInputs.Length == 0 || wrongInputs.Any(w => w);
            return new KanjiResult(kanji.ID, answer.Kanji, answers, wrongInputs, wrong, wrong ? kanji.KanaReadingForms : []);
        }

        public async Task<IReadOnlyList<KanjiResult>> CheckAnswersAsync(int userId, IReadOnlyList<KanjiAnswer> answers, CancellationToken ct = default)
        {
            var kanjis = (await repository.GetByIdsAsync(answers.Select(a => a.KanjiId).ToList(), ct)).ToDictionary(k => k.ID);
            var results = new List<KanjiResult>(answers.Count);
            foreach (var answer in answers)
            {
                if (!kanjis.TryGetValue(answer.KanjiId, out var kanji))
                {
                    results.Add(new KanjiResult(answer.KanjiId, answer.Kanji, answer.Answers, answer.Answers.Select(_ => true).ToArray(), true, []));
                    continue;
                }

                var result = Check(kanji, answer);
                await monitors.UpdateAsync(MonitorTables.Kanji, userId, kanji.ID, m => FragmentVariation.ApplyKanji(m, !result.Wrong, Now), ct);
                results.Add(result);
            }
            return results;
        }

        public Task<TKanji?> GetByLetterAsync(string letter, CancellationToken ct = default) => repository.GetByLetterAsync(letter.Trim(), ct);

        public Task<byte[]?> GetImageAsync(int id, CancellationToken ct = default) => repository.GetImageAsync(id, ct);

        /// <summary>Um único caractere visível (o original testava Length == 1, o que recusava kanjis fora do BMP).</summary>
        public static bool IsSingleCharacter(string? text) =>
            !string.IsNullOrEmpty(text) && new StringInfo(text).LengthInTextElements == 1;

        /// <summary>KanjisController.AddKanji / UpdateKanji.</summary>
        public async Task<Response> SaveAsync(TKanji kanji, CancellationToken ct = default)
        {
            if (!IsSingleCharacter(kanji.KanjiLetter))
                return Response.Fail("Informe um único kanji.");
            if (string.IsNullOrEmpty(kanji.Kana))
                return Response.Fail("Informe a(s) leitura(s) em kana.");
            if (string.IsNullOrEmpty(kanji.Romaji))
                return Response.Fail("Informe o romaji.");
            if (string.IsNullOrEmpty(kanji.Info))
                return Response.Fail("Informe as informações do kanji.");

            var existing = await repository.GetByLetterAsync(kanji.KanjiLetter!, ct);
            if (kanji.ID == 0 && existing is not null)
                return Response.Fail("Este kanji já está cadastrado.");

            return await Response.SaveAsync("Kanji", kanji.ID,
                () => repository.AddAsync(kanji, ct),
                () => Task.FromResult(existing?.ID == kanji.ID),
                () => repository.UpdateAsync(kanji, ct));
        }
    }
}
