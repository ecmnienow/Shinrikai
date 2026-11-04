using System;
using System.Collections.Generic;
using System.Linq;
using Nienow.Helper;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Core.Adaptive;
using Nienow.Shinrikai.Tests.Legacy;
using Xunit;
using Monitor = Nienow.Helper.Monitor;

namespace Nienow.Shinrikai.Tests
{
    /// <summary>Equivalência 1:1 com o código original (ver <see cref="LegacyOracle"/>).</summary>
    public class AdaptiveAlgorithmsTests
    {
        private static readonly DateTime Now = new(2026, 11, 3, 12, 0, 0);

        public static IEnumerable<object[]> AllLevelPairs() =>
            from u in Enum.GetValues<EJLPT>()
            from c in Enum.GetValues<EJLPT>()
            from r in new[] { true, false }
            select new object[] { u, c, r };

        [Theory]
        [MemberData(nameof(AllLevelPairs))]
        public void HasEnoughLevel_matches_legacy_switch(EJLPT user, EJLPT content, bool restrict) =>
            Assert.Equal(LegacyOracle.HasEnoughLevel(user, content, restrict), JlptLevel.HasEnoughLevel(user, content, restrict));

        [Theory]
        [InlineData(EJLPT.None, EJLPT.N5, false, true)]  // sem nível vê N5
        [InlineData(EJLPT.None, EJLPT.N5, true, false)]  // ...mas não no modo restrito
        [InlineData(EJLPT.N5, EJLPT.N3, false, false)]   // nunca dois níveis acima
        [InlineData(EJLPT.N1, EJLPT.N1, true, true)]
        public void HasEnoughLevel_documented_examples(EJLPT user, EJLPT content, bool restrict, bool expected) =>
            Assert.Equal(expected, JlptLevel.HasEnoughLevel(user, content, restrict));

        public static IEnumerable<object[]> MonitorStates() =>
            from counter in Enumerable.Range(-2, 15)
            from fails in new[] { 0, 3 }
            from successes in new[] { 0, 5 }
            from correct in new[] { true, false }
            select new object[] { counter, fails, successes, correct };

        private static Monitor M(int counter, int fails, int successes) =>
            new() { Counter = counter, FailCounter = fails, SuccessCounter = successes };

        private static void AssertSame(Monitor expected, Monitor actual)
        {
            Assert.Equal(expected.Counter, actual.Counter);
            Assert.Equal(expected.FailCounter, actual.FailCounter);
            Assert.Equal(expected.SuccessCounter, actual.SuccessCounter);
            Assert.Equal(expected.LastHittedTime, actual.LastHittedTime);
        }

        [Theory]
        [MemberData(nameof(MonitorStates))]
        public void Vocabulary_fragment_variation_matches_legacy(int counter, int fails, int successes, bool correct)
        {
            var legacy = M(counter, fails, successes);
            LegacyOracle.ManageMistakeVocabulary(legacy, correct, Now);

            AssertSame(legacy, FragmentVariation.ApplyVocabulary(M(counter, fails, successes), correct, Now));
            AssertSame(legacy, FragmentVariation.ApplyKanji(M(counter, fails, successes), correct, Now));
        }

        [Theory]
        [MemberData(nameof(MonitorStates))]
        public void Transcription_fragment_variation_matches_legacy(int counter, int fails, int successes, bool correct)
        {
            var legacy = M(counter, fails, successes);
            LegacyOracle.ManageMistakeTranscription(legacy, correct, Now);

            AssertSame(legacy, FragmentVariation.ApplyTranscription(M(counter, fails, successes), correct, Now));
        }

        [Theory]
        [MemberData(nameof(MonitorStates))]
        public void Lack_of_knowledge_matches_legacy(int counter, int fails, int successes, bool _)
        {
            var legacy = M(counter, fails, successes);
            LegacyOracle.RegisterLackOfKnowledge(legacy);

            AssertSame(legacy, FragmentVariation.RegisterLackOfKnowledge(M(counter, fails, successes)));
        }

        [Fact]
        public void Vocabulary_word_needs_ten_correct_answers_after_a_mistake()
        {
            var m = new Monitor();
            FragmentVariation.ApplyVocabulary(m, correct: false, Now);
            Assert.Equal(10, m.Counter);
            Assert.Equal(1, m.FailCounter);

            for (var i = 0; i < 9; i++)
                FragmentVariation.ApplyVocabulary(m, correct: true, Now);

            Assert.Equal(1, m.Counter);
            Assert.Equal(0, m.SuccessCounter);

            FragmentVariation.ApplyVocabulary(m, correct: true, Now);
            Assert.Equal(0, m.Counter);
            Assert.Equal(1, m.SuccessCounter);
            Assert.Equal(Now, m.LastHittedTime);
        }

        [Fact]
        public void Error_while_still_recovering_does_not_count_as_new_failure()
        {
            var m = new Monitor { Counter = 4, FailCounter = 1 };
            FragmentVariation.ApplyVocabulary(m, correct: false, Now);
            Assert.Equal(10, m.Counter);
            Assert.Equal(1, m.FailCounter);
        }

