using System;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Monitoring.DB;

namespace Nienow.Shinrikai.Monitoring
{
    public class MonitoringManager : ManagerCore
    {
        #region Private Fields

        private readonly DBMonitoring _db = new DBMonitoring();

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
