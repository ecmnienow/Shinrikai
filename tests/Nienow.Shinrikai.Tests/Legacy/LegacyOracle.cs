using System;
using Nienow.Helper;
using Nienow.Shinrikai.Core;
using Monitor = Nienow.Helper.Monitor;

namespace Nienow.Shinrikai.Tests.Legacy
{
    /// <summary>
    /// Cópias literais (só sem o acesso a banco) do código original em .NET Framework 4.5.
    /// Servem de oráculo: os testes comparam o port contra estas implementações em todas as
    /// combinações de entrada, garantindo equivalência 1:1 dos algoritmos de adaptabilidade.
    /// NÃO refatorar este arquivo.
    /// </summary>
    internal static class LegacyOracle
    {
        // Nienow.Shinrikai.Core/ManagerCore.cs
        public static bool HasEnoughLevel(EJLPT userLevel, EJLPT contentLevel, bool restrict = false)
        {
            switch (userLevel)
            {
                case EJLPT.None:
                    if (restrict)
                    {
                        if (contentLevel == EJLPT.None)
                            return true;
                    }
                    else
                    {
                        if (contentLevel == EJLPT.None ||
                            contentLevel == EJLPT.N5)
                            return true;
                    }
                    break;

                case EJLPT.N5:
                    if (restrict)
                    {
                        if (contentLevel == EJLPT.None ||
                            contentLevel == EJLPT.N5)
                            return true;
                    }
                    else
                    {
                        if (contentLevel == EJLPT.None ||
                            contentLevel == EJLPT.N5 ||
                            contentLevel == EJLPT.N4)
                            return true;
                    }
                    break;

                case EJLPT.N4:
                    if (restrict)
                    {
                        if (contentLevel == EJLPT.None ||
                            contentLevel == EJLPT.N5 ||
                            contentLevel == EJLPT.N4)
                            return true;
                    }
                    else
                    {
                        if (contentLevel == EJLPT.None ||
                            contentLevel == EJLPT.N5 ||
                            contentLevel == EJLPT.N4 ||
                            contentLevel == EJLPT.N3)
                            return true;
                    }
                    break;

                case EJLPT.N3:
                    if (restrict)
                    {
                        if (contentLevel == EJLPT.None ||
                            contentLevel == EJLPT.N5 ||
                            contentLevel == EJLPT.N4 ||
                            contentLevel == EJLPT.N3)
                            return true;
                    }
                    else
                    {
                        if (contentLevel == EJLPT.None ||
                            contentLevel == EJLPT.N5 ||
                            contentLevel == EJLPT.N4 ||
                            contentLevel == EJLPT.N3 ||
                            contentLevel == EJLPT.N2)
                            return true;
                    }
                    break;

                case EJLPT.N2:
                    if (restrict)
                    {
                        if (contentLevel == EJLPT.None ||
                            contentLevel == EJLPT.N5 ||
                            contentLevel == EJLPT.N4 ||
                            contentLevel == EJLPT.N3 ||
                            contentLevel == EJLPT.N2)
                            return true;
                    }
                    else
                    {
                        return true;
                    }
                    break;
                case EJLPT.N1:
                    return true;
            }

            return false;
        }

        // VocabularyManager.ManageMistake e KanjiManager.ManageMistake (idênticos)
        public static void ManageMistakeVocabulary(Monitor monitor, bool result, DateTime now)
        {
            if (result)
                if (monitor.Counter <= 1) //Pois o 1 passará a ser 0
                {
                    monitor.Counter = 0;
                    monitor.SuccessCounter++;
                    monitor.LastHittedTime = now;
                }
                else
                    monitor.Counter--;

            if (!result)
            {
                if (monitor.Counter <= 0)
                    monitor.FailCounter++;

                monitor.Counter = 10;
            }
        }

        // TranscriptionManager.ManageMistake
        public static void ManageMistakeTranscription(Monitor monitor, bool result, DateTime now)
        {
            if (result)
                if (monitor.Counter <= 0)
                {
                    monitor.Counter = 0;
                    monitor.SuccessCounter++;
                    monitor.LastHittedTime = now;
                }
                else
                    monitor.Counter--;

            if (!result)
            {
                if (monitor.Counter <= 0)
                    monitor.FailCounter++;

                monitor.Counter = 10;
            }
        }

        // TextManager.RegisterLackOfKnowledge
        public static void RegisterLackOfKnowledge(Monitor monitor)
        {
            if (monitor.Counter == 0)
                monitor.FailCounter++;

            monitor.Counter = 10;
        }

        // GrammarManager.ListGrammars (expressão de HasEnoughLevel do item)
        public static bool GrammarUnlocked(EJLPT userLevel, EJLPT contentLevel, bool hasKnowledge) =>
            HasEnoughLevel(userLevel, contentLevel, true) ||
            (hasKnowledge && HasEnoughLevel(userLevel, contentLevel));

        // TextManager.GetText (conversão da notação de furigana)
        public static string ConvertFurigana(string body)
        {
            var regex = new System.Text.RegularExpressions.Regex(@"([一-龠]{1,}「[ぁ-ゔァ-ヴー々〆〤]{1,}[」]{1})",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.Multiline);

            foreach (System.Text.RegularExpressions.Match match in regex.Matches(body))
            {
                var value = match.Groups[1].Value;

                var ruby = new System.Text.StringBuilder();
                ruby.Append("<ruby>");

                var start1 = 0;
                var end1 = value.IndexOf('「');
                ruby.Append("<rb>" + value.Substring(start1, end1) + "</rb>");

                var start2 = value.IndexOf('「') + 1;
                var end2 = (value.Length - start2) - 1;
                ruby.Append("<rt>" + value.Substring(start2, end2) + "</rt>");

                ruby.Append("</ruby>");

                body = body.Replace(match.Groups[1].Value, ruby.ToString());
            }

            return body;
        }
    }
}
