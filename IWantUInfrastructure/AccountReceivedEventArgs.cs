using System;


namespace IWantUInfrastructure
{
    public class AccountReceivedEventArgs : EventArgs
    {
        #region  Properties & Indexers
        public Account Account { get; set; }
        #endregion
    }
}