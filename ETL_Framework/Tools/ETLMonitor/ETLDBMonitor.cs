using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
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
        private SqlConnection m_conn = new SqlConnection();
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
            SqlClientPermission permission =
                new SqlClientPermission(
                PermissionState.Unrestricted);
            try
            {
                permission.Demand();
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        private void Connect(string Server,string Database)
        {
            Disconnect();
            if (Server == "")
            {
                throw new InvalidArgumentException("Server is required");
            }
            if (Database == "")
            {
                throw new InvalidArgumentException("Database is required");
            }

            m_conn.ConnectionString = String.Format("Persist Security Info=False;Integrated Security=SSPI;database={0};server={1}", Database, Server);
                        
            try
            {
                if (m_conn.State == ConnectionState.Closed)
                {
                    m_conn.Open();
                }
            }
            catch (Exception)
            {
                throw new CouldNotConnectToDBController(Server, Database);
            }

            // Create a dependency
            try
            {
                SqlDependency.Stop(m_conn.ConnectionString);
                SqlDependency.Start(m_conn.ConnectionString);
            }
            catch (Exception)
            {
                throw new CouldNotReceiveNotifications(Server, Database);
            }


        }

        public void StartWorkflow(string batch,string options)
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[dbo].[prc_Execute]";
                cmd.Connection = m_conn;
                cmd.Parameters.AddWithValue("@pBatchName", batch);
                cmd.Parameters.AddWithValue("@Options", options);
                cmd.ExecuteNonQuery();
            }
        }

        public void CancelWorkflow(int batch, int runid)
        {
            if (batch == 0 || runid == 0) return;
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[dbo].[prc_ETLCounterSet]";
                cmd.Connection = m_conn;
                cmd.Parameters.AddWithValue("@pBatchID", batch);
                cmd.Parameters.AddWithValue("@pStepID", 0);
                cmd.Parameters.AddWithValue("@pRunID", runid);
                cmd.Parameters.AddWithValue("@pName", "ExitEvent");
                cmd.Parameters.AddWithValue("@pValue", "4");
                cmd.ExecuteNonQuery();
            }
        }



        public void Connect()
        {
            Connect(m_server,m_database);
        }

        public SqlConnection Connection
        {
            get {return m_conn;}
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
            if (m_conn != null && m_conn.ConnectionString != String.Empty)
            {
                SqlDependency.Stop(m_conn.ConnectionString);
                m_conn.Close();
            }
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
