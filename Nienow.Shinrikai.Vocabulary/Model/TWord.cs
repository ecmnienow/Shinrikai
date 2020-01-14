using System;
using System.Collections.Generic;
using System.Linq;

namespace Nienow.Shinrikai.Vocabulary.Model
{
    public class TWord
    {
        #region Attributes and Properties

        public int ID { get; set; }

        public string Subject1 { get; set; }
        public string Subject2 { get; set; }
        public string Subject3 { get; set; }
        public int SubjectID1 { get; set; }
        public int SubjectID2 { get; set; }
        public int SubjectID3 { get; set; }

        public string Kana { get; set; }
        public string Romaji { get; set; }
        public string Word1 { get; set; }
        public string Word2 { get; set; }
        public string Word3 { get; set; }
        public string KanjiWord1 { get; set; }
        public string KanjiWord2 { get; set; }
        public string KanjiWord3 { get; set; }
        public string Info1 { get; set; }
        public string Info2 { get; set; }
        public string Info3 { get; set; }
        
        public bool HasImage1 { get; set; }
        public bool HasImage2 { get; set; }
        public bool HasImage3 { get; set; }
        public byte[] Image1 { get; set; }
        public byte[] Image2 { get; set; }
        public byte[] Image3 { get; set; }
        
        public bool HasAudio1 { get; set; }
        public bool HasAudio2 { get; set; }
        public bool HasAudio3 { get; set; }
        public byte[] Audio1 { get; set; }
        public byte[] Audio2 { get; set; }
        public byte[] Audio3 { get; set; }

        #endregion

        #region Public Methods

        public bool CheckTranscriptedWord(string word)
        {
            if (word.Equals(this.Kana))
                return true;

            return false;
        }

        public bool CheckTranslatedWord(string subject, string word, bool toJapanese)
        {
            if (toJapanese)
            {
                if (word.Equals(this.Kana))
                    return true;
            }
            else
            {
                var splitedWords = this.GetAllTranslatedWords(subject);
                foreach (string option in splitedWords)
                {
                    if (string.IsNullOrEmpty(option))
                        continue;

                    if (word.Equals(option))
                        return true;
                }
            }

            return false;
        }

        public bool ContainsKanjiWord(string text)
        {
            if(!string.IsNullOrEmpty(this.KanjiWord1) && KanjiWord1.Contains(text))
                return true;
            
            if(!string.IsNullOrEmpty(this.KanjiWord2) && KanjiWord2.Contains(text))
                return true;
            
            if(!string.IsNullOrEmpty(this.KanjiWord3) && KanjiWord3.Contains(text))
                return true;

            return false;
        }

        public string[] GetAllKanjiWords()
        {
            var kanjiWords = new List<string>();

            if (this.KanjiWord1 != null)
                kanjiWords.Add(this.KanjiWord1);

            if (this.KanjiWord2 != null && !kanjiWords.Any(w => w == this.KanjiWord2))
                kanjiWords.Add(this.KanjiWord2);

            if (this.KanjiWord3 != null && !kanjiWords.Any(w => w == this.KanjiWord3))
                kanjiWords.Add(this.KanjiWord3);

            return kanjiWords.ToArray();
        }

        public string[] GetAllKanjiWords(string subject)
        {
            if (string.IsNullOrEmpty(subject))
            {
                var words = new List<string>();

                if (!string.IsNullOrEmpty(this.KanjiWord1))
                    words.Add(this.KanjiWord1);

                if (!string.IsNullOrEmpty(this.KanjiWord2))
                    words.Add(this.KanjiWord2);

                if (!string.IsNullOrEmpty(this.KanjiWord3))
                    words.Add(this.KanjiWord3);

                return words.ToArray();
            }

            if (this.Subject1 == subject)
                return new string[] { this.KanjiWord1 };

            if (this.Subject2 == subject)
                return new string[] { this.KanjiWord2 };

            if (this.Subject3 == subject)
                return new string[] { this.KanjiWord3 };

            return null;
        }

        public string[] GetAllTranslatedWords(string subject)
        {
            if (string.IsNullOrEmpty(subject))
            {
                var words = new List<string>();

                if (!string.IsNullOrEmpty(this.Word1))
                    words.AddRange(this.Word1.Split(','));

                if (!string.IsNullOrEmpty(this.Word2))
                    words.AddRange(this.Word2.Split(','));

                if (!string.IsNullOrEmpty(this.Word3))
                    words.AddRange(this.Word3.Split(','));

                return words.ToArray();
            }

            if (this.Subject1 == subject)
                return this.Word1.Split(',');

            if (this.Subject2 == subject)
                return this.Word2.Split(',');

            if (this.Subject3 == subject)
                return this.Word3.Split(',');

            return null;
        }

        #endregion
    }
}
