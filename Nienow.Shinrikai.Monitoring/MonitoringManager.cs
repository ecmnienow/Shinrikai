using System;
using Nienow.DataBaseConn;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Monitoring.DB;

namespace Nienow.Shinrikai.Monitoring
{
    public class MonitoringManager : ManagerCore
    {
        public MonitoringManager(DBConnectionConfig dbConnectionConfig)
        {
            this._db = new DBMonitoring(dbConnectionConfig);
        }

        #region Private Fields

        private readonly DBMonitoring _db;

        #endregion

        #region Public Methods

        public void AddMonitor(int usrId, string screamName, string screamAction, string screamOption, string sessionID)
        {
            try
            {
                using (this._db.Connect())
                    this._db.AddMonitor(usrId, screamName, screamAction, screamOption, sessionID);
            }
            catch (Exception ex) { throw; } //VER Implementar trace
        }

        #endregion
    }
}
