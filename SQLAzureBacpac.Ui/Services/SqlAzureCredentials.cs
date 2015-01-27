using Caliburn.Micro;

namespace SQLAzureBacpac.Ui.Services
{
    public class SqlAzureCredentials : PropertyChangedBase
    {
        private string _serverName;
        private string _storageKey;
        private string _username;
        private string _password;

        public string ServerName
        {
            get { return _serverName; }
            set
            {
                if (value == _serverName) return;
                _serverName = value;
                NotifyOfPropertyChange(() => ServerName);
            }
        }

        public string StorageKey
        {
            get { return _storageKey; }
            set
            {
                if (value == _storageKey) return;
                _storageKey = value;
                NotifyOfPropertyChange(() => StorageKey);
            }
        }

        public string Username
        {
            get { return _username; }
            set
            {
                if (value == _username) return;
                _username = value;
                NotifyOfPropertyChange(() => Username);
            }
        }

        public string Password
        {
            get { return _password; }
            set
            {
                if (value == _password) return;
                _password = value;
                NotifyOfPropertyChange(() => Password);
            }
        }

        public bool IsValid()
        {
            return (!string.IsNullOrEmpty(ServerName) && !string.IsNullOrEmpty(StorageKey) &&
                    !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password));

        }
    }
}
