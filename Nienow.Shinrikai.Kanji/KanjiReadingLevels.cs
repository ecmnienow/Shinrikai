using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Nienow.Shinrikai.Core.Adaptive;

namespace Nienow.Shinrikai.Kanji
{
    public record VocabularyForm(int WordId, string Kana, string Written);

    public record ReadingWord(string Word, string Kana, string Level);

    /// <summary><see cref="RequiredFrom"/> null: nenhuma palavra de treino usa a leitura.</summary>
    public record ReadingLevelInfo(string Reading, string? Romaji, string? RequiredFrom, IReadOnlyList<ReadingWord> Words);

    /// <summary><see cref="MissingKanji"/> vazio: leitura especial (今日) ou leitura faltando no cadastro.</summary>
    public record UnalignedWord(string Word, string Kana, IReadOnlyList<string> MissingKanji);

    public record KanjiReadingReport(string Kanji, IReadOnlyList<ReadingLevelInfo> Readings, IReadOnlyList<UnalignedWord> Unaligned);

    /// <summary>
    /// Extensão ao original: uma leitura é exigida no nível L quando alguma palavra de treino de nível ≤ L a usa.
    /// O nível da palavra é o mais alto entre os seus kanjis (sem nível = N1). A correção continua aceitando
    /// qualquer leitura cadastrada.
    /// </summary>
    public sealed class ReadingRequirements
    {
        private const int NoLevel = 4;

        private readonly Dictionary<string, string[]> _readings;
        private readonly Dictionary<string, int> _levels;
        private readonly Dictionary<string, Dictionary<string, List<(VocabularyForm Form, int Level)>>> _uses = [];
        private readonly List<UnalignedWord> _unaligned = [];

        private ReadingRequirements(IEnumerable<TKanji> kanjis)
        {
            var registered = kanjis.Where(k => !string.IsNullOrEmpty(k.KanjiLetter)).GroupBy(k => k.KanjiLetter!).Select(g => g.First()).ToList();
            _readings = registered.ToDictionary(k => k.KanjiLetter!, Readings);
            _levels = registered.ToDictionary(k => k.KanjiLetter!, k => LevelIndex(k.JLPT));
        }

        public IReadOnlyList<UnalignedWord> Unaligned => _unaligned;

        public static ReadingRequirements Build(IEnumerable<TKanji> kanjis, IEnumerable<VocabularyForm> words)
        {
            var result = new ReadingRequirements(kanjis);
            foreach (var form in words.DistinctBy(w => (w.WordId, w.Written)))
                result.Add(form);
            return result;
        }

        public IReadOnlyList<string> Required(TKanji kanji, string level)
        {
            var index = LevelIndex(level);
            if (kanji.KanjiLetter is null || !_uses.TryGetValue(kanji.KanjiLetter, out var uses))
                return [];
            return Readings(kanji).Where(r => uses.TryGetValue(r, out var u) && u.Min(x => x.Level) <= index).ToList();
        }

        public int Inputs(TKanji kanji, string level) => Math.Clamp(Required(kanji, level).Count, 1, KanjiService.MaxInputs);

        public KanjiReadingReport Report(TKanji kanji)
        {
            var letter = kanji.KanjiLetter ?? string.Empty;
            var romaji = (kanji.Romaji ?? string.Empty).Split(',');
            var forms = kanji.KanaReadingForms;
            _uses.TryGetValue(letter, out var uses);

            var readings = new List<ReadingLevelInfo>();
            for (var i = 0; i < forms.Length; i++)
            {
                var reading = Normalize(forms[i]);
                if (reading.Length == 0)
                    continue;
                var used = uses is not null && uses.TryGetValue(reading, out var u) ? u : [];
                var words = used.OrderBy(x => x.Level).ThenBy(x => x.Form.Kana, StringComparer.Ordinal)
                    .Select(x => new ReadingWord(x.Form.Written, x.Form.Kana, KanjiLevels.All[x.Level]))
                    .DistinctBy(w => w.Word).ToList();
                readings.Add(new ReadingLevelInfo(forms[i].Trim(), i < romaji.Length ? romaji[i].Trim() : null,
                    used.Count == 0 ? null : KanjiLevels.All[used.Min(x => x.Level)], words));
            }

            var unaligned = _unaligned.Where(w => Elements(w.Word).Contains(letter)).ToList();
            return new KanjiReadingReport(letter, readings, unaligned);
        }

