namespace ETL_Framework
{
    partial class ETLMonitor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ETLMonitor));
            this.timerRefresh = new System.Windows.Forms.Timer(this.components);
            this.statusBar = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabelServer = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.treeViewBatchRun = new System.Windows.Forms.TreeView();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.dataGridStepRun = new System.Windows.Forms.DataGridView();
            this.Status = new System.Windows.Forms.DataGridViewImageColumn();
            this.StepDesc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StartTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.EndTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RunID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StepID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SvcName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridCounters = new System.Windows.Forms.DataGridView();
            this.CounterName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CounterValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridLog = new System.Windows.Forms.DataGridView();
            this.LogDT = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Err = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LogMessage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.connectStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.serverToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripTextBoxServer = new System.Windows.Forms.ToolStripTextBox();
            this.databaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripTextBoxDatabase = new System.Windows.Forms.ToolStripTextBox();
            this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageListStatus = new System.Windows.Forms.ImageList(this.components);
            this.actionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cancelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusBar.SuspendLayout();
            this.toolStripContainer1.BottomToolStripPanel.SuspendLayout();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridStepRun)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridCounters)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridLog)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // timerRefresh
            // 
            this.timerRefresh.Interval = 100000;
            // 
            // statusBar
            // 
            this.statusBar.Dock = System.Windows.Forms.DockStyle.None;
            this.statusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabelServer});
            this.statusBar.Location = new System.Drawing.Point(0, 0);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(1228, 22);
            this.statusBar.TabIndex = 1;
            // 
            // toolStripStatusLabelServer
            // 
            this.toolStripStatusLabelServer.Name = "toolStripStatusLabelServer";
            this.toolStripStatusLabelServer.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.BottomToolStripPanel
            // 
            this.toolStripContainer1.BottomToolStripPanel.Controls.Add(this.statusBar);
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.AutoScroll = true;
            this.toolStripContainer1.ContentPanel.Controls.Add(this.splitContainer1);
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(1228, 350);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Size = new System.Drawing.Size(1228, 396);
            this.toolStripContainer1.TabIndex = 2;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.menuStrip1);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.treeViewBatchRun);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(1228, 350);
            this.splitContainer1.SplitterDistance = 250;
            this.splitContainer1.TabIndex = 13;
            // 
            // treeViewBatchRun
            // 
            this.treeViewBatchRun.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewBatchRun.Location = new System.Drawing.Point(0, 0);
            this.treeViewBatchRun.Name = "treeViewBatchRun";
            this.treeViewBatchRun.Size = new System.Drawing.Size(250, 350);
            this.treeViewBatchRun.TabIndex = 13;
            this.treeViewBatchRun.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeViewBatchRun_NodeMouseClick);
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.splitContainer3);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.dataGridLog);
            this.splitContainer2.Size = new System.Drawing.Size(974, 350);
            this.splitContainer2.SplitterDistance = 172;
            this.splitContainer2.TabIndex = 16;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Name = "splitContainer3";
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.dataGridStepRun);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.dataGridCounters);
            this.splitContainer3.Size = new System.Drawing.Size(974, 172);
            this.splitContainer3.SplitterDistance = 592;
            this.splitContainer3.TabIndex = 13;
            // 
            // dataGridStepRun
            // 
            this.dataGridStepRun.AllowUserToAddRows = false;
            this.dataGridStepRun.AllowUserToDeleteRows = false;
            this.dataGridStepRun.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridStepRun.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Status,
            this.StepDesc,
            this.StartTime,
            this.EndTime,
            this.RunID,
            this.StepID,
            this.SvcName});
            this.dataGridStepRun.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridStepRun.Location = new System.Drawing.Point(0, 0);
            this.dataGridStepRun.MultiSelect = false;
            this.dataGridStepRun.Name = "dataGridStepRun";
            this.dataGridStepRun.ReadOnly = true;
            this.dataGridStepRun.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridStepRun.Size = new System.Drawing.Size(592, 172);
            this.dataGridStepRun.TabIndex = 13;
            this.dataGridStepRun.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridStepRun_Click);
            // 
            // Status
            // 
            this.Status.HeaderText = "Status";
            this.Status.Name = "Status";
            this.Status.ReadOnly = true;
            // 
            // StepDesc
            // 
            this.StepDesc.HeaderText = "StepDesc";
            this.StepDesc.Name = "StepDesc";
            this.StepDesc.ReadOnly = true;
            // 
            // StartTime
            // 
            this.StartTime.HeaderText = "StartTime";
            this.StartTime.Name = "StartTime";
            this.StartTime.ReadOnly = true;
            // 
            // EndTime
            // 
            this.EndTime.HeaderText = "EndTime";
            this.EndTime.Name = "EndTime";
            this.EndTime.ReadOnly = true;
            // 
            // RunID
            // 
            this.RunID.HeaderText = "RunID";
            this.RunID.Name = "RunID";
            this.RunID.ReadOnly = true;
            this.RunID.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.RunID.Visible = false;
            // 
            // StepID
            // 
            this.StepID.HeaderText = "StepID";
            this.StepID.Name = "StepID";
            this.StepID.ReadOnly = true;
            this.StepID.Visible = false;
            // 
            // SvcName
            // 
            this.SvcName.HeaderText = "SvcName";
            this.SvcName.Name = "SvcName";
            this.SvcName.ReadOnly = true;
            // 
            // dataGridCounters
            // 
            this.dataGridCounters.AllowUserToAddRows = false;
            this.dataGridCounters.AllowUserToDeleteRows = false;
            this.dataGridCounters.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridCounters.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.CounterName,
            this.CounterValue});
            this.dataGridCounters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridCounters.Location = new System.Drawing.Point(0, 0);
            this.dataGridCounters.MultiSelect = false;
            this.dataGridCounters.Name = "dataGridCounters";
            this.dataGridCounters.ReadOnly = true;
            this.dataGridCounters.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridCounters.Size = new System.Drawing.Size(378, 172);
            this.dataGridCounters.TabIndex = 12;
            // 
            // CounterName
            // 
            this.CounterName.HeaderText = "CounterName";
            this.CounterName.Name = "CounterName";
            this.CounterName.ReadOnly = true;
            // 
            // CounterValue
            // 
            this.CounterValue.HeaderText = "Value";
            this.CounterValue.Name = "CounterValue";
            this.CounterValue.ReadOnly = true;
            // 
            // dataGridLog
            // 
            this.dataGridLog.AllowUserToAddRows = false;
            this.dataGridLog.AllowUserToDeleteRows = false;
            this.dataGridLog.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridLog.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.LogDT,
            this.Err,
            this.LogMessage});
            this.dataGridLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridLog.Location = new System.Drawing.Point(0, 0);
            this.dataGridLog.MultiSelect = false;
            this.dataGridLog.Name = "dataGridLog";
            this.dataGridLog.ReadOnly = true;
            this.dataGridLog.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridLog.Size = new System.Drawing.Size(974, 174);
            this.dataGridLog.TabIndex = 16;
            // 
            // LogDT
            // 
            this.LogDT.HeaderText = "LogDate";
            this.LogDT.Name = "LogDT";
            this.LogDT.ReadOnly = true;
            // 
            // Err
            // 
            this.Err.HeaderText = "Error";
            this.Err.Name = "Err";
            this.Err.ReadOnly = true;
            // 
            // LogMessage
            // 
            this.LogMessage.HeaderText = "Message";
            this.LogMessage.Name = "LogMessage";
            this.LogMessage.ReadOnly = true;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.connectStripMenuItem,
            this.actionToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1228, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // connectStripMenuItem
            // 
            this.connectStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.serverToolStripMenuItem,
            this.databaseToolStripMenuItem,
            this.refreshToolStripMenuItem});
            this.connectStripMenuItem.Name = "connectStripMenuItem";
            this.connectStripMenuItem.Size = new System.Drawing.Size(64, 20);
            this.connectStripMenuItem.Text = "Connect";
            // 
            // serverToolStripMenuItem
            // 
            this.serverToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripTextBoxServer});
            this.serverToolStripMenuItem.Name = "serverToolStripMenuItem";
            this.serverToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.serverToolStripMenuItem.Text = "Server";
            // 
            // toolStripTextBoxServer
            // 
            this.toolStripTextBoxServer.Name = "toolStripTextBoxServer";
            this.toolStripTextBoxServer.Size = new System.Drawing.Size(100, 23);
            // 
            // databaseToolStripMenuItem
            // 
            this.databaseToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripTextBoxDatabase});
            this.databaseToolStripMenuItem.Name = "databaseToolStripMenuItem";
            this.databaseToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.databaseToolStripMenuItem.Text = "Database";
            // 
            // toolStripTextBoxDatabase
            // 
            this.toolStripTextBoxDatabase.Name = "toolStripTextBoxDatabase";
            this.toolStripTextBoxDatabase.Size = new System.Drawing.Size(100, 23);
            // 
            // refreshToolStripMenuItem
            // 
            this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            this.refreshToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.refreshToolStripMenuItem.Text = "Refresh";
            this.refreshToolStripMenuItem.Click += new System.EventHandler(this.refreshToolStripMenuItem_Click);
            // 
            // imageListStatus
            // 
            this.imageListStatus.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListStatus.ImageStream")));
            this.imageListStatus.TransparentColor = System.Drawing.Color.Transparent;
            this.imageListStatus.Images.SetKeyName(0, "new01.jpg");
            this.imageListStatus.Images.SetKeyName(1, "run01.gif");
            this.imageListStatus.Images.SetKeyName(2, "success01.gif");
            this.imageListStatus.Images.SetKeyName(3, "err01.jpg");
            this.imageListStatus.Images.SetKeyName(4, "err01.jpg");
            this.imageListStatus.Images.SetKeyName(5, "new02.gif");
            this.imageListStatus.Images.SetKeyName(6, "run02.gif");
            this.imageListStatus.Images.SetKeyName(7, "success02.gif");
            this.imageListStatus.Images.SetKeyName(8, "err02.gif");
            this.imageListStatus.Images.SetKeyName(9, "err02.gif");
            // 
            // actionToolStripMenuItem
            // 
            this.actionToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cancelToolStripMenuItem});
            this.actionToolStripMenuItem.Name = "actionToolStripMenuItem";
            this.actionToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
            this.actionToolStripMenuItem.Text = "Action";
            // 
            // cancelToolStripMenuItem
            // 
            this.cancelToolStripMenuItem.Name = "cancelToolStripMenuItem";
            this.cancelToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.cancelToolStripMenuItem.Text = "Cancel";
            this.cancelToolStripMenuItem.Click += new System.EventHandler(this.cancelToolStripMenuItem_Click);
            // 
            // ETLMonitor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1228, 396);
            this.Controls.Add(this.toolStripContainer1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "ETLMonitor";
            this.Text = "ETLMonitor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ETLMonitor_FormClosing);
            this.Load += new System.EventHandler(this.ETLMonitor_Load);
            this.statusBar.ResumeLayout(false);
            this.statusBar.PerformLayout();
            this.toolStripContainer1.BottomToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.BottomToolStripPanel.PerformLayout();
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.PerformLayout();
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            this.splitContainer3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridStepRun)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridCounters)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridLog)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer timerRefresh;
        private System.Windows.Forms.StatusStrip statusBar;
        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelServer;
        private System.Windows.Forms.ImageList imageListStatus;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView treeViewBatchRun;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.DataGridView dataGridLog;
        private System.Windows.Forms.DataGridView dataGridStepRun;
        private System.Windows.Forms.DataGridView dataGridCounters;
        private System.Windows.Forms.DataGridViewTextBoxColumn LogDT;
        private System.Windows.Forms.DataGridViewTextBoxColumn Err;
        private System.Windows.Forms.DataGridViewTextBoxColumn LogMessage;
        private System.Windows.Forms.DataGridViewTextBoxColumn CounterName;
        private System.Windows.Forms.DataGridViewTextBoxColumn CounterValue;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem connectStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem serverToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBoxServer;
        private System.Windows.Forms.ToolStripMenuItem databaseToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBoxDatabase;
        private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
        private System.Windows.Forms.DataGridViewImageColumn Status;
        private System.Windows.Forms.DataGridViewTextBoxColumn StepDesc;
        private System.Windows.Forms.DataGridViewTextBoxColumn StartTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn EndTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn RunID;
        private System.Windows.Forms.DataGridViewTextBoxColumn StepID;
        private System.Windows.Forms.DataGridViewTextBoxColumn SvcName;
        private System.Windows.Forms.ToolStripMenuItem actionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cancelToolStripMenuItem;
    }
}

