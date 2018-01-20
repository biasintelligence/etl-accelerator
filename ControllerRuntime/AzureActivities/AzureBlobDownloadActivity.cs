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

using Serilog;

using ControllerRuntime;

namespace AzureActivities
{
    /// <summary>
    /// returns true if file exists
    /// </summary>
    public class AzureBlobDownloadActivity : IWorkflowActivity
    {
        private const string CONNECTION_STRING = "ConnectionString";
        private const string INPUT_PREFIX = "Prefix";
        private const string OUTPUT_FOLDER = "OutputFolder";
        private const string CONTAINER_NAME = "Container";
        private const string ACCOUNT_NAME = "AccountName";
        private const string ACCOUNT_KEY = "AccountKey";
        private const string IS_SAS_TOKEN = "isSasToken";
        private const string TIMEOUT = "Timeout";
        private const string SORT_ORDER = "SortOrder";
        private const string COUNT = "Count";
        private const string COUNTER_NAME = "CounterName";

        private const string ETL_RUNID = "@RunId";
        private const string ETL_BATCHID = "etl:BatchId";
        private const string ETL_STEPID = "etl:StepId";

        private static readonly List<string> sortList = new List<string>(2) { "Asc", "Desc", "None" };
        private const int MAX_COUNT = 0;


        private Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private ILogger _logger;
        private List<string> _required_attributes = new List<string>()
        { CONNECTION_STRING,
            INPUT_PREFIX,
            OUTPUT_FOLDER,
            CONTAINER_NAME,
            ACCOUNT_NAME,
            ACCOUNT_KEY,
            IS_SAS_TOKEN,
            TIMEOUT,
            SORT_ORDER,
            COUNT,
            COUNTER_NAME,
            ETL_RUNID,
            ETL_BATCHID,
            ETL_STEPID
        };


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
                {
                    if (attribute.Name.Equals(SORT_ORDER, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (!sortList.Contains<string>(attribute.Value, StringComparer.InvariantCultureIgnoreCase))
                        {
                            throw new ArgumentException(String.Format("Invalid SortOrder:{0}. Supported: Asc,Desc.", attribute.Value));
                        }

                    }
                    _attributes.Add(attribute.Name, attribute.Value);
                }

            }

            _logger.Information("Download: {From} -> {To}", _attributes[INPUT_PREFIX], _attributes[OUTPUT_FOLDER]);
            _logger.Debug("Sort: {Sort}, Count: {Count}", _attributes[SORT_ORDER], _attributes[COUNT]);

        }

        public WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            int runId = 0;
            Int32.TryParse(_attributes[ETL_RUNID], out runId);

            int batchId = 0;
            Int32.TryParse(_attributes[ETL_BATCHID], out batchId);

            int stepId = 0;
            Int32.TryParse(_attributes[ETL_STEPID], out stepId);

            bool setCounterInd = !String.IsNullOrEmpty(_attributes[COUNTER_NAME]);

            int count = 0;
            Int32.TryParse(_attributes[COUNT], out count);
            if (count <= 0) count = MAX_COUNT;

            ControllerCounter counter = new ControllerCounter(_attributes[CONNECTION_STRING], _logger)
            {
                BatchId = batchId,
                StepId = stepId,
                RunId = runId
            };


            try
            {
                CloudStorageAccount account;
                if (Boolean.Parse(_attributes[IS_SAS_TOKEN]))
                {
                    StorageCredentials credentials = new StorageCredentials(_attributes[ACCOUNT_KEY]);
                    account = new CloudStorageAccount(credentials, _attributes[ACCOUNT_NAME], endpointSuffix: null, useHttps: true);
                }
                else
                {
                    StorageCredentials credentials = new StorageCredentials(_attributes[ACCOUNT_NAME], _attributes[ACCOUNT_KEY]);
                    account = new CloudStorageAccount(credentials, useHttps: true);
                }

                CloudBlobClient blobClient = account.CreateCloudBlobClient();

                //var container_list = blobClient.ListContainers();
                //List<string> containerItems = container_list.OfType<CloudBlobContainer>().Select(b => b.Name).ToList();


                CloudBlobContainer container = blobClient.GetContainerReference(_attributes[CONTAINER_NAME]);
                var list = container.ListBlobs(_attributes[INPUT_PREFIX], true);
                //List<string> blobParents = list.OfType<CloudBlob>().Select(b => b.Parent.Prefix).Distinct().ToList();

                if (_attributes[SORT_ORDER].Equals(sortList[0], StringComparison.InvariantCultureIgnoreCase))
                {
                    list = list.OfType<CloudBlob>().OrderBy(b => b.Name);
                }
                if (_attributes[SORT_ORDER].Equals(sortList[1], StringComparison.InvariantCultureIgnoreCase))
                {
                    list = list.OfType<CloudBlob>().OrderByDescending(b => b.Name);
                }

                if (count > 0)
                {
                    list = list.OfType<CloudBlob>().Take(count);
                }

                Dictionary<string, string> files = new Dictionary<string, string>();
                Directory.CreateDirectory(_attributes[OUTPUT_FOLDER]);
                int i = 0;
                foreach (var blobItem in list.OfType<CloudBlob>())
                {
                    token.ThrowIfCancellationRequested();
                    string outputFile = Path.Combine(_attributes[OUTPUT_FOLDER], blobItem.Name.Replace('/', '-'));
                    if (File.Exists(outputFile))
                        continue;

                    //CloudBlob blob = container.GetBlobReference(blobItem);
                    blobItem.DownloadToFile(outputFile, FileMode.OpenOrCreate);
                    if (setCounterInd)
                        files.Add(String.Format("{0}_{1}", _attributes[COUNTER_NAME], i++), outputFile);
                }

                if (setCounterInd)
                    counter.SetCounters(files);

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
