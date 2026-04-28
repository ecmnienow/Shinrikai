using System.Collections.Generic;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Grammar.DB;
using Nienow.Shinrikai.Grammar.Model;
using System.Linq;
using Nienow.DataBaseConn;

namespace Nienow.Shinrikai.Grammar
{
    public class GrammarManager : ManagerCore
    {
        public GrammarManager(DBConnectionConfig dbConnectionConfig)
        {
            this._db = new DBGrammar(dbConnectionConfig);
        }

        #region Private Fields

        private readonly DBGrammar _db;

        #endregion

        #region Public Methods

        public void AddGrammar(int id, string title, string level, string body, int[] requisites)
        {
            using (this._db.Connect())
                if (id == 0)
                    this._db.AddGrammar(title, level, body, requisites);
                else
                    this._db.UpdateGrammar(id, title, level, body, requisites);
        }

        public void DeclareKnowledge(int userId, int grammarId)
        {
            using (this._db.Connect())
                this._db.DeclareKnowledge(userId, grammarId);
        }

        public TGrammar GetGrammar(int id)
        {
            using (this._db.Connect())
                return this._db.GetGrammar(id);
        }

        public Dictionary<int, string> ListAllGrammars(EJLPT? jlpt)
        {
            using (this._db.Connect())
                return this._db.ListGrammars(jlpt);
        }

        public List<List<GGrammar>> ListGrammars(int userId, EJLPT userLevel)
        {
            var listedGrammars = new List<GGrammar>();

            using (this._db.Connect())
            {
                var grammars = this._db.ListGrammars();

                foreach (var grammar in grammars)
                {
                    var grammarInfo = this._db.GetGrammar(grammar.Key);
                    var grammarItem = new GGrammar() { Grammar = grammarInfo };
                    grammarItem.HasEnoughLevel = this.HasEnoughLevel(userLevel, grammarInfo.Level, true) ||
                                                 (this._db.HasKnowledge(userId, grammar.Key) &&
                                                 this.HasEnoughLevel(userLevel, grammarInfo.Level));
                    grammarItem.HasExercises = this._db.HasExercises(grammar.Key);

                    listedGrammars.Add(grammarItem);
                }

                return listedGrammars.GroupBy(a => a.Grammar.Level, (key, g) => g.OrderByDescending(c => c.HasEnoughLevel).ToList()).ToList();
            }
        }

        #endregion
    }
}
