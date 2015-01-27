using System;
using System.IO;
using System.Threading.Tasks;
using Caliburn.Micro;
using SQLAzureBacpac.Ui.Services;

namespace SQLAzureBacpac.Ui.ViewModels
{
    public class ShellViewModel :  Screen
    {
        #region fields

        private readonly IWindowManager _windowManager;
        private SqlAzureCredentials _sqlAzureCredentials;
        private string _database;
        private int _progress;
        private string _localFolder;
        private string _storageAccount;

        #endregion

        #region bindings

        public string StorageAccount
        {
            get { return _storageAccount; }
            set
            {
                if (value == _storageAccount) return;
                _storageAccount = value;
                NotifyOfPropertyChange(() => StorageAccount);
            }
        }

        public string LocalFolder
        {
            get { return _localFolder; }
            set
            {
                if (value == _localFolder) return;
                _localFolder = value;
                NotifyOfPropertyChange(() => LocalFolder);
            }
        }

        public SqlAzureCredentials SqlAzureCredentials
        {
            get { return _sqlAzureCredentials; }
            set
            {
                if (Equals(value, _sqlAzureCredentials)) return;
                _sqlAzureCredentials = value;
                NotifyOfPropertyChange(() => SqlAzureCredentials);
            }
        }

        public int Progress
        {
            get { return _progress; }
            set
            {
                if (value == _progress) return;
                _progress = value;
                NotifyOfPropertyChange(() => Progress);
            }
        }

        public string Database
        {
            get { return _database; }
            set
            {
                if (value == _database) return;
                _database = value;
                NotifyOfPropertyChange(() => Database);
            }
        }
        #endregion

        public ShellViewModel(IWindowManager windowManager)
        {
            _windowManager = windowManager;
        }

        #region methods
        protected override void OnViewLoaded(object view)
        {
            SqlAzureCredentials = new SqlAzureCredentials();
            SqlAzureCredentials.ServerName = Properties.Settings.Default.servername;
            SqlAzureCredentials.StorageKey = Properties.Settings.Default.storagekey;
            SqlAzureCredentials.Password = Properties.Settings.Default.password;
            SqlAzureCredentials.Username = Properties.Settings.Default.username;
            LocalFolder = Properties.Settings.Default.localfolder;
            Database = Properties.Settings.Default.database;

            base.OnViewLoaded(view);
        }

        public async Task ExportAsync()
        {
            SaveSettings(SqlAzureCredentials, Database);
            Progress = 1;

            var progress = new Progress<int>(i => Progress = i);

            if (SqlAzureCredentials.IsValid() && !string.IsNullOrEmpty(Database))
            {
                var ieHelper = new ImportExportHelper();

                ieHelper.EndPointUri = @"https://am1prod-dacsvc.azure.com/DACWebService.svc/";
                ieHelper.ServerName = SqlAzureCredentials.ServerName;
                ieHelper.StorageKey = SqlAzureCredentials.StorageKey;
                ieHelper.DatabaseName = Database;
                ieHelper.UserName = SqlAzureCredentials.Username;
                ieHelper.Password = SqlAzureCredentials.Password;

                var bacpacName = string.Format("{0}-{1}", Database, DateTime.Now.ToString("dd-MM-yyyy-HH-mm"));

                await ieHelper.DoExportAsync(String.Format(@"http://{0}.blob.core.windows.net/bacpac/{1}.bacpac", StorageAccount, bacpacName), progress);

                SaveBacpac(bacpacName);
                Progress = 90;
                DeleteBacpacInCloud(bacpacName);
                Progress = 100;

                
            }
        }

        private void DeleteBacpacInCloud(string bacpacName)
        {
            var utility =
                new AzureBlobUtility(
                    string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", StorageAccount, SqlAzureCredentials.StorageKey));

            utility.DeleteFile(bacpacName + ".bacpac", "bacpac");
        }

        private void SaveBacpac(string bacpacName)
        {
            var utility =
                new AzureBlobUtility(
                    string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", StorageAccount, SqlAzureCredentials.StorageKey));
            var stream = utility.Download(bacpacName + ".bacpac", "bacpac");
            stream.Position = 0;
            var file = Path.Combine(LocalFolder, bacpacName + ".bacpac");
            var fileStream = new FileStream(file, FileMode.Create, FileAccess.Write);
            stream.WriteTo(fileStream);
            fileStream.Close();
            stream.Close();
        }

        private void SaveSettings(SqlAzureCredentials sqlAzureCredentials, string database)
        {
            Properties.Settings.Default.database = database;
            Properties.Settings.Default.servername = sqlAzureCredentials.ServerName;
            Properties.Settings.Default.storagekey = sqlAzureCredentials.StorageKey;
            Properties.Settings.Default.username = sqlAzureCredentials.Username;
            Properties.Settings.Default.password = sqlAzureCredentials.Password;
            Properties.Settings.Default.localfolder = LocalFolder;
            Properties.Settings.Default.Save();
        }
        #endregion
    }
}
