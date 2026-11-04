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
        public async Task<SearchResult> SearchAsync(string? term, ESearchType type, CancellationToken ct = default) =>
            string.IsNullOrWhiteSpace(term)
                ? new SearchResult(term ?? string.Empty, [])
                : Search(await vocabulary.GetAllAsync(ct), term, type);

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

            void Add(Func<TWord, string?> foundTerm) =>
                found.AddRange(list.Select(w => (Word: w, Found: foundTerm(w))).Where(x => x.Found != null).Select(x => new SearchWord(x.Found!, x.Word)));

            if (type is ESearchType.Japanese or ESearchType.General)
            {
                Add(w => w.FirstKanjiWordContaining(term));
                Add(w => w.Kana != null && w.Kana.Contains(term) ? w.Kana : null);
                Add(w => w.Romaji != null && w.Romaji.Contains(term) ? w.Romaji : null);
            }

            if (type is ESearchType.Portuguese or ESearchType.General)
                Add(w => w.GetAllTranslatedWords(null).FirstOrDefault(p => p.Contains(term)));

            var comparer = StringComparer.InvariantCulture;
            return new SearchResult(term,
            [
                .. found.Where(r => r.FoundTerm == term),
                .. found.Where(r => r.FoundTerm.StartsWith(term, StringComparison.Ordinal) && r.FoundTerm != term).OrderBy(r => r.FoundTerm, comparer),
                .. found.Where(r => r.FoundTerm.IndexOf(term, StringComparison.Ordinal) > 0).OrderBy(r => r.FoundTerm, comparer),
            ]);
        }
    }
}
