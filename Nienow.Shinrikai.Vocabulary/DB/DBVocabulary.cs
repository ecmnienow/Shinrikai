using System;
using Nienow.DataBaseConn;
using Nienow.Helper;
using Nienow.Shinrikai.Vocabulary.Model;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace Nienow.Shinrikai.Vocabulary.DB
{
    public class DBVocabulary : IDisposable
    {
        #region Constructors

        public DBVocabulary(DBConnectionConfig dbConnectionConfig)
        {
            this._db = new ShinrikaiDB(dbConnectionConfig);
        }

        #endregion

        #region Private Fields

        private readonly ShinrikaiDB _db;

        #endregion

        public void AddMonitor(Monitor monitor)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("INSERT INTO VOC_MONITOR (");
            sql.AppendLine("  VMN_ID, VMN_USR_ID, VMN_VCB_ID, VMN_COUNTER, VMN_FAILS, VMN_SUCCESSES, VMN_LASTHIT");
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

        public void AddWord(TWord word)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("INSERT INTO VOCABULARY (");
            sql.AppendLine("  VCB_ID, VCB_KANA, VCB_ROMAJI, VCB_KANJI_1, VCB_KANJI_2, VCB_KANJI_3,");
            sql.AppendLine("  VCB_SBJ_ID_1, VCB_SBJ_ID_2, VCB_SBJ_ID_3, VCB_TRANSLATION_1,");
            sql.AppendLine("  VCB_TRANSLATION_2, VCB_TRANSLATION_3, VCB_IMAGE_1, VCB_IMAGE_2, VCB_IMAGE_3,");
            sql.AppendLine("  VCB_AUDIO_1, VCB_AUDIO_2, VCB_AUDIO_3");
            sql.AppendLine(") VALUES (");
            sql.AppendLine("  GEN_ID(VOCABULARY_ID_GEN, 1), @KAN, @RMJ, @KNJ1, @KNJ2, @KNJ3,");
            sql.AppendLine("  @SBJ1, @SBJ2, @SBJ3, @WRD1, @WRD2,");
            sql.AppendLine("  @WRD3, @IMG1, @IMG2, @IMG3,");
            sql.AppendLine("  @AUD1, @AUD2, @AUD3");
            sql.AppendLine(")");

            cmm.CommandText = sql.ToString();

            this._db.NewParameter(cmm, "@KAN", DbType.String).Value = word.Kana;
            this._db.NewParameter(cmm, "@RMJ", DbType.String).Value = word.Romaji;
            this._db.NewParameter(cmm, "@KNJ1", DbType.String).Value = word.KanjiWord1;
            this._db.NewParameter(cmm, "@KNJ2", DbType.String).Value = word.KanjiWord2;
            this._db.NewParameter(cmm, "@KNJ3", DbType.String).Value = word.KanjiWord3;
            this._db.NewParameter(cmm, "@SBJ1", DbType.Int32).Value = word.SubjectID1;
            this._db.NewParameter(cmm, "@SBJ2", DbType.Int32).Value = (word.SubjectID2 == 0 ? DBNull.Value : (object)word.SubjectID2);
            this._db.NewParameter(cmm, "@SBJ3", DbType.Int32).Value = (word.SubjectID3 == 0 ? DBNull.Value : (object)word.SubjectID3);
            this._db.NewParameter(cmm, "@WRD1", DbType.String).Value = word.Word1;
            this._db.NewParameter(cmm, "@WRD2", DbType.String).Value = word.Word2;
            this._db.NewParameter(cmm, "@WRD3", DbType.String).Value = word.Word3;
            this._db.NewParameter(cmm, "@IMG1", DbType.Byte).Value = word.Image1;
            this._db.NewParameter(cmm, "@IMG2", DbType.Byte).Value = word.Image2;
            this._db.NewParameter(cmm, "@IMG3", DbType.Byte).Value = word.Image3;
            this._db.NewParameter(cmm, "@AUD1", DbType.Byte).Value = word.Audio1;
            this._db.NewParameter(cmm, "@AUD2", DbType.Byte).Value = word.Audio2;
            this._db.NewParameter(cmm, "@AUD3", DbType.Byte).Value = word.Audio3;

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

            this._db.DB.CommandText = "SELECT * FROM VOC_MONITOR WHERE VMN_VCB_ID = " + wordId + " AND VMN_USR_ID = " + usrId;

            var dLe = this._db.DB.ExecuteReader();

            if (dLe.Read())
                monitor = new Monitor()
                              {
                                  ID = (dLe["VMN_ID"] as int?).GetValueOrDefault(),
                                  UsrID = (dLe["VMN_USR_ID"] as int?).GetValueOrDefault(),
                                  VcbID = (dLe["VMN_VCB_ID"] as int?).GetValueOrDefault(),
                                  Counter = (dLe["VMN_COUNTER"] as int?).GetValueOrDefault(),
                                  FailCounter = (dLe["VMN_FAILS"] as int?).GetValueOrDefault(),
                                  SuccessCounter = (dLe["VMN_SUCCESSES"] as int?).GetValueOrDefault(),
                                  LastHittedTime = (dLe["VMN_LASTHIT"] as DateTime?).GetValueOrDefault(),
                              };

            return monitor;
        }

        public Dictionary<int, string> GetSubjects()
        {
            var subjects = new Dictionary<int, string>();

            this._db.DB.CommandText = "SELECT SBJ_ID, SBJ_NAME FROM SUBJECTS ORDER BY SBJ_NAME";

            var dLe = this._db.DB.ExecuteReader();

            while (dLe.Read())
                subjects.Add(
                    (dLe["SBJ_ID"] as int?).GetValueOrDefault(),
                    dLe["SBJ_NAME"] as string);

            return subjects;
        }

        /// <summary>
        /// Retorna uma lista de vocabulário
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
            sql.AppendLine("  VCB_KANJI_1, VCB_KANJI_2, VCB_KANJI_3,");
            sql.AppendLine("  (CASE WHEN VCB_IMAGE_1 IS NOT NULL THEN 'T' ELSE 'F' END) IMG1,");
            sql.AppendLine("  (CASE WHEN VCB_IMAGE_2 IS NOT NULL THEN 'T' ELSE 'F' END) IMG2,");
            sql.AppendLine("  (CASE WHEN VCB_IMAGE_3 IS NOT NULL THEN 'T' ELSE 'F' END) IMG3,");
            sql.AppendLine("  (CASE WHEN VCB_AUDIO_1 IS NOT NULL THEN 'T' ELSE 'F' END) AUD1,");
            sql.AppendLine("  (CASE WHEN VCB_AUDIO_2 IS NOT NULL THEN 'T' ELSE 'F' END) AUD2,");
            sql.AppendLine("  (CASE WHEN VCB_AUDIO_3 IS NOT NULL THEN 'T' ELSE 'F' END) AUD3");
            sql.AppendLine("FROM VOCABULARY");

            if (fromMistakes && subject.HasValue)
            {
                sql.AppendLine("  LEFT OUTER JOIN VOC_MONITOR ON VMN_VCB_ID = VCB_ID");
                sql.AppendLine("WHERE VMN_COUNTER > 0 AND VMN_USR_ID = " + userId + " AND");
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
                                       Info3 = string.Empty,
                                       HasImage1 = dLe["IMG1"] as string == "T",
                                       HasImage2 = dLe["IMG2"] as string == "T",
                                       HasImage3 = dLe["IMG3"] as string == "T",
                                       HasAudio1 = dLe["AUD1"] as string == "T",
                                       HasAudio2 = dLe["AUD2"] as string == "T",
                                       HasAudio3 = dLe["AUD3"] as string == "T"
                                   });
            }

            return vocabulary;
        }

        public TWord GetWord(int wordId)
        {
            var sql = new StringBuilder();
            sql.AppendLine("SELECT VCB_ID, VCB_SBJ_ID_1, VCB_SBJ_ID_2, VCB_SBJ_ID_3,");
            sql.AppendLine("  (SELECT SBJ_NAME FROM SUBJECTS WHERE SBJ_ID = VCB_SBJ_ID_1) SUBJECT_1,");
            sql.AppendLine("  (SELECT SBJ_NAME FROM SUBJECTS WHERE SBJ_ID = VCB_SBJ_ID_2) SUBJECT_2,");
            sql.AppendLine("  (SELECT SBJ_NAME FROM SUBJECTS WHERE SBJ_ID = VCB_SBJ_ID_3) SUBJECT_3,");
            sql.AppendLine("  VCB_KANA, VCB_ROMAJI, VCB_TRANSLATION_1, VCB_TRANSLATION_2, VCB_TRANSLATION_3,");
            sql.AppendLine("  VCB_KANJI_1, VCB_KANJI_2, VCB_KANJI_3,");
            sql.AppendLine("  (CASE WHEN VCB_IMAGE_1 IS NOT NULL THEN 'T' ELSE 'F' END) IMG1,");
            sql.AppendLine("  (CASE WHEN VCB_IMAGE_2 IS NOT NULL THEN 'T' ELSE 'F' END) IMG2,");
            sql.AppendLine("  (CASE WHEN VCB_IMAGE_3 IS NOT NULL THEN 'T' ELSE 'F' END) IMG3,");
            sql.AppendLine("  (CASE WHEN VCB_AUDIO_1 IS NOT NULL THEN 'T' ELSE 'F' END) AUD1,");
            sql.AppendLine("  (CASE WHEN VCB_AUDIO_2 IS NOT NULL THEN 'T' ELSE 'F' END) AUD2,");
            sql.AppendLine("  (CASE WHEN VCB_AUDIO_3 IS NOT NULL THEN 'T' ELSE 'F' END) AUD3");
            sql.AppendLine("FROM VOCABULARY");
            sql.AppendLine("WHERE VCB_ID = " + wordId);

            this._db.DB.CommandText = sql.ToString();

            using (var dLe = this._db.DB.ExecuteReader())
                if (dLe.Read())
                    return new TWord()
                                       {
                                           ID = (dLe["VCB_ID"] as int?).GetValueOrDefault(),
                                           Subject1 = dLe["SUBJECT_1"] as string,
                                           Subject2 = dLe["SUBJECT_2"] as string,
                                           Subject3 = dLe["SUBJECT_3"] as string,
                                           SubjectID1 = (dLe["VCB_SBJ_ID_1"] as int?).GetValueOrDefault(),
                                           SubjectID2 = (dLe["VCB_SBJ_ID_2"] as int?).GetValueOrDefault(),
                                           SubjectID3 = (dLe["VCB_SBJ_ID_3"] as int?).GetValueOrDefault(),
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
                                           Info3 = string.Empty,
                                           HasImage1 = dLe["IMG1"] as string == "T",
                                           HasImage2 = dLe["IMG2"] as string == "T",
                                           HasImage3 = dLe["IMG3"] as string == "T",
                                           HasAudio1 = dLe["AUD1"] as string == "T",
                                           HasAudio2 = dLe["AUD2"] as string == "T",
                                           HasAudio3 = dLe["AUD3"] as string == "T"
                                       };

            return null;
        }

        public TWord GetWord(string word)
        {
            var sql = new StringBuilder();
            sql.AppendLine("SELECT VCB_ID, VCB_SBJ_ID_1, VCB_SBJ_ID_2, VCB_SBJ_ID_3,");
            sql.AppendLine("  (SELECT SBJ_NAME FROM SUBJECTS WHERE SBJ_ID = VCB_SBJ_ID_1) SUBJECT_1,");
            sql.AppendLine("  (SELECT SBJ_NAME FROM SUBJECTS WHERE SBJ_ID = VCB_SBJ_ID_2) SUBJECT_2,");
            sql.AppendLine("  (SELECT SBJ_NAME FROM SUBJECTS WHERE SBJ_ID = VCB_SBJ_ID_3) SUBJECT_3,");
            sql.AppendLine("  VCB_KANA, VCB_ROMAJI, VCB_TRANSLATION_1, VCB_TRANSLATION_2, VCB_TRANSLATION_3,");
            sql.AppendLine("  VCB_KANJI_1, VCB_KANJI_2, VCB_KANJI_3,");
            sql.AppendLine("  (CASE WHEN VCB_IMAGE_1 IS NOT NULL THEN 'T' ELSE 'F' END) IMG1,");
            sql.AppendLine("  (CASE WHEN VCB_IMAGE_2 IS NOT NULL THEN 'T' ELSE 'F' END) IMG2,");
            sql.AppendLine("  (CASE WHEN VCB_IMAGE_3 IS NOT NULL THEN 'T' ELSE 'F' END) IMG3,");
            sql.AppendLine("  (CASE WHEN VCB_AUDIO_1 IS NOT NULL THEN 'T' ELSE 'F' END) AUD1,");
            sql.AppendLine("  (CASE WHEN VCB_AUDIO_2 IS NOT NULL THEN 'T' ELSE 'F' END) AUD2,");
            sql.AppendLine("  (CASE WHEN VCB_AUDIO_3 IS NOT NULL THEN 'T' ELSE 'F' END) AUD3");
            sql.AppendLine("FROM VOCABULARY");
            sql.AppendLine("WHERE VCB_KANA = '" + word + "'");

            this._db.DB.CommandText = sql.ToString();

            using (var dLe = this._db.DB.ExecuteReader())
                if (dLe.Read())
                    return new TWord()
                                       {
                                           ID = (dLe["VCB_ID"] as int?).GetValueOrDefault(),
                                           Subject1 = dLe["SUBJECT_1"] as string,
                                           Subject2 = dLe["SUBJECT_2"] as string,
                                           Subject3 = dLe["SUBJECT_3"] as string,
                                           SubjectID1 = (dLe["VCB_SBJ_ID_1"] as int?).GetValueOrDefault(),
                                           SubjectID2 = (dLe["VCB_SBJ_ID_2"] as int?).GetValueOrDefault(),
                                           SubjectID3 = (dLe["VCB_SBJ_ID_3"] as int?).GetValueOrDefault(),
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
                                           Info3 = string.Empty,
                                           HasImage1 = dLe["IMG1"] as string == "T",
                                           HasImage2 = dLe["IMG2"] as string == "T",
                                           HasImage3 = dLe["IMG3"] as string == "T",
                                           HasAudio1 = dLe["AUD1"] as string == "T",
                                           HasAudio2 = dLe["AUD2"] as string == "T",
                                           HasAudio3 = dLe["AUD3"] as string == "T"
                                       };

            return null;
        }

        public void UpdateMonitor(Monitor monitor)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("UPDATE VOC_MONITOR SET");
            sql.AppendLine("  VMN_COUNTER = @CNT, VMN_FAILS = @FAI, VMN_SUCCESSES = @SUC, VMN_LASTHIT = @HIT");
            sql.AppendLine("WHERE VMN_ID = @ID");

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

        public void UpdateWord(TWord word)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("UPDATE VOCABULARY SET");
            sql.AppendLine("  VCB_KANA = @KAN, VCB_ROMAJI = @RMJ, VCB_KANJI_1 = @KNJ1, VCB_KANJI_2 = @KNJ2, VCB_KANJI_3 = @KNJ3,");
            sql.AppendLine("  VCB_SBJ_ID_1 = @SBJ1, VCB_SBJ_ID_2 = @SBJ2, VCB_SBJ_ID_3 = @SBJ3, VCB_TRANSLATION_1 = @WRD1,");
            sql.AppendLine("  VCB_TRANSLATION_2 = @WRD2, VCB_TRANSLATION_3 = @WRD3");
            if (word.Image1 != null && word.Image1.Length > 0)
                sql.AppendLine("  , VCB_IMAGE_1 = @IMG1");
            if (word.Image2 != null && word.Image2.Length > 0)
                sql.AppendLine("  , VCB_IMAGE_2 = @IMG2");
            if (word.Image3 != null && word.Image3.Length > 0)
                sql.AppendLine("  , VCB_IMAGE_3 = @IMG3");

            if (word.Audio1 != null && word.Audio1.Length > 0)
                sql.AppendLine("  , VCB_AUDIO_1 = @AUD1");
            if (word.Audio2 != null && word.Audio2.Length > 0)
                sql.AppendLine("  , VCB_AUDIO_2 = @AUD2");
            if (word.Audio3 != null && word.Audio3.Length > 0)
                sql.AppendLine("  , VCB_AUDIO_3 = @AUD3");
            sql.AppendLine("WHERE VCB_ID = @ID");

            cmm.CommandText = sql.ToString();

            this._db.NewParameter(cmm, "@ID", DbType.String).Value = word.ID;
            this._db.NewParameter(cmm, "@KAN", DbType.String).Value = word.Kana;
            this._db.NewParameter(cmm, "@RMJ", DbType.String).Value = word.Romaji;
            this._db.NewParameter(cmm, "@KNJ1", DbType.String).Value = word.KanjiWord1;
            this._db.NewParameter(cmm, "@KNJ2", DbType.String).Value = word.KanjiWord2;
            this._db.NewParameter(cmm, "@KNJ3", DbType.String).Value = word.KanjiWord3;
            this._db.NewParameter(cmm, "@SBJ1", DbType.Int32).Value = word.SubjectID1;
            this._db.NewParameter(cmm, "@SBJ2", DbType.Int32).Value = (word.SubjectID2 == 0 ? DBNull.Value : (object)word.SubjectID2);
            this._db.NewParameter(cmm, "@SBJ3", DbType.Int32).Value = (word.SubjectID3 == 0 ? DBNull.Value : (object)word.SubjectID3);
            this._db.NewParameter(cmm, "@WRD1", DbType.String).Value = word.Word1;
            this._db.NewParameter(cmm, "@WRD2", DbType.String).Value = word.Word2;
            this._db.NewParameter(cmm, "@WRD3", DbType.String).Value = word.Word3;
            if (word.Image1 != null && word.Image1.Length > 0)
                this._db.NewParameter(cmm, "@IMG1", DbType.Byte).Value = word.Image1;
            if (word.Image2 != null && word.Image2.Length > 0)
                this._db.NewParameter(cmm, "@IMG2", DbType.Byte).Value = word.Image2;
            if (word.Image3 != null && word.Image3.Length > 0)
                this._db.NewParameter(cmm, "@IMG3", DbType.Byte).Value = word.Image3;
            if (word.Audio1 != null && word.Audio1.Length > 0)
                this._db.NewParameter(cmm, "@AUD1", DbType.Byte).Value = word.Audio1;
            if (word.Audio2 != null && word.Audio2.Length > 0)
                this._db.NewParameter(cmm, "@AUD2", DbType.Byte).Value = word.Audio2;
            if (word.Audio3 != null && word.Audio3.Length > 0)
                this._db.NewParameter(cmm, "@AUD3", DbType.Byte).Value = word.Audio3;

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

        #region IDisposable Methods

        public void Dispose()
        {
            this._db.Disconnect();
        }

        #endregion
    }
}
