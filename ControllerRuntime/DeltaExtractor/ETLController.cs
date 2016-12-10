using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Xml.Serialization;
using System.IO;
using System.Data.SqlClient;
using System.Collections;
using System.Data;

namespace BIAS.Framework.DeltaExtractor
{
    [XmlRoot(Namespace = "ETLController.XSD", ElementName = "Header")]
    public class ControllerHeader
    {

        [XmlAttribute("BatchID")]
        public int BatchID { get; set; }

        [XmlAttribute("StepID")]
        public int StepID { get; set; }

        [XmlAttribute("RunID")]
        public int RunID { get; set; }

        [XmlAttribute("Options")]
        public int Options { get; set; }

        public static string build(ETLHeader input)
        {
            ControllerHeader header = new ControllerHeader();
            header.BatchID = input.BatchID;
            header.StepID = input.StepID;
            header.RunID = input.RunID;
            header.Options = input.Options;
            return build(header);
        }

        public static string build(ControllerHeader input)
        {
            XmlSerializer serparam = new XmlSerializer(typeof(ControllerHeader));
            StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
            serparam.Serialize(sw, input);
            return sw.ToString();
        }

        public string build()
        {
            return build(this);
        }


    }

    [XmlRoot(Namespace = "ETLController.XSD", ElementName = "Message")]
    public class ControllerMessage
    {

        [XmlTextAttribute()]
        public string Message { get; set; }

        [XmlAttribute("Error")]
        public int Error { get; set; }
    }

    [XmlRoot(Namespace = "ETLController.XSD", ElementName = "ProcessInfo")]
    public class ControllerProcessInfo
    {

        public ControllerHeader Header { get; set; }
        public ControllerMessage Message { get; set; }

        public static string build(ETLHeader input, string msg, int err)
        {
            ControllerHeader header = new ControllerHeader();
            ControllerMessage message = new ControllerMessage();
            header.BatchID = input.BatchID;
            header.StepID = input.StepID;
            header.RunID = input.RunID;
            header.Options = input.Options;
            message.Message = msg;
            message.Error = err;
            return build(header, message);
        }

        public static string build(ControllerHeader header, ControllerMessage message)
        {
            ControllerProcessInfo processInfo = new ControllerProcessInfo();
            processInfo.Header = header;
            processInfo.Message = message;
            XmlSerializer serparam = new XmlSerializer(typeof(ControllerProcessInfo));
            StringWriter sw = new StringWriter();
            serparam.Serialize(sw, processInfo);
            return sw.ToString();
        }

        public string build()
        {
            return build(this.Header, this.Message);
        }

    }

    public class ETLController : IDisposable
    {
        #region fields
        //source 
        private SqlConnection connection;
        //private SqlCommand print = new SqlCommand();
        private SqlCommand counterset = new SqlCommand();

        private string controllerserver;
        private string controllerdatabase;
        private string nodeserver;
        private string nodedatabase;
        private int timeout;
        private Guid conversation;
        private bool disposed = false;

        public ControllerHeader Header { get; private set; }
        public bool CanSend { get { return (Connected && !conversation.Equals(Guid.Empty)); } }
        public bool Connected { get { return (connection != null && connection.State == ConnectionState.Open); } }
        public bool Debug { get { return ((Header.Options & 1) == 1); } }


        //control counters
        #endregion

        #region Methods

        public void Connect(ETLHeader header)
        {
            System.Diagnostics.Debug.Assert(header != null);
            SqlCommand print = new SqlCommand();

            try
            {
                controllerserver = header.Controller.Server;
                controllerdatabase = header.Controller.Database;
                nodeserver = (header.Node == null) ? header.Controller.Server : header.Node.Server;
                nodedatabase = (header.Node == null) ? header.Controller.Database : header.Node.Database;
                timeout = header.Controller.QueryTimeout;
                ControllerHeader h = new ControllerHeader
                {
                    BatchID = header.BatchID
                   ,
                    StepID = header.StepID
                   ,
                    RunID = header.RunID
                   ,
                    Options = header.Options
                };
                Header = h;
                conversation = header.Conversation;

                prepare_connection();
                print.Connection = connection;
                prepare_print(print);
                prepare_counterset(counterset);
                LogMessage("Controller: starting conversation with handle:" + conversation.ToString(), Debug);
            }
            finally
            {
                if (print != null)
                {
                    print.Dispose();
                }
            }
        }


