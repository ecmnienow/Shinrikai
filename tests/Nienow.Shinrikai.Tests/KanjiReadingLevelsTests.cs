using System;
using System.Collections.Generic;
using System.Linq;
using Nienow.Shinrikai.Kanji;
using Xunit;

namespace Nienow.Shinrikai.Tests
{
    public class KanjiReadingLevelsTests
    {
        private static readonly TKanji[] Kanjis =
        [
            new() { ID = 1, KanjiLetter = "日", Kana = "にち,ひ,か,たち", Romaji = "nichi,hi,ka,tachi", JLPT = "N5" },
            new() { ID = 2, KanjiLetter = "曜", Kana = "よう", Romaji = "you", JLPT = "N5" },
            new() { ID = 3, KanjiLetter = "行", Kana = "い,こう,ぎょう", Romaji = "i,kou,gyou", JLPT = "N5" },
            new() { ID = 4, KanjiLetter = "銀", Kana = "ぎん", Romaji = "gin", JLPT = "N4" },
            new() { ID = 5, KanjiLetter = "学", Kana = "まな,がく", Romaji = "mana,gaku", JLPT = "N5" },
            new() { ID = 6, KanjiLetter = "校", Kana = "こう", Romaji = "kou", JLPT = "N5" },
            new() { ID = 7, KanjiLetter = "一", Kana = "いち,ひと", Romaji = "ichi,hito", JLPT = "N5" },
            new() { ID = 8, KanjiLetter = "人", Kana = "ひと,じん,にん", Romaji = "hito,jin,nin", JLPT = "N5" },
            new() { ID = 9, KanjiLetter = "今", Kana = "いま,こん", Romaji = "ima,kon", JLPT = "N5" },
            new() { ID = 10, KanjiLetter = "本", Kana = "ほん,もと", Romaji = "hon,moto", JLPT = "N5" },
            new() { ID = 11, KanjiLetter = "雨", Kana = "あめ", Romaji = "ame", JLPT = null },
        ];

        private static IReadOnlyList<string>? ReadingsOf(string kanji) =>
            Kanjis.FirstOrDefault(k => k.KanjiLetter == kanji)?.KanaReadingForms;

        private static string? Split(string written, string kana) =>
            ReadingRequirements.Align(written, kana, ReadingsOf) is { } a ? string.Join("+", a.Select(x => $"{x.Kanji}:{x.Reading}")) : null;

        [Theory]
        [InlineData("日曜日", "にちようび", "日:にち+曜:よう+日:ひ")] // sonorização: ひ → び
        [InlineData("行く", "いく", "行:い")]
        [InlineData("銀行", "ぎんこう", "銀:ぎん+行:こう")]
        [InlineData("学校", "がっこう", "学:がく+校:こう")] // っ no fim: がく → がっ
        [InlineData("一日", "いちにち", "一:いち+日:にち")]
        [InlineData("人々", "ひとびと", "人:ひと+人:ひと")] // 々 repete o kanji anterior
        [InlineData("日本", "ニホン", null)] // katakana vira hiragana, mas falta に em 日
        [InlineData("今日", "きょう", null)] // leitura especial
        [InlineData("行く", "いった", null)] // o kana sobrando não fecha
        public void Splits_kana_between_kanjis(string written, string kana, string? expected) =>
            Assert.Equal(expected, Split(written, kana));

        [Fact]
        public void Plain_readings_are_tried_before_voiced_or_geminated_ones()
        {
            // び serve como ひ sonorizado e como leitura cadastrada; a cadastrada vence.
            var kanji = new TKanji { KanjiLetter = "日", Kana = "ひ,び" };
            Assert.Equal("日:び", string.Join("+", ReadingRequirements.Align("日", "び", _ => kanji.KanaReadingForms)!.Select(x => $"{x.Kanji}:{x.Reading}")));
        }

        private static ReadingRequirements Requirements(params (string Written, string Kana)[] words) =>
            ReadingRequirements.Build(Kanjis, words.Select((w, i) => new VocabularyForm(i + 1, w.Kana, w.Written)));

