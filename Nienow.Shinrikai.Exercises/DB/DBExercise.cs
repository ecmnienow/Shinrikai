using Nienow.DataBaseConn;
using Nienow.Helper;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Exercises.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Nienow.Shinrikai.Exercises.DB
{
    public class DBExercise : IDisposable
    {
        #region Constructors

        public DBExercise(DBConnectionConfig dbConnectionConfig)
        {
            this._db = new ShinrikaiDB(dbConnectionConfig);
        }

        #endregion

        #region Private Fields

        private readonly ShinrikaiDB _db;

        #endregion

        #region Public Methods

        public void AddExercise(int grammarId, int textId, string question, int right, string option1, string option2, string option3, string option4, EJLPT jlpt)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("INSERT INTO EXERCISES (");
            sql.AppendLine("  EXR_ID, EXR_GRM_ID, EXR_TXT_ID, EXR_TEXT, EXR_OPTION_1, EXR_OPTION_2,");
            sql.AppendLine("  EXR_OPTION_3, EXR_OPTION_4, EXR_RIGHT, EXR_LEVEL");
            sql.AppendLine(") VALUES (");
            sql.AppendLine("  GEN_ID(EXERCISES_ID_GEN, 1), @GRM, @TXT, @QST, @OP1, @OP2,");
            sql.AppendLine("  @OP3, @OP4, @RGT, @LVL");
            sql.AppendLine(")");

            cmm.CommandText = sql.ToString();

            this._db.NewParameter(cmm, "@GRM", DbType.Int32).Value = (grammarId > 0 ? (object)grammarId : DBNull.Value);
            this._db.NewParameter(cmm, "@TXT", DbType.Int32).Value = (textId > 0 ? (object)textId : DBNull.Value);
            this._db.NewParameter(cmm, "@QST", DbType.String).Value = question;
            this._db.NewParameter(cmm, "@OP1", DbType.String).Value = option1;
            this._db.NewParameter(cmm, "@OP2", DbType.String).Value = option2;
            this._db.NewParameter(cmm, "@OP3", DbType.String).Value = option3;
            this._db.NewParameter(cmm, "@OP4", DbType.String).Value = option4;
            this._db.NewParameter(cmm, "@RGT", DbType.Int32).Value = right;
            this._db.NewParameter(cmm, "@LVL", DbType.String).Value = jlpt;

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

        public void AddMonitor(Monitor monitor)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("INSERT INTO EXE_MONITOR (");
            sql.AppendLine("  EMN_ID, EMN_USR_ID, EMN_EXR_ID, EMN_FAILS, EMN_SUCCESSES, EMN_LASTHIT");
            sql.AppendLine(") VALUES (");
            sql.AppendLine("  GEN_ID(EXEMONITOR_ID_GEN, 1), @USR, @EXR, @FAI, @SUC, @HIT");
            sql.AppendLine(")");

            cmm.CommandText = sql.ToString();

            this._db.NewParameter(cmm, "@USR", DbType.Int32).Value = monitor.UsrID;
            this._db.NewParameter(cmm, "@EXR", DbType.Int32).Value = monitor.VcbID;
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

        public IDisposable Connect()
        {
            this._db.Connect();
            return this;
        }

        public TExercise GetExercise(int id)
        {
            this._db.DB.CommandText = "SELECT * FROM EXERCISES WHERE EXR_ID = " + id;

            using (var dLe = this._db.DB.ExecuteReader())
                if (dLe.Read())
                {
                    var grammar = new TExercise()
                                     {
                                         ID = (dLe["EXR_ID"] as int?).GetValueOrDefault(),
                                         GrammarId = (dLe["EXR_GRM_ID"] as int?).GetValueOrDefault(),
                                         TextId = (dLe["EXR_TXT_ID"] as int?).GetValueOrDefault(),
                                         Question = dLe["EXR_TEXT"] as string,
                                         Option1 = dLe["EXR_OPTION_1"] as string,
                                         Option2 = dLe["EXR_OPTION_2"] as string,
                                         Option3 = dLe["EXR_OPTION_3"] as string,
                                         Option4 = dLe["EXR_OPTION_4"] as string,
                                         RightAnswer = (dLe["EXR_RIGHT"] as int?).GetValueOrDefault(),
                                         Level = (EJLPT)Enum.Parse(typeof(EJLPT), dLe["EXR_LEVEL"] as string ?? "None", true)
                                     };

                    return grammar;
                }

            return null;
        }

        public Monitor GetMonitor(int usrId, int exerciseId)
        {
            Monitor monitor = null;

            this._db.DB.CommandText = "SELECT * FROM EXE_MONITOR WHERE EMN_EXR_ID = " + exerciseId + " AND EMN_USR_ID = " + usrId;

            var dLe = this._db.DB.ExecuteReader();

            if (dLe.Read())
                monitor = new Monitor()
                              {
                                  ID = (dLe["EMN_ID"] as int?).GetValueOrDefault(),
                                  UsrID = (dLe["EMN_USR_ID"] as int?).GetValueOrDefault(),
                                  VcbID = (dLe["EMN_EXR_ID"] as int?).GetValueOrDefault(),
                                  FailCounter = (dLe["EMN_FAILS"] as int?).GetValueOrDefault(),
                                  SuccessCounter = (dLe["EMN_SUCCESSES"] as int?).GetValueOrDefault(),
                                  LastHittedTime = (dLe["EMN_LASTHIT"] as DateTime?).GetValueOrDefault(),
                              };

            return monitor;
        }

        public List<int> ListAllExercises(int userId)
        {
            var exercises = new List<int>();

            this._db.DB.CommandText = "SELECT EXR_ID FROM EXERCISES WHERE EXR_TXT_ID IS NULL";

            var dLe = this._db.DB.ExecuteReader();
            while(dLe.Read())
                exercises.Add((dLe["EXR_ID"] as int?).GetValueOrDefault());

            return exercises;
        }

        public List<int> ListExercises(int userId, int grammarId, int textId)
        {
            var exercises = new List<int>();

            var sql = new StringBuilder();
            sql.AppendLine("SELECT EXR_ID");
            sql.AppendLine("FROM EXERCISES");
            sql.AppendLine("WHERE");
            if (grammarId > 0)
                sql.AppendLine("EXR_GRM_ID = " + grammarId);
            else
                if (textId > 0)
                    sql.AppendLine("EXR_TXT_ID = " + textId);
                else
                    throw new InvalidOperationException();

            this._db.DB.CommandText = sql.ToString();

            var dLe = this._db.DB.ExecuteReader();
            while(dLe.Read())
                exercises.Add((dLe["EXR_ID"] as int?).GetValueOrDefault());

            return exercises;
        }

        public void UpdateExercise(int id, int grammarId, int textId, string question, int right, string option1, string option2, string option3, string option4, EJLPT jlpt)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("UPDATE EXERCISES SET");
            sql.AppendLine("  EXR_GRM_ID = @GRM, EXR_TXT_ID = @TXT, EXR_TEXT = @QST, EXR_OPTION_1 = @OP1, EXR_OPTION_2 = @OP2,");
            sql.AppendLine("  EXR_OPTION_3 = @OP3, EXR_OPTION_4 = @OP4, EXR_RIGHT = @RGT, EXR_LEVEL = @LVL");
            sql.AppendLine("WHERE EXR_ID = @ID");

            cmm.CommandText = sql.ToString();

            this._db.NewParameter(cmm, "@ID", DbType.Int32).Value = id;
            this._db.NewParameter(cmm, "@GRM", DbType.Int32).Value = (grammarId > 0 ? (object)grammarId : DBNull.Value);
            this._db.NewParameter(cmm, "@TXT", DbType.Int32).Value = (textId > 0 ? (object)textId : DBNull.Value);
            this._db.NewParameter(cmm, "@QST", DbType.String).Value = question;
            this._db.NewParameter(cmm, "@OP1", DbType.String).Value = option1;
            this._db.NewParameter(cmm, "@OP2", DbType.String).Value = option2;
            this._db.NewParameter(cmm, "@OP3", DbType.String).Value = option3;
            this._db.NewParameter(cmm, "@OP4", DbType.String).Value = option4;
            this._db.NewParameter(cmm, "@RGT", DbType.Int32).Value = right;
            this._db.NewParameter(cmm, "@LVL", DbType.String).Value = jlpt;

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

        public void UpdateMonitor(Monitor monitor)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("UPDATE EXE_MONITOR SET");
            sql.AppendLine("  EMN_FAILS = @FAI, EMN_SUCCESSES = @SUC, EMN_LASTHIT = @HIT");
            sql.AppendLine("WHERE EMN_ID = @ID");

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

        #endregion

        #region IDisposable Methods

        public void Dispose()
        {
            this._db.Disconnect();
        }

        #endregion
    }
}