        private void prepare_connection()
        {

            string connectionString = String.Format(CultureInfo.InvariantCulture, "Persist Security Info=False;Integrated Security=SSPI;database={0};server={1}", nodedatabase, nodeserver);
            connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {
                //PrintOutput.PrintToError("Error opening connection to cdr controller." + ex.Message);
                throw new CouldNotConnectToDBController(nodeserver, nodedatabase, ex);
            }

        }

        private void prepare_print(SqlCommand cmd)
        {
            System.Diagnostics.Debug.Assert(cmd != null);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = timeout;
            cmd.Connection = connection;
            cmd.CommandText = "dbo.prc_Print";
            cmd.Parameters.Add(CreateParameter("@RTC", 0, SqlDbType.Int, 4, ParameterDirection.ReturnValue));
            cmd.Parameters.Add("@pProcessInfo", SqlDbType.Xml);
            if (CanSend)
            {
                cmd.Parameters.Add("@pConversation", SqlDbType.UniqueIdentifier);
                cmd.Parameters["@pConversation"].Value = conversation;
            }
        }


        private void prepare_counterset(SqlCommand cmd)
        {
            System.Diagnostics.Debug.Assert(cmd != null);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = timeout;
            cmd.Connection = connection;
            cmd.CommandText = "dbo.prc_CounterSet";
            cmd.Parameters.Add(CreateParameter("@RTC", 0, SqlDbType.Int, 4, ParameterDirection.ReturnValue));
            cmd.Parameters.Add("@pHeader", SqlDbType.Xml);
            cmd.Parameters["@pHeader"].Value = Header.build();
            cmd.Parameters.Add("@pName", SqlDbType.VarChar, 100);
            cmd.Parameters.Add("@pValue", SqlDbType.NVarChar, -1);
        }


        /// <summary>
        /// 


        public void LogMessage(string message, int error, bool verbose)
        {
            if (!Connected) return;

            System.Diagnostics.Debug.Assert(connection.State == ConnectionState.Open);

            SqlCommand print = new SqlCommand();
            print.Connection = connection;

            ControllerMessage msg = new ControllerMessage();
            msg.Message = message;
            msg.Error = error;

            if (verbose || (Header.Options & 1) == 1)
            {
                string infoXML = ControllerProcessInfo.build(Header, msg);
                print.Parameters["@pProcessInfo"].Value = infoXML;
                try
                {
                    print.ExecuteNonQuery();
                    int retval = Convert.ToInt32(print.Parameters["@RTC"].Value, CultureInfo.InvariantCulture);
                    if (retval != 0)
                    {
                        throw new CouldNotSendMessage(message);
                    }
                }
                catch (Exception e)
                {
                    throw new CouldNotSendMessage(e.Message, e);
                }
                finally
                {
                    if (print != null)
                    {
                        print.Dispose();
                    }
                }
            }
        }

        public void LogMessage(string message, int err)
        {
            LogMessage(message, err, true);
        }

        public void LogMessage(string message, bool verbose)
        {
            LogMessage(message, 0, verbose);
        }

        public void LogMessage(string message)
        {
            LogMessage(message, 0, true);
        }

        public void CounterSet(string name, string value)
        {
            if (!Connected) return;

            System.Diagnostics.Debug.Assert(connection.State == ConnectionState.Open);

            counterset.Parameters["@pName"].Value = name;
            counterset.Parameters["@pValue"].Value = value;
            try
            {
                counterset.ExecuteNonQuery();
                int retval = Convert.ToInt32(counterset.Parameters["@RTC"].Value, CultureInfo.InvariantCulture);
                if (retval != 0)
                {
                    throw new CouldNotSetValue(name);
                }
            }
            catch (Exception e)
            {
                throw new CouldNotSendMessage(e.Message, e);
            }
        }


        public static SqlParameter CreateParameter(string parameterName, Object parameterValue, System.Data.SqlDbType dbType, int parameterSize, System.Data.ParameterDirection parameterDirection)
        {
            SqlParameter sParameter = new SqlParameter(parameterName, dbType, parameterSize);
            sParameter.Size = parameterSize;
            sParameter.Direction = parameterDirection;
            sParameter.Value = parameterValue;
            return sParameter;
        }



        #endregion
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    //clean up managed resources
                    if (connection != null)
                    {
                        connection.Close();
                        connection.Dispose();
                        connection = null;
                    }
                    //if (print != null)
                    //{
                    //    print.Dispose();
                    //    print = null;
                    //}
                    if (counterset != null)
                    {
                        counterset.Dispose();
                        counterset = null;
                    }
                }

                // clean up unmanaged resources

                disposed = true;
            }
        }

    }
}
