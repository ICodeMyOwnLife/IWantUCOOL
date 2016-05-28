using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CB.Net.SignalR.Client;
using IWantUClientInfrastructure;
using IWantUInfrastructure;
using Microsoft.Practices.Prism.Commands;


namespace IWantUWindowClient
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
            /*_proxy.AccountsReceived += Proxy_AccountsReceived;
            _proxy.AccountRemoved += Proxy_AccountRemoved;
            _proxy.ChosenAnnounced += Proxy_ChosenAnnounced;
            _proxy.MessagedReceived += Proxy_MessagedReceived;
            _proxy.NewAccountReceived += Proxy_NewAccountReceived;*/
            ChooseFriendAsyncCommand = DelegateCommand.FromAsyncHandler(ChooseFriendAsync, () => CanChooseFriend);
            SendMessageAsyncCommand = DelegateCommand.FromAsyncHandler(SendMessageAsync, () => CanSendMessage);
            SignInAsyncCommand = DelegateCommand.FromAsyncHandler(SignInAsync, () => CanSignIn);
            SignOutAsyncCommand = DelegateCommand.FromAsyncHandler(SignOutAsync, () => CanSignOut);
        }
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

        public ICommand ChooseFriendAsyncCommand { get; }

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

        public ICommand SendMessageAsyncCommand { get; }
        public ICommand SignInAsyncCommand { get; }
        public ICommand SignOutAsyncCommand { get; }

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


        #region Event Handlers
        private void Proxy_AccountRemoved(object sender, AccountRemovedEventArgs e)
            => RemoveFriendOnUiThread(e.Id);

        private void Proxy_AccountsReceived(object sender, AccountsReceivedEventArgs e)
            => Friends = new ObservableCollection<Account>(e.Accounts);

        private void Proxy_ChosenAnnounced(object sender, ChosenAnnouncedEventArgs e)
        {
            var friend = GetFriend(e.Id);
            string title, content;
            switch (e.ChoiceResult)
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

        private void Proxy_MessagedReceived(object sender, MessageReceivedEventArgs e)
        {
            SelectedFriend = Friends.FirstOrDefault(f => f.Id == e.SenderId);
            SelectedMessage.AddMessageContent(e.Message);
        }

        private void Proxy_NewAccountReceived(object sender, AccountReceivedEventArgs e)
            => AddFriendOnUiThread(e.Account);
        #endregion


        #region Implementation
        private void AddFriendOnUiThread(Account account) => TryInvokeOnUiThread(() => _friends.Add(account));

        private Account GetFriend(string id) => Friends.FirstOrDefault(f => f.Id == id);

        private Message GetMessage(string friendId)
        {
            var msg = _messages.FirstOrDefault(m => m.Friend.Id == friendId);
            if (msg == null)
            {
                _messages.Add(msg = new Message { Friend = Friends.FirstOrDefault(a => a.Id == friendId) });
            }
            return msg;
        }

        private void RaiseSigningCanExecuteChanged()
        {
            RaiseCommandsCanExecuteChanged(SignInAsyncCommand, SignOutAsyncCommand);
        }

        private void RemoveFriendOnUiThread(string accountId)
            => TryInvokeOnUiThread(() => _friends.Remove(_friends.FirstOrDefault(f => f.Id == accountId)));

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

// TODO: Proxy.On
// TODO: Textbox ScrollToEnd, EnterToClick
// TODO: Confirm when make choice
// TODO: Allow rechoose??
// TODO: WebServer