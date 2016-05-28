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
        #region Fields
        private SignState _signState = SignState.SignedOut;
        #endregion


        #region  Constructors & Destructor
        public IWantUProxy(): base(new SignalRConfiguration()) { }
        #endregion


        #region  Properties & Indexers
        public SignState SignState
        {
            get { return _signState; }
            private set { SetProperty(ref _signState, value); }
        }
        #endregion


        #region Events
        public event EventHandler<AccountRemovedEventArgs> AccountRemoved;
        public event EventHandler<AccountsReceivedEventArgs> AccountsReceived;
        public event EventHandler<ChosenAnnouncedEventArgs> ChosenAnnounced;
        public event EventHandler<MessageReceivedEventArgs> MessagedReceived;
        public event EventHandler<AccountReceivedEventArgs> NewAccountReceived;
        #endregion


        #region Methods
        public bool CanChooseFriend() => SignState == SignState.SignedIn;
        public bool CanSendMessage() => SignState == SignState.SignedIn;

        public bool CanSignIn()
            => ConnectionState == SignalRState.Connected && SignState == SignState.SignedOut;

        public bool CanSignOut()
            => ConnectionState == SignalRState.Connected && SignState == SignState.SignedIn;

        public virtual async Task ChooseUser(string id)
            => await TryAsync(async () => await _hubProxy.Invoke("ChooseAccount", id));

        public virtual async Task GetUsersAsync()
            => await TryAsync(async () => await _hubProxy.Invoke("GetAccounts"));

        public virtual async Task SendMessageAsync(string message, string receiverId)
            => await TryAsync(async () => await _hubProxy.Invoke("SendMessage", message, receiverId));

        public virtual async Task SignInAsync(string name)
        {
            if (SignState != SignState.SignedOut)
            {
                OnSigningError();
                return;
            }

            await TryAsync(async () =>
            {
                SignState = SignState.SigningIn;
                await _hubProxy.Invoke("SignIn", name);
                SignState = SignState.SignedIn;
            }, () => SignState = SignState.SignedOut);
        }

        public virtual async Task SignOutAsync()
        {
            if (SignState != SignState.SignedIn)
            {
                OnSigningError();
                return;
            }

            await TrySignOutAsync();
        }
        #endregion


        #region Override
        public override async Task DisconnectAsync()
        {
            await TrySignOutAsync();
            await base.DisconnectAsync();
        }

        protected override void InitializeProxy()
        {
            base.InitializeProxy();
            _hubProxy.On("announceChosen", (string id, ChoiceResult result) =>
                                           OnChosenAnnounced(id, result));
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

        protected virtual void OnAccountsReceived(AccountsReceivedEventArgs e) => AccountsReceived?.Invoke(this, e);

        private void OnChosenAnnounced(string id, ChoiceResult choiceResult)
            => OnChosenAnnounced(new ChosenAnnouncedEventArgs { Id = id, ChoiceResult = choiceResult });

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

        private void OnSigningError()
        {
            OnError($"Proxy is {SignState.ToString().ToLower()}");
        }

        private async Task TrySignOutAsync()
            => await TryAsync(async () =>
            {
                SignState = SignState.SigningOut;
                await _hubProxy.Invoke("SignOut");
                SignState = SignState.SignedOut;
            }, () => SignState = SignState.SignedIn);
        #endregion
    }
}