using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
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


        #region Methods
        public void GetUsers()
        {
            SendUsersTo(Context.ConnectionId);
        }

        public void SendMessage(string message, string receiverId)
        {
            Clients.Client(receiverId).receiveMessage(message, Context.ConnectionId);
        }
        #endregion


        #region Override
        public override Task OnConnected()
        {
            _idNameDictionary[Context.ConnectionId] = Clients.Caller.Name;
            SendUsersTo(Context.ConnectionId);
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            string name;
            _idNameDictionary.TryRemove(Context.ConnectionId, out name);
            return base.OnDisconnected(stopCalled);
        }
        #endregion


        #region Implementation
        private void SendUsersTo(string connectionId)
        {
            Clients.Client(connectionId).receiveUsers(_idNameDictionary.Where(p => p.Key != Context.ConnectionId));
        }
        #endregion
    }
}