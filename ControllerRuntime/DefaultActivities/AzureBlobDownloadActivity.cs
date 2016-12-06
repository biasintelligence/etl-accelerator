/******************************************************************
**          BIAS Intelligence LLC
**
**
**Auth:     Andrey Shishkarev
**Date:     05/12/2016
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
*******************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;


using ControllerRuntime;

namespace DefaultActivities
{
    /// <summary>
    /// returns true if file exists
    /// </summary>
    public class AzureBlobDownloadActivity : IWorkflowActivity
    {
        private const string INPUT_PREFIX = "Prefix";
        private const string OUTPUT_FOLDER = "OutputFolder";
        private const string CONTAINER_NAME = "Container";
        private const string SAS_TOKEN = "SasToken";
        private const string ACCOUNT_NAME = "AccountName";
        private const string TIMEOUT = "Timeout";


        private Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private IWorkflowLogger _logger;
        private List<string> _required_attributes = new List<string>()
        { INPUT_PREFIX, OUTPUT_FOLDER,CONTAINER_NAME,SAS_TOKEN,ACCOUNT_NAME,TIMEOUT};


        public string[] RequiredAttributes
        {
            get { return _required_attributes.ToArray(); }
        }

        public void Configure(WorkflowActivityArgs args)
        {
            _logger = args.Logger;

            if (_required_attributes.Count != args.RequiredAttributes.Length)
            {
                //_logger.WriteError(String.Format("Not all required attributes are provided"), -11);
                throw new ArgumentException("Not all required attributes are provided");
            }


            foreach (WorkflowAttribute attribute in args.RequiredAttributes)
            {
                if (_required_attributes.Contains(attribute.Name, StringComparer.InvariantCultureIgnoreCase))
                    _attributes.Add(attribute.Name, attribute.Value);
            }

            _logger.Write(String.Format("Download: {0} -> {1}", _attributes[INPUT_PREFIX], _attributes[OUTPUT_FOLDER]));

        }

        public WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            try
            {
                StorageCredentials sasToken = new StorageCredentials(_attributes[SAS_TOKEN]);
                CloudStorageAccount account = new CloudStorageAccount(sasToken, _attributes[ACCOUNT_NAME], endpointSuffix: null, useHttps: true);
                CloudBlobClient blobClient = account.CreateCloudBlobClient();

                CloudBlobContainer container = blobClient.GetContainerReference(_attributes[CONTAINER_NAME]);
                var list = container.ListBlobs(_attributes[INPUT_PREFIX], true);
                List<string> blobItems = list.OfType<CloudBlob>().Select(b => b.Name).ToList();
                Directory.CreateDirectory(_attributes[OUTPUT_FOLDER]);
                foreach (var blobItem in blobItems)
                {
                    token.ThrowIfCancellationRequested();
                    string outputFile = Path.Combine(_attributes[OUTPUT_FOLDER], blobItem.Replace('/', '-'));
                    if (File.Exists(outputFile))
                        continue;

                    CloudBlob blob = container.GetBlobReference(blobItem);
                    blob.DownloadToFile(outputFile, FileMode.OpenOrCreate);
                }


            }
            catch (Exception ex)
            {
                throw ex;
            }
            result = WfResult.Succeeded;
            return result;
        }
    }
}
