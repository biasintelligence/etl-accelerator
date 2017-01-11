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
using Microsoft.WindowsAzure.Storage.Table;

using System.Data;
using System.Data.SqlClient;


using ControllerRuntime;

namespace DefaultActivities
{
    /// <summary>
    /// returns true if file exists
    /// </summary>
    public class AzureTableCopyActivity : IWorkflowActivity
    {
        private const string AZURE_TABLE_PARTITION_KEY_COL = "PartitionKey";
        private const string AZURE_TABLE_ROW_KEY_COL = "RowKey";
        private const string AZURE_TABLE_TIMESTAMP_COL = "Timestamp";

        private const string CONNECTION_STRING = "ConnectionString";
        private const string AZURE_TABLE_NAME = "AzureTableName";
        private const string SQL_TABLE_NAME = "SqlTableName";
        private const string ACCOUNT_NAME = "AccountName";
        private const string ACCOUNT_KEY = "AccountKey";
        private const string IS_SAS_TOKEN = "isSasToken";
        private const string TIMEOUT = "Timeout";
        private const string CONTROL_COLUMN = "ControlColumn";
        private const string CONTROL_VALUE = "ControlValue";

        private Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private IWorkflowLogger _logger;
        private List<string> _required_attributes = new List<string>()
        { CONNECTION_STRING,
            AZURE_TABLE_NAME,
            SQL_TABLE_NAME,
            ACCOUNT_NAME,
            ACCOUNT_KEY,
            IS_SAS_TOKEN,
            TIMEOUT,
            CONTROL_COLUMN,
            CONTROL_VALUE,
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

            _logger.WriteDebug(String.Format("ConnectionString: {0}", _attributes[CONNECTION_STRING]));
            _logger.Write(String.Format("Copy: {0} -> {1}", _attributes[AZURE_TABLE_NAME], _attributes[SQL_TABLE_NAME]));
            _logger.Write(String.Format("Delta: {0} > {1}", _attributes[CONTROL_COLUMN], _attributes[CONTROL_VALUE]));

        }

        public WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

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

                CloudTableClient tblClient = account.CreateCloudTableClient();

                CloudTable table = tblClient.GetTableReference(_attributes[AZURE_TABLE_NAME]);
                //PrintTableProperties(table);

                //get sql table schema to construct azure table query request
                DataTable schema = GetSchema(_attributes[SQL_TABLE_NAME]);
                IList<string> columnList = schema.Columns.OfType<DataColumn>()
                    .Select(c => c.ColumnName)
                    //.Union(new string[]{ AZURE_TABLE_PARTITION_KEY_COL,AZURE_TABLE_ROW_KEY_COL,AZURE_TABLE_TIMESTAMP_COL})
                    .ToList();

                //TableQuery<DynamicTableEntity> tableQuery = new TableQuery<DynamicTableEntity>().Select(columnList);
                TableQuery tableQuery = new TableQuery().Select(columnList);
                if (!String.IsNullOrEmpty(_attributes[CONTROL_COLUMN]))
                {
                    if (String.IsNullOrEmpty(_attributes[CONTROL_VALUE]))
                        throw new ArgumentException(String.Format("ControlValue is expected for {0}", _attributes[CONTROL_COLUMN]));

                    tableQuery = tableQuery.Where(this.GenerateFilterCondition(schema, _attributes[CONTROL_COLUMN], _attributes[CONTROL_VALUE]));
                }

                // Initialize the continuation token to null to start from the beginning of the table.
                TableContinuationToken continuationToken = null;
                TableRequestOptions requestOptions = GetRequestOptions(schema);
                OperationContext ctx = new OperationContext();

                EntityResolver<DataRow> resolver = (pk, rk, ts, props, etag) =>
                {
                    DataRow dataRow = schema.NewRow();
                    dataRow[AZURE_TABLE_PARTITION_KEY_COL] = pk;
                    dataRow[AZURE_TABLE_ROW_KEY_COL] = rk;
                    dataRow[AZURE_TABLE_TIMESTAMP_COL] = ts;
                    foreach (var prop in props)
                    {
                        SetValue(schema, dataRow, prop);
                    }
                    schema.Rows.Add(dataRow);
                    return dataRow;
                };

