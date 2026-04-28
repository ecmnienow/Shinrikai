using System;
using System.Data;
using System.Text;
using Nienow.DataBaseConn;

namespace Nienow.Shinrikai.Monitoring.DB
{
    public class DBMonitoring : IDisposable
    {
        #region Constructors

        public DBMonitoring(DBConnectionConfig dbConnectionConfig)
        {
            this._db = new ShinrikaiDB(dbConnectionConfig);
        }

        #endregion
        
        #region Private Fields

        private readonly ShinrikaiDB _db;

        #endregion

        #region Public Methods

        public void AddMonitor(int usrId, string screamName, string screamAction, string screamOption, string sessionID)
        {
            var trans = this._db.NewTransaction();
            var cmm = this._db.CreateCommand(trans);

            var sql = new StringBuilder();
            sql.AppendLine("INSERT INTO INTERACTIONS (");
            sql.AppendLine("  ITR_ID, ITR_USR_ID, ITR_SCR_NAME, ITR_SCR_ACTION, ITR_SCR_OPTION, ITR_DATETIME, ITR_SESSIONID");
            sql.AppendLine(") VALUES (");
            sql.AppendLine("  GEN_ID(INTERACTIONS_ID_GEN, 1), @USR, @NAM, @ACT, @OPT, @DAT, @SES");
            sql.AppendLine(")");

            cmm.CommandText = sql.ToString();
            
            this._db.NewParameter(cmm, "@USR", DbType.Int32).Value = usrId;
            this._db.NewParameter(cmm, "@NAM", DbType.String).Value = screamName;
            this._db.NewParameter(cmm, "@ACT", DbType.String).Value = screamAction;
            this._db.NewParameter(cmm, "@OPT", DbType.String).Value = screamOption;
            this._db.NewParameter(cmm, "@DAT", DbType.Byte).Value = DateTime.Now;
            this._db.NewParameter(cmm, "@SES", DbType.String).Value = sessionID;

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

        #endregion

        #region IDisposable Methods

        public void Dispose()
        {
            this._db.Disconnect();
        }

        #endregion
    }
}
