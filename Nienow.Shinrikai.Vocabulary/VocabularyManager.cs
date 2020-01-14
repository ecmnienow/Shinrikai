using System;
using System.Collections.Generic;
using System.Linq;
using Nienow.Helper;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Vocabulary.DB;
using Nienow.Shinrikai.Vocabulary.Model;

namespace Nienow.Shinrikai.Vocabulary
{
    public class VocabularyManager : ManagerCore
    {
        #region Attributes and Properties

        private List<TWord> _vocabularyList;
        public List<TWord> VocabularyList
        {
            get
            {
                if (this._vocabularyList == null)
                    using (this._db.Connect())
                        //Neste momento a ID do usuário é irrelevante, por isso 0
                        this._vocabularyList = this._db.GetVocabulary(0).ToList();

                return this._vocabularyList;
            }
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
                float pages = this.ExerciseVocabularyList.Count / 10f;
                this._totalPages = (int)pages;

                if (pages % 1f != 0)
                    this._totalPages++;

                return this._totalPages;
            }
        }

        private readonly List<List<TWord>> _currentVocabularyList = new List<List<TWord>>();
        private List<List<TWord>> CurrentVocabularyList
        {
            get { return this._currentVocabularyList; }
        }

        private readonly List<TWord> _exerciseVocabularyList = new List<TWord>();
        private List<TWord> ExerciseVocabularyList
        {
            get { return this._exerciseVocabularyList; }
        }

        #endregion

        #region Private Fields

        private readonly DBVocabulary _db = new DBVocabulary();

        public bool _allCorrect = true;

        private string _currentSubject = string.Empty;

        private int _lastRandomNumber;

        #endregion

        #region Public Methods

        //Working

        /// <summary>
        /// Faz o cadastro de um novo item de vocabulário no banco
        /// </summary>
        /// <param name="word">Objeto contendo as informações para cadastro do item de vocabulário</param>
        public void AddWord(TWord word)
        {
            using (this._db.Connect())
                this._db.AddWord(word);
        }

        /// <summary>
        /// Verifica se as respostas estão corretas
        /// </summary>
        /// <param name="userId">ID do usuário logado</param>
        /// <param name="answers">Lista de respostas do usuário</param>
        /// <param name="toJapanese">Se português para japonês ou não</param>
        /// <returns>Em caso de erros, retorna uma lista com as respostas corretas</returns>
        public List<GWordAnswer> CheckAnswers(int userId, List<GWordAnswer> answers, bool toJapanese)
        {
            var current = this.CurrentVocabularyList[this.CurrentPage - 1];
            var results = new List<GWordAnswer>();
            var count = 0;

            foreach (var a in answers)
            {
                var answer = a.Answer;
                var word = current[count];

                using (this._db.Connect())
                    if (!string.IsNullOrWhiteSpace(answer) && (word.CheckTranslatedWord(this._currentSubject, answer, toJapanese)))
                    { //A.Intersect(B).Any();
                        results.Add(new GWordAnswer() { Word = a.Word, Answer = answer });
                        this.ManageMistake(userId, word, true);
                    }
                    else
                    {
                        var words = toJapanese ? new string[] { word.Kana } : word.GetAllTranslatedWords(this._currentSubject);
                        results.Add(new GWordAnswer() { Word = a.Word, Answer = answer + " (" + string.Join(", ", words) + ")", Wrong = true });
                        this.ManageMistake(userId, word, false);
                    }

                count++;
            }

            return results;
        }

        /// <summary>
        /// Carrega a lista de vocabulário selecionada
        /// </summary>
        /// <param name="subject">Assunto do vocabulário</param>
        /// <param name="mistakes">Indica que é para trazer apenas o vocabulário que o usuário possui dificuldades</param>
        public void GenerateVocabulary(int userId, string subject, bool mistakes)
        {
            //Guarda o assunto atual
            this._currentSubject = subject;

            //Limpa as listas atuais
            this.CurrentVocabularyList.Clear();

            var remainVocabularyList = new List<TWord>();
            remainVocabularyList.AddRange(this.StartLoadingVocabulary(userId, subject, mistakes));


            //Define a página atual
            this.CurrentPage = 0;

            while (true)
            {
                var tempVocabularyList = new List<TWord>();

                for (int i = 0; i < 10; i++)
                {
                    if (remainVocabularyList.Count == 0)
                        break;

                    int index = this.RandomNumber(0, remainVocabularyList.Count, ref this._lastRandomNumber);

                    tempVocabularyList.Add(remainVocabularyList[index]);
                    remainVocabularyList.RemoveAt(index);
                }

                this.CurrentVocabularyList.Add(tempVocabularyList);

                if (remainVocabularyList.Count == 0)
                    break;
            }
        }

