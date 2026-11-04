using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nienow.DataBaseConn;
using Nienow.Shinrikai.Vocabulary.Model;

namespace Nienow.Shinrikai.Vocabulary
{
    public interface IVocabularyRepository
    {
        Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken ct = default);
        Task<IReadOnlyList<TWord>> GetVocabularyBySubjectAsync(int subjectId, CancellationToken ct = default);
        Task<IReadOnlyList<TWord>> GetMistakesAsync(int userId, int subjectId, CancellationToken ct = default);
        Task<IReadOnlyList<TWord>> GetWordsAsync(IReadOnlyList<int> ids, CancellationToken ct = default);
        Task<TWord?> GetWordAsync(int id, CancellationToken ct = default);
        Task<TWord?> GetWordByKanaAsync(string kana, CancellationToken ct = default);
        Task<byte[]?> GetMediaAsync(int wordId, VocabularyMedia media, int slot, CancellationToken ct = default);
        Task AddWordAsync(TWord word, CancellationToken ct = default);
        Task UpdateWordAsync(TWord word, CancellationToken ct = default);
        Task<IReadOnlyList<TWord>> GetAllAsync(CancellationToken ct = default);
    }

    public enum VocabularyMedia { Image, Audio }

    /// <summary>Port de DBVocabulary.cs — todas as consultas parametrizadas.</summary>
    public sealed class VocabularyRepository(IShinrikaiDb db) : IVocabularyRepository
    {
        internal const string SelectWord =
            """
            SELECT VCB_ID, VCB_SBJ_ID_1, VCB_SBJ_ID_2, VCB_SBJ_ID_3,
              (SELECT SBJ_NAME FROM SUBJECTS WHERE SBJ_ID = VCB_SBJ_ID_1) SUBJECT_1,
              (SELECT SBJ_NAME FROM SUBJECTS WHERE SBJ_ID = VCB_SBJ_ID_2) SUBJECT_2,
              (SELECT SBJ_NAME FROM SUBJECTS WHERE SBJ_ID = VCB_SBJ_ID_3) SUBJECT_3,
              VCB_KANA, VCB_ROMAJI, VCB_TRANSLATION_1, VCB_TRANSLATION_2, VCB_TRANSLATION_3,
              VCB_KANJI_1, VCB_KANJI_2, VCB_KANJI_3,
              (CASE WHEN VCB_IMAGE_1 IS NOT NULL THEN 'T' ELSE 'F' END) IMG1,
              (CASE WHEN VCB_IMAGE_2 IS NOT NULL THEN 'T' ELSE 'F' END) IMG2,
              (CASE WHEN VCB_IMAGE_3 IS NOT NULL THEN 'T' ELSE 'F' END) IMG3,
              (CASE WHEN VCB_AUDIO_1 IS NOT NULL THEN 'T' ELSE 'F' END) AUD1,
              (CASE WHEN VCB_AUDIO_2 IS NOT NULL THEN 'T' ELSE 'F' END) AUD2,
              (CASE WHEN VCB_AUDIO_3 IS NOT NULL THEN 'T' ELSE 'F' END) AUD3
            FROM VOCABULARY
            """;

        internal const string SubjectFilter = "(VCB_SBJ_ID_1 = @SBJ OR VCB_SBJ_ID_2 = @SBJ OR VCB_SBJ_ID_3 = @SBJ)";

        public Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken ct = default) =>
            db.QueryAsync("SELECT SBJ_ID, SBJ_NAME FROM SUBJECTS ORDER BY SBJ_NAME", MapSubject, ct);

        /// <summary>
        /// Modo normal. O original carregava o vocabulário inteiro e filtrava pelo nome do assunto
        /// em memória; aqui o filtro vai para o SQL (mesmo resultado).
        /// </summary>
        public Task<IReadOnlyList<TWord>> GetVocabularyBySubjectAsync(int subjectId, CancellationToken ct = default) =>
            db.QueryAsync($"{SelectWord} WHERE {SubjectFilter}", Map, ct, ("@SBJ", subjectId));

        /// <summary>Modo "Refazer erradas": palavras com VMN_COUNTER &gt; 0 para o aluno.</summary>
        public Task<IReadOnlyList<TWord>> GetMistakesAsync(int userId, int subjectId, CancellationToken ct = default) =>
            db.QueryAsync(
                $"""
                {SelectWord}
                  LEFT OUTER JOIN VOC_MONITOR ON VMN_VCB_ID = VCB_ID
                WHERE VMN_COUNTER > 0 AND VMN_USR_ID = @USR AND {SubjectFilter}
                """,
                Map, ct, ("@USR", userId), ("@SBJ", subjectId));

        public Task<IReadOnlyList<TWord>> GetWordsAsync(IReadOnlyList<int> ids, CancellationToken ct = default) =>
            GetWordsAsync(db, ids, ct);

        public Task<TWord?> GetWordAsync(int id, CancellationToken ct = default) =>
            db.QuerySingleOrDefaultAsync($"{SelectWord} WHERE VCB_ID = @ID", Map, ct, ("@ID", id));

        public Task<TWord?> GetWordByKanaAsync(string kana, CancellationToken ct = default) =>
            db.QuerySingleOrDefaultAsync($"{SelectWord} WHERE VCB_KANA = @KAN", Map, ct, ("@KAN", kana));

        public async Task<byte[]?> GetMediaAsync(int wordId, VocabularyMedia media, int slot, CancellationToken ct = default)
        {
            if (slot is < 1 or > 3)
                throw new ArgumentOutOfRangeException(nameof(slot));

            // Nome de coluna vem de um conjunto fechado (enum + 1..3), nunca de entrada livre.
            var column = (media == VocabularyMedia.Image ? "VCB_IMAGE_" : "VCB_AUDIO_") + slot;
            return await db.ScalarAsync($"SELECT {column} FROM VOCABULARY WHERE VCB_ID = @ID", ct, ("@ID", wordId)) as byte[];
        }

        public Task AddWordAsync(TWord word, CancellationToken ct = default)
        {
            var blobs = Blobs(word);
            return db.ExecuteAsync(
                $"""
                INSERT INTO VOCABULARY (
                  VCB_ID, VCB_KANA, VCB_ROMAJI, VCB_KANJI_1, VCB_KANJI_2, VCB_KANJI_3,
                  VCB_SBJ_ID_1, VCB_SBJ_ID_2, VCB_SBJ_ID_3, VCB_TRANSLATION_1,
                  VCB_TRANSLATION_2, VCB_TRANSLATION_3, {string.Join(", ", blobs.Select(b => b.Column))}
                ) VALUES (
                  GEN_ID(VOCABULARY_ID_GEN, 1), @KAN, @RMJ, @KNJ1, @KNJ2, @KNJ3,
                  @SBJ1, @SBJ2, @SBJ3, @WRD1, @WRD2, @WRD3, {string.Join(", ", blobs.Select(b => b.Name))}
                )
                """,
                ct, [.. WordParameters(word), .. blobs.Select(b => (b.Name, (object?)b.Value))]);
        }

        public Task UpdateWordAsync(TWord word, CancellationToken ct = default)
        {
            // Como no original: blobs só são sobrescritos quando um arquivo novo foi enviado.
            var blobs = Blobs(word).Where(b => b.Value is { Length: > 0 }).ToList();
            return db.ExecuteAsync(
                $"""
                UPDATE VOCABULARY SET
                  VCB_KANA = @KAN, VCB_ROMAJI = @RMJ, VCB_KANJI_1 = @KNJ1, VCB_KANJI_2 = @KNJ2, VCB_KANJI_3 = @KNJ3,
                  VCB_SBJ_ID_1 = @SBJ1, VCB_SBJ_ID_2 = @SBJ2, VCB_SBJ_ID_3 = @SBJ3, VCB_TRANSLATION_1 = @WRD1,
                  VCB_TRANSLATION_2 = @WRD2, VCB_TRANSLATION_3 = @WRD3{string.Concat(blobs.Select(b => $", {b.Column} = {b.Name}"))}
                WHERE VCB_ID = @ID
                """,
                ct, [.. WordParameters(word), .. blobs.Select(b => (b.Name, (object?)b.Value)), ("@ID", word.ID)]);
        }

        /// <summary>Vocabulário inteiro (VocabularyManager.VocabularyList — usado pelo dicionário).</summary>
        public Task<IReadOnlyList<TWord>> GetAllAsync(CancellationToken ct = default) => db.QueryAsync(SelectWord, Map, ct);

        internal static Task<IReadOnlyList<TWord>> GetWordsAsync(IShinrikaiDb db, IReadOnlyList<int> ids, CancellationToken ct)
        {
            if (ids.Count == 0)
                return Task.FromResult<IReadOnlyList<TWord>>([]);
            var (inSql, parameters) = DbExtensions.InClause(ids);
            return db.QueryAsync($"{SelectWord} WHERE VCB_ID IN ({inSql})", Map, ct, parameters);
        }

        internal static Subject MapSubject(DbDataReader r) => new(r.GetInt("SBJ_ID"), r.GetNullableString("SBJ_NAME") ?? "");

        private static (string Column, string Name, byte[]? Value)[] Blobs(TWord w) =>
        [
            ("VCB_IMAGE_1", "@IMG1", w.Image1), ("VCB_IMAGE_2", "@IMG2", w.Image2), ("VCB_IMAGE_3", "@IMG3", w.Image3),
            ("VCB_AUDIO_1", "@AUD1", w.Audio1), ("VCB_AUDIO_2", "@AUD2", w.Audio2), ("VCB_AUDIO_3", "@AUD3", w.Audio3),
        ];

        private static (string, object?)[] WordParameters(TWord word) =>
        [
            ("@KAN", word.Kana),
            ("@RMJ", word.Romaji),
            ("@KNJ1", word.KanjiWord1),
            ("@KNJ2", word.KanjiWord2),
            ("@KNJ3", word.KanjiWord3),
            ("@SBJ1", word.SubjectID1),
            ("@SBJ2", word.SubjectID2 == 0 ? null : word.SubjectID2),
            ("@SBJ3", word.SubjectID3 == 0 ? null : word.SubjectID3),
            ("@WRD1", word.Word1),
            ("@WRD2", word.Word2),
            ("@WRD3", word.Word3),
        ];

        internal static TWord Map(DbDataReader r) => new()
        {
            ID = r.GetInt("VCB_ID"),
            SubjectID1 = r.GetInt("VCB_SBJ_ID_1"),
            SubjectID2 = r.GetInt("VCB_SBJ_ID_2"),
            SubjectID3 = r.GetInt("VCB_SBJ_ID_3"),
            Subject1 = r.GetNullableString("SUBJECT_1"),
            Subject2 = r.GetNullableString("SUBJECT_2"),
            Subject3 = r.GetNullableString("SUBJECT_3"),
            Kana = r.GetNullableString("VCB_KANA"),
            Romaji = r.GetNullableString("VCB_ROMAJI"),
            Word1 = r.GetNullableString("VCB_TRANSLATION_1"),
            Word2 = r.GetNullableString("VCB_TRANSLATION_2"),
            Word3 = r.GetNullableString("VCB_TRANSLATION_3"),
            KanjiWord1 = r.GetNullableString("VCB_KANJI_1"),
            KanjiWord2 = r.GetNullableString("VCB_KANJI_2"),
            KanjiWord3 = r.GetNullableString("VCB_KANJI_3"),
            HasImage1 = r.GetFlag("IMG1"),
            HasImage2 = r.GetFlag("IMG2"),
            HasImage3 = r.GetFlag("IMG3"),
            HasAudio1 = r.GetFlag("AUD1"),
            HasAudio2 = r.GetFlag("AUD2"),
            HasAudio3 = r.GetFlag("AUD3"),
        };
    }
}
