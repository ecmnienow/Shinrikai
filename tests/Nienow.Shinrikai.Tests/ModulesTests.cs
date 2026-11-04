using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nienow.DataBaseConn;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Dictionary;
using Nienow.Shinrikai.Exercises;
using Nienow.Shinrikai.Grammar;
using Nienow.Shinrikai.Kanji;
using Nienow.Shinrikai.Tests.Legacy;
using Nienow.Shinrikai.Text;
using Nienow.Shinrikai.Vocabulary;
using Nienow.Shinrikai.Vocabulary.Model;
using Xunit;
using Monitor = Nienow.Helper.Monitor;

namespace Nienow.Shinrikai.Tests
{
    public class KanjiTests
    {
        public static IEnumerable<object[]> ReadingCases()
        {
            string[] kanas = ["にち,じつ,ひ,か", "やま", "みず,すい"];
            string?[] values = ["", "にち", "じつ", "ひ", "やま", "すい", "x"];
            foreach (var kana in kanas)
                foreach (var a in values)
                    foreach (var b in values)
                        foreach (var c in values)
                            yield return [kana, a ?? "", b ?? "", c ?? ""];
        }

        [Theory]
        [MemberData(nameof(ReadingCases))]
        public void CheckReading_matches_legacy(string kana, string a, string b, string c)
        {
            var kanji = new TKanji { Kana = kana };
            Assert.Equal(LegacyOracleModules.CheckReading(kana, a, b, c), kanji.CheckReading(a, b, c));
        }

        [Theory]
        [MemberData(nameof(ReadingCases))]
        public void Check_answers_matches_legacy(string kana, string a, string b, string c)
        {
            var inputs = LegacyOracleModules.KanjiInputs(kana);
            var answers = new List<string> { a, b, c }.Take(inputs).ToList();
            var legacy = LegacyOracleModules.KanjiCheckAnswers(kana, answers);

            var result = KanjiService.Check(new TKanji { ID = 1, Kana = kana }, new KanjiAnswer(1, "日", answers));

            Assert.Equal(legacy.Wrong, result.Wrong);
            Assert.Equal(legacy.WrongInputs, result.WrongInputs);
        }

        [Theory]
        [InlineData("やま", 1)]
        [InlineData("みず,すい", 2)]
        [InlineData("にち,じつ,ひ,か", 3)]
        public void Inputs_per_kanji_match_legacy(string kana, int expected)
        {
            Assert.Equal(LegacyOracleModules.KanjiInputs(kana), expected);
            var session = KanjiService.BuildSession("N5", [new TKanji { ID = 1, KanjiLetter = "日", Kana = kana }], false, new Random(1));
            Assert.Equal(expected, session.Pages[0][0].Inputs);
        }

        [Fact]
        public void Levels_are_cumulative_and_N1_means_all()
        {
            Assert.Equal(["N5"], KanjiLevels.Cumulative("N5"));
            Assert.Equal(["N5", "N4", "N3"], KanjiLevels.Cumulative("n3"));
            Assert.Null(KanjiLevels.Cumulative("N1"));
            Assert.Throws<ArgumentException>(() => KanjiLevels.Cumulative("N9"));
        }

