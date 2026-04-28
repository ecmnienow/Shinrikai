using System;
using Nienow.Helper;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Kanji.DB;
using Nienow.Shinrikai.Kanji.Model;
using System.Linq;
using System.Collections.Generic;
using Nienow.DataBaseConn;

namespace Nienow.Shinrikai.Kanji
{
    public class KanjiManager : ManagerCore
    {
        public KanjiManager(DBConnectionConfig dbConnectionConfig)
        {
            this._db = new DBKanji(dbConnectionConfig);
        }

        #region Attributes and Properties

        private List<TKanji> _kanjiList;
        public List<TKanji> KanjiList
        {
            get
            {
                if (this._kanjiList == null)
                    using(this._db.Connect())
                        //Neste momento a ID do usuário é irrelevante, por isso 0
                        this._kanjiList = this._db.GetKanjis(0).ToList();

                return this._kanjiList;
            }
            private set { this._kanjiList = value; }
        }

        private List<List<TKanji>> _currentKanjiList = new List<List<TKanji>>();
        public List<List<TKanji>> CurrentKanjiList
        {
            get { return this._currentKanjiList; }
            private set { this._currentKanjiList = value; }
        }

        private int _currentPage = 0;
        public int CurrentPage
        {
            get { return this._currentPage; }
            set
            {
                this._currentPage = value;

                if (this._currentPage > this.TotalPages)
                    this._currentPage = 1;
            }
        }

        private int _totalPages;
        public int TotalPages
        {
            get
            {
                float pages = this.CurrentKanjiList.Count;
                this._totalPages = (int)pages;

                if (pages % 1f != 0)
                    this._totalPages++;

                return this._totalPages;
            }
        }

        #endregion

        #region Private Fields

        private readonly DBKanji _db;
        private string _currentLevel = string.Empty;

        private int _lastRandomNumber;

        #endregion

        #region Private Methods

        private void ManageMistake(int usrId, TKanji kanji, bool result)
        {
            using (this._db.Connect())
            {
                var monitor = this._db.GetMonitor(usrId, kanji.ID);

                if (monitor == null)
                    monitor = new Monitor() { UsrID = usrId, VcbID = kanji.ID, };

                if (result)
                    if (monitor.Counter <= 1) //Pois o 1 passará a ser 0
                    {
                        monitor.Counter = 0;
                        monitor.SuccessCounter++;
                        monitor.LastHittedTime = DateTime.Now;
                    }
                    else
                        monitor.Counter--;

                if (!result)
                {
                    if (monitor.Counter <= 0)
                        monitor.FailCounter++;

                    monitor.Counter = 10;
                }

                if (monitor.ID == -1)
                    this._db.AddMonitor(monitor);
                else
                    this._db.UpdateMonitor(monitor);
            }
        }

        private List<TKanji> StartLoadingKanjis(int userId, string level, bool mistakes)
        {
            var kanjisList = new List<TKanji>();

            if (mistakes)
                using (this._db.Connect())
                    kanjisList.AddRange(this._db.GetKanjis(userId, level, true));
            else
            {
                if (level == "N5")
                    kanjisList.AddRange(this.KanjiList
                        .Where(w => w.JLPT == "N5")
                        .ToList());

                if (level == "N4")
                    kanjisList.AddRange(this.KanjiList
                        .Where(w => w.JLPT == "N5" || w.JLPT == "N4")
                        .ToList());

                if (level == "N3")
                    kanjisList.AddRange(this.KanjiList
                        .Where(w => w.JLPT == "N5" || w.JLPT == "N4" || w.JLPT == "N3")
                        .ToList());

                if (level == "N2")
                    kanjisList.AddRange(this.KanjiList
                        .Where(w => w.JLPT == "N5" || w.JLPT == "N4" || w.JLPT == "N3" || w.JLPT == "N2")
                        .ToList());

                if (level == "N1")
                    kanjisList.AddRange(this.KanjiList);
            }

            return kanjisList;
        }

        #endregion

        #region Public Methods

        public void AddKanji(TKanji kanji)
        {
            using (this._db.Connect())
                this._db.AddKanji(kanji);
        }

