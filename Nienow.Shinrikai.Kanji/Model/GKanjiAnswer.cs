using System.Collections.Generic;

namespace Nienow.Shinrikai.Kanji.Model
{
    public class GKanjiAnswer
    {
        #region Constructors

        public GKanjiAnswer()
        {
        }

        public GKanjiAnswer(TKanji kanji)
        {
            this.Kanji = kanji.KanjiLetter;
            this.Answers = new List<string>();

            int count = 0;
            foreach(var answer in kanji.KanaReadingForms)
            {
                if (count == 3)
                    return;

                this.Answers.Add(string.Empty);
                count++;
            }
        }

        #endregion

        #region Attributes and Properties

        public List<string> Answers { get; set; }
        public string Kanji { get; set; }
        public bool Wrong { get; set; }

        #endregion
    }
}
