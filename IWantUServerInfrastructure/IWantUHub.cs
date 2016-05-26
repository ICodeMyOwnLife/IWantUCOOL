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
        private static readonly ConcurrentDictionary<string, string> _coupleChoices =
            new ConcurrentDictionary<string, string>();

        private static readonly ConcurrentDictionary<string, string> _idNameDictionary =
            new ConcurrentDictionary<string, string>();
        #endregion


        #region  Constructors & Destructor
        public IWantUHub() { }

        public IWantUHub(ILog logger): base(logger) { }
        #endregion


        #region Methods
        public void ChooseAccount(string receiverId)
        {
            var senderId = Context.ConnectionId;
            _coupleChoices[senderId] = receiverId;

            string receiverChoice;
            if (!_coupleChoices.TryGetValue(receiverId, out receiverChoice)) return;

            if (receiverChoice == senderId)
            {
                Clients.Caller.announceChosen(receiverId, true);
                Clients.Client(receiverId).announceChosen(senderId, true);
            }
            else
            {
                Clients.Caller.announceChosen(receiverId, false);
            }
        }

        public void GetAccounts()
            => SendUsersTo(Context.ConnectionId);

        public void SendMessage(string message, string receiverId)
            => Clients.Client(receiverId).receiveMessage(message, Context.ConnectionId);

        public void SignIn()
        {
            var id = Context.ConnectionId;
            var name = Clients.Caller.Name;
            _idNameDictionary[id] = name;
            SendUsersTo(Context.ConnectionId);

            Clients.Others.receiveNewAccount(id, name);
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
            => Clients.Client(connectionId).receiveAccounts(_idNameDictionary.Where(p => p.Key != Context.ConnectionId));
        #endregion
    }
}