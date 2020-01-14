using Nienow.Shinrikai.Core;

namespace Nienow.Shinrikai.Grammar.Model
{
    public class TGrammar
    {
        #region Constructors

        public TGrammar()
        {
            this.Index = 0;
            this.Title = string.Empty;
            this.Level = EJLPT.None;
            this.Body = string.Empty;
            this.Requisites = null;
        }

        public TGrammar(int index, string title, EJLPT level, string body)
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
        
        public string Body { get; set; }
        public EJLPT Level { get; set; }
        public int[] Requisites { get; set; }
        public string Title { get; set; }

        #endregion
    }
}