                do
                {

                    // Retrieve a segment (up to 1,000 entities).
                    Task<TableQuerySegment<DataRow>> task =
                        table.ExecuteQuerySegmentedAsync<DataRow>(tableQuery, resolver, continuationToken, requestOptions, ctx, token);

                    var tableQueryResult = task.Result;
                    token.ThrowIfCancellationRequested();

                    // Assign the new continuation token to tell the service where to
                    // continue on the next iteration (or null if it has reached the end).
                    continuationToken = tableQueryResult.ContinuationToken;
                    if (schema.Rows.Count > 9000 || continuationToken == null)
                    {
                        _logger.Write(String.Format("Copy {0} rows", schema.Rows.Count));
                        BulkLoad(schema);
                        schema.Clear();
                    }

                    // Loop until a null continuation token is received, indicating the end of the table.
                } while (continuationToken != null);

            }
            catch (Exception ex)
            {
                throw ex;
            }
            result = WfResult.Succeeded;
            return result;
        }

        private DataTable GetSchema(string tableName)
        {
            using (SqlConnection cn = new SqlConnection(_attributes[CONNECTION_STRING]))
            {
                try
                {
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand(String.Format("select top (0) * from {0}", tableName), cn))
                    {
                        SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly);
                        //return reader.GetSchemaTable();
                        DataTable dt = new DataTable(tableName);
                        dt.Load(reader);
                        return dt;
                    }
                }
                catch (SqlException ex)
                {
                    throw ex;
                }
                finally
                {
                    if (cn.State != ConnectionState.Closed)
                        cn.Close();
                }
            }
        }

        private TableRequestOptions GetRequestOptions(DataTable schema)
        {

            TableRequestOptions options = new TableRequestOptions();
            options.PropertyResolver = (partitionKey, rowKey, propName, propValue) =>
            {
                if (schema.Columns.Contains(propName))
                {
                    return EdmTypeMap(schema.Columns[propName].DataType);
                }
                else
                    return EdmType.String;
            };
            return options;

        }

        private void SetValue(DataTable schema, DataRow dataRow, KeyValuePair<string, EntityProperty> prop)
        {
            if (!schema.Columns.Contains(prop.Key))
                return;

            DataColumn match = schema.Columns[prop.Key];
            if (match.DataType == typeof(bool))
            {
                bool res;
                if (prop.Value.PropertyType == EdmType.Boolean
                    && prop.Value.BooleanValue != null)
                    dataRow[match.ColumnName] = prop.Value.BooleanValue;
                else if (prop.Value.PropertyType == EdmType.String
                    && prop.Value.StringValue != null
                    && bool.TryParse(prop.Value.StringValue, out res))
                    dataRow[match.ColumnName] = res;
                else if (prop.Value.PropertyAsObject != null
                    && bool.TryParse(prop.Value.PropertyAsObject.ToString(), out res))
                    dataRow[match.ColumnName] = res;
            }
            else if (match.DataType == typeof(byte[])
                && (prop.Value.PropertyType == EdmType.Binary
                && prop.Value.BinaryValue != null))
            {
                dataRow[match.ColumnName] = prop.Value.BinaryValue;
            }
            else if (match.DataType == typeof(DateTime))
            {
                DateTime res;
                if (prop.Value.PropertyType == EdmType.DateTime
                    && prop.Value.DateTime != null)
                    dataRow[match.ColumnName] = prop.Value.DateTime;
                else if (prop.Value.PropertyType == EdmType.String
                    && prop.Value.StringValue != null
                    && DateTime.TryParse(prop.Value.StringValue, out res))
                    dataRow[match.ColumnName] = res;
                else if (prop.Value.PropertyAsObject != null
                    && DateTime.TryParse(prop.Value.PropertyAsObject.ToString(), out res))
                    dataRow[match.ColumnName] = res;
            }
            else if (match.DataType == typeof(DateTimeOffset))
            {
                DateTimeOffset res;
                if (prop.Value.PropertyType == EdmType.DateTime
                    && prop.Value.DateTimeOffsetValue != null)
                    dataRow[match.ColumnName] = prop.Value.DateTimeOffsetValue;
                else if (prop.Value.PropertyType == EdmType.String
                    && prop.Value.StringValue != null
                    && DateTimeOffset.TryParse(prop.Value.StringValue, out res))
                    dataRow[match.ColumnName] = res;
                else if (prop.Value.PropertyAsObject != null
                    && DateTimeOffset.TryParse(prop.Value.PropertyAsObject.ToString(), out res))
                    dataRow[match.ColumnName] = res;
            }
            else if (match.DataType == typeof(double))
            {
                Double res;
                if (prop.Value.PropertyType == EdmType.Double
                    && prop.Value.DoubleValue != null)
                    dataRow[match.ColumnName] = prop.Value.DoubleValue;
                else if (prop.Value.PropertyType == EdmType.String
                    && prop.Value.StringValue != null
                    && Double.TryParse(prop.Value.StringValue, out res))
                    dataRow[match.ColumnName] = res;
                else if (prop.Value.PropertyAsObject != null
                    && Double.TryParse(prop.Value.PropertyAsObject.ToString(), out res))
                    dataRow[match.ColumnName] = res;
            }
            else if (match.DataType == typeof(Guid))
            {
                Guid res;
                if (prop.Value.PropertyType == EdmType.Guid
                    && prop.Value.GuidValue != null)
                    dataRow[match.ColumnName] = prop.Value.GuidValue;
                else if (prop.Value.PropertyType == EdmType.String
                    && prop.Value.StringValue != null
                    && Guid.TryParse(prop.Value.StringValue, out res))
                    dataRow[match.ColumnName] = res;
                else if (prop.Value.PropertyAsObject != null
                    && Guid.TryParse(prop.Value.PropertyAsObject.ToString(), out res))
                    dataRow[match.ColumnName] = res;
            }
            else if (match.DataType == typeof(Int32))
            {
                Int32 res;
                if (prop.Value.PropertyType == EdmType.Int32
                    && prop.Value.Int32Value != null)
                    dataRow[match.ColumnName] = prop.Value.Int32Value;
                else if (prop.Value.PropertyType == EdmType.String
                    && prop.Value.StringValue != null
                    && Int32.TryParse(prop.Value.StringValue, out res))
                    dataRow[match.ColumnName] = res;
                else if (prop.Value.PropertyAsObject != null
                    && Int32.TryParse(prop.Value.PropertyAsObject.ToString(), out res))
                    dataRow[match.ColumnName] = res;
            }
            else if (match.DataType == typeof(Int64))
            {
                Int64 res;
                if (prop.Value.PropertyType == EdmType.Int64
                    && prop.Value.Int64Value != null)
                    dataRow[match.ColumnName] = prop.Value.Int64Value;
                else if (prop.Value.PropertyType == EdmType.String
                    && prop.Value.StringValue != null
                    && Int64.TryParse(prop.Value.StringValue, out res))
                    dataRow[match.ColumnName] = res;
                else if (prop.Value.PropertyAsObject != null
                    && Int64.TryParse(prop.Value.PropertyAsObject.ToString(), out res))
                    dataRow[match.ColumnName] = res;
            }
            else if (match.DataType == typeof(string))
            {
                if (prop.Value.PropertyType == EdmType.String
                    && prop.Value.StringValue != null)
                    dataRow[match.ColumnName] = prop.Value.StringValue;
                else if (prop.Value.PropertyAsObject != null)
                    dataRow[match.ColumnName] = prop.Value.PropertyAsObject.ToString();
            }

        }

        private EdmType EdmTypeMap(Type type)
        {
            if (type == typeof(Byte[]))
                return EdmType.Binary;
            else if (type == typeof(Boolean))
                return EdmType.Boolean;
            else if (type == typeof(Int32))
                return EdmType.Int32;
            else if (type == typeof(Int64))
                return EdmType.Int64;
            else if (type == typeof(DateTime))
                return EdmType.DateTime;
            else if (type == typeof(DateTimeOffset))
                return EdmType.DateTime;
            else if (type == typeof(Double))
                return EdmType.Double;
            else if (type == typeof(Guid))
                return EdmType.Guid;
            else return EdmType.String;
        }

        private void BulkLoad(DataTable dataTable)
        {
            if (dataTable.Rows.Count == 0) return;

            int timeout = Int32.Parse(_attributes[TIMEOUT]);
            using (SqlConnection cn = new SqlConnection(_attributes[CONNECTION_STRING]))
            {
                try
                {
                    cn.Open();
                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(cn))
                    {
                        bulkCopy.BatchSize = 10000;
                        bulkCopy.BulkCopyTimeout = timeout;
                        bulkCopy.DestinationTableName = _attributes[SQL_TABLE_NAME];
                        bulkCopy.WriteToServer(dataTable.CreateDataReader());
                    }
                }
                catch (SqlException ex)
                {
                    throw ex;
                }
                finally
                {
                    if (cn.State != ConnectionState.Closed)
                        cn.Close();
                }
            }
        }

        private string GenerateFilterCondition(DataTable schema, string columnName, string columnValue)
        {
            if (columnName.Equals(AZURE_TABLE_PARTITION_KEY_COL))
            {
                return TableQuery.GenerateFilterCondition(columnName, QueryComparisons.GreaterThan, columnValue);
            }
            else if (columnName.Equals(AZURE_TABLE_ROW_KEY_COL))
            {
                return TableQuery.GenerateFilterCondition(columnName, QueryComparisons.GreaterThan, columnValue);
            }
            else if (columnName.Equals(AZURE_TABLE_TIMESTAMP_COL))
            {
                return TableQuery.GenerateFilterConditionForDate(columnName, QueryComparisons.GreaterThan, DateTimeOffset.Parse(columnValue));
            }
            else if (!schema.Columns.Contains(columnName))
                return String.Empty;
            else
            {

                Type type = schema.Columns[columnName].DataType;
                if (type == typeof(Byte[]))
                    return String.Empty;
                else if (type == typeof(Boolean))
                    return TableQuery.GenerateFilterConditionForBool(columnName, QueryComparisons.GreaterThan, Boolean.Parse(columnValue));
                else if (type == typeof(Int32))
                    return TableQuery.GenerateFilterConditionForInt(columnName, QueryComparisons.GreaterThan, Int32.Parse(columnValue));
                else if (type == typeof(Int64))
                    return TableQuery.GenerateFilterConditionForLong(columnName, QueryComparisons.GreaterThan, Int64.Parse(columnValue));
                else if (type == typeof(DateTime))
                    return TableQuery.GenerateFilterConditionForDate(columnName, QueryComparisons.GreaterThan, DateTime.Parse(columnValue));
                else if (type == typeof(DateTimeOffset))
                    return TableQuery.GenerateFilterConditionForDate(columnName, QueryComparisons.GreaterThan, DateTimeOffset.Parse(columnValue));
                else if (type == typeof(Double))
                    return TableQuery.GenerateFilterConditionForDouble(columnName, QueryComparisons.GreaterThan, Double.Parse(columnValue));
                else if (type == typeof(Guid))
                    return TableQuery.GenerateFilterConditionForGuid(columnName, QueryComparisons.GreaterThan, Guid.Parse(columnValue));
                else if (type == typeof(String))
                    return TableQuery.GenerateFilterCondition(columnName, QueryComparisons.GreaterThan, columnValue);
                else return String.Empty;
            }
        }

        private void PrintTableProperties(CloudTable table)
        {
            TableQuery<DynamicTableEntity> tableQuery = new TableQuery<DynamicTableEntity>();
            var tableQueryResult =
                table.ExecuteQuery(tableQuery).Take(1);
            var res = tableQueryResult.ToList();

            DynamicTableEntity entry = res[0];
            _logger.Write(String.Format("{0} {1} {2}", entry.PartitionKey, entry.RowKey, entry.Timestamp));
            foreach (var prop in entry.Properties)
            {
                _logger.Write(String.Format("{0} {1}", prop.Key, prop.Value.PropertyType.ToString()));
            }

        }
    }
}
