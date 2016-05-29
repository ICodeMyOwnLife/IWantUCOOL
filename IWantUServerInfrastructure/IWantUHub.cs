using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private static readonly ConcurrentDictionary<string, string> _accountDictionary =
            new ConcurrentDictionary<string, string>();

        private static readonly ConcurrentDictionary<string, string> _coupleChoices =
            new ConcurrentDictionary<string, string>();
        #endregion


        #region  Constructors & Destructor
        public IWantUHub() { }

        public IWantUHub(ILog logger): base(logger) { }
        #endregion


        #region Methods
        public string AddGroup(string groupName, IEnumerable<string> ids)
        {
            var groupId = Guid.NewGuid().ToString();
            foreach (var id in ids)
            {
                Groups.Add(id, groupId);
            }
            Groups.Add(Context.ConnectionId, groupId);
            return groupId;
        }

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

        public void SendMessageToGroup(string message, string groupId)
            => Clients.Group(groupId).receiveMessageFromGroup(message, groupId, Context.ConnectionId);

        public void SignIn(string name)
        {
            var id = Context.ConnectionId;
            Clients.Clients(_accountDictionary.Keys.ToList()).receiveNewAccount(id, name);
            _accountDictionary[id] = name;
            SendUsersTo(Context.ConnectionId);

            _logger.Log($"{id} was signed in as {name}.");
        }

        public void SignOut()
            => SignOut(Context.ConnectionId);
        #endregion


        #region Override
        public override Task OnDisconnected(bool stopCalled)
        {
            SignOut(Context.ConnectionId);
            return base.OnDisconnected(stopCalled);
        }
        #endregion


        #region Implementation
        private static string GetName(string accountId)
        {
            string name;
            return _accountDictionary.TryGetValue(accountId, out name) ? name : null;
        }

        private void SendUsersTo(string connectionId)
            =>
                Clients.Client(connectionId).receiveAccounts(_accountDictionary.Where(p => p.Key != Context.ConnectionId));

        private void SignOut(string id)
        {
            string name;
            if (_accountDictionary.TryRemove(id, out name))
            {
                Clients.Others.removeAccount(id);
                _logger.Log($"{id} was signed out.");
            }
        }
        #endregion
    }
}