        [Theory]
        [InlineData("n5", "N5")]
        [InlineData("none", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void Level_is_normalized(string? input, string? expected) => Assert.Equal(expected, KanjiLevels.Normalize(input));

        [Theory]
        [InlineData("日", true)]
        [InlineData("𠮷", true)] // fora do BMP: o original (Length == 1) recusava
        [InlineData("日本", false)]
        [InlineData("", false)]
        public void Single_character(string text, bool expected) => Assert.Equal(expected, KanjiService.IsSingleCharacter(text));
    }

    public class GrammarTests
    {
        private static TGrammar G(int id, EJLPT level, string? req = null) =>
            new() { Index = id, Title = "G" + id, Level = level, RawRequisites = req };

        [Fact]
        public void Map_unlocks_by_level_and_requisites()
        {
            var grammars = new[]
            {
                G(1, EJLPT.N5),
                G(2, EJLPT.N4, "001"),
                G(3, EJLPT.N4, "001;002"),
                G(4, EJLPT.N3),
            };

            // Aluno N5 que já leu o conteúdo 1.
            var map = GrammarService.BuildMap(grammars, new HashSet<int> { 1 }, new HashSet<int> { 2 }, EJLPT.N5);

            Assert.Equal([EJLPT.N5, EJLPT.N4, EJLPT.N3], map.Select(g => g.Level));
            var n4 = map[1].Items;
            Assert.Equal([2, 3], n4.Select(i => i.Id)); // liberados primeiro
            Assert.True(n4[0].HasEnoughLevel);   // N4 = um acima + requisito cumprido
            Assert.False(n4[1].HasEnoughLevel);  // falta o requisito 2
            Assert.True(n4[0].HasExercises);
            Assert.False(map[2].Items[0].HasEnoughLevel); // N3 = dois acima
        }

        [Fact]
        public void Locked_items_go_after_unlocked_in_the_same_level()
        {
            var map = GrammarService.BuildMap([G(1, EJLPT.N4, "009"), G(2, EJLPT.N4)], new HashSet<int>(), new HashSet<int>(), EJLPT.N5);
            Assert.Equal([2, 1], map[0].Items.Select(i => i.Id));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("none", null)]
        [InlineData("n4", "N4")]
        public void Level_is_normalized(string? input, string? expected) => Assert.Equal(expected, GrammarService.NormalizeLevel(input));

        [Fact]
        public void Requisites_are_padded_like_the_original()
        {
            Assert.Equal("001;010", GrammarRepository.FormatRequisites([1, 10]));
            Assert.Null(GrammarRepository.FormatRequisites([]));
        }
    }

    public class TextTests
    {
        [Fact]
        public void Texts_without_level_go_to_N5_and_respect_user_level()
        {
            var texts = new[]
            {
                new TText { Index = 1, Title = "a", Level = EJLPT.None },
                new TText { Index = 2, Title = "b", Level = EJLPT.N3 },
                new TText { Index = 3, Title = "c", Level = EJLPT.N4 },
            };
            var list = TextService.BuildList(texts, new HashSet<int> { 3 }, EJLPT.N5);

            Assert.Equal([EJLPT.N5, EJLPT.N4, EJLPT.N3], list.Select(g => g.Level));
            Assert.True(list[0].Items[0].HasEnoughLevel);
            Assert.True(list[1].Items[0].HasEnoughLevel);  // N4 = um acima
            Assert.True(list[1].Items[0].HasExercises);
            Assert.False(list[2].Items[0].HasEnoughLevel); // N3 = dois acima
        }

        [Fact]
        public void Invalid_ruby_elements_are_removed()
        {
            Assert.Equal("ab", TextService.RemoveInvalidElements("a<ruby></ruby><ruby><rb></rb></ruby>b<ruby><rt></rt></ruby>"));
        }

        private sealed class FakeTexts(TText text) : ITextRepository
        {
            public Task<IReadOnlyList<TText>> ListAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<TText>>([text]);
            public Task<TText?> GetAsync(int id, CancellationToken ct = default) =>
                Task.FromResult<TText?>(new TText { Index = text.Index, Title = text.Title, Level = text.Level, Body = text.Body });
            public Task<IReadOnlySet<int>> GetTextsWithExercisesAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlySet<int>>(new HashSet<int>());
            public Task AddAsync(string title, string? level, string body, CancellationToken ct = default) => Task.CompletedTask;
            public Task UpdateAsync(int id, string title, string? level, string body, CancellationToken ct = default) => Task.CompletedTask;
        }

        private sealed class FakeMonitors : IMonitorStore
        {
            public Dictionary<(string Table, int User, int Item), Monitor> Rows { get; } = [];

            public Task<Monitor?> GetAsync(MonitorTable table, int userId, int itemId, CancellationToken ct = default) =>
                Task.FromResult(Rows.TryGetValue((table.Table, userId, itemId), out var m) ? m : null);

            public Task<IReadOnlyDictionary<int, int>> GetCountersAsync(MonitorTable table, int userId, IReadOnlyList<int> itemIds, CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyDictionary<int, int>>(Rows
                    .Where(r => r.Key.Table == table.Table && r.Key.User == userId && itemIds.Contains(r.Key.Item))
                    .ToDictionary(r => r.Key.Item, r => r.Value.Counter));

            public Task SaveAsync(MonitorTable table, Monitor monitor, CancellationToken ct = default)
            {
                if (monitor.IsNew) monitor.ID = Rows.Count + 1;
                Rows[(table.Table, monitor.UsrID, monitor.VcbID)] = monitor;
                return Task.CompletedTask;
            }
        }

        private sealed class NoKnowledge : IKnowledgeStore
        {
            public Task DeclareAsync(KnowledgeTable table, int userId, int itemId, DateTime now, CancellationToken ct = default) => Task.CompletedTask;
            public Task<IReadOnlySet<int>> GetDeclaredAsync(KnowledgeTable table, int userId, CancellationToken ct = default) => Task.FromResult<IReadOnlySet<int>>(new HashSet<int>());
        }

        [Fact]
        public async Task Reading_hides_furigana_until_the_student_clicks_then_shows_until_learned()
        {
            const string body = "<p>日本語「にほんご」 <span class=\"stretchText\" data-id=\"7\"><ruby><rb>猫</rb><rt>ねこ</rt></ruby></span></p>";
            var monitors = new FakeMonitors();
            var service = new TextService(new FakeTexts(new TText { Index = 1, Title = "t", Body = body }), new NoKnowledge(), monitors);

            // Nunca clicou: escondida (UnknownStatus). A notação antiga vira <ruby>.
            var first = await service.GetForReadingAsync(1, userId: 5);
            Assert.Contains("<ruby><rb>日本語</rb><rt>にほんご</rt></ruby>", first!.Body);
            Assert.Contains("<rt style=\"display: none;\">ねこ</rt>", first.Body);

            // Clicou: contador 10 em TRL_MONITOR → furigana visível.
            await service.RegisterLackOfKnowledgeAsync(5, 7);
            Assert.Equal(10, monitors.Rows[("TRL_MONITOR", 5, 7)].Counter);
            Assert.Equal(1, monitors.Rows[("TRL_MONITOR", 5, 7)].FailCounter); // contador estava em 0
            var second = await service.GetForReadingAsync(1, userId: 5);
            Assert.Contains("<rt>ねこ</rt>", second!.Body);

            // Para outro aluno continua escondida.
            var other = await service.GetForReadingAsync(1, userId: 6);
            Assert.Contains("<rt style=\"display: none;\">ねこ</rt>", other!.Body);
        }
    }

    public class ExercisesTests
    {
        private sealed class FakeExercises : IExerciseRepository
        {
            public List<TExercise> Items { get; } = [];
            public Dictionary<(int, int), Monitor> Monitors { get; } = [];

            public Task<IReadOnlyList<(int Id, EJLPT Level)>> ListAllAsync(CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<(int, EJLPT)>>(Items.Where(e => e.TextId == 0).Select(e => (e.ID, e.Level)).ToList());
            public Task<IReadOnlyList<(int Id, EJLPT Level)>> ListAsync(int grammarId, int textId, CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<(int, EJLPT)>>(Items
                    .Where(e => grammarId > 0 ? e.GrammarId == grammarId : e.TextId == textId)
                    .Select(e => (e.ID, e.Level)).ToList());
            public Task<TExercise?> GetAsync(int id, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(e => e.ID == id));
            public Task AddAsync(TExercise exercise, CancellationToken ct = default) { Items.Add(exercise); return Task.CompletedTask; }
            public Task UpdateAsync(TExercise exercise, CancellationToken ct = default) => Task.CompletedTask;
            public Task<Monitor?> GetMonitorAsync(int userId, int exerciseId, CancellationToken ct = default) =>
                Task.FromResult(Monitors.TryGetValue((userId, exerciseId), out var m) ? m : null);
            public Task SaveMonitorAsync(Monitor monitor, CancellationToken ct = default)
            {
                if (monitor.IsNew) monitor.ID = Monitors.Count + 1;
                Monitors[(monitor.UsrID, monitor.VcbID)] = monitor;
                return Task.CompletedTask;
            }
        }

        private static FakeExercises Seed()
        {
            var repo = new FakeExercises();
            for (var i = 1; i <= 15; i++)
                repo.Items.Add(new TExercise { ID = i, GrammarId = i <= 5 ? 1 : 0, TextId = i > 12 ? 9 : 0, Level = EJLPT.N5, Question = "q", Option1 = "a", Option2 = "b", Option3 = "c", Option4 = "d", RightAnswer = 2 });
            repo.Items.Add(new TExercise { ID = 99, GrammarId = 1, Level = EJLPT.N2, RightAnswer = 1 });
            return repo;
        }

        [Fact]
        public async Task General_list_takes_ten_without_text_exercises()
        {
            var ids = await new ExercisesService(Seed()).ListAsync(EJLPT.N5, 0, 0, new Random(3));
            Assert.Equal(10, ids.Count);
            Assert.DoesNotContain(ids, id => id > 12 && id < 99);
        }

        [Fact]
        public async Task Grammar_list_filters_by_level()
        {
            var ids = await new ExercisesService(Seed()).ListAsync(EJLPT.N5, 1, 0, new Random(3));
            Assert.Equal([1, 2, 3, 4, 5], ids.Order());
        }

        [Fact]
        public async Task Both_grammar_and_text_is_rejected()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => new ExercisesService(Seed()).ListAsync(EJLPT.N5, 1, 9));
        }

        [Fact]
        public async Task Check_answer_feeds_monitor_and_tells_the_right_option_text()
        {
            var repo = Seed();
            var service = new ExercisesService(repo);

            Assert.True((await service.CheckAnswerAsync(4, 1, 2))!.OK);
            var wrong = await service.CheckAnswerAsync(4, 1, 3);
            Assert.False(wrong!.OK);
            Assert.Contains("\"b\"", wrong.Message);
            Assert.Equal((1, 1), (repo.Monitors[(4, 1)].SuccessCounter, repo.Monitors[(4, 1)].FailCounter));
        }

        [Fact]
        public async Task Question_does_not_leak_the_right_answer()
        {
            var q = await new ExercisesService(Seed()).GetQuestionAsync(1);
            Assert.Equal(4, q!.Options.Count);
            Assert.DoesNotContain("RightAnswer", System.Text.Json.JsonSerializer.Serialize(q));
        }

        [Theory]
        [InlineData("", "a", 1, false)]
        [InlineData("q", "", 1, false)]
        [InlineData("q", "a", 0, false)]
        [InlineData("q", "a", 4, true)]
        public void Validation(string question, string option, int right, bool ok) =>
            Assert.Equal(ok, ExercisesService.Validate(new TExercise { Question = question, Option1 = option, Option2 = "b", Option3 = "c", Option4 = "d", RightAnswer = right }).OK);
    }

    public class DictionaryTests
    {
        private static readonly TWord[] Words =
        [
            new() { ID = 1, Kana = "ねこ", Romaji = "neko", KanjiWord1 = "猫", Subject1 = "Animais", Word1 = "gato,felino" },
            new() { ID = 2, Kana = "こねこ", Romaji = "koneko", KanjiWord1 = "子猫", Subject1 = "Animais", Word1 = "gatinho" },
            new() { ID = 3, Kana = "いぬ", Romaji = "inu", KanjiWord1 = null, Subject1 = "Animais", Word1 = "cachorro" },
        ];

        [Fact]
        public void Exact_then_prefix_then_contains()
        {
            var result = DictionaryService.Search(Words, "ねこ", ESearchType.Japanese);
            Assert.Equal(["ねこ", "こねこ"], result.Results.Select(r => r.FoundTerm));
        }

        [Fact]
        public void Portuguese_search_finds_first_matching_translation()
        {
            var result = DictionaryService.Search(Words, "gat", ESearchType.Portuguese);
            Assert.Equal(["gatinho", "gato"], result.Results.Select(r => r.FoundTerm));
        }

        [Fact]
        public void General_search_includes_kanji_and_ignores_missing_fields()
        {
            var result = DictionaryService.Search(Words, "猫", ESearchType.General);
            Assert.Equal(["猫", "子猫"], result.Results.Select(r => r.FoundTerm));
            Assert.Empty(DictionaryService.Search(Words, "猫", ESearchType.Portuguese).Results);
        }
    }

    public class TranscriptionTests
    {
        [Fact]
        public void Words_without_kanji_for_the_subject_are_left_out()
        {
            var words = new[]
            {
                new TWord { ID = 1, Kana = "ねこ", KanjiWord1 = "猫", Subject1 = "Animais" },
                new TWord { ID = 2, Kana = "いぬ", KanjiWord1 = null, Subject1 = "Animais", KanjiWord2 = "犬", Subject2 = "Outro" },
            };
            var session = TranscriptionService.BuildSession("Animais", words, false, new Random(1));

            var item = Assert.Single(session.Pages.SelectMany(p => p));
            Assert.Equal((1, "猫"), (item.WordId, item.Prompt));
        }

        [Theory]
        [InlineData("ねこ", true)]
        [InlineData("ねこ ", false)]
        [InlineData("", false)]
        public void Check_is_exact_against_kana(string answer, bool ok) =>
            Assert.Equal(ok, TranscriptionService.Check(new TWord { Kana = "ねこ" }, answer));
    }
}
