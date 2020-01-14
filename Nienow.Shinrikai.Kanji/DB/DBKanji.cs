using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Nienow.DataBaseConn;
using Nienow.Helper;
using Nienow.Shinrikai.Kanji.Model;

namespace Nienow.Shinrikai.Kanji.DB
{
    public class DBKanji : IDisposable
    {
        #region Constructors

        public DBKanji()
        {
            this._db = new ShinrikaiDB();
        }

        #endregion

        #region Private Fields

        public readonly ShinrikaiDB _db;

        #endregion

        #region Public Methods

        public void AddKanji(TKanji kanji)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("INSERT INTO KANJI (");
            sql.AppendLine("  KNJ_ID, KNJ_KANJI, KNJ_KANA, KNJ_ROMAJI, KNJ_JLPT, KNJ_INFO, KNJ_IMAGE");
            sql.AppendLine(") VALUES (");
            sql.AppendLine("  GEN_ID(KANJI_ID_GEN, 1), @KNJ, @KAN, @RMJ, @LVL, @INF, @IMG");
            sql.AppendLine(")");

            cmm.CommandText = sql.ToString();

            this._db.NewParameter(cmm, "@KNJ", DbType.String).Value = kanji.KanjiLetter;
            this._db.NewParameter(cmm, "@KAN", DbType.String).Value = kanji.Kana;
            this._db.NewParameter(cmm, "@RMJ", DbType.String).Value = kanji.Romaji;
            this._db.NewParameter(cmm, "@LVL", DbType.String).Value = kanji.JLPT == "none" ? DBNull.Value : (object)kanji.JLPT;
            this._db.NewParameter(cmm, "@INF", DbType.String).Value = kanji.Info == "none" ? DBNull.Value : (object)kanji.Info;
            this._db.NewParameter(cmm, "@IMG", DbType.Byte).Value = kanji.Image;

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
            sql.AppendLine("INSERT INTO KAN_MONITOR (");
            sql.AppendLine("  KMN_ID, KMN_USR_ID, KMN_KNJ_ID, KMN_COUNTER, KMN_FAILS, KMN_SUCCESSES, KMN_LASTHIT");
            sql.AppendLine(") VALUES (");
            sql.AppendLine("  GEN_ID(KANMONITOR_ID_GEN, 1), @USR, @VCB, @CNT, @FAI, @SUC, @HIT");
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

        public IDisposable Connect()
        {
            this._db.Connect();
            return this;
        }

        public TKanji GetKanji(string kanjiLetter)
        {
            var sql = new StringBuilder();
            sql.AppendLine("SELECT KNJ_ID, KNJ_KANJI, KNJ_KANA, KNJ_ROMAJI, KNJ_JLPT, KNJ_INFO, KNJ_IMAGE");
            sql.AppendLine("FROM KANJI");
            sql.AppendLine("WHERE KNJ_KANJI = '" + kanjiLetter + "'");

            this._db.DB.CommandText = sql.ToString();

            var dLe = this._db.DB.ExecuteReader();

            if (dLe.Read())
                return new TKanji()
                                   {
                                       ID = (dLe["KNJ_ID"] as int?).GetValueOrDefault(),
                                       KanjiLetter = dLe["KNJ_KANJI"] as string,
                                       Kana = dLe["KNJ_KANA"] as string,
                                       Romaji = dLe["KNJ_ROMAJI"] as string,
                                       JLPT = dLe["KNJ_JLPT"] as string,
                                       Info = dLe["KNJ_INFO"] as string,
                                       HasImage = (dLe["KNJ_IMAGE"] as byte[]) != null,
                                   };

            return null;
        }

