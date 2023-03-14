using Nienow.DataBaseConn;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.User.Model;
using System;
using System.Data;
using System.Text;

namespace Nienow.Shinrikai.User.DB
{
    public class DBUser : IDisposable
    {
        #region Constructors

        public DBUser()
        {
            this._db = new ShinrikaiDB();
        }

        #endregion

        #region Private Fields

        private readonly ShinrikaiDB _db;

        #endregion

        #region Public Methods

        public IDisposable Connect()
        {
            this._db.Connect();
            return this;
        }

        public void DeclareUnderstanding(int userId, int msgId)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("INSERT INTO NOT_STATUS (");
            sql.AppendLine("  NST_ID, NST_USR_ID, NST_NTF_ID, NST_UNDERSTAND, NST_DATE");
            sql.AppendLine(") VALUES (");
            sql.AppendLine("  GEN_ID(NOTSTATUS_ID_GEN, 1), @USR, @NTF, 'S', @DT");
            sql.AppendLine(")");

            cmm.CommandText = sql.ToString();
            
            this._db.NewParameter(cmm, "@USR", DbType.Int32).Value = userId;
            this._db.NewParameter(cmm, "@NTF", DbType.Int32).Value = msgId;
            this._db.NewParameter(cmm, "@DT", DbType.DateTime).Value = DateTime.Now;
            
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

        public TUser Login(string login, string password)
        {
            var sql = new StringBuilder();
            sql.AppendLine("SELECT USRID, USR_NAME, USR_EMAIL, USR_LOGIN, USR_PASS, USR_SEX, USR_BIRTHDAY,");
            sql.AppendLine("  USR_PROFISSION, USR_OTHERLANGUAGES, USR_JLPT, USR_TYPE");
            sql.AppendLine("FROM USERS");
            sql.AppendLine("WHERE USR_LOGIN = '" + login + "' AND USR_PASS = '" + password + "'");

            this._db.DB.CommandText = sql.ToString();

            var dLe = this._db.DB.ExecuteReader();

            if (dLe.Read())
                return new TUser()
                           {
                               Name = dLe["USR_NAME"] as string,
                               Email = dLe["USR_EMAIL"] as string,
                               Login = dLe["USR_LOGIN"] as string,
                               Sex = dLe["USR_SEX"] as string == "M" ? ESex.Male : ESex.Female,
                               Birthday = (dLe["USR_BIRTHDAY"] as DateTime?).GetValueOrDefault(),
                               Profession = dLe["USR_PROFISSION"] as string,
                               OtherLanguages = dLe["USR_OTHERLANGUAGES"] as string,
                               JLPT = (EJLPT)Enum.Parse(typeof(EJLPT), dLe["USR_JLPT"] as string ?? "None", true),
                               Type = dLe["USR_TYPE"] as string,
                           };

            return null;
        }

        public string GetNotification(int userId, int notificationId, bool force)
        {
            var sql = new StringBuilder();
            sql.AppendLine("SELECT FIRST 1 NTF_MESSAGE FROM NOTIFICATIONS");
            sql.AppendLine("  LEFT OUTER JOIN NOT_STATUS ON NTF_ID = NST_NTF_ID AND");
            sql.AppendLine("    NST_USR_ID = " + userId);
            sql.AppendLine("WHERE NTF_ID = " + notificationId);
            if(!force)
                sql.AppendLine("  AND (NST_UNDERSTAND IS NULL OR NST_UNDERSTAND <> 'S')");

            this._db.DB.CommandText = sql.ToString();

            var notification = this._db.DB.ExecuteScalar() as string ?? "";
            return notification;
        }

