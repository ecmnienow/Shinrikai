using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nienow.Shinrikai.Vocabulary;
using Nienow.Shinrikai.Vocabulary.Model;

namespace Nienow.Shinrikai.Dictionary
{
    public enum ESearchType
    {
        Japanese,
        Portuguese,
        General
    }

    /// <summary>GSearchWord: o termo encontrado e a palavra a que pertence.</summary>
    public record SearchWord(string FoundTerm, TWord Word);

    /// <summary>GResult.</summary>
    public record SearchResult(string Term, IReadOnlyList<SearchWord> Results);

    /// <summary>Port de DictionaryManager.cs + DictionaryController.Search.</summary>
    public sealed class DictionaryService(IVocabularyRepository vocabulary)
    {
        public async Task<SearchResult> SearchAsync(string? term, ESearchType type, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(term))
                return new SearchResult(term ?? string.Empty, []);

            var words = await vocabulary.GetAllAsync(ct);
            return Search(words, term, type);
        }

        /// <summary>
        /// DictionaryManager.SearchRelatedWords + ordenação de DictionaryController.Search:
        /// termo exato, depois os que começam com o termo, depois os que o contêm (ordem alfabética).
        /// Comparações sensíveis a maiúsculas, como no original. Uma palavra pode aparecer mais de uma
        /// vez (por kanji, kana, romaji e tradução), também como no original.
        /// </summary>
        public static SearchResult Search(IEnumerable<TWord> vocabulary, string term, ESearchType type)
        {
            var list = vocabulary.ToList();
            var found = new List<SearchWord>();
            var japanese = type is ESearchType.Japanese or ESearchType.General;
            var portuguese = type is ESearchType.Portuguese or ESearchType.General;

            if (japanese)
            {
                // Vocabulário simples (kanji word)
                found.AddRange(list
                    .Where(w => w.ContainsKanjiWord(term))
                    .Select(w => new SearchWord(w.FirstKanjiWordContaining(term)!, w)));

                // Vocabulário simples (kana)
                found.AddRange(list
                    .Where(w => w.Kana != null && w.Kana.Contains(term))
                    .Select(w => new SearchWord(w.Kana!, w)));

                // Vocabulário simples (romaji)
                found.AddRange(list
                    .Where(w => w.Romaji != null && w.Romaji.Contains(term))
                    .Select(w => new SearchWord(w.Romaji!, w)));
            }

            if (portuguese)
            {
                // Vocabulário simples (português)
                found.AddRange(list
                    .Select(w => (Word: w, Found: w.GetAllTranslatedWords(null).FirstOrDefault(p => p.Contains(term))))
                    .Where(x => x.Found != null)
                    .Select(x => new SearchWord(x.Found!, x.Word)));
            }

            var comparer = StringComparer.InvariantCulture;
            var ordered = new List<SearchWord>();
            ordered.AddRange(found.Where(r => r.FoundTerm == term));
            ordered.AddRange(found.Where(r => r.FoundTerm.StartsWith(term, StringComparison.Ordinal) && r.FoundTerm != term).OrderBy(r => r.FoundTerm, comparer));
            ordered.AddRange(found.Where(r => r.FoundTerm.IndexOf(term, StringComparison.Ordinal) > 0).OrderBy(r => r.FoundTerm, comparer));

            return new SearchResult(term, ordered);
        }
    }
}
