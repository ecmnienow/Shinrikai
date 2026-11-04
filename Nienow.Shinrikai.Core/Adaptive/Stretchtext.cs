using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Nienow.Shinrikai.Core.Adaptive
{
    /// <summary>
    /// Técnica 4.3 — <b>stretchtext</b> (leitura de textos com furigana adaptativa).
    /// </summary>
    public static partial class Stretchtext
    {
        // TextManager.GetText — notação manual 漢字「かんじ」
        [GeneratedRegex(@"([一-龠]{1,}「[ぁ-ゔァ-ヴー々〆〤]{1,}[」]{1})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline)]
        private static partial Regex FuriganaNotationRegex();

        // TextController.GetText — palavra ligada ao vocabulário
        [GeneratedRegex("<span class=\"stretchText\" data-id=\"([0-9]{0,})\">.*?<rt\\s{0,}>.*?<\\/rt>.*?<\\/span>")]
        private static partial Regex StretchSpanRegex();

        /// <summary>
        /// Converte a notação <c>漢字「かんじ」</c> em <c>&lt;ruby&gt;&lt;rb&gt;漢字&lt;/rb&gt;&lt;rt&gt;かんじ&lt;/rt&gt;&lt;/ruby&gt;</c>.
        /// Mantido para exibir textos antigos; o editor novo (TipTap) já grava &lt;ruby&gt; diretamente.
        /// </summary>
        public static string ConvertFuriganaNotation(string body)
        {
            foreach (Match match in FuriganaNotationRegex().Matches(body))
            {
                var value = match.Groups[1].Value;
                var open = value.IndexOf('「');

                var ruby = new StringBuilder()
                    .Append("<ruby>")
                    .Append("<rb>").Append(value, 0, open).Append("</rb>")
                    .Append("<rt>").Append(value, open + 1, value.Length - open - 2).Append("</rt>")
                    .Append("</ruby>")
                    .ToString();

                body = body.Replace(value, ruby);
            }

            return body;
        }

        /// <summary>
        /// Para cada <c>&lt;span class="stretchText" data-id="N"&gt;</c>, esconde o &lt;rt&gt; (furigana)
        /// quando <paramref name="hideFurigana"/> retornar true para a palavra N.
        /// O critério do original está em <see cref="ShouldHideFurigana"/>.
        /// </summary>
        public static string ApplyKnowledge(string body, Func<int, bool> hideFurigana)
        {
            foreach (Match grp in StretchSpanRegex().Matches(body))
            {
                var original = grp.Groups[0].Value;
                if (string.IsNullOrEmpty(grp.Groups[1].Value))
                    continue; // original: Trace.TraceWarning e segue

                var wordId = int.Parse(grp.Groups[1].Value);
                if (!hideFurigana(wordId))
                    continue;

                var hidden = original.Replace("<rt>", "<rt style=\"display: none;\">");
                body = body.Replace(original, hidden);
            }

            return body;
        }

        /// <summary>IDs de palavra referenciados no corpo (para buscar o conhecimento em lote).</summary>
        public static IReadOnlyList<int> ReferencedWordIds(string body) =>
            StretchSpanRegex().Matches(body)
                .Select(m => m.Groups[1].Value)
                .Where(v => v.Length > 0)
                .Select(int.Parse)
                .Distinct()
                .ToArray();

        /// <summary>
        /// Critério do original (DBText): esconde a furigana se o aluno conhece a palavra
        /// (registro em TRL_MONITOR com contador 0) <b>ou</b> se não há registro algum
        /// ("UnknownStatus"). Só mostra quando há registro com contador diferente de 0.
        /// </summary>
        /// <param name="monitorCounter">TMN_COUNTER do aluno para a palavra, ou null se não há registro.</param>
        public static bool ShouldHideFurigana(int? monitorCounter) =>
            monitorCounter is null || monitorCounter == 0;
    }
}
