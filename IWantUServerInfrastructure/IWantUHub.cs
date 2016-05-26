using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using CB.Model.Common;
using CB.Net.SignalR.Server;


namespace IWantUServerInfrastructure
{
    // ReSharper disable once InconsistentNaming
    public class IWantUHub: SignalRHubBase
    {
        #region Fields
        private static readonly ConcurrentDictionary<string, string> _idNameDictionary =
            new ConcurrentDictionary<string, string>();
        #endregion


        #region  Constructors & Destructor
        public IWantUHub() { }

        public IWantUHub(ILog logger): base(logger) { }
        #endregion


        #region Methods
        public void GetAccounts()
        {
            SendUsersTo(Context.ConnectionId);
        }

        public void SendMessage(string message, string receiverId)
        {
            Clients.Client(receiverId).receiveMessage(message, Context.ConnectionId);
        }

        public void SignIn()
        {
            var id = Context.ConnectionId;
            var name = Clients.Caller.Name;
            _idNameDictionary[id] = name;
            SendUsersTo(Context.ConnectionId);

            Clients.Others.addNewAccount(id, name);
        }
        #endregion


        #region Override
        public override Task OnDisconnected(bool stopCalled)
        {
            string name;
            var id = Context.ConnectionId;
            _idNameDictionary.TryRemove(id, out name);
            Clients.Others.removeAccount(id);
            return base.OnDisconnected(stopCalled);
        }
        #endregion


        #region Implementation
        private void SendUsersTo(string connectionId)
        {
            Clients.Client(connectionId).receiveAccounts(_idNameDictionary.Where(p => p.Key != Context.ConnectionId));
        }
        #endregion
    }
}