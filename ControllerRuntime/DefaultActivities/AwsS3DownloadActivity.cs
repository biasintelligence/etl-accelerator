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
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;


using ControllerRuntime;
using Serilog;

namespace DefaultActivities
{
    /// <summary>
    /// returns true if file exists
    /// </summary>
    public class AwsS3DownloadActivity : IWorkflowActivity
    {
        private const string CONNECTION_STRING = "ConnectionString";
        private const string INPUT_PREFIX = "Prefix";
        private const string OUTPUT_FOLDER = "OutputFolder";
        private const string CONTAINER_NAME = "BucketName";
        //private const string PROFILE_NAME = "ProfileName";
        private const string REGION_NAME = "RegionName";
        private const string ACCOUNT_NAME = "AccountName";
        private const string ACCOUNT_KEY = "AccountKey";
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
            REGION_NAME,
            //PROFILE_NAME,
            ACCOUNT_KEY,
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

                //if(!Amazon.Util.ProfileManager.IsProfileKnown(_attributes[PROFILE_NAME]))
                //{
                //    Amazon.Util.ProfileManager.RegisterProfile(_attributes[PROFILE_NAME], _attributes[ACCOUNT_NAME], _attributes[ACCOUNT_KEY]);
                //}

                //var credentials = new StoredProfileAWSCredentials(_attributes[PROFILE_NAME]);
                RegionEndpoint endpoint = RegionEndpoint.GetBySystemName(_attributes[REGION_NAME]);
                AWSCredentials credentials = null;
                if (!String.IsNullOrEmpty(_attributes[ACCOUNT_NAME]))
                {
                    credentials = new BasicAWSCredentials(_attributes[ACCOUNT_NAME], _attributes[ACCOUNT_KEY]);
                }
                using (var client = new AmazonS3Client(credentials, endpoint))
                {


                        //ListBucketsRequest bucketRequest = new ListBucketsRequest();
                        //ListBucketsResponse backetResponse;
                        //backetResponse = client.ListBuckets(bucketRequest);
                        //var buckets = backetResponse.Buckets;

                        //GetBucketLocationResponse bucketLocationResponse = client.GetBucketLocation(_attributes[CONTAINER_NAME]);


                    ListObjectsV2Request objectRequest = new ListObjectsV2Request
                    {
                        BucketName = _attributes[CONTAINER_NAME],
                        MaxKeys = 100,
                        Prefix = String.IsNullOrEmpty(_attributes[INPUT_PREFIX]) ? null : _attributes[INPUT_PREFIX]
                    };

                    List<S3Object> objectlist = new List<S3Object>();
                    ListObjectsV2Response objectResponse;
                    do
                    {
                        objectResponse = client.ListObjectsV2(objectRequest);
                        objectlist.AddRange(objectResponse.S3Objects);
                        objectRequest.ContinuationToken = objectResponse.NextContinuationToken;
                    } while (objectResponse.IsTruncated == true);

                    IEnumerable<S3Object> list = objectlist;
                    if (_attributes[SORT_ORDER].Equals(sortList[0], StringComparison.InvariantCultureIgnoreCase))
                    {
                        list = list.OfType<S3Object>().OrderBy(b => b.Key);
                    }
                    if (_attributes[SORT_ORDER].Equals(sortList[1], StringComparison.InvariantCultureIgnoreCase))
                    {
                        list = list.OfType<S3Object>().OrderByDescending(b => b.Key);
                    }

                    if (count > 0)
                    {
                        list = list.OfType<S3Object>().Take(count);
                    }

                    Dictionary<string, string> files = new Dictionary<string, string>();
                    Directory.CreateDirectory(_attributes[OUTPUT_FOLDER]);
                    int i = 0;
                    foreach (var blobItem in list.OfType<S3Object>())
                    {
                        token.ThrowIfCancellationRequested();
                        string outputFile = Path.Combine(_attributes[OUTPUT_FOLDER], Path.GetFileName(blobItem.Key));
                        if (File.Exists(outputFile))
                            continue;

                        GetObjectRequest blobRequest = new GetObjectRequest
                        {
                            BucketName = _attributes[CONTAINER_NAME],
                            Key = blobItem.Key
                        };

                        using (FileStream outputFileStream = File.Create(outputFile))
                        using (GetObjectResponse blobResponse = client.GetObject(blobRequest))
                        using (Stream responseStream = blobResponse.ResponseStream)
                        //using (StreamReader reader = new StreamReader(responseStream))
                        {
                            responseStream.CopyTo(outputFileStream);
                            //responseBody = reader.ReadToEnd();
                        }

                        _logger.Debug("downloaded: {File}", outputFile);

                        if (setCounterInd)
                            files.Add(String.Format("{0}_{1}", _attributes[COUNTER_NAME], i++), outputFile);
                    }

                    if (setCounterInd)
                        counter.SetCounters(files);
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