        public TUser GetUser(string login)
        {
            var sql = new StringBuilder();
            sql.AppendLine("SELECT USRID, USR_NAME, USR_EMAIL, USR_LOGIN, USR_PASS, USR_SEX, USR_BIRTHDAY,");
            sql.AppendLine("  USR_PROFISSION, USR_OTHERLANGUAGES, USR_JLPT, USR_TYPE");
            sql.AppendLine("FROM USERS");
            sql.AppendLine("WHERE USR_LOGIN = '" + login + "'");

            this._db.DB.CommandText = sql.ToString();

            var dLe = this._db.DB.ExecuteReader();

            if (dLe.Read())
                return new TUser()
                           {
                               ID = (dLe["USRID"] as int?).GetValueOrDefault(),
                               Name = dLe["USR_NAME"] as string,
                               Email = dLe["USR_EMAIL"] as string,
                               Login = dLe["USR_LOGIN"] as string,
                               Sex = dLe["USR_SEX"] as string == "M" ? ESex.Male : ESex.Female,
                               Birthday = (dLe["USR_BIRTHDAY"] as DateTime?).GetValueOrDefault(),
                               Profession = dLe["USR_PROFISSION"] as string,
                               OtherLanguages = dLe["USR_OTHERLANGUAGES"] as string,
                               JLPT = (EJLPT)Enum.Parse(typeof(EJLPT), dLe["USR_JLPT"] as string ?? "None", true),
                               Type = dLe["USR_TYPE"] as string,
                           };

            return null;
        }

        public TUser GetUserByEmail(string email)
        {
            var sql = new StringBuilder();
            sql.AppendLine("SELECT USRID, USR_NAME, USR_EMAIL, USR_LOGIN, USR_PASS, USR_SEX, USR_BIRTHDAY,");
            sql.AppendLine("  USR_PROFISSION, USR_OTHERLANGUAGES, USR_JLPT, USR_TYPE");
            sql.AppendLine("FROM USERS");
            sql.AppendLine("WHERE USR_EMAIL = '" + email + "'");

            this._db.DB.CommandText = sql.ToString();

            var dLe = this._db.DB.ExecuteReader();

            if (dLe.Read())
                return new TUser()
                           {
                               ID = (dLe["USRID"] as int?).GetValueOrDefault(),
                               Name = dLe["USR_NAME"] as string,
                               Email = dLe["USR_EMAIL"] as string,
                               Login = dLe["USR_LOGIN"] as string,
                               Sex = dLe["USR_SEX"] as string == "M" ? ESex.Male : ESex.Female,
                               Birthday = (dLe["USR_BIRTHDAY"] as DateTime?).GetValueOrDefault(),
                               Profession = dLe["USR_PROFISSION"] as string,
                               OtherLanguages = dLe["USR_OTHERLANGUAGES"] as string,
                               JLPT = (EJLPT)Enum.Parse(typeof(EJLPT), dLe["USR_JLPT"] as string ?? "None", true),
                               Type = dLe["USR_TYPE"] as string,
                           };

            return null;
        }

        public void SignUp(TUser user)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("INSERT INTO USERS (");
            sql.AppendLine("  USRID, USR_NAME, USR_EMAIL, USR_LOGIN, USR_PASS,");
            sql.AppendLine("  USR_SEX, USR_BIRTHDAY, USR_PROFISSION, USR_OTHERLANGUAGES, USR_JLPT");
            sql.AppendLine(") VALUES (");
            sql.AppendLine("  GEN_ID(USERS_ID_GEN, 1), @USR, @EML, @LGN, @PSS,");
            sql.AppendLine("  @SEX, @DAT, @JOB, @LNG, @JLPT");
            sql.AppendLine(")");

            cmm.CommandText = sql.ToString();
            
            this._db.NewParameter(cmm, "@USR", DbType.String).Value = user.Name;
            this._db.NewParameter(cmm, "@EML", DbType.String).Value = user.Email;
            this._db.NewParameter(cmm, "@LGN", DbType.String).Value = user.Login;
            this._db.NewParameter(cmm, "@PSS", DbType.String).Value = user.Password;
            this._db.NewParameter(cmm, "@SEX", DbType.String).Value = (user.Sex == ESex.Male ? "M" : "F");
            this._db.NewParameter(cmm, "@DAT", DbType.DateTime).Value = user.Birthday;
            this._db.NewParameter(cmm, "@JOB", DbType.String).Value = user.Profession;
            this._db.NewParameter(cmm, "@LNG", DbType.String).Value = user.OtherLanguages;
            this._db.NewParameter(cmm, "@JLPT", DbType.String).Value = (user.JLPT == EJLPT.None ? DBNull.Value : (object)user.JLPT.ToString());
            
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