        private static TKanji K(string letter) => Kanjis.Single(k => k.KanjiLetter == letter);

        [Fact]
        public void Reading_is_required_from_the_level_of_the_words_that_use_it()
        {
            var r = Requirements(("行く", "いく"), ("銀行", "ぎんこう"), ("日曜日", "にちようび"));

            // 銀行 só entra no N4 (nível do 銀), então こう não é exigida no N5.
            Assert.Equal(["い"], r.Required(K("行"), "N5"));
            Assert.Equal(["い", "こう"], r.Required(K("行"), "N4"));
            Assert.Equal(["い", "こう"], r.Required(K("行"), "N1"));
            Assert.Equal(["にち", "ひ"], r.Required(K("日"), "N5"));
        }

        [Fact]
        public void Word_with_kanji_without_level_only_counts_at_N1()
        {
            var r = Requirements(("雨の日", "あめのひ"));
            Assert.Empty(r.Required(K("日"), "N2"));
            Assert.Equal(["ひ"], r.Required(K("日"), "N1"));
        }

        [Fact]
        public void Inputs_follow_required_readings_with_at_least_one_and_at_most_three()
        {
            var r = Requirements(("行く", "いく"), ("一日", "いちにち"), ("日曜日", "にちようび"), ("三日", "みっか"), ("今日は", "こんにちは"));

            Assert.Equal(1, r.Inputs(K("行"), "N5"));
            Assert.Equal(1, r.Inputs(K("本"), "N5")); // nenhuma palavra: 1 campo, aceita qualquer leitura
            Assert.Equal(2, r.Inputs(K("日"), "N5")); // にち, ひ (三日 falha: 三 não está cadastrado)
            Assert.Equal(1, r.Inputs(K("今"), "N5")); // こん (今日は = こん + にち + は)
        }

        [Fact]
        public void Session_uses_required_readings_and_legacy_overload_is_unchanged()
        {
            var r = Requirements(("行く", "いく"));
            var kanji = K("行");

            var session = KanjiService.BuildSession("N5", [kanji], false, new Random(1), k => r.Inputs(k, "N5"));
            Assert.Equal(1, session.Pages[0][0].Inputs);
            Assert.Equal(3, KanjiService.BuildSession("N5", [kanji], false, new Random(1)).Pages[0][0].Inputs);
        }

        [Fact]
        public void Any_registered_reading_is_still_accepted_and_required_ones_come_first()
        {
            var kanji = K("行");

            var right = KanjiService.Check(kanji, new KanjiAnswer(3, "行", ["ぎょう"]), ["い"]);
            Assert.False(right.Wrong);

            var wrong = KanjiService.Check(kanji, new KanjiAnswer(3, "行", ["x"]), ["こう"]);
            Assert.True(wrong.Wrong);
            Assert.Equal(["こう", "い", "ぎょう"], wrong.Expected);
            Assert.Equal(["こう"], wrong.Required);
        }

        [Fact]
        public void Report_lists_levels_words_and_unaligned_words()
        {
            var r = Requirements(("行く", "いく"), ("銀行", "ぎんこう"), ("日本", "にほん"), ("日傘", "ひがさ"));
            var report = r.Report(K("日"));

            Assert.Equal(["にち", "ひ", "か", "たち"], report.Readings.Select(x => x.Reading));
            Assert.All(report.Readings, x => Assert.Null(x.RequiredFrom));
            Assert.Equal(["日本", "日傘"], report.Unaligned.Select(u => u.Word));
            Assert.Empty(report.Unaligned[0].MissingKanji); // leitura especial ou faltando no cadastro
            Assert.Equal(["傘"], report.Unaligned[1].MissingKanji); // kanji não cadastrado

            var kou = r.Report(K("行")).Readings.Single(x => x.Reading == "こう");
            Assert.Equal("N4", kou.RequiredFrom);
            Assert.Equal("kou", kou.Romaji);
            Assert.Equal([new ReadingWord("銀行", "ぎんこう", "N4")], kou.Words);
        }
    }
}
