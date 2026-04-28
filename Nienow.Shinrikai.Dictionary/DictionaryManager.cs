using Nienow.Shinrikai.Core;
using System.Collections.Generic;
using System.Linq;
using Nienow.Shinrikai.Dictionary.DB;
using Nienow.Shinrikai.Dictionary.Models;
using Nienow.Shinrikai.Vocabulary;
using Nienow.DataBaseConn;

namespace Nienow.Shinrikai.Dictionary
{
    public class DictionaryManager : ManagerCore
    {
        public DictionaryManager(DBConnectionConfig dbConnectionConfig)
        {
            this._db = new DBDictionary(dbConnectionConfig);
            this._dbConnectionConfig = dbConnectionConfig;
        }

        #region Private Fields

        private readonly DBDictionary _db;
        private readonly DBConnectionConfig _dbConnectionConfig;

        #endregion

        #region Private Methods

        private List<GSearchWord> SearchRelatedWords(ESearchType searchType, string text)
        {
            //var kanjiManager = new KanjiManager();
            var vocManager = new VocabularyManager(this._dbConnectionConfig);

            var words = new List<GSearchWord>();

            //Vocabulario simples (kanji word)
            if (searchType == ESearchType.Japanese || searchType == ESearchType.General)
                words.AddRange(vocManager.VocabularyList
                    .Where(w => w.ContainsKanjiWord(text))
                    .Select(w => new GSearchWord()
                                     {
                                         FoundTerm = (w.KanjiWord1.Contains(text) ? w.KanjiWord1 : (w.KanjiWord2.Contains(text) ? w.KanjiWord2 : w.KanjiWord3)),
                                         Word = w
                                     })
                    .ToList());

            //Vocabulario simples (kana)
            if (searchType == ESearchType.Japanese || searchType == ESearchType.General)
                words.AddRange(vocManager.VocabularyList
                    .Where(w => w.Kana.Contains(text))
                    .Select(w => new GSearchWord() { FoundTerm = w.Kana, Word = w })
                    .ToList());

            //Vocabulario simples (romaji)
            if (searchType == ESearchType.Japanese || searchType == ESearchType.General)
                words.AddRange(vocManager.VocabularyList
                    .Where(w => w.Romaji.Contains(text))
                    .Select(w => new GSearchWord() { FoundTerm = w.Romaji, Word = w })
                    .ToList());

            //Vocabulario simples (português)
            //VER Vai dar problema quando uma mesma palavra possuir mais de 1 resultado
            if (searchType == ESearchType.Portuguese || searchType == ESearchType.General)
                words.AddRange(vocManager.VocabularyList
                    .Where(w => w.GetAllTranslatedWords(null).Any(p => p.Contains(text)))
                    .Select(w =>
                        new GSearchWord()
                                     {
                                         FoundTerm = w.GetAllTranslatedWords(null).FirstOrDefault(p => p.Contains(text)),
                                         Word = w
                                     })
                    .ToList());

            return words;
        }

        #endregion

        #region Public Methods

        public byte[] GetAudio(int audioId)
        {
            using (this._db.Connect())
                return this._db.GetAudio(audioId);
        }

        public byte[] GetImage(int imageId)
        {
            using (this._db.Connect())
                return this._db.GetImage(imageId);
        }

        public GResult SearchWord(string term, ESearchType searchType)
        {
            var response = new GResult() { Term = term };
            response.Results = this.SearchRelatedWords(searchType, term);

            return response;
        }

        #endregion
    }
}
