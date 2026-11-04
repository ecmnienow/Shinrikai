using System.Data.Common;
using System.Text;
using Nienow.DataBaseConn;
using Nienow.Helper;
using Monitor = Nienow.Helper.Monitor;
using Nienow.Shinrikai.Vocabulary.Model;

namespace Nienow.Shinrikai.Vocabulary;

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
    Task<Monitor?> GetMonitorAsync(int userId, int wordId, CancellationToken ct = default);
    Task SaveMonitorAsync(Monitor monitor, CancellationToken ct = default);
}

public enum VocabularyMedia { Image, Audio }

/// <summary>Port de DBVocabulary.cs — todas as consultas parametrizadas.</summary>
public sealed class VocabularyRepository(IShinrikaiDb db) : IVocabularyRepository
{
    private const string SelectWord =
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

    private const string SubjectFilter = "(VCB_SBJ_ID_1 = @SBJ OR VCB_SBJ_ID_2 = @SBJ OR VCB_SBJ_ID_3 = @SBJ)";

    public async Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken ct = default)
    {
        await using var conn = await db.OpenAsync(ct);
        return await conn.QueryAsync(
            "SELECT SBJ_ID, SBJ_NAME FROM SUBJECTS ORDER BY SBJ_NAME",
            r => new Subject(r.GetInt("SBJ_ID"), r.GetNullableString("SBJ_NAME") ?? ""), ct);
    }

    /// <summary>
    /// Modo normal. O original carregava o vocabulário inteiro e filtrava pelo nome do assunto
    /// em memória; aqui o filtro vai para o SQL (mesmo resultado).
    /// </summary>
    public async Task<IReadOnlyList<TWord>> GetVocabularyBySubjectAsync(int subjectId, CancellationToken ct = default)
    {
        await using var conn = await db.OpenAsync(ct);
        return await conn.QueryAsync($"{SelectWord} WHERE {SubjectFilter}", Map, ct, ("@SBJ", subjectId));
    }

    /// <summary>Modo "Refazer erradas": palavras com VMN_COUNTER &gt; 0 para o aluno.</summary>
    public async Task<IReadOnlyList<TWord>> GetMistakesAsync(int userId, int subjectId, CancellationToken ct = default)
    {
        await using var conn = await db.OpenAsync(ct);
        return await conn.QueryAsync(
            $"""
            {SelectWord}
              LEFT OUTER JOIN VOC_MONITOR ON VMN_VCB_ID = VCB_ID
            WHERE VMN_COUNTER > 0 AND VMN_USR_ID = @USR AND {SubjectFilter}
            """,
            Map, ct, ("@USR", userId), ("@SBJ", subjectId));
    }

    public async Task<IReadOnlyList<TWord>> GetWordsAsync(IReadOnlyList<int> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0)
            return [];

        var (inSql, parameters) = DbExtensions.InClause(ids.Distinct().ToList());
        await using var conn = await db.OpenAsync(ct);
        return await conn.QueryAsync($"{SelectWord} WHERE VCB_ID IN ({inSql})", Map, ct, parameters);
    }

    public async Task<TWord?> GetWordAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await db.OpenAsync(ct);
        return await conn.QuerySingleOrDefaultAsync($"{SelectWord} WHERE VCB_ID = @ID", Map, ct, ("@ID", id));
    }

    public async Task<TWord?> GetWordByKanaAsync(string kana, CancellationToken ct = default)
    {
        await using var conn = await db.OpenAsync(ct);
        return await conn.QuerySingleOrDefaultAsync($"{SelectWord} WHERE VCB_KANA = @KAN", Map, ct, ("@KAN", kana));
    }

    public async Task<byte[]?> GetMediaAsync(int wordId, VocabularyMedia media, int slot, CancellationToken ct = default)
    {
        if (slot is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(slot));

        // Nome de coluna vem de um conjunto fechado (enum + 1..3), nunca de entrada livre.
        var column = (media == VocabularyMedia.Image ? "VCB_IMAGE_" : "VCB_AUDIO_") + slot;
        await using var conn = await db.OpenAsync(ct);
        return await conn.ScalarAsync($"SELECT {column} FROM VOCABULARY WHERE VCB_ID = @ID", ct, ("@ID", wordId)) as byte[];
    }

    public async Task AddWordAsync(TWord word, CancellationToken ct = default)
    {
        await using var conn = await db.OpenAsync(ct);
        await conn.ExecuteInTransactionAsync(
            """
            INSERT INTO VOCABULARY (
              VCB_ID, VCB_KANA, VCB_ROMAJI, VCB_KANJI_1, VCB_KANJI_2, VCB_KANJI_3,
              VCB_SBJ_ID_1, VCB_SBJ_ID_2, VCB_SBJ_ID_3, VCB_TRANSLATION_1,
              VCB_TRANSLATION_2, VCB_TRANSLATION_3, VCB_IMAGE_1, VCB_IMAGE_2, VCB_IMAGE_3,
              VCB_AUDIO_1, VCB_AUDIO_2, VCB_AUDIO_3
            ) VALUES (
              GEN_ID(VOCABULARY_ID_GEN, 1), @KAN, @RMJ, @KNJ1, @KNJ2, @KNJ3,
              @SBJ1, @SBJ2, @SBJ3, @WRD1, @WRD2,
              @WRD3, @IMG1, @IMG2, @IMG3,
              @AUD1, @AUD2, @AUD3
            )
            """,
            ct,
            [.. WordParameters(word),
             ("@IMG1", word.Image1), ("@IMG2", word.Image2), ("@IMG3", word.Image3),
             ("@AUD1", word.Audio1), ("@AUD2", word.Audio2), ("@AUD3", word.Audio3)]);
    }

    public async Task UpdateWordAsync(TWord word, CancellationToken ct = default)
    {
        var sql = new StringBuilder(
            """
            UPDATE VOCABULARY SET
              VCB_KANA = @KAN, VCB_ROMAJI = @RMJ, VCB_KANJI_1 = @KNJ1, VCB_KANJI_2 = @KNJ2, VCB_KANJI_3 = @KNJ3,
              VCB_SBJ_ID_1 = @SBJ1, VCB_SBJ_ID_2 = @SBJ2, VCB_SBJ_ID_3 = @SBJ3, VCB_TRANSLATION_1 = @WRD1,
              VCB_TRANSLATION_2 = @WRD2, VCB_TRANSLATION_3 = @WRD3
            """);
        var parameters = new List<(string, object?)>(WordParameters(word)) { ("@ID", word.ID) };

        // Como no original: blobs só são sobrescritos quando um arquivo novo foi enviado.
        void AddBlob(string column, string name, byte[]? value)
        {
            if (value is not { Length: > 0 }) return;
            sql.Append($", {column} = {name}");
            parameters.Add((name, value));
        }
        AddBlob("VCB_IMAGE_1", "@IMG1", word.Image1);
        AddBlob("VCB_IMAGE_2", "@IMG2", word.Image2);
        AddBlob("VCB_IMAGE_3", "@IMG3", word.Image3);
        AddBlob("VCB_AUDIO_1", "@AUD1", word.Audio1);
        AddBlob("VCB_AUDIO_2", "@AUD2", word.Audio2);
        AddBlob("VCB_AUDIO_3", "@AUD3", word.Audio3);

        sql.Append(" WHERE VCB_ID = @ID");

        await using var conn = await db.OpenAsync(ct);
        await conn.ExecuteInTransactionAsync(sql.ToString(), ct, [.. parameters]);
    }

    public async Task<Monitor?> GetMonitorAsync(int userId, int wordId, CancellationToken ct = default)
    {
        await using var conn = await db.OpenAsync(ct);
        return await conn.QuerySingleOrDefaultAsync(
            "SELECT * FROM VOC_MONITOR WHERE VMN_VCB_ID = @VCB AND VMN_USR_ID = @USR",
            r => new Monitor
            {
                ID = r.GetInt("VMN_ID"),
                UsrID = r.GetInt("VMN_USR_ID"),
                VcbID = r.GetInt("VMN_VCB_ID"),
                Counter = r.GetInt("VMN_COUNTER"),
                FailCounter = r.GetInt("VMN_FAILS"),
                SuccessCounter = r.GetInt("VMN_SUCCESSES"),
                LastHittedTime = r.GetDateTimeOrDefault("VMN_LASTHIT"),
            },
            ct, ("@VCB", wordId), ("@USR", userId));
    }

    public async Task SaveMonitorAsync(Monitor monitor, CancellationToken ct = default)
    {
        await using var conn = await db.OpenAsync(ct);
        if (monitor.IsNew)
        {
            await conn.ExecuteInTransactionAsync(
                """
                INSERT INTO VOC_MONITOR (
                  VMN_ID, VMN_USR_ID, VMN_VCB_ID, VMN_COUNTER, VMN_FAILS, VMN_SUCCESSES, VMN_LASTHIT
                ) VALUES (
                  GEN_ID(VOCMONITOR_ID_GEN, 1), @USR, @VCB, @CNT, @FAI, @SUC, @HIT
                )
                """,
                ct,
                ("@USR", monitor.UsrID), ("@VCB", monitor.VcbID), ("@CNT", monitor.Counter),
                ("@FAI", monitor.FailCounter), ("@SUC", monitor.SuccessCounter), ("@HIT", monitor.LastHittedTime));
        }
        else
        {
            await conn.ExecuteInTransactionAsync(
                """
                UPDATE VOC_MONITOR SET
                  VMN_COUNTER = @CNT, VMN_FAILS = @FAI, VMN_SUCCESSES = @SUC, VMN_LASTHIT = @HIT
                WHERE VMN_ID = @ID
                """,
                ct,
                ("@ID", monitor.ID), ("@CNT", monitor.Counter), ("@FAI", monitor.FailCounter),
                ("@SUC", monitor.SuccessCounter), ("@HIT", monitor.LastHittedTime));
        }
    }

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

    private static TWord Map(DbDataReader r) => new()
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
        HasImage1 = r.GetNullableString("IMG1") == "T",
        HasImage2 = r.GetNullableString("IMG2") == "T",
        HasImage3 = r.GetNullableString("IMG3") == "T",
        HasAudio1 = r.GetNullableString("AUD1") == "T",
        HasAudio2 = r.GetNullableString("AUD2") == "T",
        HasAudio3 = r.GetNullableString("AUD3") == "T",
    };
}
