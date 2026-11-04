using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Nienow.DataBaseConn;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Core.Adaptive;

namespace Nienow.Shinrikai.Exercises
{
    /// <summary>Port de TExercise.cs.</summary>
    public class TExercise
    {
        public int ID { get; set; }
        public int GrammarId { get; set; }
        public int TextId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Option1 { get; set; } = string.Empty;
        public string Option2 { get; set; } = string.Empty;
        public string Option3 { get; set; } = string.Empty;
        public string Option4 { get; set; } = string.Empty;
        /// <summary>1 a 4.</summary>
        public int RightAnswer { get; set; }
        public EJLPT Level { get; set; } = EJLPT.None;

        [JsonIgnore] public string[] Options => [Option1, Option2, Option3, Option4];
    }

    /// <summary>
    /// O que o aluno recebe (GetExercise). O original devolvia o TExercise inteiro, inclusive a
    /// resposta certa — visível nas ferramentas do navegador. Aqui ela fica só no servidor.
    /// </summary>
    public record ExerciseQuestion(int Id, string Question, IReadOnlyList<ExerciseOption> Options);

    public record ExerciseOption(int Value, string Text);

    public interface IExerciseRepository
    {
        Task<IReadOnlyList<(int Id, EJLPT Level)>> ListAllAsync(CancellationToken ct = default);
        Task<IReadOnlyList<(int Id, EJLPT Level)>> ListAsync(int grammarId, int textId, CancellationToken ct = default);
        Task<TExercise?> GetAsync(int id, CancellationToken ct = default);
        Task AddAsync(TExercise exercise, CancellationToken ct = default);
        Task UpdateAsync(TExercise exercise, CancellationToken ct = default);
    }

    /// <summary>Port de DBExercise.cs (SQL parametrizado).</summary>
    public sealed class ExerciseRepository(IShinrikaiDb db) : IExerciseRepository
    {
        /// <summary>ListAllExercises: exercícios gerais e de gramática (os de texto ficam de fora).</summary>
        public Task<IReadOnlyList<(int Id, EJLPT Level)>> ListAllAsync(CancellationToken ct = default) =>
            db.QueryAsync("SELECT EXR_ID, EXR_LEVEL FROM EXERCISES WHERE EXR_TXT_ID IS NULL", MapIdLevel, ct);

        public Task<IReadOnlyList<(int Id, EJLPT Level)>> ListAsync(int grammarId, int textId, CancellationToken ct = default)
        {
            var (column, id) = grammarId > 0 ? ("EXR_GRM_ID", grammarId) : ("EXR_TXT_ID", textId);
            return db.QueryAsync($"SELECT EXR_ID, EXR_LEVEL FROM EXERCISES WHERE {column} = @ID", MapIdLevel, ct, ("@ID", id));
        }

        public Task<TExercise?> GetAsync(int id, CancellationToken ct = default) =>
            db.QuerySingleOrDefaultAsync("SELECT * FROM EXERCISES WHERE EXR_ID = @ID", Map, ct, ("@ID", id));

        public Task AddAsync(TExercise e, CancellationToken ct = default) =>
            db.ExecuteAsync(
                """
                INSERT INTO EXERCISES (
                  EXR_ID, EXR_GRM_ID, EXR_TXT_ID, EXR_TEXT, EXR_OPTION_1, EXR_OPTION_2,
                  EXR_OPTION_3, EXR_OPTION_4, EXR_RIGHT, EXR_LEVEL
                ) VALUES (
                  GEN_ID(EXERCISES_ID_GEN, 1), @GRM, @TXT, @QST, @OP1, @OP2,
                  @OP3, @OP4, @RGT, @LVL
                )
                """,
                ct, Parameters(e));

        public Task UpdateAsync(TExercise e, CancellationToken ct = default) =>
            db.ExecuteAsync(
                """
                UPDATE EXERCISES SET
                  EXR_GRM_ID = @GRM, EXR_TXT_ID = @TXT, EXR_TEXT = @QST, EXR_OPTION_1 = @OP1, EXR_OPTION_2 = @OP2,
                  EXR_OPTION_3 = @OP3, EXR_OPTION_4 = @OP4, EXR_RIGHT = @RGT, EXR_LEVEL = @LVL
                WHERE EXR_ID = @ID
                """,
                ct, [.. Parameters(e), ("@ID", e.ID)]);

        private static (string, object?)[] Parameters(TExercise e) =>
        [
            ("@GRM", e.GrammarId > 0 ? e.GrammarId : null),
            ("@TXT", e.TextId > 0 ? e.TextId : null),
            ("@QST", e.Question),
            ("@OP1", e.Option1),
            ("@OP2", e.Option2),
            ("@OP3", e.Option3),
            ("@OP4", e.Option4),
            ("@RGT", e.RightAnswer),
            ("@LVL", e.Level.ToString()),
        ];

