using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using SQLAzureBacpac.Ui.DacService;

namespace SQLAzureBacpac.Ui
{
    internal class ImportExportHelper
    {
        public ImportExportHelper()
        {
            EndPointUri = "";
            ServerName = "";
            StorageKey = "";
            DatabaseName = "";
            UserName = "";
            Password = "";
        }

        public string EndPointUri { get; set; }
        public string StorageKey { get; set; }
        public string BlobUri { get; set; }
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public async Task<string> DoExportAsync(string blobUri, IProgress<int> progress)
        {
            //Setup Web Request for Export Operation 
            WebRequest webRequest = WebRequest.Create(EndPointUri + @"/Export");
            webRequest.Method = WebRequestMethods.Http.Post;
            webRequest.ContentType = @"application/xml";

            //Create Web Request Inputs - Blob Storage Credentials and Server Connection Info 
            var exportInputs = new ExportInput
            {
                BlobCredentials = new BlobStorageAccessKeyCredentials
                {
                    StorageAccessKey = StorageKey,
                    Uri = String.Format(blobUri, DatabaseName, DateTime.UtcNow.Ticks)
                },
                ConnectionInfo = new ConnectionInfo
                {
                    ServerName = ServerName,
                    DatabaseName = DatabaseName,
                    UserName = UserName,
                    Password = Password
                }
            };

            //Perform Web Request 
            Stream webRequestStream = await webRequest.GetRequestStreamAsync();
            progress.Report(20);
            var dataContractSerializer = new DataContractSerializer(exportInputs.GetType());
            dataContractSerializer.WriteObject(webRequestStream, exportInputs);
            webRequestStream.Close();

            return await ExportAwaiterAsync(webRequest, progress);
        }

        private async Task<string> ExportAwaiterAsync(WebRequest webRequest, IProgress<int> progress)
        {
            bool exportComplete = false;
            string exportedBlobPath = null;

            //Get Response and Extract Request Identifier 

            //Initialize the WebResponse to the response from the WebRequest 
            webRequest.Timeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;
            var webResponse = webRequest.GetResponse();
            progress.Report(30);
            var xmlStreamReader = XmlReader.Create(webResponse.GetResponseStream());
            xmlStreamReader.ReadToFollowing("guid");
            string requestGuid = xmlStreamReader.ReadElementContentAsString();

            //Get Export Operation Status

            return await Task.Run(async () =>
            {
                while (!exportComplete)
                {
                    List<StatusInfo> statusInfoList = CheckRequestStatus(requestGuid);

                    if (statusInfoList.FirstOrDefault().Status == "Failed")
                    {
                        Console.WriteLine("Database export failed: {0}", statusInfoList.FirstOrDefault().ErrorMessage);
                        exportComplete = true;
                    }

                    if (statusInfoList.FirstOrDefault().Status == "Completed")
                    {
                        exportedBlobPath = statusInfoList.FirstOrDefault().BlobUri;
                        Console.WriteLine("Export Complete - Database exported to: {0}\n\r", exportedBlobPath);
                        exportComplete = true;
                    }
                }
                progress.Report(80);
                return exportedBlobPath;
            });
        }

        public List<StatusInfo> CheckRequestStatus(string requestGuid)
        {
            WebRequest webRequest =
                WebRequest.Create(EndPointUri +
                                  string.Format("/Status?servername={0}&username={1}&password={2}&reqId={3}",
                                      HttpUtility.UrlEncode(ServerName),
                                      HttpUtility.UrlEncode(UserName),
                                      HttpUtility.UrlEncode(Password),
                                      HttpUtility.UrlEncode(requestGuid)));

            webRequest.Method = WebRequestMethods.Http.Get;
            webRequest.ContentType = @"application/xml";
            WebResponse webResponse = webRequest.GetResponse();
            XmlReader xmlStreamReader = XmlReader.Create(webResponse.GetResponseStream());
            var dataContractSerializer = new DataContractSerializer(typeof (List<StatusInfo>));

            return (List<StatusInfo>) dataContractSerializer.ReadObject(xmlStreamReader, true);
        }

