using System.Collections.Generic;
using System.Threading.Tasks;
using CB.Net.SignalR;
using CB.Net.SignalR.Client;
using IWantUInfrastructure;


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
        #endregion


        #region Implementation
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


        public async Task<string> AddGroupAsync(string groupName, IEnumerable<string> ids)
            => await _hubProxy.Invoke<string>("AddGroup", groupName, ids);
    }
}