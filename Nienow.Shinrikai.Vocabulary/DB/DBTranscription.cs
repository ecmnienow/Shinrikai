using Nienow.DataBaseConn;
using Nienow.Helper;
using Nienow.Shinrikai.Vocabulary.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Nienow.Shinrikai.Vocabulary.DB
{
    public class DBTranscription : IDisposable
    {
        #region Constructors

        public DBTranscription(DBConnectionConfig dbConnectionConfig)
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
            sql.AppendLine("  GEN_ID(TRLCMONITOR_ID_GEN, 1), @USR, @VCB, @CNT, @FAI, @SUC, @HIT");
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

        public Dictionary<int, string> GetSubjects()
        {
            var subjects = new Dictionary<int, string>();

            var sql = new StringBuilder();
            sql.AppendLine("SELECT SBJ_ID, SBJ_NAME");
            sql.AppendLine("FROM SUBJECTS");
            sql.AppendLine("WHERE");
            sql.AppendLine(" (SELECT COUNT(*)");
            sql.AppendLine("    FROM VOCABULARY");
            sql.AppendLine("    WHERE ((VCB_SBJ_ID_1 = SBJ_ID AND VCB_KANJI_1 IS NOT NULL) OR");
            sql.AppendLine("           (VCB_SBJ_ID_2 = SBJ_ID AND VCB_KANJI_2 IS NOT NULL) OR");
            sql.AppendLine("           (VCB_SBJ_ID_3 = SBJ_ID AND VCB_KANJI_3 IS NOT NULL))) > 0");

            this._db.DB.CommandText = sql.ToString();

            var dLe = this._db.DB.ExecuteReader();

            while (dLe.Read())
                subjects.Add(
                    (dLe["SBJ_ID"] as int?).GetValueOrDefault(),
                    dLe["SBJ_NAME"] as string);

            return subjects;
        }

        /// <summary>
        /// Retorna uma lista de vocabulário para transcrição
        /// </summary>
        /// <param name="subject">ID do assunto (apenas quando "fromMistakes" = true)</param>
        /// <param name="fromMistakes">Indica que é para trazer apenas o vocabulário que o usuário possui dificuldades</param>
        public IEnumerable<TWord> GetVocabulary(int userId, int? subject = null, bool fromMistakes = false)
        {
            var vocabulary = new List<TWord>();

            var sql = new StringBuilder();
            sql.AppendLine("SELECT VCB_ID,");
            sql.AppendLine("  (SELECT SBJ_NAME FROM SUBJECTS WHERE SBJ_ID = VCB_SBJ_ID_1) SUBJECT_1,");
            sql.AppendLine("  (SELECT SBJ_NAME FROM SUBJECTS WHERE SBJ_ID = VCB_SBJ_ID_2) SUBJECT_2,");
            sql.AppendLine("  (SELECT SBJ_NAME FROM SUBJECTS WHERE SBJ_ID = VCB_SBJ_ID_3) SUBJECT_3,");
            sql.AppendLine("  VCB_KANA, VCB_ROMAJI, VCB_TRANSLATION_1, VCB_TRANSLATION_2, VCB_TRANSLATION_3,");
            sql.AppendLine("  VCB_KANJI_1, VCB_KANJI_2, VCB_KANJI_3");
            sql.AppendLine("FROM VOCABULARY");
            if (fromMistakes && subject.HasValue)
                sql.AppendLine("  LEFT OUTER JOIN TRL_MONITOR ON TMN_VCB_ID = VCB_ID");
            sql.AppendLine("WHERE (VCB_KANJI_1 IS NOT NULL OR VCB_KANJI_2 IS NOT NULL OR VCB_KANJI_3 IS NOT NULL)");
            if (fromMistakes && subject.HasValue)
            {
                sql.AppendLine("  AND TMN_COUNTER > 0 AND TMN_USR_ID = " + userId + " AND");
                sql.AppendLine("  (VCB_SBJ_ID_1 = " + subject.Value + " OR VCB_SBJ_ID_2 = " + subject.Value + " OR VCB_SBJ_ID_3 = " + subject.Value + ")");
            }

            this._db.DB.CommandText = sql.ToString();

            var dLe = this._db.DB.ExecuteReader();

            while (dLe.Read())
            {
                vocabulary.Add(new TWord()
                                   {
                                       ID = (dLe["VCB_ID"] as int?).GetValueOrDefault(),
                                       Subject1 = dLe["SUBJECT_1"] as string,
                                       Subject2 = dLe["SUBJECT_2"] as string,
                                       Subject3 = dLe["SUBJECT_3"] as string,
                                       Kana = dLe["VCB_KANA"] as string,
                                       Romaji = dLe["VCB_ROMAJI"] as string,
                                       Word1 = dLe["VCB_TRANSLATION_1"] as string,
                                       Word2 = dLe["VCB_TRANSLATION_2"] as string,
                                       Word3 = dLe["VCB_TRANSLATION_3"] as string,
                                       KanjiWord1 = dLe["VCB_KANJI_1"] as string,
                                       KanjiWord2 = dLe["VCB_KANJI_2"] as string,
                                       KanjiWord3 = dLe["VCB_KANJI_3"] as string,
                                       Info1 = string.Empty,
                                       Info2 = string.Empty,
                                       Info3 = string.Empty
                                   });
            }

            return vocabulary;
        }

        public IEnumerable<TWord> SearchKanjiWord(string term)
        {
            var words = new List<TWord>();

            var sql = new StringBuilder();
            sql.AppendLine("SELECT VCB_ID,");
            sql.AppendLine("  (SELECT SBJ_NAME FROM SUBJECTS WHERE SBJ_ID = VCB_SBJ_ID_1) SUBJECT_1,");
            sql.AppendLine("  (SELECT SBJ_NAME FROM SUBJECTS WHERE SBJ_ID = VCB_SBJ_ID_2) SUBJECT_2,");
            sql.AppendLine("  (SELECT SBJ_NAME FROM SUBJECTS WHERE SBJ_ID = VCB_SBJ_ID_3) SUBJECT_3,");
            sql.AppendLine("  VCB_KANA, VCB_ROMAJI, VCB_TRANSLATION_1, VCB_TRANSLATION_2, VCB_TRANSLATION_3,");
            sql.AppendLine("  VCB_KANJI_1, VCB_KANJI_2, VCB_KANJI_3");
            sql.AppendLine("FROM VOCABULARY");
            sql.AppendLine("WHERE VCB_KANJI_1 LIKE '%" + term + "%' OR");
            sql.AppendLine("  VCB_KANJI_2 LIKE '%" + term + "%' OR");
            sql.AppendLine("  VCB_KANJI_3 LIKE '%" + term + "%' OR");
            sql.AppendLine("  (VCB_TRANSLATION_1 LIKE '%" + term + "%' AND VCB_KANJI_1 IS NOT NULL) OR");
            sql.AppendLine("  (VCB_TRANSLATION_2 LIKE '%" + term + "%' AND VCB_KANJI_2 IS NOT NULL) OR");
            sql.AppendLine("  (VCB_TRANSLATION_3 LIKE '%" + term + "%' AND VCB_KANJI_3 IS NOT NULL)");

            this._db.DB.CommandText = sql.ToString();

            var dLe = this._db.DB.ExecuteReader();

            while (dLe.Read())
            {
                words.Add(new TWord()
                                   {
                                       ID = (dLe["VCB_ID"] as int?).GetValueOrDefault(),
                                       Subject1 = dLe["SUBJECT_1"] as string,
                                       Subject2 = dLe["SUBJECT_2"] as string,
                                       Subject3 = dLe["SUBJECT_3"] as string,
                                       Kana = dLe["VCB_KANA"] as string,
                                       Romaji = dLe["VCB_ROMAJI"] as string,
                                       Word1 = dLe["VCB_TRANSLATION_1"] as string,
                                       Word2 = dLe["VCB_TRANSLATION_2"] as string,
                                       Word3 = dLe["VCB_TRANSLATION_3"] as string,
                                       KanjiWord1 = dLe["VCB_KANJI_1"] as string,
                                       KanjiWord2 = dLe["VCB_KANJI_2"] as string,
                                       KanjiWord3 = dLe["VCB_KANJI_3"] as string,
                                       Info1 = string.Empty,
                                       Info2 = string.Empty,
                                       Info3 = string.Empty
                                   });
            }

            return words;
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

        #endregion

        #region IDisposable Methods

        public void Dispose()
        {
            this._db.Disconnect();
        }

        #endregion
    }
}
