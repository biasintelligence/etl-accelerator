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
        private const string CONNECTION_STRING = "ConnectionString";
        private const string INPUT_PREFIX = "Prefix";
        private const string OUTPUT_FOLDER = "OutputFolder";
        private const string CONTAINER_NAME = "Container";
        private const string SAS_TOKEN = "SasToken";
        private const string ACCOUNT_NAME = "AccountName";
        private const string TIMEOUT = "Timeout";
        private const string SORT_ORDER = "SortOrder";
        private const string COUNT = "Count";
        private const string COUNTER_NAME = "CounterName";

        private const string ETL_RUNID = "@RunId";
        private const string ETL_BATCHID = "etl:BatchId";
        private const string ETL_STEPID = "etl:StepId";

        private static readonly List<string> sortList = new List<string>(2) { "Asc", "Desc" };
        private const int MAX_COUNT = 100;


        private Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private IWorkflowLogger _logger;
        private List<string> _required_attributes = new List<string>()
        { CONNECTION_STRING,
            INPUT_PREFIX,
            OUTPUT_FOLDER,
            CONTAINER_NAME,
            SAS_TOKEN,
            ACCOUNT_NAME,
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

            _logger.Write(String.Format("Download: {0} -> {1}", _attributes[INPUT_PREFIX], _attributes[OUTPUT_FOLDER]));
            _logger.WriteDebug(String.Format("Sort: {0}, Count: {1}", _attributes[SORT_ORDER], _attributes[COUNT]));

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


            ControllerCounter counter = new ControllerCounter(_attributes[CONNECTION_STRING], _logger)
            {
                BatchId = batchId,
                StepId = stepId,
                RunId = runId
            };


            try
            {
                StorageCredentials sasToken = new StorageCredentials(_attributes[SAS_TOKEN]);
                CloudStorageAccount account = new CloudStorageAccount(sasToken, _attributes[ACCOUNT_NAME], endpointSuffix: null, useHttps: true);
                CloudBlobClient blobClient = account.CreateCloudBlobClient();

                //var container_list = blobClient.ListContainers();
                //List<string> containerItems = container_list.OfType<CloudBlobContainer>().Select(b => b.Name).ToList();


                CloudBlobContainer container = blobClient.GetContainerReference(_attributes[CONTAINER_NAME]);
                var list = container.ListBlobs(_attributes[INPUT_PREFIX], true);
                //List<string> blobParents = list.OfType<CloudBlob>().Select(b => b.Parent.Prefix).Distinct().ToList();

                int count = 0;
                Int32.TryParse(_attributes[COUNT], out count);
                if (count <= 0) count = MAX_COUNT;

                List<string> blobItems;
                if (_attributes[SORT_ORDER].Equals(sortList[0], StringComparison.InvariantCultureIgnoreCase))
                {
                    blobItems =
                    list.OfType<CloudBlob>()
                    .Select(b => b.Name)
                    .OrderBy(b => b)
                    .Take(count)
                    .ToList();
                }
                else
                {
                    blobItems =
                    list.OfType<CloudBlob>()
                    .Select(b => b.Name)
                    .OrderByDescending(b => b)
                    .Take(count)
                    .ToList();
                }

                Dictionary<string, string> files = new Dictionary<string, string>();
                Directory.CreateDirectory(_attributes[OUTPUT_FOLDER]);
                int i = 0;
                foreach (var blobItem in blobItems)
                {
                    token.ThrowIfCancellationRequested();
                    string outputFile = Path.Combine(_attributes[OUTPUT_FOLDER], blobItem.Replace('/', '-'));
                    if (File.Exists(outputFile))
                        continue;

                    CloudBlob blob = container.GetBlobReference(blobItem);
                    blob.DownloadToFile(outputFile, FileMode.OpenOrCreate);
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
