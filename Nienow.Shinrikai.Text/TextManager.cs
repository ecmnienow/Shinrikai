using System.Collections.Generic;
using Nienow.Helper;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Text.DB;
using Nienow.Shinrikai.Text.Model;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;

namespace Nienow.Shinrikai.Text
{
    public class TextManager : ManagerCore
    {
        #region Private Fields

        private readonly DBText _db = new DBText();

        #endregion

        #region Private Methods

        public string RemoveInvalidElements(string text)
        {
            text = text.Replace("<ruby></ruby>", "");
            text = text.Replace("<ruby><rb></rb></ruby>", "");
            text = text.Replace("<ruby><rt></rt></ruby>", "");

            return text;
        }

        #endregion

        #region Public Methods

        public void AddText(string title, string level, string body)
        {
            body = this.RemoveInvalidElements(body);

            using (this._db.Connect())
                this._db.AddText(title, level, body);
        }

        public void DeclareKnowledge(int userId, int textId)
        {
            using (this._db.Connect())
                this._db.DeclareKnowledge(userId, textId);
        }

        public TText GetText(int id)
        {
            TText text;
            using (this._db.Connect())
                text = this._db.GetText(id);

            var regex = new Regex(@"([一-龠]{1,}「[ぁ-ゔァ-ヴー々〆〤]{1,}[」]{1})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline);

            foreach (Match match in regex.Matches(text.Body))
            {
                var value = match.Groups[1].Value;

                var ruby = new StringBuilder();
                ruby.Append("<ruby>");

                var start1 = 0;
                var end1 = value.IndexOf('「');
                ruby.Append("<rb>" + value.Substring(start1, end1) + "</rb>");

                var start2 = value.IndexOf('「') + 1;
                var end2 = (value.Length - start2) - 1;
                ruby.Append("<rt>" + value.Substring(start2, end2) + "</rt>");

                ruby.Append("</ruby>");

                text.Body = text.Body.Replace(match.Groups[1].Value, ruby.ToString()); ;
            }

            return text;
        }

        public List<List<GText>> ListTexts(int userId, EJLPT userLevel)
        {
            var listedTexts = new List<GText>();

            using (this._db.Connect())
            {
                var texts = this._db.ListTexts();

                foreach (var text in texts)
                {
                    var textInfo = this._db.GetText(text.Key);

                    if (textInfo.Level == EJLPT.None)
                        textInfo.Level = EJLPT.N5;

                    var textItem = new GText() { Text = textInfo };
                    textItem.HasEnoughLevel = this.HasEnoughLevel(userLevel, textInfo.Level);
                    textItem.HasExercises = this._db.HasExercises(text.Key);

                    listedTexts.Add(textItem);
                }

                return listedTexts.GroupBy(a => a.Text.Level, (key, g) => g.OrderByDescending(c => c.HasEnoughLevel).ToList()).ToList();
            }
        }

        public void RegisterLackOfKnowledge(int usrId, int wordId)
        {
            using (this._db.Connect())
            {
                //Pega ou cria o monitor
                var monitor = this._db.GetMonitor(usrId, wordId);
                if (monitor == null)
                    monitor = new Monitor() { UsrID = usrId, VcbID = wordId, };

                if (monitor.Counter == 0)
                    monitor.FailCounter++;

                monitor.Counter = 10;

                if (monitor.ID == -1)
                    this._db.AddMonitor(monitor);
                else
                    this._db.UpdateMonitor(monitor);
            }
        }

        public bool UnknownStatus(int userId, int wordId)
        {
            using (this._db.Connect())
                return this._db.UnknownStatus(userId, wordId);
        }

        public void UpdateText(int id, string title, string level, string body)
        {
            body = this.RemoveInvalidElements(body);

            using (this._db.Connect())
                this._db.UpdateText(id, title, level, body);
        }

        public bool UserHasWordKnowledge(int userId, int wordId)
        {
            using (this._db.Connect())
                return this._db.UserHasWordKnowledge(userId, wordId);
        }

        #endregion
    }
}
