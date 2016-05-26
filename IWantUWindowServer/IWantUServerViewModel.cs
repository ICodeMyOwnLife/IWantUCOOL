using System;
using CB.Net.SignalR.Server;
using IWantUServerInfrastructure;


namespace IWantUWindowServer
{
    // ReSharper disable once InconsistentNaming
    public class IWantUServerViewModel: SignalRServerViewModelBase<IWantUServer>
    {
        #region Fields
        private string _logMessage;
        #endregion


        #region  Constructors & Destructor
        public IWantUServerViewModel(): base(new IWantUServer()) { }
        #endregion


        #region  Properties & Indexers
        public string LogMessage
        {
            get { return _logMessage; }
            private set { SetProperty(ref _logMessage, value); }
        }
        #endregion


        #region Override
        public override void Log(string logContent)
            => LogMessage = LogMessage == null ? logContent : LogMessage + Environment.NewLine + logContent;

        public override void LogError(Exception exception)
            => Log(exception.Message);
        #endregion
    }
}