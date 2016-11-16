

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
using ETL_Framework.DESQLCLR;
using System.IO;

namespace ETL_Framework.DESQLCLR
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
    //[XmlSerializerAssemblyAttribute("DeSqlClr.XmlSerializers")]
    public partial class DeFunctions
    {

        static readonly SqlMetaData[] m = new SqlMetaData[1] { new SqlMetaData("msg", SqlDbType.NVarChar, 4000) };

        protected enum OutputType
        {
            Print,
            Rowset,
            None
        }


        //[Microsoft.SqlServer.Server.SqlFunction(DataAccess = DataAccessKind.Read)]
        [Microsoft.SqlServer.Server.SqlProcedure]
        public static SqlInt32 ExecuteDE(SqlString exe, SqlString arg, SqlInt32 timeout, SqlString options)
        {
            WindowsIdentity clientId = null;
            //WindowsImpersonationContext impersonatedUser = null;
            clientId = SqlContext.WindowsIdentity;
            bool debug = options.ToString().Contains("debug");

            OutputType output = OutputType.None;
            if (!options.ToString().Contains("noutput"))
            {
                output = options.ToString().Contains("rowset") ? OutputType.Rowset : OutputType.Print;
            }


            int ret = 0;
            try
            {
                Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                SqlPipe pipe = SqlContext.Pipe;
                if (output == OutputType.Rowset)
                {
                    SqlDataRecord rec = new SqlDataRecord(m);
                    pipe.SendResultsStart(rec);
                }

                Send(pipe, String.Format("SqlClr ExecuteDE Version {0} Executing as {1}", v, clientId.Name), debug);

                try
                {
                    //impersonatedUser = clientId.Impersonate();
                    //if (impersonatedUser != null)
                    //{
                    // use this for impersonation
                    //}

                    using (DeProcess p = new DeProcess())
                    {
                        p.Timeout = timeout.Value;
                        p.haveOutput += delegate(object sender, HaveOutputEventArgs e)
                        {
                            Send(pipe, e.Output, (output != OutputType.None));
                        };

                        ret = p.Execute(exe.Value, arg.Value);
                    }
                }
                catch (Exception ex)
                {
                    ret = 1;
                    Send(pipe, "SqlClr ExecuteDE Failed: " + ex.Message, true);
                    throw ex;
                }
                finally
                {
                    //if (impersonatedUser != null)
                    //    impersonatedUser.Undo();
                    Send(pipe, String.Format("SqlClr ExecuteDE Completed with ExitCode: {0}", ret), debug);
                    if (pipe.IsSendingResults)
                    {
                        pipe.SendResultsEnd();
                    }
                }
            }
            catch
            {
                throw;
            }

            return ret;
        }

        private static void Send(SqlPipe pipe, string m, bool debug)
        {

            if (!debug)
            {
                return;
            }

            if (pipe.IsSendingResults)
            {
                SqlDataRecord rec = new SqlDataRecord(DeFunctions.m);
                rec.SetSqlString(0, m);
                pipe.SendResultsRow(rec);
            }
            else
            {
                pipe.Send(m);
            }
        }

    }
}

