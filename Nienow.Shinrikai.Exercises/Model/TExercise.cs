using Nienow.Shinrikai.Core;

namespace Nienow.Shinrikai.Exercises.Model
{
    public class TExercise
    {
        #region Constructors

        public TExercise()
        {
            this.ID = 0;
            this.GrammarId = 0;
            this.TextId = 0;
            this.Question = string.Empty;
            this.Option1 = string.Empty;
            this.Option2 = string.Empty;
            this.Option3 = string.Empty;
            this.Option4 = string.Empty;
            this.RightAnswer = 0;
            this.Level = EJLPT.None;
        }

        public TExercise(int index, int grammarId, int textId, string text, string option1, string option2, string option3,
            string option4, int rightAnswer, EJLPT jlpt)
            : this()
        {
            this.ID = index;
            this.GrammarId = grammarId;
            this.TextId = textId;
            this.Question = text;
            this.Option1 = option1;
            this.Option2 = option2;
            this.Option3 = option3;
            this.Option4 = option4;
            this.RightAnswer = rightAnswer;
            this.Level = jlpt;
        }

        #endregion

        #region Attributes and Properties

        public int ID { get; set; }
        public int GrammarId { get; set; }
        public int TextId { get; set; }
        public string Question { get; set; }
        public string Option1 { get; set; }
        public string Option2 { get; set; }
        public string Option3 { get; set; }
        public string Option4 { get; set; }
        public int RightAnswer { get; set; }
        public EJLPT Level { get; set; }

        #endregion
    }
}