        [Fact]
        public void Transcription_differs_from_vocabulary_at_counter_one()
        {
            var voc = FragmentVariation.ApplyVocabulary(new Monitor { Counter = 1 }, true, Now);
            var trn = FragmentVariation.ApplyTranscription(new Monitor { Counter = 1 }, true, Now);

            Assert.Equal((0, 1), (voc.Counter, voc.SuccessCounter));
            Assert.Equal((0, 0), (trn.Counter, trn.SuccessCounter));
        }

        [Fact]
        public void Exercise_monitor_only_accumulates()
        {
            var m = new Monitor { Counter = 7 };
            FragmentVariation.ApplyExercise(m, true, Now);
            FragmentVariation.ApplyExercise(m, false, Now);
            Assert.Equal((7, 1, 1), (m.Counter, m.SuccessCounter, m.FailCounter));
        }

        // ---- 4.2 Mapa adaptativo ----

        public static IEnumerable<object[]> UnlockCases() =>
            from u in Enum.GetValues<EJLPT>()
            from c in Enum.GetValues<EJLPT>()
            from k in new[] { true, false }
            select new object[] { u, c, k };

        [Theory]
        [MemberData(nameof(UnlockCases))]
        public void Grammar_unlock_matches_legacy(EJLPT user, EJLPT content, bool hasKnowledge) =>
            Assert.Equal(LegacyOracle.GrammarUnlocked(user, content, hasKnowledge), AdaptiveMap.IsUnlocked(user, content, hasKnowledge));

        [Theory]
        [InlineData(null, 0, true)]
        [InlineData("", 0, true)]
        [InlineData("001", 0, false)]
        [InlineData("001", 1, true)]
        [InlineData("001;002;010", 2, false)]
        [InlineData("001;002;010", 3, true)]
        public void Grammar_has_knowledge(string? requisites, int declared, bool expected) =>
            Assert.Equal(expected, AdaptiveMap.HasKnowledge(requisites, declared));

        [Fact]
        public void Grammar_requisites_are_parsed_as_ids()
        {
            Assert.Equal([1, 2, 10], AdaptiveMap.ParseRequisites("001;002;010"));
            Assert.Empty(AdaptiveMap.ParseRequisites(null));
            Assert.Equal([5], AdaptiveMap.ParseRequisites("005; ;x"));
        }

        // ---- 4.3 Stretchtext ----

        [Theory]
        [InlineData("私は日本語「にほんご」を勉強「べんきょう」します。")]
        [InlineData("漢字「かんじ」と漢字「かんじ」")]
        [InlineData("sem notação nenhuma")]
        [InlineData("片仮名「カタカナ」ー")]
        public void Furigana_notation_matches_legacy(string body) =>
            Assert.Equal(LegacyOracle.ConvertFurigana(body), Stretchtext.ConvertFuriganaNotation(body));

        [Fact]
        public void Furigana_notation_generates_ruby()
        {
            Assert.Equal("<ruby><rb>日本語</rb><rt>にほんご</rt></ruby>", Stretchtext.ConvertFuriganaNotation("日本語「にほんご」"));
        }

        [Fact]
        public void Stretchtext_hides_rt_only_for_selected_words()
        {
            const string body =
                "<p><span class=\"stretchText\" data-id=\"7\"><ruby><rb>猫</rb><rt>ねこ</rt></ruby></span>と" +
                "<span class=\"stretchText\" data-id=\"9\"><ruby><rb>犬</rb><rt>いぬ</rt></ruby></span></p>";

            var result = Stretchtext.ApplyKnowledge(body, id => id == 7);

            Assert.Contains("<rb>猫</rb><rt style=\"display: none;\">ねこ</rt>", result);
            Assert.Contains("<rb>犬</rb><rt>いぬ</rt>", result);
            Assert.Equal([7, 9], Stretchtext.ReferencedWordIds(body));
        }

        [Theory]
        [InlineData(null, true)]  // nunca clicou → esconde (UnknownStatus)
        [InlineData(0, true)]     // conhece → esconde
        [InlineData(10, false)]   // clicou recentemente → mostra
        public void Stretchtext_hide_rule(int? counter, bool hide) =>
            Assert.Equal(hide, Stretchtext.ShouldHideFurigana(counter));

        // ---- 4.4 Ordenação adaptativa ----

        [Fact]
        public void Exercises_are_filtered_by_level_and_shuffled()
        {
            var exercises = Enumerable.Range(1, 30).Select(i => (i, (EJLPT)(i % 6))).ToList();

            var selected = AdaptiveOrdering.Select(exercises, EJLPT.N5, rng: new Random(42));

            var expectedIds = exercises.Where(e => LegacyOracle.HasEnoughLevel(EJLPT.N5, e.Item2)).Select(e => e.i).Order();
            Assert.Equal(expectedIds, selected.Order());
            Assert.NotEqual(selected.Order(), selected); // embaralhado (semente fixa)
        }

        [Fact]
        public void All_exercises_takes_ten()
        {
            var exercises = Enumerable.Range(1, 30).Select(i => (i, EJLPT.N5)).ToList();
            Assert.Equal(10, AdaptiveOrdering.Select(exercises, EJLPT.N5, AdaptiveOrdering.AllExercisesTake).Count);
        }
    }
}
