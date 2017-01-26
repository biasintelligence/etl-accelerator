using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Security.Permissions;
using System.Data.SqlTypes;
using ETL_Framework.Properties;


namespace ETL_Framework
{
    public partial class ETLMonitor : Form
    {
        ETLDBMonitor m_dbmon = new ETLDBMonitor();
        DataSet m_DataSet;
        int m_BatchID = 0;
        int m_StepID = 0;
        int m_RunID = 0;
        int m_LogID = 0;  //Log query delta control
        SqlDateTime m_BatchRunStatusDT = SqlDateTime.Null; //BatchRun query delta control
        SqlDateTime m_StepRunStatusDT = SqlDateTime.Null; //StepRun query delta control
        bool m_BatchChanged = false;
        bool m_BatchRunChanged = false;
        bool m_StepRunChanged = false;
        bool m_StepRunHistoryChanged = false;
        bool m_CountersChanged = false;
        bool m_LogChanged = false;
        bool m_StepRunMode = false;

        public ETLMonitor()
        {
            InitializeComponent();
        }

        private void ETLMonitor_Load(object sender, EventArgs e)
        {
            try
            {
                Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                this.Text = String.Format("ETL Monitor v{0} {1}-bit", v.ToString(), 8*IntPtr.Size);

                timerRefresh.Interval = 10000;
                timerRefresh.Tick += new EventHandler(refresh_OnTimer);
                timerRefresh.Enabled = true;

                toolStripTextBoxServer.Text = m_dbmon.Server;
                toolStripTextBoxDatabase.Text = m_dbmon.Database;

                toolStripStatusLabelServer.ForeColor = Color.Empty;
                toolStripStatusLabelServer.Text = m_dbmon.Server + "/" + m_dbmon.Database;

                treeViewBatchRun.ImageList = imageListStatus;
                m_DataSet = new DataSet();

                m_dbmon.Connect();

                RefreshAll();
                timerRefresh.Start();
            }
            catch (Exception ex)
            {
                toolStripStatusLabelServer.Text = ex.Message;
                toolStripStatusLabelServer.ForeColor = Color.Red;

            }
        }


