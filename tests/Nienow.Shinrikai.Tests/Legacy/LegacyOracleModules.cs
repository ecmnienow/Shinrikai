using System.Collections.Generic;
using System.Linq;

namespace Nienow.Shinrikai.Tests.Legacy
{
    /// <summary>
    /// Mais cópias literais do original (sem banco) para os módulos portados depois.
    /// NÃO refatorar este arquivo.
    /// </summary>
    internal static class LegacyOracleModules
    {
        // Nienow.Shinrikai.Kanji/Model/TKanji.cs — CheckReading (KanaReadingForms = Kana.Split(','))
        public static int CheckReading(string kana, params string[] readingForms)
        {
            var kanaReadingForms = kana.Split(',');
            var resultsArray = new int[] { 1, 2, 4 };

            int results = 0;

            for (int i = 0; i < readingForms.Length; i++)
            {
                var text = readingForms[i];

                if (string.IsNullOrEmpty(text))
                    continue;

                if (readingForms.Where(t => t == text).Count() > 1)
                    continue;

                if (kanaReadingForms.FirstOrDefault(o => o == text) != null)
                    results += resultsArray[i];
            }

            return results;
        }

        // Nienow.Shinrikai.Kanji/KanjiManager.cs — CheckAnswers (parte da decisão, por kanji)
        public static (bool Wrong, List<bool> WrongInputs) KanjiCheckAnswers(string kana, List<string> answers)
        {
            var checkResults = CheckReading(kana, answers.ToArray());
            var wrong = false;
            var wrongInputs = new List<bool>();

            if ((checkResults & 1) != 1) { wrong = true; wrongInputs.Add(true); } else wrongInputs.Add(false);

            if (answers.Count >= 2)
                if ((checkResults & 2) != 2) { wrong = true; wrongInputs.Add(true); } else wrongInputs.Add(false);

            if (answers.Count >= 3)
                if ((checkResults & 4) != 4) { wrong = true; wrongInputs.Add(true); } else wrongInputs.Add(false);

            return (wrong, wrongInputs);
        }

        // Nienow.Shinrikai.Kanji/Model/GKanjiAnswer.cs — quantidade de caixas por kanji
        public static int KanjiInputs(string kana)
        {
            int count = 0;
            foreach (var answer in kana.Split(','))
            {
                if (count == 3)
                    return count;
                count++;
            }
            return count;
        }
    }
}
