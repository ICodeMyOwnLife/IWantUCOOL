using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CB.Net.SignalR.Client;
using CB.Prism.Interactivity;
using IWantUClientInfrastructure;
using IWantUInfrastructure;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Practices.Prism.Commands;


namespace IWantUWindowClient.ViewModels
{
    // ReSharper disable once InconsistentNaming
    public class IWanUClientViewModel: SignalRClientViewModelBase<IWantUProxy>
    {
        #region Fields
        private bool _canChooseFriend;
        private bool _canSendMessage;
        private bool _canSignIn;
        private bool _canSignOut;
        private ObservableCollection<Account> _friends = new ObservableCollection<Account>();
        private string _message;
        private readonly IList<Message> _messages = new List<Message>();
        private Account _selectedFriend;
        private Message _selectedMessage;
        private string _userName;
        #endregion


        #region  Constructors & Destructor
        public IWanUClientViewModel()
        {
            AddGroupCommand = new DelegateCommand(AddGroup);
            ChooseFriendAsyncCommand = DelegateCommand.FromAsyncHandler(ChooseFriendAsync, () => CanChooseFriend);
            SendMessageAsyncCommand = DelegateCommand.FromAsyncHandler(SendMessageAsync, () => CanSendMessage);
            SignInAsyncCommand = DelegateCommand.FromAsyncHandler(SignInAsync, () => CanSignIn);
            SignOutAsyncCommand = DelegateCommand.FromAsyncHandler(SignOutAsync, () => CanSignOut);
        }
        #endregion


        #region  Commands
        public ICommand AddGroupCommand { get; }
        public ICommand ChooseFriendAsyncCommand { get; }
        public ICommand SendMessageAsyncCommand { get; }
        public ICommand SignInAsyncCommand { get; }
        public ICommand SignOutAsyncCommand { get; }
        #endregion


        #region  Properties & Indexers
        public virtual bool CanChooseFriend
        {
            get { return _canChooseFriend; }
            protected set
            {
                if (SetProperty(ref _canChooseFriend, value))
                {
                    RaiseCommandsCanExecuteChanged(ChooseFriendAsyncCommand);
                }
            }
        }

        public virtual bool CanSendMessage
        {
            get { return _canSendMessage; }
            protected set
            {
                if (SetProperty(ref _canSendMessage, value))
                {
                    RaiseCommandsCanExecuteChanged(SendMessageAsyncCommand);
                }
            }
        }

        public virtual bool CanSignIn
        {
            get { return _canSignIn; }
            protected set
            {
                if (SetProperty(ref _canSignIn, value))
                {
                    RaiseSigningCanExecuteChanged();
                }
            }
        }

        public virtual bool CanSignOut
        {
            get { return _canSignOut; }
            protected set
            {
                if (SetProperty(ref _canSignOut, value))
                {
                    RaiseSigningCanExecuteChanged();
                }
            }
        }

        public ConfirmationInteractionRequest<CreateGroupViewModel> CreateGroupRequest { get; } =
            new ConfirmationInteractionRequest<CreateGroupViewModel>();

        public IEnumerable<Account> Friends
        {
            get { return _friends; }
            private set
            {
                SetProperty(ref _friends,
                    value as ObservableCollection<Account> ?? new ObservableCollection<Account>(value));
            }
        }

        public string Message
        {
            get { return _message; }
            set
            {
                if (SetProperty(ref _message, value))
                {
                    SetSendMessageAbility();
                }
            }
        }

        public Account SelectedFriend
        {
            get { return _selectedFriend; }
            set
            {
                if (SetProperty(ref _selectedFriend, value))
                {
                    SelectedMessage = GetMessage(value.Id);
                    SetChoosingAbility();
                }
            }
        }

        public Message SelectedMessage
        {
            get { return _selectedMessage; }
            private set { SetProperty(ref _selectedMessage, value); }
        }

        public string UserName
        {
            get { return _userName; }
            set
            {
                if (SetProperty(ref _userName, value))
                {
                    SetSigningAbility();
                }
            }
        }
        #endregion


        #region Methods
        public void AddGroup()
        {
            var vmd = new CreateGroupViewModel { Accounts = Friends };
            CreateGroupRequest.Raise(vmd, async res =>
            {
                if (!res.Confirmed || !(res.SelectedAccounts?.Count > 0)) return;
                
                var groupId = await _proxy.AddGroupAsync(res.GroupName,
                    res.SelectedAccounts.OfType<Account>().Select(a => a.Id));

                NotificationRequestProvider.Notify("Group Id", groupId);
            });
        }

