using System.Windows;
using CB.Model.Common;
using IWantUServerInfrastructure;


namespace IWantUWindowServer
{
    // ReSharper disable once InconsistentNaming
    public class IWantUWindowHub: IWantUHub
    {
        #region  Constructors & Destructor
        public IWantUWindowHub()
        {
            Application.Current.Dispatcher?.Invoke(() => _logger = Application.Current.MainWindow?.DataContext as ILog);
        }
        #endregion
    }
}