        private void GetBatchData()
        {
            if (m_DataSet.Tables["Batch"] != null)
            { m_DataSet.Tables["Batch"].Clear(); }

            using (SqlConnection conn = new SqlConnection(m_dbmon.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(Resources.QueryBatch, conn))
            using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
            {
                adapter.Fill(m_DataSet, "Batch");
                DataTable br = m_DataSet.Tables["Batch"];
                foreach (DataRow r in br.Rows)
                {
                    int bIdx = treeViewBatchRun.Nodes.IndexOfKey("b" + r["BatchID"].ToString());
                    if (bIdx == -1)
                    {
                        TreeNode node = treeViewBatchRun.Nodes.Add("b" + r["BatchID"].ToString(), r["BatchID"].ToString() + "-" + r["BatchName"].ToString());
                        node.Tag = r["BatchID"];
                        int iidx = 0;
                        node.ImageIndex = iidx;
                        node.SelectedImageIndex = iidx;
                    }
                }
            }
        }



        private void GetBatchRunData()
        {
            if (m_DataSet.Tables["BatchRun"] != null)
            { m_DataSet.Tables["BatchRun"].Clear(); }

            using (SqlConnection conn = new SqlConnection(m_dbmon.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(Resources.QueryBatchRun, conn))
            {

                cmd.Parameters.Add("@StatusDT", SqlDbType.DateTime);
                cmd.Parameters.Add("@TheDate", SqlDbType.DateTime);
                cmd.Parameters["@TheDate"].Value = DateTime.Today.AddDays(-Settings.Default.DaysBack);

                cmd.Notification = null;
                cmd.Parameters["@StatusDT"].Value = m_BatchRunStatusDT;

                SqlDependency dependency = new SqlDependency(cmd);
                dependency.OnChange += new OnChangeEventHandler(dependency_OnBatchRunChange);

                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(m_DataSet, "BatchRun");
                    DataTable br = m_DataSet.Tables["BatchRun"];
                    foreach (DataRow r in br.Rows)
                    {
                        int bIdx = treeViewBatchRun.Nodes.IndexOfKey("b" + r["BatchID"].ToString());
                        if (bIdx == -1) continue;

                        int rIdx = treeViewBatchRun.Nodes[bIdx].Nodes.IndexOfKey("r" + r["RunID"].ToString());
                        int iidx = Convert.ToInt32(r["StatusID"]);

                        if (rIdx == -1)
                        {
                            TreeNode node = treeViewBatchRun.Nodes[bIdx].Nodes.Add("r" + r["RunID"].ToString(), "Run-" + r["RunID"].ToString());
                            node.Tag = r["RunID"];

                            node.ImageIndex = iidx;

                            node.SelectedImageIndex = GetSelectedImageIndex(iidx);
                            node.Parent.ImageIndex = iidx;
                            node.Parent.SelectedImageIndex = GetSelectedImageIndex(iidx);
                        }
                        else
                        {
                            treeViewBatchRun.Nodes[bIdx].Nodes[rIdx].ImageIndex = iidx;
                            treeViewBatchRun.Nodes[bIdx].ImageIndex = iidx;
                            treeViewBatchRun.Nodes[bIdx].Nodes[rIdx].SelectedImageIndex = GetSelectedImageIndex(iidx);
                            treeViewBatchRun.Nodes[bIdx].SelectedImageIndex = GetSelectedImageIndex(iidx);

                        }

                        if (r["StatusDT"] != DBNull.Value && (m_BatchRunStatusDT.IsNull || m_BatchRunStatusDT < Convert.ToDateTime(r["StatusDT"])))
                        { m_BatchRunStatusDT = Convert.ToDateTime(r["StatusDT"]); }

                    }
                }
            }
        }

        private int GetSelectedImageIndex(int idx)
        {
            return idx + 5;
        }
        
        private void GetStepRunData()
        {

            if (m_DataSet.Tables["StepRun"] != null)
            { m_DataSet.Tables["StepRun"].Clear(); }

            using (SqlConnection conn = new SqlConnection(m_dbmon.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(Resources.QueryStepRun, conn))
            {

                cmd.Parameters.AddWithValue("@BatchID", m_BatchID);
                cmd.Parameters.Add("@StatusDT", SqlDbType.DateTime);

                cmd.Notification = null;
                cmd.Parameters["@BatchID"].Value = m_BatchID;
                cmd.Parameters["@StatusDT"].Value = m_StepRunStatusDT;
                m_StepRunMode = true;


                SqlDependency dependency = new SqlDependency(cmd);
                dependency.OnChange += new OnChangeEventHandler(dependency_OnStepRunChange);

                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(m_DataSet, "StepRun");
                }

                //dataGridStepRun.DataSource = m_DataSet;
                //dataGridStepRun.DataMember = "StepRun";

                if (m_StepRunStatusDT.IsNull || m_DataSet.Tables["StepRun"].Rows.Count == 0)
                {
                    dataGridStepRun.Rows.Clear();
                }


                foreach (DataRow r in m_DataSet.Tables["StepRun"].Rows)
                {
                    int rIdx = -1;
                    if (!m_StepRunStatusDT.IsNull)
                    {
                        rIdx = FindRowByColumnValue(dataGridStepRun, "StepID", Convert.ToInt32(r["StepID"]));
                    }

                    if (rIdx == -1)
                    {
                        rIdx = dataGridStepRun.Rows.Add();
                        dataGridStepRun.Rows[rIdx].Cells["StepDesc"].Value = r["StepDesc"];
                        dataGridStepRun.Rows[rIdx].Cells["StepID"].Value = r["StepID"];
                        dataGridStepRun.Rows[rIdx].Cells["RunID"].Value = r["RunID"];
                        dataGridStepRun.Rows[rIdx].Cells["StartTime"].Value = r["StartTime"];
                        dataGridStepRun.Rows[rIdx].Cells["EndTime"].Value = r["EndTime"];
                        dataGridStepRun.Rows[rIdx].Cells["SvcName"].Value = r["SvcName"];

                    }
                    else
                    {
                        dataGridStepRun.Rows[rIdx].Cells["StartTime"].Value = r["StartTime"];
                        dataGridStepRun.Rows[rIdx].Cells["EndTime"].Value = r["EndTime"];

                    }

                    int iidx = Convert.ToInt32(r["StatusID"]);
                    dataGridStepRun.Rows[rIdx].Cells["Status"].Value = imageListStatus.Images[iidx];


                    if (r["EndTime"] != DBNull.Value && (m_StepRunStatusDT.IsNull || m_StepRunStatusDT < (DateTime)r["EndTime"]))
                    { m_StepRunStatusDT = (DateTime)r["EndTime"]; }
                }

                dataGridStepRun.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                dataGridStepRun.ReadOnly = true;
            }
        }

        private int FindRowByColumnValue(DataGridView grid, string ColName, int Value)
        {
            foreach (DataGridViewRow r in grid.Rows)
            {
                if (Convert.ToInt32(r.Cells[ColName].Value) == Value)
                {
                    return r.Index;
                }
            }
            return -1;
        }

        private void GetStepRunHistoryData()
        {
            if (m_DataSet.Tables["StepRunHistory"] != null)
            { m_DataSet.Tables["StepRunHistory"].Clear(); }

            using (SqlConnection conn = new SqlConnection(m_dbmon.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(Resources.QueryStepRunHistory, conn))
            {

                cmd.Parameters.AddWithValue("@BatchID", m_BatchID);
                cmd.Parameters.AddWithValue("@RunID", m_RunID);

                cmd.Parameters["@BatchID"].Value = m_BatchID;
                cmd.Parameters["@RunID"].Value = m_RunID;
                m_StepRunMode = false;

                dataGridStepRun.Rows.Clear();

                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(m_DataSet, "StepRunHistory");
                }

                //dataGridStepRun.DataSource = m_DataSet;
                //dataGridStepRun.DataMember = "StepRunHistory";

                foreach (DataRow r in m_DataSet.Tables["StepRunHistory"].Rows)
                {
                    int rIdx = dataGridStepRun.Rows.Add();
                    dataGridStepRun.Rows[rIdx].Cells["StepDesc"].Value = r["StepDesc"];
                    dataGridStepRun.Rows[rIdx].Cells["StepID"].Value = r["StepID"];
                    dataGridStepRun.Rows[rIdx].Cells["RunID"].Value = r["RunID"];
                    dataGridStepRun.Rows[rIdx].Cells["StartTime"].Value = r["StartTime"];
                    dataGridStepRun.Rows[rIdx].Cells["EndTime"].Value = r["EndTime"];
                    dataGridStepRun.Rows[rIdx].Cells["SvcName"].Value = r["SvcName"];

                    int iidx = Convert.ToInt32(r["StatusID"]);
                    dataGridStepRun.Rows[rIdx].Cells["Status"].Value = imageListStatus.Images[iidx];

                }


                dataGridStepRun.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                dataGridStepRun.ReadOnly = true;
            }
        }


        private void GetCountersData()
        {
            if (m_DataSet.Tables["Counters"] != null)
            { m_DataSet.Tables["Counters"].Clear(); }


            using (SqlConnection conn = new SqlConnection(m_dbmon.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(Resources.QueryCounters, conn))
            {

                cmd.Parameters.AddWithValue("@BatchID", m_BatchID);
                cmd.Parameters.Add("@StepID", SqlDbType.Int);
                cmd.Parameters.Add("@RunID", SqlDbType.Int);

                cmd.Notification = null;
                cmd.Parameters["@BatchID"].Value = m_BatchID;
                cmd.Parameters["@StepID"].Value = m_StepID;
                cmd.Parameters["@RunID"].Value = m_RunID;

                dataGridCounters.Rows.Clear();

                SqlDependency dependency = new SqlDependency(cmd);
                dependency.OnChange += new OnChangeEventHandler(dependency_OnCountersChange);

                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(m_DataSet, "Counters");
                }

                //dataGridCounters.DataSource = m_DataSet;
                //dataGridCounters.DataMember = "Counters";
                foreach (DataRow r in m_DataSet.Tables["Counters"].Rows)
                {
                    int rIdx = dataGridCounters.Rows.Add();
                    dataGridCounters.Rows[rIdx].Cells["CounterName"].Value = r["CounterName"];
                    dataGridCounters.Rows[rIdx].Cells["CounterValue"].Value = r["CounterValue"];
                }



                dataGridCounters.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                dataGridCounters.ReadOnly = true;
            }
        }

        private void GetLogData()
        {
            if (m_DataSet.Tables["Log"] != null)
            { m_DataSet.Tables["Log"].Clear(); }

            using (SqlConnection conn = new SqlConnection(m_dbmon.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(Resources.QueryLog, conn))
            {

                cmd.Parameters.AddWithValue("@BatchID", m_BatchID);
                cmd.Parameters.Add("@StepID", SqlDbType.Int);
                cmd.Parameters.Add("@RunID", SqlDbType.Int);
                cmd.Parameters.Add("@LogID", SqlDbType.Int);

                cmd.Notification = null;
                cmd.Parameters["@BatchID"].Value = m_BatchID;
                cmd.Parameters["@StepID"].Value = m_StepID;
                cmd.Parameters["@RunID"].Value = m_RunID;
                cmd.Parameters["@LogID"].Value = m_LogID;

                if (m_LogID == 0)
                {
                    dataGridLog.Rows.Clear();
                }

                SqlDependency dependency = new SqlDependency(cmd);
                dependency.OnChange += new OnChangeEventHandler(dependency_OnLogChange);

                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(m_DataSet, "Log");
                }

                //dataGridLog.DataSource = m_DataSet;
                //dataGridLog.DataMember = "Log";
                foreach (DataRow r in m_DataSet.Tables["Log"].Rows)
                {
                    int rIdx = dataGridLog.Rows.Add();
                    dataGridLog.Rows[rIdx].Cells["LogDT"].Value = r["LogDT"];

                    int err = Convert.ToInt32(r["Err"]);
                    if (err != 0)
                    {
                        dataGridLog.Rows[rIdx].DefaultCellStyle.ForeColor = Color.Red;
                    }
                    dataGridLog.Rows[rIdx].Cells["Err"].Value = err;

                    dataGridLog.Rows[rIdx].Cells["LogMessage"].Value = r["LogMessage"];
                }

                dataGridLog.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                dataGridLog.ReadOnly = true;

                foreach (DataRow r in m_DataSet.Tables["Log"].Rows)
                {
                    if (m_LogID < Convert.ToInt32(r["LogID"]))
                    { m_LogID = Convert.ToInt32(r["LogID"]); }
                }
            }
        }



        delegate void UIDelegate();
        private void refresh_OnTimer(object sender, EventArgs e)
        {
            UIDelegate uidel = new UIDelegate(RefreshData);
            this.Invoke(uidel, null);
        }

        private void dependency_OnBatchRunChange(object sender, SqlNotificationEventArgs e)
        {
            //Remove the handler as it is used for a single notification.
            m_BatchRunChanged = true;
            SqlDependency dependency = (SqlDependency)sender;
            dependency.OnChange -= dependency_OnBatchRunChange;

        }        
        private void dependency_OnStepRunChange(object sender, SqlNotificationEventArgs e)
        {
            //Remove the handler as it is used for a single notification.
            m_StepRunChanged = m_StepRunMode;
            SqlDependency dependency = (SqlDependency)sender;
            dependency.OnChange -= dependency_OnStepRunChange;

        }
        private void dependency_OnCountersChange(object sender, SqlNotificationEventArgs e)
        {
            //Remove the handler as it is used for a single notification.
            m_CountersChanged = true;
            SqlDependency dependency = (SqlDependency)sender;
            dependency.OnChange -= dependency_OnCountersChange;

        }
        private void dependency_OnLogChange(object sender, SqlNotificationEventArgs e)
        {
            //Remove the handler as it is used for a single notification.
            m_LogChanged = true;
            SqlDependency dependency = (SqlDependency)sender;
            dependency.OnChange -= dependency_OnLogChange;

        }


        private void RefreshData()
        {

            try
            {
                if (m_BatchChanged)
                {
                    m_BatchChanged = false;
                    GetBatchData();
                }
                if (m_BatchRunChanged)
                {
                    m_BatchRunChanged = false;
                    GetBatchRunData();
                }
                if (m_StepRunChanged)
                {
                    m_StepRunChanged = false;
                    GetStepRunData();
                }
                if (m_StepRunHistoryChanged)
                {
                    m_StepRunHistoryChanged = false;
                    GetStepRunHistoryData();
                }
                if (m_CountersChanged)
                {
                    m_CountersChanged = false;
                    GetCountersData();
                }
                if (m_LogChanged)
                {
                    m_LogChanged = false;
                    GetLogData();
                }
            }
            catch
            {
                //toolStripStatusLabelServer.Text = ex.Message;
                toolStripStatusLabelServer.Text = String.Format(System.Globalization.CultureInfo.InvariantCulture, "Lost Connection to {0}/{1}", m_dbmon.Server, m_dbmon.Database);
                toolStripStatusLabelServer.ForeColor = Color.Red;
            }
        }

        private void ClearControls()
        {
            cancelToolStripMenuItem.Enabled = false;
            treeViewBatchRun.Nodes.Clear();
            dataGridStepRun.Rows.Clear();
            dataGridCounters.Rows.Clear();
            dataGridLog.Rows.Clear();
        }

        private void RefreshAll()
        {
            ClearControls();

            m_BatchID = 0;
            m_StepID = 0;
            m_RunID = 0;
            m_LogID = 0;
            m_BatchRunStatusDT = SqlDateTime.Null; 
            m_StepRunStatusDT = SqlDateTime.Null;


            m_BatchChanged = true;
            m_BatchRunChanged = true;
            RefreshData();
        }

        private void ETLMonitor_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_dbmon.Disconnect();
        }

