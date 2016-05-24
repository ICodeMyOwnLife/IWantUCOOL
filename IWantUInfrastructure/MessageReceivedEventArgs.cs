using System;


namespace IWantUInfrastructure
{
    public class MessageReceivedEventArgs: EventArgs
    {
        #region  Properties & Indexers
        public string Message { get; set; }
        public string SenderId { get; set; }
        #endregion
    }
}