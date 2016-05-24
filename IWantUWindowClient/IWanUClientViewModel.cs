using System;
using System.Collections.Generic;
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
        private Account[] _friends = new Account[0];
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
            _proxy.MessagedReceived += Proxy_MessagedReceived;
            SendMessageCommand = DelegateCommand.FromAsyncHandler(SendMessageAsync);
            SignInCommand = DelegateCommand.FromAsyncHandler(SignInAsync);
        }
        #endregion


        #region  Properties & Indexers
        public Account[] Friends
        {
            get { return _friends; }
            private set { SetProperty(ref _friends, value); }
        }

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (SetProperty(ref _name, value))
                {
                    _proxy.Name = value;
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
                }
            }
        }

        public Message SelectedMessage
        {
            get { return _selectedMessage; }
            private set { SetProperty(ref _selectedMessage, value); }
        }

        public ICommand SendMessageCommand { get; }

        public ICommand SignInCommand { get; }
        #endregion


        #region Methods
        public async Task SendMessageAsync()
            => await _proxy.SendMessageAsync(Message, SelectedFriend.Id);

        public async Task SignInAsync()
            => await _proxy.ConnectAsync();
        #endregion


        #region Event Handlers
        private void Proxy_AccountsReceived(object sender, AccountsReceivedEventArgs e)
        {
            Friends = e.Accounts;
        }

        private void Proxy_MessagedReceived(object sender, MessageReceivedEventArgs e)
        {
            SelectedFriend = Friends.FirstOrDefault(f => f.Id == e.SenderId);
            AddMessageContent(SelectedMessage, e.Message);
        }
        #endregion


        #region Implementation
        private void AddMessageContent(Message message, string content)
        {
            var text = $"{message.Friend.Name}: {content}";
            message.Content = message.Content == null ? text : message.Content + Environment.NewLine + text;
        }

        private Message GetMessage(string friendId)
        {
            var msg = _messages.FirstOrDefault(m => m.Friend.Id == friendId);
            if (msg == null)
            {
                _messages.Add(msg = new Message { Friend = Friends.FirstOrDefault(a => a.Id == friendId) });
            }
            return msg;
        }
        #endregion
    }
}