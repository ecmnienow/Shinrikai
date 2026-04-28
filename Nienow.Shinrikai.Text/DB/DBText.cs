using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Nienow.DataBaseConn;
using Nienow.Helper;
using System;
using Nienow.Shinrikai.Text.Model;
using Nienow.Shinrikai.Core;

namespace Nienow.Shinrikai.Text.DB
{
    public class DBText : IDisposable
    {
        #region Constructors

        public DBText(DBConnectionConfig dbConnectionConfig)
        {
            this._db = new ShinrikaiDB(dbConnectionConfig);
        }

        #endregion

        #region Private Fields

        private readonly ShinrikaiDB _db;

        #endregion

        #region Public Methods

        public void AddMonitor(Monitor monitor)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("INSERT INTO TRL_MONITOR (");
            sql.AppendLine("  TMN_ID, TMN_USR_ID, TMN_VCB_ID, TMN_COUNTER, TMN_FAILS, TMN_SUCCESSES, TMN_LASTHIT");
            sql.AppendLine(") VALUES (");
            sql.AppendLine("  GEN_ID(VOCMONITOR_ID_GEN, 1), @USR, @VCB, @CNT, @FAI, @SUC, @HIT");
            sql.AppendLine(")");

            cmm.CommandText = sql.ToString();

            this._db.NewParameter(cmm, "@USR", DbType.Int32).Value = monitor.UsrID;
            this._db.NewParameter(cmm, "@VCB", DbType.Int32).Value = monitor.VcbID;
            this._db.NewParameter(cmm, "@CNT", DbType.Int32).Value = monitor.Counter;
            this._db.NewParameter(cmm, "@FAI", DbType.Int32).Value = monitor.FailCounter;
            this._db.NewParameter(cmm, "@SUC", DbType.Int32).Value = monitor.SuccessCounter;
            this._db.NewParameter(cmm, "@HIT", DbType.DateTime).Value = monitor.LastHittedTime;

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

        public void AddText(string title, string level, string body)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("INSERT INTO TEXTS (TXT_ID, TXT_TITLE, TXT_LEVEL, TXT_BODY)");
            sql.AppendLine("VALUES (GEN_ID(TEXTS_ID_GEN, 1), @TTL, @LVL, @BODY)");


            cmm.CommandText = sql.ToString();

            this._db.NewParameter(cmm, "@TTL", DbType.String).Value = title;
            this._db.NewParameter(cmm, "@LVL", DbType.String).Value = level == "none" ? DBNull.Value : (object)level;
            this._db.NewParameter(cmm, "@BODY", DbType.String).Value = body;

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
        
        public void DeclareKnowledge(int userId, int textId)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            //Verifica se já conhece
            cmm.CommandText = "SELECT TMN_ID FROM TEX_MONITOR WHERE TMN_TXT_ID = " + textId + " AND TMN_USR_ID = " + userId;

