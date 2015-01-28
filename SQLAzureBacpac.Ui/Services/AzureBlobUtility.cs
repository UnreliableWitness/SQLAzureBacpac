using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace SQLAzureBacpac.Ui.Services
{
    public sealed class AzureBlobUtility
    {
        private readonly CloudBlobClient _blobClient;


        public AzureBlobUtility(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            _blobClient = storageAccount.CreateCloudBlobClient();
        }

        public void DeleteFile(string fileName, string containerName)
        {
            var container = _blobClient.GetContainerReference(containerName.ToLower());

            var blob = container.GetBlockBlobReference(fileName);
            blob.DeleteIfExists();
        }

        public async Task<MemoryStream> DownloadAsync(string fileName, string containerName)
        {
            
            // Retrieve reference to a previously created container.
            var container = _blobClient.GetContainerReference(containerName.ToLower());

            var blob = container.GetBlockBlobReference(fileName);
            var s = new MemoryStream();
            try
            {
                await blob.DownloadToStreamAsync(s);
                return s;
            }
            catch (Exception)
            {
                return new MemoryStream();
            }
        }
    }
}
