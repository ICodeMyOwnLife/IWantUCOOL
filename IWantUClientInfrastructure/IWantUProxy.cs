using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CB.Net.SignalR;
using CB.Net.SignalR.Client;
using IWantUInfrastructure;
using Microsoft.AspNet.SignalR.Client;


namespace IWantUClientInfrastructure
{
    // ReSharper disable once InconsistentNaming
    public class IWantUProxy: SignalRProxyBase
    {
        #region  Constructors & Destructor
        public IWantUProxy(): base(new SignalRConfiguration()) { }
        #endregion


        #region Events
        public event EventHandler<AccountRemovedEventArgs> AccountRemoved;
        public event EventHandler<AccountsReceivedEventArgs> AccountsReceived;
        public event EventHandler<ChosenAnnouncedEventArgs> ChosenAnnounced;
        public event EventHandler<MessageReceivedEventArgs> MessagedReceived;
        public event EventHandler<AccountReceivedEventArgs> NewAccountReceived;
        #endregion


        #region Methods
        public virtual async Task ChooseUser(string id)
            => await _hubProxy.Invoke("ChooseAccount", id);

        public virtual async Task GetUsersAsync()
            => await _hubProxy.Invoke("GetAccounts");

        public virtual async Task SendMessageAsync(string message, string receiverId)
            => await _hubProxy.Invoke("SendMessage", message, receiverId);

        public virtual async Task SignInAsync(string name)
        {
            _hubProxy["Name"] = name;
            await _hubProxy.Invoke("SignIn");
        }
        #endregion


        #region Override
        protected override void InitializeProxy()
        {
            base.InitializeProxy();
            _hubProxy.On("announceChosen", (string id, bool isChosen) =>
                                           OnChosenAnnounced(id, isChosen));
            _hubProxy.On("receiveAccounts",
                (IEnumerable<KeyValuePair<string, string>> users) =>
                OnAccountsReceived(users.Select(p => new Account { Id = p.Key, Name = p.Value })));
            _hubProxy.On("receiveNewAccount",
                (string id, string name) => OnNewAccountReceived(new Account { Id = id, Name = name }));
            _hubProxy.On("receiveMessage", (string message, string senderId) => OnMessagedReceived(message, senderId));
            _hubProxy.On("removeAccount", (string id) => OnAccountRemoved(id));
        }
        #endregion


        #region Implementation
        private void OnAccountRemoved(string id)
            => OnAccountRemoved(new AccountRemovedEventArgs { Id = id });

        protected virtual void OnAccountRemoved(AccountRemovedEventArgs e)
            => AccountRemoved?.Invoke(this, e);

        protected virtual void OnAccountsReceived(IEnumerable<Account> accounts)
            => OnAccountsReceived(new AccountsReceivedEventArgs { Accounts = accounts });

        protected virtual void OnAccountsReceived(AccountsReceivedEventArgs e)
            => AccountsReceived?.Invoke(this, e);

        private void OnChosenAnnounced(string id, bool isChosen)
            => OnChosenAnnounced(new ChosenAnnouncedEventArgs { Id = id, IsChosen = isChosen });

        protected virtual void OnChosenAnnounced(ChosenAnnouncedEventArgs e)
            => ChosenAnnounced?.Invoke(this, e);

        protected virtual void OnMessagedReceived(string message, string senderId)
            => OnMessagedReceived(new MessageReceivedEventArgs { Message = message, SenderId = senderId });

        protected virtual void OnMessagedReceived(MessageReceivedEventArgs e)
            => MessagedReceived?.Invoke(this, e);

        private void OnNewAccountReceived(Account account)
            => OnNewAccountReceived(new AccountReceivedEventArgs { Account = account });

        protected virtual void OnNewAccountReceived(AccountReceivedEventArgs e)
            => NewAccountReceived?.Invoke(this, e);
        #endregion
    }
}