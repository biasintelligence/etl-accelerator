using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Security;
//using System.Security.Permissions;
using System.Data;
using System.Data.Sql;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using ETL_Framework.Properties;

namespace ETL_Framework
{
    class ETLDBMonitor: IDisposable
    {
        private string m_server = Settings.Default.Server;
        private string m_database = Settings.Default.Database;
        private string m_connectionString;
        private bool disposed = false;

        public ETLDBMonitor()
        {

            if (!CanRequestNotifications())
            {
                throw new CouldNotReceiveNotifications(m_server, m_database);
            }

        }


        private bool CanRequestNotifications()
        {
            //SqlClientPermission permission =
            //    new SqlClientPermission(
            //    PermissionState.Unrestricted);
            //try
            //{
            //    permission.Demand();
            //    return true;
            //}
            //catch (System.Exception)
            //{
            //    return false;
            //}
            return true;
        }

        private void Connect(string Server,string Database,string User,string Password)
        {
            if (Server == "")
            {
                throw new InvalidArgumentException("Server is required");
            }
            if (Database == "")
            {
                throw new InvalidArgumentException("Database is required");
            }

            if (String.IsNullOrEmpty(User))
                m_connectionString = String.Format("Persist Security Info=False;Integrated Security=SSPI;database={0};server={1}", Database, Server);
            else
                m_connectionString = String.Format("Persist Security Info=True;database={0};server={1};user id={2};password={3};", Database, Server, User, Password);

            // Create a dependency
            //try
            //{
            //    SqlDependency.Stop(m_connectionString);
            //    SqlDependency.Start(m_connectionString);
            //}
            //catch (Exception)
            //{
            //    throw new CouldNotReceiveNotifications(Server, Database);
            //}


        }

        public void StartWorkflow(string batch,string options)
        {
            using (SqlConnection con = new SqlConnection(m_connectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                try
                {
                    con.Open();
                    cmd.Connection = con;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "[dbo].[prc_Execute]";
                    cmd.Parameters.AddWithValue("@pBatchName", batch);
                    cmd.Parameters.AddWithValue("@Options", options);
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    if (con.State != ConnectionState.Closed)
                        con.Close();
                }
            }
        }

        public void CancelWorkflow(int batch, int runid)
        {
            if (batch == 0 || runid == 0) return;
            using (SqlConnection con = new SqlConnection(m_connectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                try
                {
                    con.Open();
                    cmd.Connection = con;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "[dbo].[prc_ETLCounterSet]";
                    cmd.Parameters.AddWithValue("@pBatchID", batch);
                    cmd.Parameters.AddWithValue("@pStepID", 0);
                    cmd.Parameters.AddWithValue("@pRunID", runid);
                    cmd.Parameters.AddWithValue("@pName", "ExitEvent");
                    cmd.Parameters.AddWithValue("@pValue", "4");
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    if (con.State != ConnectionState.Closed)
                        con.Close();
                }
            }
        }



        public void Connect(string User,string Password)
        {
            Connect(m_server,m_database,User,Password);
        }

        public string ConnectionString
        {
            get { return m_connectionString; }
        }

        public String Server
        {
            get { return m_server; }
            set { m_server = value; }
        }

        public String Database
        {
            get { return m_database; }
            set { m_database = value; }
        }


        public void Disconnect()
        {
            //if (!String.IsNullOrEmpty(m_connectionString))
            //{
            //    SqlDependency.Stop(m_connectionString);
            //}
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {

            if (!this.disposed)
            {
                if (disposing)
                {
                    //clean up managed resources
                    Disconnect();
                }

                // clean up unmanaged resources

                disposed = true;
            }
        }
    }
}
