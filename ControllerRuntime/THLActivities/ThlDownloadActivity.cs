using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ControllerRuntime;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;

using Amazon.SQS;
using Amazon.SQS.Model;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Net.Http;

using System.Data;
using System.Data.SqlClient;

namespace THLActivities
{
    public class ThlDownloadActivity : IWorkflowActivity
    {
        //destination db connection string
        private const string CONNECTION_STRING = "ConnectionString";

        //sqs access (accessKey,secretKey)
        private const string ACCOUNT_NAME = "AccountName";
        private const string ACCOUNT_KEY = "AccountKey";
        private const string REGION_NAME = "RegionName";
        private const string SQS_URL = "SqsUrl";
        private const string TIMEOUT = "Timeout";


        private const string QUERY = @"if not exists (select 1 from stage.MessageRequestData where messageId = @messageId)
insert stage.MessageRequestData (messageId,organizationId,operationType,resourceType,eTag,[timestamp],messageData,resourceData)
values (@messageId,@organizationId,@operationType,@resourceType,@eTag,@timestamp,@messageData,@resourceData)";



        private Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private IWorkflowLogger _logger;
        private List<string> _required_attributes = new List<string>()
        {
            CONNECTION_STRING,
            REGION_NAME,
            ACCOUNT_KEY,
            ACCOUNT_NAME,
            SQS_URL,
            TIMEOUT,
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
                    _attributes.Add(attribute.Name, attribute.Value);
                }

            }

            //obfuscate the password
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_attributes[CONNECTION_STRING]);
            _logger.Write($"Download: { _attributes[SQS_URL]} -> {builder.DataSource}.{builder.InitialCatalog}");

        }

        public WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;

            try
            {

                int messageCount = 0; 
                //prepare insert query parameters
                SqlParameter[] p = new SqlParameter[]
                {
                    new SqlParameter("@messageId", SqlDbType.UniqueIdentifier, 0),
                    new SqlParameter("@organizationId", SqlDbType.UniqueIdentifier, 0),
                    new SqlParameter("@operationType", SqlDbType.Char, 1),
                    new SqlParameter("@resourceType", SqlDbType.NVarChar, 100),
                    new SqlParameter("@eTag", SqlDbType.BigInt, 0),
                    new SqlParameter("@timestamp", SqlDbType.DateTime, 0),
                    new SqlParameter("@messageData", SqlDbType.NVarChar, -1),
                    new SqlParameter("@resourceData", SqlDbType.NVarChar, -1),
                };


                //if(!Amazon.Util.ProfileManager.IsProfileKnown(_attributes[PROFILE_NAME]))
                //{
                //    Amazon.Util.ProfileManager.RegisterProfile(_attributes[PROFILE_NAME], _attributes[ACCOUNT_NAME], _attributes[ACCOUNT_KEY]);
                //}

                AWSCredentials credentials = null;
                if (!String.IsNullOrEmpty(_attributes[ACCOUNT_NAME]))
                {
                    credentials = new BasicAWSCredentials(_attributes[ACCOUNT_NAME], _attributes[ACCOUNT_KEY]);
                }

                RegionEndpoint endpoint = RegionEndpoint.GetBySystemName(_attributes[REGION_NAME]);
                using (var sqlClient = new SqlConnection(_attributes[CONNECTION_STRING]))
                using (var sqsClient = new AmazonSQSClient(credentials, endpoint))
                {
                    try
                    {

                        sqlClient.Open();
                        using (SqlCommand cmd = new SqlCommand(QUERY, sqlClient))
                        {
                            cmd.CommandTimeout = Int32.Parse(_attributes[TIMEOUT]);

                            cmd.Parameters.AddRange(p);
                            cmd.Prepare();

                            //var credentials = new StoredProfileAWSCredentials(_attributes[PROFILE_NAME]);
                            var receiveMessageRequest = new ReceiveMessageRequest { QueueUrl = _attributes[SQS_URL], MaxNumberOfMessages = 10 };
                            while (true)
                            {
                                var receiveMessageResponse = sqsClient.ReceiveMessage(receiveMessageRequest);
                                if ((receiveMessageResponse?.Messages?.Count ?? 0) == 0)
                                    break;


                                foreach (var message in receiveMessageResponse.Messages)
                                {
                                    if (token.IsCancellationRequested)
                                        return WfResult.Canceled;

                                    Guid messageId = Guid.Parse(message.MessageId);
                                    dynamic body = JsonConvert.DeserializeObject(message.Body);
                                    DateTime timestamp = body.Timestamp;

                                    JToken messageData = JObject.Parse((string)body.Message);

                                    string operationType = (string)messageData.SelectToken("type");
                                    string organizationId = (string)messageData.SelectToken("resource.organizationId");
                                    string resourceType = (string)messageData.SelectToken("resource.type");
                                    string eTag = (string)messageData.SelectToken("resource.eTag");
                                    string resourceData = null;

                                    string href = (string)messageData.SelectToken("resource.href");

                                    //data payload
                                    if (String.IsNullOrEmpty(href))
                                    {
                                        resourceData = (messageData.SelectToken("resource.data")).ToString();
                                        if (String.IsNullOrEmpty(resourceData))
                                            _logger.Write($"Warning: no payload is found for message: {messageId}");

                                    }
                                    //href payload
                                    else
                                    {
                                        _logger.WriteDebug($"Href: {href}");
                                        using (var s3Client = new HttpClient())
                                        {
                                            try
                                            {
                                                resourceData = s3Client.GetStringAsync(href).Result;
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.Write($"Warning: invalid href: {href}");
                                                _logger.Write(ex.Message);
                                            }
                                        }
                                    }
                                   
                                    using (token.Register(cmd.Cancel))
                                    {
                                        cmd.Parameters[0].Value = messageId;
                                        cmd.Parameters[1].Value = (String.IsNullOrEmpty(organizationId)) ? Guid.Empty : Guid.Parse(organizationId);
                                        cmd.Parameters[2].Value = (String.IsNullOrEmpty(operationType)) ? "N" : operationType.Substring(0, 1);
                                        cmd.Parameters[3].Value = (String.IsNullOrEmpty(resourceType)) ? "Unknown" : resourceType;
                                        cmd.Parameters[4].Value = (String.IsNullOrEmpty(eTag)) ? 0 : Int64.Parse(eTag);
                                        cmd.Parameters[5].Value = timestamp;
                                        cmd.Parameters[6].Value = messageData.ToString();
                                        cmd.Parameters[7].Value = (String.IsNullOrEmpty(resourceData)) ? (object)DBNull.Value : resourceData;

                                        cmd.ExecuteNonQuery();
                                    }

                                    messageCount++;

                                    //Deleting the message
                                    var deleteRequest = new DeleteMessageRequest { QueueUrl = _attributes[SQS_URL], ReceiptHandle = message.ReceiptHandle };
                                    sqsClient.DeleteMessage(deleteRequest);

                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        if (sqlClient.State == ConnectionState.Open)
                            sqlClient.Close();
                    }
                }

                _logger.Write($"Activity Processed {messageCount} messages");

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
