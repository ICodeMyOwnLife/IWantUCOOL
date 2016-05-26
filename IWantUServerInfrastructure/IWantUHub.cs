using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using CB.Model.Common;
using CB.Net.SignalR.Server;
using IWantUInfrastructure;


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

            if (_coupleChoices.ContainsKey(senderId))
            {
                Clients.Caller.announceChosen(receiverId, ChoiceResult.Done);
                return;
            }

            _coupleChoices[senderId] = receiverId;
            _logger.Log($"{GetName(senderId)} chose {GetName(receiverId)}.");

            foreach (var userChooseMe in _coupleChoices.Where(p => p.Value == senderId))
            {
                Clients.Client(userChooseMe.Key).announceChosen(senderId,
                    userChooseMe.Key == receiverId ? ChoiceResult.Successful : ChoiceResult.Failed);
            }

            string receiverChoice;
            var result = !_coupleChoices.TryGetValue(receiverId, out receiverChoice)
                             ? ChoiceResult.Undone
                             : receiverChoice == senderId
                                   ? ChoiceResult.Successful
                                   : ChoiceResult.Failed;
            Clients.Caller.announceChosen(receiverId, result);
        }

        public void GetAccounts()
            => SendUsersTo(Context.ConnectionId);

        public void SendMessage(string message, string receiverId)
            => Clients.Client(receiverId).receiveMessage(message, Context.ConnectionId);

        public void SignIn(string name)
        {
            var id = Context.ConnectionId;
            Clients.Clients(_idNameDictionary.Keys.ToList()).receiveNewAccount(id, name);
            _idNameDictionary[id] = name;
            SendUsersTo(Context.ConnectionId);

            _logger.Log($"{id} is signed in as {name}");
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
        private static string GetName(string accountId)
        {
            string name;
            return _idNameDictionary.TryGetValue(accountId, out name) ? name : null;
        }

        private void SendUsersTo(string connectionId)
            => Clients.Client(connectionId).receiveAccounts(_idNameDictionary.Where(p => p.Key != Context.ConnectionId));
        #endregion
    }
}