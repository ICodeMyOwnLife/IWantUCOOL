using CB.Net.SignalR.Client;


namespace IWantUClientInfrastructure
{
    // ReSharper disable once InconsistentNaming
    public class IWantUProxy: SignalRProxyBase
    {
        #region  Constructors & Destructor
        public IWantUProxy(string signalRUrl, string hubName): base(signalRUrl, hubName) { }
        #endregion
    }
}