using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Xml;
using System.Collections;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;
using System.Xml.Serialization;
using System.IO;
using ETL_Framework.ControllerCLRExtensions;

namespace ETL_Framework.ControllerCLRExtensions
{
    /// <summary>
    /// This class needs to be installed as a CLR assembly with UNSAFE permissions.
    /// 
    /// In order to install an external access assembly in a database, the database needs to be set to TRUSTWORTHY 
    /// and the database owner needs the EXTERNAL ACCESS ASSEMBLY server privilege.
    /// 
    /// In the common case where you are a sysadmin and you own the database, then you just need to set the database
    /// to TRUSTWORTHY.
    /// 
    /// ALTER DATABASE MyDb SET TRUSTOWRTHY ON
    /// 
    /// 
    /// </summary>
    public partial class ControllerExtensions
    {
        private static void Send(SqlPipe p, SqlDataRecord r, string m, bool d)
        {

            if (!d)
            {
                return;
            }

            if (r != null)
            {
                r.SetSqlString(0, m);
                p.SendResultsRow(r);
            }
            else
            {
                p.Send(m);
            }
        }

        //[Microsoft.SqlServer.Server.SqlFunction(DataAccess = DataAccessKind.Read)]
        [Microsoft.SqlServer.Server.SqlProcedure]
        public static SqlInt32 EventPost(SqlString Server, SqlString Database, SqlString EventType, SqlDateTime EventPosted, SqlXml EventArgs, SqlString Options)
        {
            WindowsIdentity clientId = null;
            //WindowsImpersonationContext impersonatedUser = null;
            clientId = SqlContext.WindowsIdentity;
            bool debug = Options.ToString().Contains("debug");
            string ConnectionString = String.Format("Persist Security Info=False;Integrated Security=SSPI;database={0};server={1}", Database.ToString(), Server.ToString());

            SqlInt32 ret = 1;
            try
            {
                Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                SqlMetaData[] m = new SqlMetaData[1] { new SqlMetaData("msg", SqlDbType.NVarChar, 4000) };
                SqlDataRecord rec = null;
                SqlPipe pipe = SqlContext.Pipe;

                Send(pipe, rec, String.Format("Controller Clr Extensions Version {0} Executing as {1}", v, clientId.Name), debug);
                EventFunctions.PostEvent(ConnectionString, EventType, EventPosted, EventArgs, Options);
                Send(pipe, rec, String.Format("SqlClr EventPost {1}.{2} - {3} completed"
                , ret, Server.ToString(), Database.ToString(), EventType.ToString()), debug);
                ret = 0;
            }
            catch
            {
                throw;
            }

            return ret;
        }

        //[Microsoft.SqlServer.Server.SqlFunction(DataAccess = DataAccessKind.Read)]
        [Microsoft.SqlServer.Server.SqlProcedure]
        public static SqlInt32 EventReceive(SqlString Server, SqlString Database, out SqlGuid EventId, out SqlDateTime EventPosted, out SqlDateTime EventReceived, out SqlXml EventArgs, SqlString EventType, SqlString Options)
        {
            WindowsIdentity clientId = null;
            //WindowsImpersonationContext impersonatedUser = null;
            clientId = SqlContext.WindowsIdentity;
            bool debug = Options.ToString().Contains("debug");
            string ConnectionString = String.Format("Persist Security Info=False;Integrated Security=SSPI;database={0};server={1}", Database.ToString(), Server.ToString());

            SqlInt32 ret = 1;
            try
            {
                Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                SqlMetaData[] m = new SqlMetaData[1] { new SqlMetaData("msg", SqlDbType.NVarChar, 4000) };
                SqlDataRecord rec = null;
                SqlPipe pipe = SqlContext.Pipe;

                Send(pipe, rec, String.Format("Controller CLR Extensions Version {0} Executing as {1}", v, clientId.Name), debug);
                EventFunctions.ReceiveEvent(ConnectionString, out EventId, out EventPosted, out EventReceived, out EventArgs, EventType, Options);
                Send(pipe, rec, String.Format("SqlClr EventReceive {1}.{2} - {3} completed"
                , ret, Server.ToString(), Database.ToString(), EventType.ToString()), debug);
                ret = 0;
            }
            catch
            {
                throw;
            }

            return ret;
        }
    }
}