        /// <summary>
        /// Verifica se as respostas estão corretas
        /// </summary>
        /// <param name="answers">Lista de respostas do usuário</param>
        /// <returns>Em caso de erros, retorna uma lista com as respostas corretas</returns>
        public List<GKanjiAnswer> CheckAnswers(int usrId, List<GKanjiAnswer> answers)
        {
            var current = this.CurrentKanjiList[this.CurrentPage - 1];
            var results = new List<GKanjiAnswer>();
            var count = 0;

            foreach (var answer in answers)
            {
                var result = new GKanjiAnswer() { Kanji = answer.Kanji, Answers = new List<string>() };
                results.Add(result);

                var kanji = current[count];
                var checkResults = kanji.CheckReading(answer.Answers.ToArray());

                if ((checkResults & 1) != 1)
                {
                    result.Answers.Add(string.Format("{0} ( {1} )", answer.Answers[0], string.Join(", ", kanji.KanaReadingForms)));
                    result.Wrong = true;
                }
                else
                    result.Answers.Add(answer.Answers[0]);

                if (answer.Answers.Count >= 2)
                    if ((checkResults & 2) != 2)
                    {
                        result.Answers.Add(string.Format("{0} ( {1} )", answer.Answers[1], string.Join(", ", kanji.KanaReadingForms)));
                        result.Wrong = true;
                    }
                    else
                        result.Answers.Add(answer.Answers[1]);

                if (answer.Answers.Count >= 3)
                    if ((checkResults & 4) != 4)
                    {
                        result.Answers.Add(string.Format("{0} ( {1} )", answer.Answers[2], string.Join(", ", kanji.KanaReadingForms)));
                        result.Wrong = true;
                    }
                    else
                        result.Answers.Add(answer.Answers[2]);

                if(result.Wrong)
                    this.ManageMistake(usrId, kanji, false);
                else
                    this.ManageMistake(usrId, kanji, true);

                count++;
            }

            return results;
        }

        /// <summary>
        /// Carrega a lista de kanjis selecionada
        /// </summary>
        /// <param name="level">Nível do JLPT em que aperecem os kanjis</param>
        public void GenerateLists(int userId, string level, bool mistakes)
        {
            //Guarda o nível atual
            this._currentLevel = level;

            //Limpa as listas atuais
            this.CurrentKanjiList.Clear();

            var remainKanjiList = new List<TKanji>();
            remainKanjiList.AddRange(this.StartLoadingKanjis(userId, level, mistakes));


            //Define a página atual
            this.CurrentPage = 0;

            while (true)
            {
                var tempVocabularyList = new List<TKanji>();

                for (int i = 0; i < 10; i++)
                {
                    if (remainKanjiList.Count == 0)
                        break;

                    int index = this.RandomNumber(0, remainKanjiList.Count, ref this._lastRandomNumber);

                    tempVocabularyList.Add(remainKanjiList[index]);
                    remainKanjiList.RemoveAt(index);
                }

                this.CurrentKanjiList.Add(tempVocabularyList);

                if (remainKanjiList.Count == 0)
                    break;
            }
        }

        public TKanji GetKanji(string kanjiLetter)
        {
            using (this._db.Connect())
                return this._db.GetKanji(kanjiLetter);
        }

        /// <summary>
        /// Carrega a lista seguinte da lista de kanjis selecionada
        /// </summary>
        public List<TKanji> NextPage()
        {
            if (string.IsNullOrEmpty(this._currentLevel))
                return null;

            this.CurrentPage++;

            return this.CurrentKanjiList[this.CurrentPage - 1];
        }

        /// <summary>
        /// Carrega a lista anterior da lista de kanjis selecionada
        /// </summary>
        public List<TKanji> PreviousPage()
        {
            if (string.IsNullOrEmpty(this._currentLevel))
                return null;

            this.CurrentPage--;

            return this.CurrentKanjiList[this.CurrentPage - 1];
        }

        /// <summary>
        /// Reordena a lista atualmente exibida
        /// </summary>
        public List<TKanji> ReloadList()
        {
            var current = this.CurrentKanjiList[this.CurrentPage - 1];

            //Faz uma cópia da lista atual
            var tempList = new List<TKanji>();
            tempList.AddRange(current);

            //Limpa a lista atual
            current.Clear();

            for (int i = 0; i < 10; i++)
            {
                if (tempList.Count == 0)
                    break;

                int index = this.RandomNumber(0, tempList.Count, ref this._lastRandomNumber);

                current.Add(tempList[index]);
                tempList.RemoveAt(index);
            }

            return current;
        }

        /// <summary>
        /// Identifica a página atual e o total de páginas
        /// </summary>
        public string ShowPageStatus()
        {
            if (this.CurrentKanjiList.Count == 0)
                return string.Empty;

            return string.Format("{0} / {1}", this.CurrentPage, this.TotalPages);
        }

        public void UpdateKanji(TKanji kanji)
        {
            using (this._db.Connect())
                this._db.UpdateKanji(kanji);
        }

        #endregion
    }
}