        #region tofix

        //public bool DoImport(string blobUri)
        //{
        //    Console.Write("Starting Import Operation - {0}\n\r", DateTime.Now);
        //    string requestGuid;
        //    bool importComplete = false;

        //    //Setup Web Request for Import Operation 
        //    WebRequest webRequest = WebRequest.Create(this.EndPointUri + @"/Import");
        //    webRequest.Method = WebRequestMethods.Http.Post;
        //    webRequest.ContentType = @"application/xml";

        //    //Create Web Request Inputs - Database Size & Edition, Blob Store Credentials and Server Connection Info 
        //    ImportInput importInputs = new ImportInput
        //    {
        //        AzureEdition = "Web",
        //        DatabaseSizeInGB = 1,
        //        BlobCredentials = new BlobStorageAccessKeyCredentials
        //        {
        //            StorageAccessKey = StorageKey,
        //            Uri = String.Format(blobUri, DatabaseName, DateTime.UtcNow.Ticks.ToString())
        //        },
        //        ConnectionInfo = new ConnectionInfo
        //        {
        //            ServerName = ServerName,
        //            DatabaseName = DatabaseName,
        //            UserName = UserName,
        //            Password = Password
        //        }
        //    };

        //    //Perform Web Request 
        //    Console.WriteLine("Making Web Request for Import Operation...");
        //    Stream webRequestStream = webRequest.GetRequestStream();
        //    var dataContractSerializer = new DataContractSerializer(importInputs.GetType());
        //    dataContractSerializer.WriteObject(webRequestStream, importInputs);
        //    webRequestStream.Close();

        //    //Get Response and Extract Request Identifier 
        //    Console.WriteLine("Serializing response and extracting guid...");
        //    WebResponse webResponse = null;
        //    XmlReader xmlStreamReader = null;

        //    try
        //    {
        //        //Initialize the WebResponse to the response from the WebRequest 
        //        webResponse = webRequest.GetResponse();

        //        xmlStreamReader = XmlReader.Create(webResponse.GetResponseStream());
        //        xmlStreamReader.ReadToFollowing("guid");
        //        requestGuid = xmlStreamReader.ReadElementContentAsString();
        //        Console.WriteLine(String.Format("Request Guid: {0}", requestGuid));

        //        //Get Status of Import Operation 
        //        while (!importComplete)
        //        {
        //            Console.WriteLine("Checking status of Import...");
        //            List<StatusInfo> statusInfoList = CheckRequestStatus(requestGuid);
        //            Console.WriteLine(statusInfoList.FirstOrDefault().Status);

        //            var firstOrDefault = statusInfoList.FirstOrDefault();
        //            if (firstOrDefault != null && firstOrDefault.Status == "Failed")
        //            {
        //                Console.WriteLine(String.Format("Database import failed: {0}", firstOrDefault.ErrorMessage));
        //                importComplete = true;
        //            }

        //            var statusInfo = statusInfoList.FirstOrDefault();
        //            if (statusInfo != null && statusInfo.Status == "Completed")
        //            {
        //                Console.WriteLine(String.Format("Import Complete - Database imported to: {0}\n\r", statusInfo.DatabaseName));
        //                importComplete = true;
        //            }
        //        }
        //        return importComplete;
        //    }
        //    catch (WebException responseException)
        //    {
        //        Console.WriteLine("Request Falied: {0}", responseException.Message);
        //        {
        //            Console.WriteLine("Status Code: {0}", ((HttpWebResponse)responseException.Response).StatusCode);
        //            Console.WriteLine("Status Description: {0}\n\r", ((HttpWebResponse)responseException.Response).StatusDescription);
        //        }

        //        return importComplete;
        //    }
        //}

        #endregion
    }
}