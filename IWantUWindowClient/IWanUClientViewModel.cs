using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private ObservableCollection<Account> _friends = new ObservableCollection<Account>();
        private string _message;
        private readonly IList<Message> _messages = new List<Message>();
        private string _name;
        private Account _selectedFriend;
        private Message _selectedMessage;
        #endregion


        #region  Constructors & Destructor
        public IWanUClientViewModel(): base(new IWantUProxy())
        {
            _proxy.AccountsReceived += Proxy_AccountsReceived;
            _proxy.AccountRemoved += Proxy_AccountRemoved;
            _proxy.ChosenAnnounced += Proxy_ChosenAnnounced;
            _proxy.MessagedReceived += Proxy_MessagedReceived;
            _proxy.NewAccountReceived += Proxy_NewAccountReceived;
            ChooseFriendAsyncCommand = DelegateCommand.FromAsyncHandler(ChooseFriendAsync);
            SendMessageAsyncCommand = DelegateCommand.FromAsyncHandler(SendMessageAsync);
            SignInAsyncCommand = DelegateCommand.FromAsyncHandler(SignInAsync);
        }
        #endregion


        #region  Properties & Indexers
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
            set { SetProperty(ref _message, value); }
        }

        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        public Account SelectedFriend
        {
            get { return _selectedFriend; }
            set
            {
                if (SetProperty(ref _selectedFriend, value))
                {
                    SelectedMessage = GetMessage(value.Id);
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
        #endregion


        #region Methods
        public async Task ChooseFriendAsync()
            => await _proxy.ChooseUser(SelectedFriend.Id);

        public async Task SendMessageAsync()
            => await _proxy.SendMessageAsync(Message, SelectedFriend.Id);

        public async Task SignInAsync()
        {
            await _proxy.ConnectAsync();
            await _proxy.SignInAsync(Name);
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
            if (e.IsChosen)
            {
                title = "Congratulation!";
                content = $"{friend.Name} chose you.";
            }
            else
            {
                title = "Sorry!";
                content = $"{friend.Name} didn't choose you.";
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
        private void AddFriendOnUiThread(Account account)
            => TryInvokeOnUiThread(() => _friends.Add(account));

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

        private void RemoveFriendOnUiThread(string accountId)
            => TryInvokeOnUiThread(() => _friends.Remove(_friends.FirstOrDefault(f => f.Id == accountId)));
        #endregion
    }
}