        private void Add(VocabularyForm form)
        {
            var elements = Elements(form.Written);
            var kanjis = elements.Where(IsKanji).ToList();
            if (kanjis.Count == 0)
                return;

            var missing = kanjis.Where(k => k != "々" && !_readings.ContainsKey(k)).Distinct().ToList();
            var alignment = missing.Count == 0 ? Align(elements, ToHiragana(form.Kana), k => _readings.GetValueOrDefault(k)) : null;
            if (alignment is null)
            {
                _unaligned.Add(new UnalignedWord(form.Written, form.Kana, missing));
                return;
            }

            var level = kanjis.Where(k => k != "々").Select(k => _levels[k]).DefaultIfEmpty(NoLevel).Max();
            foreach (var (kanji, reading) in alignment)
            {
                var byReading = _uses.TryGetValue(kanji, out var d) ? d : _uses[kanji] = [];
                (byReading.TryGetValue(reading, out var list) ? list : byReading[reading] = []).Add((form, level));
            }
        }

        /// <summary>Divide o kana entre os kanjis, aceitando sonorização (ひ → び) e っ no fim (いち → いっ).</summary>
        public static IReadOnlyList<(string Kanji, string Reading)>? Align(string written, string kana, Func<string, IReadOnlyList<string>?> readingsOf) =>
            Align(Elements(written), ToHiragana(kana), readingsOf);

        private static IReadOnlyList<(string Kanji, string Reading)>? Align(IReadOnlyList<string> elements, string kana, Func<string, IReadOnlyList<string>?> readingsOf)
        {
            var result = new List<(string, string)>();
            return Step(0, 0) ? result : null;

            bool Step(int i, int j)
            {
                if (i == elements.Count)
                    return j == kana.Length;

                var element = elements[i];
                if (!IsKanji(element))
                {
                    var text = ToHiragana(element);
                    return string.CompareOrdinal(kana, j, text, 0, text.Length) == 0 && Step(i + 1, j + text.Length);
                }

                var kanji = element == "々" && i > 0 ? elements[i - 1] : element;
                if (readingsOf(kanji) is not { } readings)
                    return false;
                foreach (var (surface, reading) in Candidates(readings))
                {
                    if (string.CompareOrdinal(kana, j, surface, 0, surface.Length) != 0)
                        continue;
                    result.Add((kanji, reading));
                    if (Step(i + 1, j + surface.Length))
                        return true;
                    result.RemoveAt(result.Count - 1);
                }
                return false;
            }
        }

        private static readonly Dictionary<char, string> Voiced = new()
        {
            ['か'] = "が", ['き'] = "ぎ", ['く'] = "ぐ", ['け'] = "げ", ['こ'] = "ご",
            ['さ'] = "ざ", ['し'] = "じ", ['す'] = "ず", ['せ'] = "ぜ", ['そ'] = "ぞ",
            ['た'] = "だ", ['ち'] = "ぢ", ['つ'] = "づ", ['て'] = "で", ['と'] = "ど",
            ['は'] = "ばぱ", ['ひ'] = "びぴ", ['ふ'] = "ぶぷ", ['へ'] = "べぺ", ['ほ'] = "ぼぽ",
        };

        // A leitura como está vem antes da sonorizada e da com っ.
        private static IEnumerable<(string Surface, string Reading)> Candidates(IReadOnlyList<string> all)
        {
            var readings = all.Where(r => r.Length > 0).ToList();
            var voiced = readings.SelectMany(r => (Voiced.GetValueOrDefault(r[0]) ?? string.Empty).Select(v => (v + r[1..], r))).ToList();
            var geminated = readings.Select(r => (r, r)).Concat(voiced)
                .Where(x => x.Item1.Length > 1 && "つちくき".Contains(x.Item1[^1]))
                .Select(x => (x.Item1[..^1] + "っ", x.Item2)).ToList();
            return readings.Select(r => (r, r)).Concat(voiced).Concat(geminated).Distinct();
        }

        private static string[] Readings(TKanji kanji) =>
            kanji.KanaReadingForms.Select(Normalize).Where(r => r.Length > 0).Distinct().ToArray();

        private static string Normalize(string reading) => ToHiragana(reading.Trim());

        private static int LevelIndex(string? level) =>
            Array.IndexOf(KanjiLevels.All, JlptLevel.Normalize(level)) is var i and >= 0 ? i : NoLevel;

        private static List<string> Elements(string text)
        {
            var list = new List<string>();
            var e = StringInfo.GetTextElementEnumerator(text);
            while (e.MoveNext())
                list.Add(e.GetTextElement());
            return list;
        }

        private static bool IsKanji(string element)
        {
            if (element == "々")
                return true;
            var rune = Rune.GetRuneAt(element, 0);
            return rune.Value is < 0x3040 or > 0x30FF && Rune.GetUnicodeCategory(rune) == UnicodeCategory.OtherLetter;
        }

        private static string ToHiragana(string text)
        {
            var chars = text.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
                if (chars[i] is >= 'ァ' and <= 'ヶ')
                    chars[i] = (char)(chars[i] - 0x60);
            return new string(chars);
        }
    }
}
