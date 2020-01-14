using Nienow.DataBaseConn;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Grammar.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Nienow.Shinrikai.Grammar.DB
{
    public class DBGrammar : IDisposable
    {
        #region Constructors

        public DBGrammar()
        {
            this._db = new ShinrikaiDB();
        }

        #endregion

        #region Private Fields

        private readonly ShinrikaiDB _db;

        #endregion

        #region Public Methods

        public void AddGrammar(string title, string level, string body, int[] requisites)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("INSERT INTO GRAMMAR (GRM_ID, GRM_TITLE, GRM_LEVEL, GRM_BODY, GRM_REQUISITES)");
            sql.AppendLine("VALUES (GEN_ID(GRAMMAR_ID_GEN, 1), @TTL, @LVL, @BODY, @REQ)");

            cmm.CommandText = sql.ToString();

            this._db.NewParameter(cmm, "@TTL", DbType.String).Value = title;
            this._db.NewParameter(cmm, "@LVL", DbType.String).Value = (level == "none" ? DBNull.Value : (object)level);
            this._db.NewParameter(cmm, "@BODY", DbType.String).Value = body;
            this._db.NewParameter(cmm, "@REQ", DbType.String).Value =
                (requisites != null && requisites.Length > 0 ? (object)string.Join(";", requisites.Select(r => r.ToString("000"))) : DBNull.Value);

            try
            {
                cmm.ExecuteNonQuery();
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
            finally
            {
                cmm.Parameters.Clear();
                cmm.Transaction = null;
            }
        }

        public IDisposable Connect()
        {
            this._db.Connect();
            return this;
        }

        public void DeclareKnowledge(int userId, int grammarId)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            //Verifica se já conhece
            cmm.CommandText = "SELECT GMN_ID FROM GRA_MONITOR WHERE GMN_GRM_ID = " + grammarId + " AND GMN_USR_ID = " + userId;

            using (var dLe = cmm.ExecuteReader())
                //Se conhece, apenas atualiza o horário do último acesso
                if (dLe.Read())
                {
                    var monitorId = (dLe["GMN_ID"] as int?).GetValueOrDefault();

                    var sql = new StringBuilder();
                    sql.AppendLine("UPDATE GRA_MONITOR SET GMN_ACCESS = @ACC");
                    sql.AppendLine("WHERE GMN_ID = @ID");

                    cmm.CommandText = sql.ToString();

                    this._db.NewParameter(cmm, "@ID", DbType.Int32).Value = monitorId;
                    this._db.NewParameter(cmm, "@ACC", DbType.DateTime).Value = DateTime.Now;
                }
                //Se não, declara o novo conhecimento
                else
                {
                    var sql = new StringBuilder();
                    sql.AppendLine("INSERT INTO GRA_MONITOR (GMN_ID, GMN_USR_ID, GMN_GRM_ID, GMN_KNOWLEDGE, GMN_ACCESS)");
                    sql.AppendLine("VALUES (GEN_ID(GRAMONITOR_ID_GEN, 1), @USR, @GRM, 'S', @ACC)");

                    cmm.CommandText = sql.ToString();

                    this._db.NewParameter(cmm, "@USR", DbType.Int32).Value = userId;
                    this._db.NewParameter(cmm, "@GRM", DbType.Int32).Value = grammarId;
                    this._db.NewParameter(cmm, "@ACC", DbType.DateTime).Value = DateTime.Now;
                }

            try
            {
                cmm.ExecuteNonQuery();
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
            finally
            {
                cmm.Parameters.Clear();
                cmm.Transaction = null;
            }
        }

        public TGrammar GetGrammar(int grammarId)
        {
            this._db.DB.CommandText = "SELECT * FROM GRAMMAR WHERE GRM_ID = " + grammarId;

            using (var dLe = this._db.DB.ExecuteReader())
                if (dLe.Read())
                {
                    var grammar = new TGrammar()
                                     {
                                         Index = (dLe["GRM_ID"] as int?).GetValueOrDefault(),
                                         Title = dLe["GRM_TITLE"] as string,
                                         Level = (EJLPT)Enum.Parse(typeof(EJLPT), dLe["GRM_LEVEL"] as string ?? "N5", true),
                                         Body = dLe["GRM_BODY"] as string
                                     };

                    var requisites = dLe["GRM_REQUISITES"] as string;
                    if (!string.IsNullOrEmpty(requisites))
                        grammar.Requisites = requisites.Split(';').Select(r => Convert.ToInt32(r)).ToArray();

                    return grammar;
                }

            return null;
        }

        public bool HasExercises(int grammarId)
        {
            var sql = new StringBuilder();
            sql.AppendLine("SELECT EXR_ID FROM EXERCISES");
            sql.AppendLine("WHERE EXR_GRM_ID = " + grammarId);

            this._db.DB.CommandText = sql.ToString();

            using (var dLe = this._db.DB.ExecuteReader())
                if (dLe.Read())
                    return true;

            return false;
        }

        public bool HasKnowledge(int userId, int grammarId)
        {
            this._db.DB.CommandText = "SELECT GRM_REQUISITES FROM GRAMMAR WHERE GRM_ID = " + grammarId;

            var reqString = this._db.DB.ExecuteScalar() as string;
            if (string.IsNullOrEmpty(reqString))
                return true;

            var requisites = reqString.Split(';');

            this._db.DB.CommandText = "SELECT COUNT(*) FROM GRA_MONITOR WHERE GMN_GRM_ID IN (";
            this._db.DB.CommandText += string.Join(", ", requisites.Select(r => r.TrimStart('0')));
            this._db.DB.CommandText += ") AND GMN_USR_ID = " + userId;

            var count = (this._db.DB.ExecuteScalar() as int?).GetValueOrDefault();
            if (count == requisites.Length)
                return true;

            return false;
        }

        public Dictionary<int, string> ListGrammars(EJLPT? jlpt = null)
        {
            var grammars = new Dictionary<int, string>();

            var sql = new StringBuilder();
            sql.AppendLine("SELECT * FROM GRAMMAR");

            if(jlpt.HasValue)
            {
                if(jlpt.Value == EJLPT.None || jlpt.Value == EJLPT.N5)
                    sql.AppendLine("WHERE GRM_LEVEL IS NULL OR UPPER(GRM_LEVEL) = 'N5'");
                else
                    sql.AppendLine("WHERE UPPER(GRM_LEVEL) = '" + jlpt.ToString().ToUpper() + "'");
            }

            sql.AppendLine("ORDER BY (CASE WHEN GRM_REQUISITES IS NULL THEN 0 ELSE 1 END),");
            sql.AppendLine("  GRM_REQUISITES, 1");

            this._db.DB.CommandText = sql.ToString();

            using (var dLe = this._db.DB.ExecuteReader())
                while (dLe.Read())
                    grammars.Add(
                        (dLe["GRM_ID"] as int?).GetValueOrDefault(),
                        dLe["GRM_TITLE"] as string);

            return grammars;
        }

        public void UpdateGrammar(int id, string title, string level, string body, int[] requisites)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("UPDATE GRAMMAR SET GRM_TITLE = @TTL, GRM_LEVEL = @LVL, GRM_BODY = @BODY, GRM_REQUISITES = @REQ");
            sql.AppendLine("WHERE GRM_ID = @ID");

            cmm.CommandText = sql.ToString();

            this._db.NewParameter(cmm, "@ID", DbType.String).Value = id;
            this._db.NewParameter(cmm, "@TTL", DbType.String).Value = title;
            this._db.NewParameter(cmm, "@LVL", DbType.String).Value = (level == "none" ? DBNull.Value : (object)level);
            this._db.NewParameter(cmm, "@BODY", DbType.String).Value = body;
            this._db.NewParameter(cmm, "@REQ", DbType.String).Value =
                (requisites != null && requisites.Length > 0 ? (object)string.Join(";", requisites) : DBNull.Value);

            try
            {
                cmm.ExecuteNonQuery();
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
            finally
            {
                cmm.Parameters.Clear();
                cmm.Transaction = null;
            }
        }

        #endregion

        #region IDisposable Methods

        public void Dispose()
        {
            this._db.Disconnect();
        }

        #endregion
    }
}
