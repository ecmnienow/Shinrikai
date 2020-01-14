using System.Linq;

namespace Nienow.Shinrikai.Kanji.Model
{
    public class TKanji
    {
        #region Constructors

        public TKanji() { }

        public TKanji(int id, string kanjiLetter, string kana, string romaji, string info, string jlpt, byte[] image)
            : this()
        {
            this.ID = id;
            this.KanjiLetter = kanjiLetter;
            this.Kana = kana;
            this.Romaji = romaji;
            this.Image = image;
            this.Info = info;
            this.JLPT = jlpt;
        }

        #endregion

        #region Attributes and Properties

        public int ID { get; set; }
        public string KanjiLetter { get; set; }
        public string Kana { get; set; }
        public string Romaji { get; set; }
        public byte[] Image { get; set; }
        public string Info { get; set; }
        public string JLPT { get; set; }
        public bool HasImage { get; set; }

        public string[] KanaReadingForms
        {
            get { return this.Kana.Split(','); }
        }

        #endregion

        #region Public Methods

        public int CheckReading(params string[] readingForms)
        {
            var resultsArray = new int[] { 1, 2, 4 };

            int results = 0;

            for (int i = 0; i < readingForms.Length; i++)
            {
                var text = readingForms[i];

                if (string.IsNullOrEmpty(text))
                    continue;

                if (readingForms.Where(t => t == text).Count() > 1)
                    continue;

                if (this.KanaReadingForms.FirstOrDefault(o => o == text) != null)
                    results += resultsArray[i];
            }

            return results;
        }

        #endregion
    }
}
