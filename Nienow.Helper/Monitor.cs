using System;

namespace Nienow.Helper
{
    public class Monitor
    {
        #region Constructors

        public Monitor()
        {
            this.ID = -1;
            UsrID = -1;
            VcbID = -1;
            SuccessCounter = 0;
            FailCounter = 0;
            Counter = 0;
            LastHittedTime = DateTime.MinValue;
        }

        public Monitor(int monitoredItemIndex)
        {
            this.ID = monitoredItemIndex;
        }

        #endregion

        #region Attributes and Properties

        public int ID { get; set; }
        public int UsrID { get; set; }
        public int VcbID { get; set; }
        public int FailCounter { get; set; }
        public int Counter { get; set; }
        public int SuccessCounter { get; set; }
        public DateTime LastHittedTime { get; set; }

        #endregion
    }
}
