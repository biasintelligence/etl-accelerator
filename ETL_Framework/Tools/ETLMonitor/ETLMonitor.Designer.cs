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
            this.splitContainer4 = new System.Windows.Forms.SplitContainer();
            this.dataGridCounters = new System.Windows.Forms.DataGridView();
            this.dataGridStepCounters = new System.Windows.Forms.DataGridView();
            this.dataGridLog = new System.Windows.Forms.DataGridView();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.connectStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.serverToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripTextBoxServer = new System.Windows.Forms.ToolStripTextBox();
            this.databaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripTextBoxDatabase = new System.Windows.Forms.ToolStripTextBox();
            this.userToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripTextBoxUser = new System.Windows.Forms.ToolStripTextBox();
            this.passwordToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripTextBoxPassword = new System.Windows.Forms.ToolStripTextBox();
            this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.actionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cancelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageListStatus = new System.Windows.Forms.ImageList(this.components);
            this.StepID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Status = new System.Windows.Forms.DataGridViewImageColumn();
            this.StepDesc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StartTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.EndTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RunID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SvcName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StepOrder = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PriGroup = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SeqGroup = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LogDT = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Err = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LogMessage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CounterName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CounterValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StepCounterName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StepCounterValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.statusBar.SuspendLayout();
            this.toolStripContainer1.BottomToolStripPanel.SuspendLayout();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridStepRun)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).BeginInit();
            this.splitContainer4.Panel1.SuspendLayout();
            this.splitContainer4.Panel2.SuspendLayout();
            this.splitContainer4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridCounters)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridStepCounters)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridLog)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // timerRefresh
            // 
            this.timerRefresh.Interval = 120000;
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
            this.splitContainer3.Panel2.Controls.Add(this.splitContainer4);
            this.splitContainer3.Panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.splitContainer3_Panel2_Paint);
            this.splitContainer3.Size = new System.Drawing.Size(974, 172);
            this.splitContainer3.SplitterDistance = 800;
            this.splitContainer3.TabIndex = 13;
            // 
            // dataGridStepRun
            // 
            this.dataGridStepRun.AllowUserToAddRows = false;
            this.dataGridStepRun.AllowUserToDeleteRows = false;
            this.dataGridStepRun.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridStepRun.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.StepID,
            this.Status,
            this.StepDesc,
            this.StartTime,
            this.EndTime,
            this.RunID,
            this.SvcName,
            this.StepOrder,
            this.PriGroup,
            this.SeqGroup});
            this.dataGridStepRun.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridStepRun.Location = new System.Drawing.Point(0, 0);
            this.dataGridStepRun.MultiSelect = false;
            this.dataGridStepRun.Name = "dataGridStepRun";
            this.dataGridStepRun.ReadOnly = true;
            this.dataGridStepRun.RowHeadersVisible = false;
            this.dataGridStepRun.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridStepRun.Size = new System.Drawing.Size(800, 172);
            this.dataGridStepRun.TabIndex = 13;
            this.dataGridStepRun.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridStepRun_Click);
            // 
            // splitContainer4
            // 
            this.splitContainer4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer4.Location = new System.Drawing.Point(0, 0);
            this.splitContainer4.Name = "splitContainer4";
            this.splitContainer4.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer4.Panel1
            // 
            this.splitContainer4.Panel1.Controls.Add(this.dataGridCounters);
            this.splitContainer4.Panel1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            // 
            // splitContainer4.Panel2
            // 
            this.splitContainer4.Panel2.Controls.Add(this.dataGridStepCounters);
            this.splitContainer4.Panel2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.splitContainer4.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.splitContainer4.Size = new System.Drawing.Size(170, 172);
            this.splitContainer4.SplitterDistance = 86;
            this.splitContainer4.TabIndex = 14;
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
            this.dataGridCounters.RowHeadersVisible = false;
            this.dataGridCounters.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridCounters.Size = new System.Drawing.Size(170, 86);
            this.dataGridCounters.TabIndex = 13;
            // 
            // dataGridStepCounters
            // 
            this.dataGridStepCounters.AllowUserToAddRows = false;
            this.dataGridStepCounters.AllowUserToDeleteRows = false;
            this.dataGridStepCounters.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridStepCounters.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.StepCounterName,
            this.StepCounterValue});
            this.dataGridStepCounters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridStepCounters.Location = new System.Drawing.Point(0, 0);
            this.dataGridStepCounters.MultiSelect = false;
            this.dataGridStepCounters.Name = "dataGridStepCounters";
            this.dataGridStepCounters.ReadOnly = true;
            this.dataGridStepCounters.RowHeadersVisible = false;
            this.dataGridStepCounters.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridStepCounters.Size = new System.Drawing.Size(170, 82);
            this.dataGridStepCounters.TabIndex = 14;
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
            this.dataGridLog.RowHeadersVisible = false;
            this.dataGridLog.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridLog.Size = new System.Drawing.Size(974, 174);
            this.dataGridLog.TabIndex = 16;
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
            this.menuStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.menuStrip1_ItemClicked);
            // 
            // connectStripMenuItem
            // 
            this.connectStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.serverToolStripMenuItem,
            this.databaseToolStripMenuItem,
            this.userToolStripMenuItem,
            this.passwordToolStripMenuItem,
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
            this.serverToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
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
            this.databaseToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.databaseToolStripMenuItem.Text = "Database";
            // 
            // toolStripTextBoxDatabase
            // 
            this.toolStripTextBoxDatabase.Name = "toolStripTextBoxDatabase";
            this.toolStripTextBoxDatabase.Size = new System.Drawing.Size(100, 23);
            // 
            // userToolStripMenuItem
            // 
            this.userToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripTextBoxUser});
            this.userToolStripMenuItem.Name = "userToolStripMenuItem";
            this.userToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.userToolStripMenuItem.Text = "User";
            // 
            // toolStripTextBoxUser
            // 
            this.toolStripTextBoxUser.Name = "toolStripTextBoxUser";
            this.toolStripTextBoxUser.Size = new System.Drawing.Size(100, 23);
            // 
            // passwordToolStripMenuItem
            // 
            this.passwordToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripTextBoxPassword});
            this.passwordToolStripMenuItem.Name = "passwordToolStripMenuItem";
            this.passwordToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.passwordToolStripMenuItem.Text = "Password";
            // 
            // toolStripTextBoxPassword
            // 
            this.toolStripTextBoxPassword.Name = "toolStripTextBoxPassword";
            this.toolStripTextBoxPassword.Size = new System.Drawing.Size(100, 23);
            // 
            // refreshToolStripMenuItem
            // 
            this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            this.refreshToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.refreshToolStripMenuItem.Text = "Refresh";
            this.refreshToolStripMenuItem.Click += new System.EventHandler(this.refreshToolStripMenuItem_Click);
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
            this.cancelToolStripMenuItem.Size = new System.Drawing.Size(110, 22);
            this.cancelToolStripMenuItem.Text = "Cancel";
            this.cancelToolStripMenuItem.Click += new System.EventHandler(this.cancelToolStripMenuItem_Click);
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
            // StepID
            // 
            this.StepID.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.StepID.HeaderText = "Id";
            this.StepID.Name = "StepID";
            this.StepID.ReadOnly = true;
            this.StepID.Width = 41;
            // 
            // Status
            // 
            this.Status.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.Status.HeaderText = "Status";
            this.Status.Name = "Status";
            this.Status.ReadOnly = true;
            this.Status.Width = 43;
            // 
            // StepDesc
            // 
            this.StepDesc.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.StepDesc.HeaderText = "StepDesc";
            this.StepDesc.Name = "StepDesc";
            this.StepDesc.ReadOnly = true;
            this.StepDesc.Width = 281;
            // 
            // StartTime
            // 
            this.StartTime.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.StartTime.HeaderText = "StartTime";
            this.StartTime.Name = "StartTime";
            this.StartTime.ReadOnly = true;
            this.StartTime.Width = 77;
            // 
            // EndTime
            // 
            this.EndTime.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.EndTime.HeaderText = "EndTime";
            this.EndTime.Name = "EndTime";
            this.EndTime.ReadOnly = true;
            this.EndTime.Width = 74;
            // 
            // RunID
            // 
            this.RunID.HeaderText = "RunId";
            this.RunID.Name = "RunID";
            this.RunID.ReadOnly = true;
            this.RunID.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.RunID.Visible = false;
            // 
            // SvcName
            // 
            this.SvcName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.SvcName.HeaderText = "SvcName";
            this.SvcName.Name = "SvcName";
            this.SvcName.ReadOnly = true;
            this.SvcName.Width = 79;
            // 
            // StepOrder
            // 
            this.StepOrder.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.StepOrder.HeaderText = "Order";
            this.StepOrder.Name = "StepOrder";
            this.StepOrder.ReadOnly = true;
            this.StepOrder.Width = 58;
            // 
            // PriGroup
            // 
            this.PriGroup.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.PriGroup.HeaderText = "Priority";
            this.PriGroup.Name = "PriGroup";
            this.PriGroup.ReadOnly = true;
            this.PriGroup.Width = 63;
            // 
            // SeqGroup
            // 
            this.SeqGroup.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.SeqGroup.HeaderText = "Sequence";
            this.SeqGroup.Name = "SeqGroup";
            this.SeqGroup.ReadOnly = true;
            this.SeqGroup.Width = 81;
            // 
            // LogDT
            // 
            this.LogDT.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.LogDT.HeaderText = "LogDate";
            this.LogDT.Name = "LogDT";
            this.LogDT.ReadOnly = true;
            this.LogDT.Width = 73;
            // 
            // Err
            // 
            this.Err.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.Err.HeaderText = "Error";
            this.Err.Name = "Err";
            this.Err.ReadOnly = true;
            this.Err.Width = 54;
            // 
            // LogMessage
            // 
            this.LogMessage.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.LogMessage.HeaderText = "Message";
            this.LogMessage.Name = "LogMessage";
            this.LogMessage.ReadOnly = true;
            this.LogMessage.Width = 771;
            // 
            // CounterName
            // 
            this.CounterName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.CounterName.HeaderText = "WF Counter";
            this.CounterName.Name = "CounterName";
            this.CounterName.ReadOnly = true;
            this.CounterName.Width = 89;
            // 
            // CounterValue
            // 
            this.CounterValue.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.CounterValue.HeaderText = "Value";
            this.CounterValue.Name = "CounterValue";
            this.CounterValue.ReadOnly = true;
            this.CounterValue.Width = 59;
            // 
            // StepCounterName
            // 
            this.StepCounterName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.StepCounterName.HeaderText = "Step Counter";
            this.StepCounterName.Name = "StepCounterName";
            this.StepCounterName.ReadOnly = true;
            this.StepCounterName.Width = 94;
            // 
            // StepCounterValue
            // 
            this.StepCounterValue.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.StepCounterValue.HeaderText = "Value";
            this.StepCounterValue.Name = "StepCounterValue";
            this.StepCounterValue.ReadOnly = true;
            this.StepCounterValue.Width = 59;
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
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
            this.splitContainer3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridStepRun)).EndInit();
            this.splitContainer4.Panel1.ResumeLayout(false);
            this.splitContainer4.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).EndInit();
            this.splitContainer4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridCounters)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridStepCounters)).EndInit();
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
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem connectStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem serverToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBoxServer;
        private System.Windows.Forms.ToolStripMenuItem databaseToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBoxDatabase;
        private System.Windows.Forms.ToolStripMenuItem userToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem actionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cancelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem passwordToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBoxPassword;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBoxUser;
        private System.Windows.Forms.SplitContainer splitContainer4;
        private System.Windows.Forms.DataGridView dataGridCounters;
        private System.Windows.Forms.DataGridView dataGridStepCounters;
        private System.Windows.Forms.DataGridViewTextBoxColumn StepID;
        private System.Windows.Forms.DataGridViewImageColumn Status;
        private System.Windows.Forms.DataGridViewTextBoxColumn StepDesc;
        private System.Windows.Forms.DataGridViewTextBoxColumn StartTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn EndTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn RunID;
        private System.Windows.Forms.DataGridViewTextBoxColumn SvcName;
        private System.Windows.Forms.DataGridViewTextBoxColumn StepOrder;
        private System.Windows.Forms.DataGridViewTextBoxColumn PriGroup;
        private System.Windows.Forms.DataGridViewTextBoxColumn SeqGroup;
        private System.Windows.Forms.DataGridViewTextBoxColumn LogDT;
        private System.Windows.Forms.DataGridViewTextBoxColumn Err;
        private System.Windows.Forms.DataGridViewTextBoxColumn LogMessage;
        private System.Windows.Forms.DataGridViewTextBoxColumn CounterName;
        private System.Windows.Forms.DataGridViewTextBoxColumn CounterValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn StepCounterName;
        private System.Windows.Forms.DataGridViewTextBoxColumn StepCounterValue;
    }
}

