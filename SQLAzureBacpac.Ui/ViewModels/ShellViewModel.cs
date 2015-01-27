using System;
using System.IO;
using System.Threading.Tasks;
using Caliburn.Micro;
using SQLAzureBacpac.Ui.Properties;
using SQLAzureBacpac.Ui.Services;

namespace SQLAzureBacpac.Ui.ViewModels
{
    public class ShellViewModel : Screen
    {
        #region fields

        private readonly IWindowManager _windowManager;
        private string _database;
        private string _localFolder;
        private int _progress;
        private SqlAzureCredentials _sqlAzureCredentials;
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
            SqlAzureCredentials.ServerName = Settings.Default.servername;
            SqlAzureCredentials.StorageKey = Settings.Default.storagekey;
            SqlAzureCredentials.Password = Settings.Default.password;
            SqlAzureCredentials.Username = Settings.Default.username;
            StorageAccount = Settings.Default.storageaccount;
            LocalFolder = Settings.Default.localfolder;
            Database = Settings.Default.database;

            base.OnViewLoaded(view);
        }

        public async Task ExportAsync()
        {
            try
            {
                SaveSettings(SqlAzureCredentials, Database);
                Progress = 1;

                var progress = new Progress<int>(i => Progress = i);

                if (SqlAzureCredentials.IsValid() && !string.IsNullOrEmpty(Database) && !string.IsNullOrEmpty(StorageAccount))
                {
                    var ieHelper = new ImportExportHelper();

                    ieHelper.EndPointUri = @"https://am1prod-dacsvc.azure.com/DACWebService.svc/";
                    ieHelper.ServerName = SqlAzureCredentials.ServerName;
                    ieHelper.StorageKey = SqlAzureCredentials.StorageKey;
                    ieHelper.DatabaseName = Database;
                    ieHelper.UserName = SqlAzureCredentials.Username;
                    ieHelper.Password = SqlAzureCredentials.Password;

                    string bacpacName = string.Format("{0}{1}", Database, DateTime.Now.Ticks);

                    await
                        ieHelper.DoExportAsync(
                            String.Format(@"http://{0}.blob.core.windows.net/bacpac/{1}.bacpac", StorageAccount,
                                bacpacName), progress);

                    await SaveBacpac(bacpacName);
                    Progress = 90;
                    await DeleteBacpacInCloudAsync(bacpacName);
                    Progress = 100;
                }
            }
            catch (AggregateException aggregateException)
            {
                foreach (Exception innerException in aggregateException.InnerExceptions)
                {
                    Console.WriteLine(innerException);
                }
                throw;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        private async Task DeleteBacpacInCloudAsync(string bacpacName)
        {
            var utility =
                new AzureBlobUtility(
                    string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", StorageAccount,
                        SqlAzureCredentials.StorageKey));

            await utility.DeleteFileAsync(bacpacName + ".bacpac", "bacpac");
        }

        private async Task SaveBacpac(string bacpacName)
        {
            var utility =
                new AzureBlobUtility(
                    string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", StorageAccount,
                        SqlAzureCredentials.StorageKey));
            MemoryStream stream = await utility.DownloadAsync(bacpacName + ".bacpac", "bacpac");
            stream.Position = 0;
            string file = Path.Combine(LocalFolder, bacpacName + ".bacpac");
            var fileStream = new FileStream(file, FileMode.Create, FileAccess.Write);
            stream.WriteTo(fileStream);
            fileStream.Close();
            stream.Close();
        }

        private void SaveSettings(SqlAzureCredentials sqlAzureCredentials, string database)
        {
            Settings.Default.database = database;
            Settings.Default.servername = sqlAzureCredentials.ServerName;
            Settings.Default.storagekey = sqlAzureCredentials.StorageKey;
            Settings.Default.username = sqlAzureCredentials.Username;
            Settings.Default.password = sqlAzureCredentials.Password;
            Settings.Default.localfolder = LocalFolder;
            Settings.Default.storageaccount = StorageAccount;
            Settings.Default.Save();
        }

        #endregion
    }
}