using CB.Net.SignalR;
using CB.Net.SignalR.Server;


namespace IWantUServerInfrastructure
{
    // ReSharper disable once InconsistentNaming
    public class IWantUServer: SignalRServerBase
    {
        #region  Constructors & Destructor
        public IWantUServer(): this(new SignalRConfiguration()) { }
        public IWantUServer(SignalRConfiguration configuration): base(configuration) { }
        public IWantUServer(string signalRUrl): base(signalRUrl) { }
        #endregion
    }
}