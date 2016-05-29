using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CB.Model.Prism;
using IWantUInfrastructure;


namespace IWantUWindowClient.ViewModels
{
    public class CreateGroupViewModel: ConfirmationViewModelBase
    {
        #region Fields
        private ObservableCollection<Account> _accounts;
        private string _groupName;
        private IList _selectedAccounts;
        private bool _userSet;
        #endregion


        #region  Properties & Indexers
        public IEnumerable<Account> Accounts
        {
            get { return _accounts; }
            set
            {
                SetProperty(ref _accounts,
                    value as ObservableCollection<Account> ?? new ObservableCollection<Account>(value));
            }
        }

        public string GroupName
        {
            get { return _groupName; }
            set
            {
                if (SetProperty(ref _groupName, value))
                {
                    _userSet = true;
                }
            }
        }

        public IList SelectedAccounts
        {
            get { return _selectedAccounts; }
            set
            {
                SetProperty(ref _selectedAccounts, value);
                if (_userSet) return;

                _groupName = string.Join(", ", SelectedAccounts.OfType<Account>().Select(a => a.Name));
                NotifyPropertyChanged(nameof(GroupName));
            }
        }
        #endregion
    }
}