            using(var dLe = cmm.ExecuteReader())
                //Se conhece, apenas atualiza o horário do último acesso
                if (dLe.Read())
                {
                    var monitorId = (dLe["TMN_ID"] as int?).GetValueOrDefault();

                    var sql = new StringBuilder();
                    sql.AppendLine("UPDATE TEX_MONITOR SET TMN_ACCESS = @ACC");
                    sql.AppendLine("WHERE TMN_ID = @ID");

                    cmm.CommandText = sql.ToString();

                    this._db.NewParameter(cmm, "@ID", DbType.Int32).Value = monitorId;
                    this._db.NewParameter(cmm, "@ACC", DbType.DateTime).Value = DateTime.Now;
                }
                //Se não, declara o novo conhecimento
                else
                {
                    var sql = new StringBuilder();
                    sql.AppendLine("INSERT INTO TEX_MONITOR (TMN_ID, TMN_USR_ID, TMN_TXT_ID, TMN_KNOWLEDGE, TMN_ACCESS)");
                    sql.AppendLine("VALUES (GEN_ID(TEXMONITOR_ID_GEN, 1), @USR, @GRM, 'S', @ACC)");

                    cmm.CommandText = sql.ToString();

                    this._db.NewParameter(cmm, "@USR", DbType.Int32).Value = userId;
                    this._db.NewParameter(cmm, "@GRM", DbType.Int32).Value = textId;
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

        public Dictionary<int, string> ListTexts()
        {
            var texts = new Dictionary<int, string>();

            this._db.DB.CommandText = "SELECT * FROM TEXTS";

            var dLe = this._db.DB.ExecuteReader();

            while (dLe.Read())
                texts.Add(
                    (dLe["TXT_ID"] as int?).GetValueOrDefault(),
                    dLe["TXT_TITLE"] as string);

            return texts;
        }

        public Monitor GetMonitor(int usrId, int wordId)
        {
            Monitor monitor = null;

            this._db.DB.CommandText = "SELECT * FROM TRL_MONITOR WHERE TMN_VCB_ID = " + wordId + " AND TMN_USR_ID = " + usrId;

            var dLe = this._db.DB.ExecuteReader();

            if (dLe.Read())
                monitor = new Monitor()
                              {
                                  ID = (dLe["TMN_ID"] as int?).GetValueOrDefault(),
                                  UsrID = (dLe["TMN_USR_ID"] as int?).GetValueOrDefault(),
                                  VcbID = (dLe["TMN_VCB_ID"] as int?).GetValueOrDefault(),
                                  Counter = (dLe["TMN_COUNTER"] as int?).GetValueOrDefault(),
                                  FailCounter = (dLe["TMN_FAILS"] as int?).GetValueOrDefault(),
                                  SuccessCounter = (dLe["TMN_SUCCESSES"] as int?).GetValueOrDefault(),
                                  LastHittedTime = (dLe["TMN_LASTHIT"] as DateTime?).GetValueOrDefault(),
                              };

            return monitor;
        }

        public TText GetText(int id)
        {
            this._db.DB.CommandText = "SELECT * FROM TEXTS WHERE TXT_ID = " + id;

            var dLe = this._db.DB.ExecuteReader();

            if (dLe.Read())
            {
                return new TText()
                           {
                               Index = (dLe["TXT_ID"] as int?).GetValueOrDefault(),
                               Title = dLe["TXT_TITLE"] as string,
                               Level = (EJLPT) Enum.Parse(typeof(EJLPT), dLe["TXT_LEVEL"] as string ?? "None", true),
                               Body = dLe["TXT_BODY"] as string,
                           };
            }

            return null;
        }
        
        public bool HasExercises(int textId)
        {
            var sql = new StringBuilder();
            sql.AppendLine("SELECT EXR_ID FROM EXERCISES");
            sql.AppendLine("WHERE EXR_TXT_ID = " + textId);

            this._db.DB.CommandText = sql.ToString();

            using (var dLe = this._db.DB.ExecuteReader())
                if (dLe.Read())
                    return true;

            return false;
        }

        public bool UnknownStatus(int userId, int wordId)
        {
            var sql = new StringBuilder();
            sql.AppendLine("SELECT TMN_ID FROM TRL_MONITOR");
            sql.AppendLine("WHERE TMN_USR_ID = " + userId);
            sql.AppendLine("  AND TMN_VCB_ID = " + wordId);

            this._db.DB.CommandText = sql.ToString();

            using (var dLe = this._db.DB.ExecuteReader())
                if (dLe.Read())
                    return false;

            return true;
        }

        public void UpdateMonitor(Monitor monitor)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("UPDATE TRL_MONITOR SET");
            sql.AppendLine("  TMN_COUNTER = @CNT, TMN_FAILS = @FAI, TMN_SUCCESSES = @SUC, TMN_LASTHIT = @HIT");
            sql.AppendLine("WHERE TMN_ID = @ID");

            cmm.CommandText = sql.ToString();

            this._db.NewParameter(cmm, "@ID", DbType.Int32).Value = monitor.ID;
            this._db.NewParameter(cmm, "@CNT", DbType.Int32).Value = monitor.Counter;
            this._db.NewParameter(cmm, "@FAI", DbType.Int32).Value = monitor.FailCounter;
            this._db.NewParameter(cmm, "@SUC", DbType.Int32).Value = monitor.SuccessCounter;
            this._db.NewParameter(cmm, "@HIT", DbType.DateTime).Value = monitor.LastHittedTime;

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

        internal void UpdateText(int id, string title, string level, string body)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("UPDATE TEXTS SET TXT_TITLE = @TTL, TXT_LEVEL = @LVL, TXT_BODY = @BODY");
            sql.AppendLine("WHERE TXT_ID = @ID");


            cmm.CommandText = sql.ToString();
            
            this._db.NewParameter(cmm, "@ID", DbType.Int32).Value = id;
            this._db.NewParameter(cmm, "@TTL", DbType.String).Value = title;
            this._db.NewParameter(cmm, "@LVL", DbType.String).Value = level == "none" ? DBNull.Value : (object)level;
            this._db.NewParameter(cmm, "@BODY", DbType.String).Value = body;

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

        public bool UserHasWordKnowledge(int userId, int wordId)
        {
            var sql = new StringBuilder();
            sql.AppendLine("SELECT TMN_ID FROM TRL_MONITOR");
            sql.AppendLine("WHERE TMN_USR_ID = " + userId);
            sql.AppendLine("  AND TMN_VCB_ID = " + wordId);
            sql.AppendLine("  AND TMN_COUNTER = 0");

            this._db.DB.CommandText = sql.ToString();

            using (var dLe = this._db.DB.ExecuteReader())
                if (dLe.Read())
                    return true;

            return false;
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
