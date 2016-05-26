using System;
using System.Collections.Generic;


namespace IWantUInfrastructure
{
    public class AccountsReceivedEventArgs: EventArgs
    {
        #region  Properties & Indexers
        public IEnumerable<Account> Accounts { get; set; }
        #endregion
    }
}