        public TWord GetWord(int id)
        {
            using (this._db.Connect())
                return this._db.GetWord(id);
        }

        public TWord GetWord(string word)
        {
            using (this._db.Connect())
                return this._db.GetWord(word);
        }

        /// <summary>
        /// Carrega a lista seguinte do vocabulário selecionado
        /// </summary>
        /// <param name="toJapanese">True se for português para japonês, caso contrário, false</param>
        public List<string> NextPage(bool toJapanese)
        {
            if (string.IsNullOrEmpty(this._currentSubject))
                return null;

            this.CurrentPage++;

            var list = new List<string>();
            if (toJapanese)
                foreach (var word in this.CurrentVocabularyList[this.CurrentPage - 1])
                {
                    string[] words = word.GetAllTranslatedWords(this._currentSubject);
                    int randomNumber = this.RandomNumber(0, words.Length, ref this._lastRandomNumber);
                    list.Add(words[randomNumber]);
                }
            else
                foreach (var word in this.CurrentVocabularyList[this.CurrentPage - 1])
                    list.Add(word.Kana);

            return list;
        }

        /// <summary>
        /// Carrega a lista anterior do vocabulário selecionado
        /// </summary>
        /// <param name="toJapanese">True se for português para japonês, caso contrário, false</param>
        public List<string> PreviousPage(bool toJapanese)
        {
            if (string.IsNullOrEmpty(this._currentSubject))
                return null;

            this.CurrentPage--;

            var list = new List<string>();
            if (toJapanese)
                foreach (var word in this.CurrentVocabularyList[this.CurrentPage - 1])
                {
                    string[] words = word.GetAllTranslatedWords(this._currentSubject);
                    int randomNumber = this.RandomNumber(0, words.Length, ref this._lastRandomNumber);
                    list.Add(words[randomNumber]);
                }
            else
                foreach (var word in this.CurrentVocabularyList[this.CurrentPage - 1])
                    list.Add(word.Kana);

            return list;
        }

        /// <summary>
        /// Reordena a lista atualmente exibida
        /// </summary>
        /// <param name="toJapanese">True se for português para japonês, caso contrário, false</param>
        public List<string> ReloadVocabulary(bool toJapanese)
        {
            var current = this.CurrentVocabularyList[this.CurrentPage - 1];

            //Faz uma cópia da lista atual
            var tempList = new List<TWord>();
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

            var list = new List<string>();
            if (toJapanese)
                foreach (var word in current)
                {
                    string[] words = word.GetAllTranslatedWords(this._currentSubject);
                    int randomNumber = this.RandomNumber(0, words.Length, ref this._lastRandomNumber);
                    list.Add(words[randomNumber]);
                }
            else
                foreach (var word in current)
                    list.Add(word.Kana);

            return list;
        }

        /// <summary>
        /// Identifica a página atual e o total de páginas
        /// </summary>
        public string ShowPageStatus()
        {
            //Contém páginas de vocabulário
            if (this.CurrentVocabularyList.Count == 0)
                return string.Empty;

            //A primeira páginna de vocabulário não pode estar vazia
            if (this.CurrentVocabularyList[0].Count == 0)
                return string.Empty;

            return string.Format("{0} / {1}", this.CurrentPage, this.TotalPages);
        }

        /// <summary>
        /// Retorna a lista de assuntos
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, string> Subjects()
        {
            using (this._db.Connect())
                return this._db.GetSubjects();
        }

        public void UpdateWord(TWord word)
        {
            using (this._db.Connect())
                this._db.UpdateWord(word);
        }

        #endregion

        #region Private Methods

        private void ManageMistake(int userId, TWord word, bool result)
        {
            var monitor = this._db.GetMonitor(userId, word.ID);

            if (monitor == null)
                monitor = new Monitor() { UsrID = userId, VcbID = word.ID, };

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

        private List<TWord> StartLoadingVocabulary(int userId, string subject, bool mistakes)
        {
            this.ExerciseVocabularyList.Clear();

            if (mistakes)
                using (this._db.Connect())
                {
                    var vocId = this._db.GetSubjects().FirstOrDefault(s => s.Value == subject).Key;
                    this.ExerciseVocabularyList.AddRange(this._db.GetVocabulary(userId, vocId, true));
                }
            else
                this.ExerciseVocabularyList.AddRange(this.VocabularyList
                    .Where(w => w.Subject1 == subject || w.Subject2 == subject || w.Subject3 == subject)
                    .ToList());

            return this.ExerciseVocabularyList;
        }

        #endregion
    }
}
