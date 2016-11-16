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

    public class ETLController:IDisposable
    {
        #region fields
        //source 
        private static SqlConnection connection;
        private static SqlCommand print = new SqlCommand();
        private static SqlCommand counterset = new SqlCommand();

        private static string controllerserver;
        private static string controllerdatabase;
        private static string nodeserver;
        private static string nodedatabase;
        private static int timeout;
        private static Guid conversation;
        private static bool disposed = false;

        public static ControllerHeader Header { get; private set; }
        public static bool CanSend { get { return (ETLController.Connected && !ETLController.conversation.Equals(Guid.Empty)); } }
        public static bool Connected { get { return (ETLController.connection != null && ETLController.connection.State == ConnectionState.Open); } }
        public static bool Debug { get { return ((ETLController.Header.Options & 1) == 1); } }


        //control counters
        #endregion

        #region Methods

        public static void Connect(ETLHeader header)
        {
            System.Diagnostics.Debug.Assert(header != null);

            ETLController.controllerserver = header.Controller.Server;
            ETLController.controllerdatabase = header.Controller.Database;
            ETLController.nodeserver = (header.Node == null)?header.Controller.Server:header.Node.Server;
            ETLController.nodedatabase = (header.Node == null)?header.Controller.Database:header.Node.Database;
            ETLController.timeout = header.Controller.QueryTimeout;
            ControllerHeader h = new ControllerHeader
            {
                BatchID = header.BatchID
               ,StepID = header.StepID
               ,RunID = header.RunID
               ,Options = header.Options
            };
            ETLController.Header = h;
            ETLController.conversation = header.Conversation;
        
            prepare_connection();
            prepare_print(ETLController.print);
            prepare_counterset(ETLController.counterset);
            LogMessage("Controller: starting conversation with handle:" + ETLController.conversation.ToString(), DERun.Debug);

        }


        private static void prepare_connection()
        {

            string connectionString = String.Format(CultureInfo.InvariantCulture, "Persist Security Info=False;Integrated Security=SSPI;database={0};server={1}", ETLController.nodedatabase, ETLController.nodeserver);
            ETLController.connection = new SqlConnection(connectionString);
            try
            {
                ETLController.connection.Open();
            }
            catch (Exception ex)
            {
                //PrintOutput.PrintToError("Error opening connection to cdr controller." + ex.Message);
                throw new CouldNotConnectToDBController(ETLController.nodeserver, ETLController.nodedatabase, ex);
            }

        }

        private static void prepare_print(SqlCommand cmd)
        {
            System.Diagnostics.Debug.Assert(cmd != null);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = ETLController.timeout;
            cmd.Connection = ETLController.connection;
            cmd.CommandText = "dbo.prc_Print";
            cmd.Parameters.Add(CreateParameter("@RTC", 0, SqlDbType.Int, 4, ParameterDirection.ReturnValue));
            cmd.Parameters.Add("@pProcessInfo", SqlDbType.Xml);
            if (ETLController.CanSend)
            {
                cmd.Parameters.Add("@pConversation", SqlDbType.UniqueIdentifier);
                cmd.Parameters["@pConversation"].Value = ETLController.conversation;
            }
        }


        private static void prepare_counterset(SqlCommand cmd)
        {
            System.Diagnostics.Debug.Assert(cmd != null);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = ETLController.timeout;
            cmd.Connection = ETLController.connection;
            cmd.CommandText = "dbo.prc_CounterSet";
            cmd.Parameters.Add(CreateParameter("@RTC", 0, SqlDbType.Int, 4, ParameterDirection.ReturnValue));
            cmd.Parameters.Add("@pHeader", SqlDbType.Xml);
            cmd.Parameters["@pHeader"].Value = ETLController.Header.build();
            cmd.Parameters.Add("@pName", SqlDbType.VarChar, 100);
            cmd.Parameters.Add("@pValue", SqlDbType.NVarChar,-1);
        }


        /// <summary>
        /// 
                

        public static void LogMessage(string message,int error, bool verbose)
        {
            if (!Connected) return;

            System.Diagnostics.Debug.Assert(ETLController.connection.State == ConnectionState.Open);

            ControllerMessage msg = new ControllerMessage();
            msg.Message = message;
            msg.Error = error;

            if (verbose || (ETLController.Header.Options & 1) == 1)
            {
                string infoXML = ControllerProcessInfo.build(ETLController.Header, msg);
                ETLController.print.Parameters["@pProcessInfo"].Value = infoXML;
                try
                {
                    ETLController.print.ExecuteNonQuery();
                    int retval = Convert.ToInt32(ETLController.print.Parameters["@RTC"].Value, CultureInfo.InvariantCulture);
                    if (retval != 0)
                    {
                        throw new CouldNotSendMessage(message);
                    }
                }
                catch (Exception e)
                {
                    throw new CouldNotSendMessage(e.Message, e);
                }
            }
        }

        public static void LogMessage(string message,int err)
        {
            LogMessage(message, err, true);
        }

        public static void LogMessage(string message, bool verbose)
        {
            LogMessage(message, 0, verbose);
        }
 
        public static void LogMessage(string message)
        {
            LogMessage(message, 0, true);
        }

        public static void CounterSet(string name, string value)
        {
            if (!Connected) return;

            System.Diagnostics.Debug.Assert(ETLController.connection.State == ConnectionState.Open);

            ETLController.counterset.Parameters["@pName"].Value = name;
            ETLController.counterset.Parameters["@pValue"].Value = value;
            try
            {
                ETLController.counterset.ExecuteNonQuery();
                int retval = Convert.ToInt32(ETLController.counterset.Parameters["@RTC"].Value, CultureInfo.InvariantCulture);
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

            if (!ETLController.disposed)
            {
                if (disposing)
                {
                    //clean up managed resources
                    if (ETLController.connection != null)
                    {
                        ETLController.connection.Close();
                        ETLController.connection.Dispose();
                        ETLController.connection = null;
                    }
                    if (ETLController.print != null)
                    {
                        ETLController.print.Dispose();
                        ETLController.print = null;
                    }
                    if (ETLController.counterset != null)
                    {
                        ETLController.counterset.Dispose();
                        ETLController.counterset = null;
                    }
                }

                // clean up unmanaged resources

                ETLController.disposed = true;
            }
        }

    }
}