        public async Task ChooseFriendAsync()
            => await _proxy.ChooseUser(SelectedFriend.Id);

        public async Task SendMessageAsync()
        {
            await _proxy.SendMessageAsync(Message, SelectedFriend.Id);
            Message = null;
        }

        public async Task SignInAsync()
        {
            if (!CanSignIn) return;

            await _proxy.SignInAsync(UserName);
        }

        public async Task SignOutAsync()
        {
            if (!CanSignOut) return;

            await _proxy.SignOutAsync();
        }
        #endregion


        #region Override
        protected override void InitializeHubProxy(IHubProxy hubProxy)
        {
            base.InitializeHubProxy(hubProxy);

            hubProxy.On<string, ChoiceResult>("announceChosen", Proxy_ChosenAnnounced);
            hubProxy.On<IEnumerable<KeyValuePair<string, string>>>("receiveAccounts", Proxy_AccountsReceived);
            hubProxy.On<string, string>("receiveNewAccount",
                Proxy_NewAccountReceived);
            hubProxy.On<string, string>("receiveMessage", Proxy_MessagedReceived);
            hubProxy.On<string>("removeAccount", Proxy_AccountRemoved);
        }

        protected override void OnProxyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnProxyPropertyChanged(sender, e);
            switch (e.PropertyName)
            {
                case nameof(IWantUProxy.SignState):
                case nameof(IWantUProxy.ConnectionState):
                    SetSigningAbility();
                    SetSendMessageAbility();
                    SetChoosingAbility();
                    break;
            }
        }
        #endregion


        #region Implementation
        private void AddFriendOnUiThread(Account account) => TryInvokeOnUiThread(() => _friends.Add(account));

        private Account GetFriend(string id)
            => Friends.FirstOrDefault(f => f.Id == id);

        private Message GetMessage(string friendId)
        {
            var msg = _messages.FirstOrDefault(m => m.Friend.Id == friendId);
            if (msg == null)
            {
                _messages.Add(msg = new Message { Friend = Friends.FirstOrDefault(a => a.Id == friendId) });
            }
            return msg;
        }

        private void Proxy_AccountRemoved(string id)
        {
            var removedFriend = GetFriend(id);
            RemoveFriendOnUiThread(removedFriend);
            if (SelectedFriend == removedFriend)
            {
                SelectedFriend = Friends.FirstOrDefault();
            }
        }

        private void Proxy_AccountsReceived(IEnumerable<KeyValuePair<string, string>> accountPairs)
            => Friends =
               new ObservableCollection<Account>(accountPairs.Select(p => new Account { Id = p.Key, Name = p.Value }));

        private void Proxy_ChosenAnnounced(string id, ChoiceResult result)
        {
            var friend = GetFriend(id);
            string title, content;
            switch (result)
            {
                case ChoiceResult.Undone:
                    title = "Wait!";
                    content = $"{friend.Name} didn't make a choice.";
                    break;
                case ChoiceResult.Successful:
                    title = "Congratulation!";
                    content = $"{friend.Name} chose you.";
                    break;
                case ChoiceResult.Failed:
                    title = "Sorry!";
                    content = $"{friend.Name} didn't choose you.";
                    break;
                case ChoiceResult.Done:
                    title = "Error!";
                    content = "You completed your choice.";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            NotificationRequestProvider.NotifyOnUiThread(title, content);
        }

        private void Proxy_MessagedReceived(string message, string senderId)
        {
            SelectedFriend = GetFriend(senderId);
            SelectedMessage.AddMessageContent(message);
        }

        private void Proxy_NewAccountReceived(string id, string name)
            => AddFriendOnUiThread(new Account { Id = id, Name = name });

        private void RaiseSigningCanExecuteChanged()
        {
            RaiseCommandsCanExecuteChanged(SignInAsyncCommand, SignOutAsyncCommand);
        }

        private void RemoveFriendOnUiThread(Account account)
            => TryInvokeOnUiThread(() => _friends.Remove(account));

        private void SetChoosingAbility()
            => CanChooseFriend = _proxy.CanChooseFriend() && SelectedFriend != null;

        private void SetSendMessageAbility()
            => CanSendMessage = _proxy.CanSendMessage() && !string.IsNullOrEmpty(Message) && SelectedFriend != null;

        private void SetSigningAbility()
        {
            CanSignIn = _proxy.CanSignIn() && !string.IsNullOrEmpty(UserName);
            CanSignOut = _proxy.CanSignOut();
        }
        #endregion
    }
}


// TODO: Implement group, chat group
// TODO: Full Message (1st person, group person, media)
// TODO: Confirm when make choice
// TODO: Allow rechoose??
// TODO: WebServer