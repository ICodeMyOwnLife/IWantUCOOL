using System;


namespace IWantUInfrastructure
{
    public class AccountsReceivedEventArgs: EventArgs
    {
        #region  Properties & Indexers
        public Account[] Accounts { get; set; }
        #endregion
    }
}