        private void dataGridStepRun_Click(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1)
            { return; }

            DataGridView grid = (DataGridView)sender;
            m_StepID = (int)grid.Rows[e.RowIndex].Cells["StepID"].Value;
            m_RunID = (int)grid.Rows[e.RowIndex].Cells["RunID"].Value;
            m_CountersChanged = true;
            m_LogChanged = true;
            m_LogID = 0;

            RefreshData();

        }

        private void treeViewBatchRun_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {

            e.Node.SelectedImageIndex = GetSelectedImageIndex(e.Node.ImageIndex);
            //show steprun when batch is running
            if (e.Node.ImageIndex == 1)
            {
                m_StepRunChanged = true;
            }
                //show step run history
            else
            {
                m_StepRunHistoryChanged = true;
            }

            if(e.Node.Level == 0)
            {
                m_BatchID = (int)e.Node.Tag;
                m_RunID = 0;
            }
            else
            {
                m_BatchID = (int)e.Node.Parent.Tag;
                m_RunID = (int)e.Node.Tag;
            }

            cancelToolStripMenuItem.Enabled = false;
            if (e.Node.ImageIndex == 1 && m_RunID > 0)
            {
                cancelToolStripMenuItem.Enabled = true;
            }


            m_StepID = 0;
            m_CountersChanged = true;
            m_LogChanged = true;
            m_LogID = 0;
            m_BatchRunStatusDT = SqlDateTime.Null;
            m_StepRunStatusDT = SqlDateTime.Null;
            RefreshData();
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripStatusLabelServer.ForeColor = Color.Empty;
            m_dbmon.Server = toolStripTextBoxServer.Text;
            m_dbmon.Database = toolStripTextBoxDatabase.Text;
            toolStripStatusLabelServer.Text = m_dbmon.Server + "/" + m_dbmon.Database;
            try
            {
                timerRefresh.Stop();
                m_dbmon.Connect();

                RefreshAll();
                timerRefresh.Start();
            }
            catch
            {
                ClearControls();
                //toolStripStatusLabelServer.Text = ex.Message;
                toolStripStatusLabelServer.Text = String.Format(System.Globalization.CultureInfo.InvariantCulture,"Failed to connect to {0}/{1}",m_dbmon.Server,m_dbmon.Database);
                toolStripStatusLabelServer.ForeColor = Color.Red;

            }
        }

        
        private void cancelToolStripMenuItem_Click(object sender, EventArgs e)
        {

            try
            {
                m_dbmon.CancelWorkflow(m_BatchID,m_RunID);
            }
            catch
            {
                //toolStripStatusLabelServer.Text = ex.Message;
                toolStripStatusLabelServer.Text = String.Format(System.Globalization.CultureInfo.InvariantCulture, "Failed to cancel the workflow {0}", m_BatchID);
                toolStripStatusLabelServer.ForeColor = Color.Red;
            }
        }


    }
}