        public IEnumerable<TKanji> GetKanjis(int userId, string level = null, bool fromMistakes = false)
        {
            var kanjis = new List<TKanji>();

            var sql = new StringBuilder();
            sql.AppendLine("SELECT KNJ_ID, KNJ_KANJI, KNJ_KANA, KNJ_ROMAJI, KNJ_JLPT, KNJ_INFO");
            sql.AppendLine("FROM KANJI");

            if (fromMistakes && !string.IsNullOrEmpty(level))
            {
                sql.AppendLine("  LEFT OUTER JOIN KAN_MONITOR ON KMN_KNJ_ID = KNJ_ID");
                sql.AppendLine("WHERE KMN_COUNTER > 0 AND KMN_USR_ID = " + userId);

                if (level == "N5")
                    sql.AppendLine("  AND KNJ_JLPT = 'N5'");
                if (level == "N4")
                    sql.AppendLine("  AND (KNJ_JLPT = 'N5' OR KNJ_JLPT = 'N4')");
                if (level == "N3")
                    sql.AppendLine("  AND (KNJ_JLPT = 'N5' OR KNJ_JLPT = 'N4' OR KNJ_JLPT = 'N3')");
                if (level == "N2")
                    sql.AppendLine("  AND (KNJ_JLPT = 'N5' OR KNJ_JLPT = 'N4' OR KNJ_JLPT = 'N3' OR KNJ_JLPT = 'N2')");
            }

            this._db.DB.CommandText = sql.ToString();

            var dLe = this._db.DB.ExecuteReader();

            while (dLe.Read())
            {
                kanjis.Add(new TKanji()
                                   {
                                       ID = (dLe["KNJ_ID"] as int?).GetValueOrDefault(),
                                       KanjiLetter = dLe["KNJ_KANJI"] as string,
                                       Kana = dLe["KNJ_KANA"] as string,
                                       Romaji = dLe["KNJ_ROMAJI"] as string,
                                       JLPT = dLe["KNJ_JLPT"] as string,
                                       Info = dLe["KNJ_INFO"] as string,
                                   });
            }

            kanjis = kanjis.OrderBy(s => s.KanjiLetter).ToList();

            return kanjis;
        }

        public Monitor GetMonitor(int usrId, int kanjiId)
        {
            Monitor monitor = null;

            this._db.DB.CommandText = "SELECT * FROM KAN_MONITOR WHERE KMN_KNJ_ID = " + kanjiId + " AND KMN_USR_ID = " + usrId;

            var dLe = this._db.DB.ExecuteReader();

            if (dLe.Read())
                monitor = new Monitor()
                              {
                                  ID = (dLe["KMN_ID"] as int?).GetValueOrDefault(),
                                  UsrID = (dLe["KMN_USR_ID"] as int?).GetValueOrDefault(),
                                  VcbID = (dLe["KMN_KNJ_ID"] as int?).GetValueOrDefault(),
                                  Counter = (dLe["KMN_COUNTER"] as int?).GetValueOrDefault(),
                                  FailCounter = (dLe["KMN_FAILS"] as int?).GetValueOrDefault(),
                                  SuccessCounter = (dLe["KMN_SUCCESSES"] as int?).GetValueOrDefault(),
                                  LastHittedTime = (dLe["KMN_LASTHIT"] as DateTime?).GetValueOrDefault(),
                              };

            return monitor;
        }

        public void UpdateKanji(TKanji kanji)
        {
            
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("UPDATE KANJI SET");
            sql.AppendLine("  KNJ_KANJI = @KNJ, KNJ_KANA = @KAN, KNJ_ROMAJI = @RMJ,");
            sql.AppendLine("  KNJ_JLPT = @LVL, KNJ_INFO = @INF");
            if (kanji.Image != null && kanji.Image.Length > 0)
                sql.AppendLine("  , KNJ_IMAGE = @IMG");
            sql.AppendLine("WHERE KNJ_ID = @ID");

            cmm.CommandText = sql.ToString();
            
            this._db.NewParameter(cmm, "@ID", DbType.String).Value = kanji.ID;
            this._db.NewParameter(cmm, "@KNJ", DbType.String).Value = kanji.KanjiLetter;
            this._db.NewParameter(cmm, "@KAN", DbType.String).Value = kanji.Kana;
            this._db.NewParameter(cmm, "@RMJ", DbType.String).Value = kanji.Romaji;
            this._db.NewParameter(cmm, "@LVL", DbType.String).Value = kanji.JLPT == "none" ? DBNull.Value : (object)kanji.JLPT;
            this._db.NewParameter(cmm, "@INF", DbType.String).Value = kanji.Info == "none" ? DBNull.Value : (object)kanji.Info;
            if (kanji.Image != null && kanji.Image.Length > 0)
                this._db.NewParameter(cmm, "@IMG", DbType.Byte).Value = kanji.Image;

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
            sql.AppendLine("UPDATE KAN_MONITOR SET");
            sql.AppendLine("  KMN_COUNTER = @CNT, KMN_FAILS = @FAI, KMN_SUCCESSES = @SUC, KMN_LASTHIT = @HIT");
            sql.AppendLine("WHERE KMN_ID = @ID");

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
