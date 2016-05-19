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


        #region  Properties & Indexers
        public string Name { get; set; } = "User";
        #endregion


        #region Events
        public event EventHandler<AccountsReceivedEventArgs> AccountsReceived;
        public event EventHandler<MessageReceivedEventArgs> MessagedReceived;
        #endregion


        #region Methods
        public virtual async Task GetUsersAsync()
            => await _hubProxy.Invoke("GetUsers");

        public virtual async Task SendMessageAsync(string message, string receiverId)
            => await _hubProxy.Invoke("SendMessage", message, receiverId);
        #endregion


        #region Override
        protected override void InitializeProxy()
        {
            base.InitializeProxy();
            _hubProxy["Name"] = Name;
            _hubProxy.On("receiveUsers",
                (IEnumerable<KeyValuePair<string, string>> users) =>
                {
                    OnAccountsReceived(users.Select(p => new Account { Id = p.Key, Name = p.Value }).ToArray());
                });
            _hubProxy.On("receiveMessage", (string message, string senderId) => OnMessagedReceived(message, senderId));
        }
        #endregion


        #region Implementation
        protected virtual void OnAccountsReceived(Account[] accounts)
        {
            OnAccountsReceived(new AccountsReceivedEventArgs { Accounts = accounts });
        }

        protected virtual void OnAccountsReceived(AccountsReceivedEventArgs e)
        {
            AccountsReceived?.Invoke(this, e);
        }

        protected virtual void OnMessagedReceived(string message, string senderId)
        {
            OnMessagedReceived(new MessageReceivedEventArgs { Message = message, SenderId = senderId });
        }

        protected virtual void OnMessagedReceived(MessageReceivedEventArgs e)
        {
            MessagedReceived?.Invoke(this, e);
        }
        #endregion
    }
}