        private static (int, EJLPT) MapIdLevel(DbDataReader r) => (r.GetInt("EXR_ID"), JlptLevel.Parse(r.GetNullableString("EXR_LEVEL")));

        private static TExercise Map(DbDataReader r) => new()
        {
            ID = r.GetInt("EXR_ID"),
            GrammarId = r.GetInt("EXR_GRM_ID"),
            TextId = r.GetInt("EXR_TXT_ID"),
            Question = r.GetNullableString("EXR_TEXT") ?? string.Empty,
            Option1 = r.GetNullableString("EXR_OPTION_1") ?? string.Empty,
            Option2 = r.GetNullableString("EXR_OPTION_2") ?? string.Empty,
            Option3 = r.GetNullableString("EXR_OPTION_3") ?? string.Empty,
            Option4 = r.GetNullableString("EXR_OPTION_4") ?? string.Empty,
            RightAnswer = r.GetInt("EXR_RIGHT"),
            Level = JlptLevel.Parse(r.GetNullableString("EXR_LEVEL")),
        };
    }

    /// <summary>Port de ExercisesManager.cs + ExercisesController.</summary>
    public sealed class ExercisesService(IExerciseRepository repository, IMonitorStore monitors, TimeProvider? clock = null)
    {
        /// <summary>
        /// Index(grammarId, textId): de um conteúdo (gramática XOR texto) → todos os exercícios dele
        /// liberados para o nível, embaralhados; sem filtro → 10 exercícios gerais (técnica 4.4).
        /// </summary>
        public async Task<IReadOnlyList<int>> ListAsync(EJLPT userLevel, int grammarId, int textId, Random? rng = null, CancellationToken ct = default)
        {
            if (grammarId > 0 && textId > 0)
                throw new ArgumentException("Informe gramática ou texto, não os dois.");

            return grammarId > 0 || textId > 0
                ? AdaptiveOrdering.Select(await repository.ListAsync(grammarId, textId, ct), userLevel, rng: rng)
                : AdaptiveOrdering.Select(await repository.ListAllAsync(ct), userLevel, AdaptiveOrdering.AllExercisesTake, rng);
        }

        public async Task<ExerciseQuestion?> GetQuestionAsync(int id, CancellationToken ct = default) =>
            await repository.GetAsync(id, ct) is { } e
                ? new ExerciseQuestion(e.ID, e.Question, e.Options.Select((text, i) => new ExerciseOption(i + 1, text)).ToList())
                : null;

        public Task<TExercise?> GetAsync(int id, CancellationToken ct = default) => repository.GetAsync(id, ct);

        /// <summary>
        /// ExercisesManager.CheckAnswer. O original dizia o número da opção certa, mas as opções são
        /// embaralhadas na tela; aqui a mensagem traz o texto da opção.
        /// </summary>
        public async Task<Response?> CheckAnswerAsync(int userId, int exerciseId, int answer, CancellationToken ct = default)
        {
            var exercise = await repository.GetAsync(exerciseId, ct);
            if (exercise is null)
                return null;

            var correct = exercise.RightAnswer == answer;
            var now = (clock ?? TimeProvider.System).GetLocalNow().DateTime;
            await monitors.UpdateAsync(MonitorTables.Exercise, userId, exercise.ID, m => FragmentVariation.ApplyExercise(m, correct, now), ct);

            var rightText = exercise.RightAnswer is >= 1 and <= 4 ? exercise.Options[exercise.RightAnswer - 1] : exercise.RightAnswer.ToString();
            return correct ? Response.Ok() : Response.Fail($"A resposta correta era: \"{rightText}\"");
        }

        public static Response Validate(TExercise e)
        {
            if (string.IsNullOrWhiteSpace(e.Question))
                return Response.Fail("É obrigatório que informe uma questão para o exercício a ser cadastrado.");
            if (e.Options.Any(string.IsNullOrWhiteSpace))
                return Response.Fail("Todas as opções de respostas devem ser preenchidas.");
            if (e.RightAnswer is < 1 or > 4)
                return Response.Fail("Nenhuma resposta foi marcada como correta.");
            if (e.GrammarId > 0 && e.TextId > 0)
                return Response.Fail("O exercício pertence a uma gramática ou a um texto, não aos dois.");
            return Response.Ok();
        }

        /// <summary>ExercisesController.AddExercise (cadastro e alteração).</summary>
        public async Task<Response> SaveAsync(TExercise exercise, CancellationToken ct = default)
        {
            var validation = Validate(exercise);
            if (!validation.OK)
                return validation;

            return await Response.SaveAsync("Exercício", exercise.ID,
                () => repository.AddAsync(exercise, ct),
                async () => await repository.GetAsync(exercise.ID, ct) is not null,
                () => repository.UpdateAsync(exercise, ct));
        }
    }
}
