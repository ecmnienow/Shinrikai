using Nienow.Shinrikai.Core;

namespace Nienow.Shinrikai.Text.Model
{
    public class TText
    {
        #region Constructors

        public TText()
        {
            this.Index = 0;
            this.Title = string.Empty;
            this.Level = EJLPT.None;
            this.Body = string.Empty;
        }

        public TText(int index, string title, EJLPT level, string body)
            : this()
        {
            this.Index = index;
            this.Title = title;
            this.Level = level;
            this.Body = body;
        }

        #endregion

        #region Attributes and Properties

        public int Index { get; set; }
        public string Title { get; set; }
        public EJLPT Level { get; set; }
        public string Body { get; set; }

        #endregion
    }
}
