namespace SOLTIUS_Scheduler_Add_On.UI
{
    partial class FormMain
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.configurationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.passwordToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportProfilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importProfilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
                        this.schedulerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
                        this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabsync = new System.Windows.Forms.TabPage();
            this.grpBox1 = new System.Windows.Forms.GroupBox();
            this.chkDryRun = new System.Windows.Forms.CheckBox();
            this.btnSync = new System.Windows.Forms.Button();
            this.pBar = new System.Windows.Forms.ProgressBar();
            this.grpBox1_1 = new System.Windows.Forms.GroupBox();
            this.chkSL = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.chkSO = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.chkPO = new System.Windows.Forms.CheckBox();
            this.labelPO = new System.Windows.Forms.Label();
            this.chkLogData = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.chkAll1 = new System.Windows.Forms.CheckBox();
            this.tabPending = new System.Windows.Forms.TabPage();
            this.chkSelectAllPending = new System.Windows.Forms.CheckBox();
            this.lblPendingFilterInfo = new System.Windows.Forms.Label();
            this.txtSearchPending = new System.Windows.Forms.TextBox();
            this.btnRefreshPending = new System.Windows.Forms.Button();
            this.btnSyncSelected = new System.Windows.Forms.Button();
            this.dgvPendingQueue = new System.Windows.Forms.DataGridView();
            this.tablog = new System.Windows.Forms.TabPage();
            this.txtSearchLog = new System.Windows.Forms.TextBox();
            this.btnExportExcel = new System.Windows.Forms.Button();
            this.cbFunction = new System.Windows.Forms.ComboBox();
            this.btnViewLog = new System.Windows.Forms.Button();
            this.btnRetryFailed = new System.Windows.Forms.Button();
            this.cbLogLevel = new System.Windows.Forms.ComboBox();
            this.dtpStgTo = new System.Windows.Forms.DateTimePicker();
            this.lblStgTo = new System.Windows.Forms.Label();
            this.dtpStgFrom = new System.Windows.Forms.DateTimePicker();
            this.lblStgFrom = new System.Windows.Forms.Label();
            this.dgvlLogData = new System.Windows.Forms.DataGridView();
            this.label8 = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabsync.SuspendLayout();
            this.grpBox1.SuspendLayout();
            this.grpBox1_1.SuspendLayout();
            this.tabPending.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPendingQueue)).BeginInit();
            this.tablog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvlLogData)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                        this.configurationToolStripMenuItem,
                        this.schedulerToolStripMenuItem,
                        this.passwordToolStripMenuItem,
                        this.exportProfilesToolStripMenuItem,
                        this.importProfilesToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(936, 28);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // configurationToolStripMenuItem
            // 
            this.configurationToolStripMenuItem.Name = "configurationToolStripMenuItem";
            this.configurationToolStripMenuItem.Size = new System.Drawing.Size(114, 24);
            this.configurationToolStripMenuItem.Text = "Configuration";
            // 
            // passwordToolStripMenuItem
            // 
            this.passwordToolStripMenuItem.Name = "passwordToolStripMenuItem";
            this.passwordToolStripMenuItem.Size = new System.Drawing.Size(84, 24);
            this.passwordToolStripMenuItem.Text = "Password";
            // 
            // exportProfilesToolStripMenuItem
            // 
            this.exportProfilesToolStripMenuItem.Name = "exportProfilesToolStripMenuItem";
            this.exportProfilesToolStripMenuItem.Size = new System.Drawing.Size(113, 24);
            this.exportProfilesToolStripMenuItem.Text = "Export Profile";
            // 
            // importProfilesToolStripMenuItem
            // 
            this.importProfilesToolStripMenuItem.Name = "importProfilesToolStripMenuItem";
            this.importProfilesToolStripMenuItem.Size = new System.Drawing.Size(115, 24);
            this.importProfilesToolStripMenuItem.Text = "Import Profile";
            // 
            // schedulerToolStripMenuItem
            // 
            this.schedulerToolStripMenuItem.Name = "schedulerToolStripMenuItem";
            this.schedulerToolStripMenuItem.Size = new System.Drawing.Size(85, 24);
            this.schedulerToolStripMenuItem.Text = "Scheduler";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabsync);
            this.tabControl1.Controls.Add(this.tabPending);
            this.tabControl1.Controls.Add(this.tablog);
            this.tabControl1.Location = new System.Drawing.Point(12, 76);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(901, 474);
            this.tabControl1.TabIndex = 2;
            // 
            // tabsync
            // 
            this.tabsync.Controls.Add(this.grpBox1);
            this.tabsync.Controls.Add(this.chkAll1);
            this.tabsync.Location = new System.Drawing.Point(4, 25);
            this.tabsync.Name = "tabsync";
            this.tabsync.Padding = new System.Windows.Forms.Padding(3);
            this.tabsync.Size = new System.Drawing.Size(893, 445);
            this.tabsync.TabIndex = 0;
            this.tabsync.Text = "Synchronize";
            this.tabsync.UseVisualStyleBackColor = true;
            // 
            // grpBox1
            // 
            this.grpBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpBox1.Controls.Add(this.chkDryRun);
            this.grpBox1.Controls.Add(this.btnSync);
            this.grpBox1.Controls.Add(this.pBar);
            this.grpBox1.Controls.Add(this.grpBox1_1);
            this.grpBox1.Controls.Add(this.chkLogData);
            this.grpBox1.Controls.Add(this.label7);
            this.grpBox1.Location = new System.Drawing.Point(7, 36);
            this.grpBox1.Margin = new System.Windows.Forms.Padding(4);
            this.grpBox1.Name = "grpBox1";
            this.grpBox1.Padding = new System.Windows.Forms.Padding(4);
            this.grpBox1.Size = new System.Drawing.Size(866, 378);
            this.grpBox1.TabIndex = 4;
            this.grpBox1.TabStop = false;
            this.grpBox1.Text = "Syncronize";
            // 
            // chkDryRun
            // 
            this.chkDryRun.AutoSize = true;
            this.chkDryRun.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkDryRun.ForeColor = System.Drawing.Color.DarkOrange;
            this.chkDryRun.Location = new System.Drawing.Point(170, 280);
            this.chkDryRun.Margin = new System.Windows.Forms.Padding(4);
            this.chkDryRun.Name = "chkDryRun";
            this.chkDryRun.Size = new System.Drawing.Size(134, 21);
            this.chkDryRun.TabIndex = 10;
            this.chkDryRun.Text = "Dry-Run Mode";
            this.chkDryRun.UseVisualStyleBackColor = true;
            // 
            // btnSync
            // 
            this.btnSync.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSync.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSync.Location = new System.Drawing.Point(740, 313);
            this.btnSync.Margin = new System.Windows.Forms.Padding(4);
            this.btnSync.Name = "btnSync";
            this.btnSync.Size = new System.Drawing.Size(118, 35);
            this.btnSync.TabIndex = 5;
            this.btnSync.Text = "Synchronize";
            this.btnSync.UseVisualStyleBackColor = true;
            // 
            // pBar
            // 
            this.pBar.Location = new System.Drawing.Point(11, 316);
            this.pBar.Margin = new System.Windows.Forms.Padding(4);
            this.pBar.Name = "pBar";
            this.pBar.Size = new System.Drawing.Size(720, 29);
            this.pBar.TabIndex = 5;
            // 
            // grpBox1_1
            // 
            this.grpBox1_1.Controls.Add(this.chkSL);
            this.grpBox1_1.Controls.Add(this.label6);
            this.grpBox1_1.Controls.Add(this.chkPO);
            this.grpBox1_1.Controls.Add(this.labelPO);
            this.grpBox1_1.Controls.Add(this.chkSO);
            this.grpBox1_1.Controls.Add(this.label3);
            this.grpBox1_1.Location = new System.Drawing.Point(8, 20);
            this.grpBox1_1.Margin = new System.Windows.Forms.Padding(4);
            this.grpBox1_1.Name = "grpBox1_1";
            this.grpBox1_1.Padding = new System.Windows.Forms.Padding(4);
            this.grpBox1_1.Size = new System.Drawing.Size(724, 256);
            this.grpBox1_1.TabIndex = 4;
            this.grpBox1_1.TabStop = false;
            this.grpBox1_1.Text = "Other to SAP B1";
            // 
            // chkSO
            // 
            this.chkSO.AutoSize = true;
            this.chkSO.Location = new System.Drawing.Point(8, 20);
            this.chkSO.Margin = new System.Windows.Forms.Padding(4);
            this.chkSO.Name = "chkSO";
            this.chkSO.Size = new System.Drawing.Size(134, 20);
            this.chkSO.TabIndex = 14;
            this.chkSO.Text = "Sync Sales Order";
            this.chkSO.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(328, 20);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(59, 17);
            this.label3.TabIndex = 15;
            this.label3.Text = "-ORDR";
            // 
            // chkPO
            // 
            this.chkPO.AutoSize = true;
            this.chkPO.Location = new System.Drawing.Point(8, 48);
            this.chkPO.Margin = new System.Windows.Forms.Padding(4);
            this.chkPO.Name = "chkPO";
            this.chkPO.Size = new System.Drawing.Size(155, 20);
            this.chkPO.TabIndex = 16;
            this.chkPO.Text = "Sync Purchase Order";
            this.chkPO.UseVisualStyleBackColor = true;
            // 
            // labelPO
            // 
            this.labelPO.AutoSize = true;
            this.labelPO.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPO.Location = new System.Drawing.Point(328, 48);
            this.labelPO.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelPO.Name = "labelPO";
            this.labelPO.Size = new System.Drawing.Size(59, 17);
            this.labelPO.TabIndex = 17;
            this.labelPO.Text = "-OPOR";
            // 
            // chkSL
            // 
            this.chkSL.AutoSize = true;
            this.chkSL.Location = new System.Drawing.Point(8, 76);
            this.chkSL.Margin = new System.Windows.Forms.Padding(4);
            this.chkSL.Name = "chkSL";
            this.chkSL.Size = new System.Drawing.Size(145, 20);
            this.chkSL.TabIndex = 18;
            this.chkSL.Text = "Sync Service Layer";
            this.chkSL.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(328, 76);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(45, 17);
            this.label6.TabIndex = 19;
            this.label6.Text = "-OSL";
            // 
            // chkLogData
            // 
            this.chkLogData.AutoSize = true;
            this.chkLogData.Location = new System.Drawing.Point(15, 280);
            this.chkLogData.Margin = new System.Windows.Forms.Padding(4);
            this.chkLogData.Name = "chkLogData";
            this.chkLogData.Size = new System.Drawing.Size(119, 20);
            this.chkLogData.TabIndex = 8;
            this.chkLogData.Text = "Clear Log Data";
            this.chkLogData.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(335, 280);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(68, 17);
            this.label7.TabIndex = 9;
            this.label7.Text = "-CLOGD";
            // 
            // chkAll1
            // 
            this.chkAll1.AutoSize = true;
            this.chkAll1.Location = new System.Drawing.Point(7, 10);
            this.chkAll1.Margin = new System.Windows.Forms.Padding(4);
            this.chkAll1.Name = "chkAll1";
            this.chkAll1.Size = new System.Drawing.Size(154, 20);
            this.chkAll1.TabIndex = 3;
            this.chkAll1.Text = "Check All Syncronize";
            this.chkAll1.UseVisualStyleBackColor = true;
            this.chkAll1.Visible = false;
            // 
            // tabPending
            // 
            this.tabPending.Controls.Add(this.chkSelectAllPending);
            this.tabPending.Controls.Add(this.lblPendingFilterInfo);
            this.tabPending.Controls.Add(this.txtSearchPending);
            this.tabPending.Controls.Add(this.btnRefreshPending);
            this.tabPending.Controls.Add(this.btnSyncSelected);
            this.tabPending.Controls.Add(this.dgvPendingQueue);
            this.tabPending.Location = new System.Drawing.Point(4, 25);
            this.tabPending.Name = "tabPending";
            this.tabPending.Padding = new System.Windows.Forms.Padding(3);
            this.tabPending.Size = new System.Drawing.Size(893, 445);
            this.tabPending.TabIndex = 1;
            this.tabPending.Text = "Antrean Sync (Pending / Gagal)";
            this.tabPending.UseVisualStyleBackColor = true;
            // 
            // chkSelectAllPending
            // 
            this.chkSelectAllPending.AutoSize = true;
            this.chkSelectAllPending.Checked = true;
            this.chkSelectAllPending.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSelectAllPending.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkSelectAllPending.Location = new System.Drawing.Point(20, 18);
            this.chkSelectAllPending.Name = "chkSelectAllPending";
            this.chkSelectAllPending.Size = new System.Drawing.Size(108, 21);
            this.chkSelectAllPending.TabIndex = 30;
            this.chkSelectAllPending.Text = "Select All";
            this.chkSelectAllPending.UseVisualStyleBackColor = true;
            // 
            // lblPendingFilterInfo
            // 
            this.lblPendingFilterInfo.AutoSize = true;
            this.lblPendingFilterInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPendingFilterInfo.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblPendingFilterInfo.Location = new System.Drawing.Point(145, 19);
            this.lblPendingFilterInfo.Name = "lblPendingFilterInfo";
            this.lblPendingFilterInfo.Size = new System.Drawing.Size(400, 17);
            this.lblPendingFilterInfo.TabIndex = 31;
            this.lblPendingFilterInfo.Text = "Filter modul aktif: -";
            // 
            // txtSearchPending
            // 
            this.txtSearchPending.Location = new System.Drawing.Point(20, 48);
            this.txtSearchPending.Name = "txtSearchPending";
            this.txtSearchPending.Size = new System.Drawing.Size(236, 22);
            this.txtSearchPending.TabIndex = 32;
            // 
            // btnRefreshPending
            // 
            this.btnRefreshPending.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRefreshPending.Location = new System.Drawing.Point(263, 47);
            this.btnRefreshPending.Name = "btnRefreshPending";
            this.btnRefreshPending.Size = new System.Drawing.Size(150, 25);
            this.btnRefreshPending.TabIndex = 33;
            this.btnRefreshPending.Text = "Refresh Antrean";
            this.btnRefreshPending.UseVisualStyleBackColor = true;
            // 
            // btnSyncSelected
            // 
            this.btnSyncSelected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSyncSelected.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSyncSelected.ForeColor = System.Drawing.Color.DarkGreen;
            this.btnSyncSelected.Location = new System.Drawing.Point(660, 46);
            this.btnSyncSelected.Name = "btnSyncSelected";
            this.btnSyncSelected.Size = new System.Drawing.Size(187, 26);
            this.btnSyncSelected.TabIndex = 34;
            this.btnSyncSelected.Text = "Sync Selected";
            this.btnSyncSelected.UseVisualStyleBackColor = true;
            // 
            // dgvPendingQueue
            // 
            this.dgvPendingQueue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvPendingQueue.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPendingQueue.Location = new System.Drawing.Point(20, 77);
            this.dgvPendingQueue.Name = "dgvPendingQueue";
            this.dgvPendingQueue.RowHeadersWidth = 51;
            this.dgvPendingQueue.Size = new System.Drawing.Size(827, 331);
            this.dgvPendingQueue.TabIndex = 35;
            // 
            // tablog
            // 
            this.tablog.Controls.Add(this.txtSearchLog);
            this.tablog.Controls.Add(this.btnExportExcel);
            this.tablog.Controls.Add(this.cbFunction);
            this.tablog.Controls.Add(this.btnViewLog);
            this.tablog.Controls.Add(this.btnRetryFailed);
            this.tablog.Controls.Add(this.cbLogLevel);
            this.tablog.Controls.Add(this.dtpStgTo);
            this.tablog.Controls.Add(this.lblStgTo);
            this.tablog.Controls.Add(this.dtpStgFrom);
            this.tablog.Controls.Add(this.lblStgFrom);
            this.tablog.Controls.Add(this.dgvlLogData);
            this.tablog.Location = new System.Drawing.Point(4, 25);
            this.tablog.Name = "tablog";
            this.tablog.Padding = new System.Windows.Forms.Padding(3);
            this.tablog.Size = new System.Drawing.Size(893, 445);
            this.tablog.TabIndex = 2;
            this.tablog.Text = "Log History";
            this.tablog.UseVisualStyleBackColor = true;
            // 
            // txtSearchLog
            // 
            this.txtSearchLog.Location = new System.Drawing.Point(20, 48);
            this.txtSearchLog.Name = "txtSearchLog";
            this.txtSearchLog.Size = new System.Drawing.Size(236, 22);
            this.txtSearchLog.TabIndex = 28;
            // 
            // btnExportExcel
            // 
            this.btnExportExcel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExportExcel.ForeColor = System.Drawing.Color.DarkGreen;
            this.btnExportExcel.Location = new System.Drawing.Point(263, 47);
            this.btnExportExcel.Name = "btnExportExcel";
            this.btnExportExcel.Size = new System.Drawing.Size(150, 25);
            this.btnExportExcel.TabIndex = 29;
            this.btnExportExcel.Text = "Export to Excel";
            this.btnExportExcel.UseVisualStyleBackColor = true;
            // 
            // cbFunction
            // 
            this.cbFunction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbFunction.FormattingEnabled = true;
            this.cbFunction.Location = new System.Drawing.Point(659, 16);
            this.cbFunction.Margin = new System.Windows.Forms.Padding(4);
            this.cbFunction.Name = "cbFunction";
            this.cbFunction.Size = new System.Drawing.Size(188, 24);
            this.cbFunction.TabIndex = 27;
            // 
            // btnViewLog
            // 
            this.btnViewLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnViewLog.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnViewLog.Location = new System.Drawing.Point(464, 48);
            this.btnViewLog.Margin = new System.Windows.Forms.Padding(4);
            this.btnViewLog.Name = "btnViewLog";
            this.btnViewLog.Size = new System.Drawing.Size(187, 25);
            this.btnViewLog.TabIndex = 22;
            this.btnViewLog.Text = "View Filter";
            this.btnViewLog.UseVisualStyleBackColor = true;
            // 
            // btnRetryFailed
            // 
            this.btnRetryFailed.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRetryFailed.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRetryFailed.ForeColor = System.Drawing.Color.DarkRed;
            this.btnRetryFailed.Location = new System.Drawing.Point(660, 46);
            this.btnRetryFailed.Margin = new System.Windows.Forms.Padding(4);
            this.btnRetryFailed.Name = "btnRetryFailed";
            this.btnRetryFailed.Size = new System.Drawing.Size(187, 25);
            this.btnRetryFailed.TabIndex = 25;
            this.btnRetryFailed.Text = "Retry Failed Sync";
            this.btnRetryFailed.UseVisualStyleBackColor = true;
            // 
            // cbLogLevel
            // 
            this.cbLogLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbLogLevel.FormattingEnabled = true;
            this.cbLogLevel.Location = new System.Drawing.Point(501, 16);
            this.cbLogLevel.Margin = new System.Windows.Forms.Padding(4);
            this.cbLogLevel.Name = "cbLogLevel";
            this.cbLogLevel.Size = new System.Drawing.Size(150, 24);
            this.cbLogLevel.TabIndex = 21;
            // 
            // dtpStgTo
            // 
            this.dtpStgTo.CustomFormat = "dd MMMM yyyy";
            this.dtpStgTo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpStgTo.Location = new System.Drawing.Point(331, 16);
            this.dtpStgTo.Margin = new System.Windows.Forms.Padding(4);
            this.dtpStgTo.Name = "dtpStgTo";
            this.dtpStgTo.Size = new System.Drawing.Size(162, 22);
            this.dtpStgTo.TabIndex = 20;
            // 
            // lblStgTo
            // 
            this.lblStgTo.AutoSize = true;
            this.lblStgTo.Location = new System.Drawing.Point(291, 18);
            this.lblStgTo.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStgTo.Name = "lblStgTo";
            this.lblStgTo.Padding = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.lblStgTo.Size = new System.Drawing.Size(32, 20);
            this.lblStgTo.TabIndex = 19;
            this.lblStgTo.Text = "To";
            // 
            // dtpStgFrom
            // 
            this.dtpStgFrom.CustomFormat = "dd MMMM yyyy";
            this.dtpStgFrom.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpStgFrom.Location = new System.Drawing.Point(121, 16);
            this.dtpStgFrom.Margin = new System.Windows.Forms.Padding(4);
            this.dtpStgFrom.Name = "dtpStgFrom";
            this.dtpStgFrom.Size = new System.Drawing.Size(162, 22);
            this.dtpStgFrom.TabIndex = 18;
            // 
            // lblStgFrom
            // 
            this.lblStgFrom.AutoSize = true;
            this.lblStgFrom.Location = new System.Drawing.Point(16, 18);
            this.lblStgFrom.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStgFrom.Name = "lblStgFrom";
            this.lblStgFrom.Padding = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.lblStgFrom.Size = new System.Drawing.Size(98, 20);
            this.lblStgFrom.TabIndex = 17;
            this.lblStgFrom.Text = "Staging Order";
            // 
            // dgvlLogData
            // 
            this.dgvlLogData.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvlLogData.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            this.dgvlLogData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle8.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle8.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvlLogData.DefaultCellStyle = dataGridViewCellStyle8;
            this.dgvlLogData.Location = new System.Drawing.Point(20, 77);
            this.dgvlLogData.Margin = new System.Windows.Forms.Padding(4);
            this.dgvlLogData.Name = "dgvlLogData";
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle9.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle9.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle9.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle9.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle9.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvlLogData.RowHeadersDefaultCellStyle = dataGridViewCellStyle9;
            this.dgvlLogData.RowHeadersWidth = 51;
            this.dgvlLogData.Size = new System.Drawing.Size(827, 331);
            this.dgvlLogData.TabIndex = 16;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(20, 40);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(85, 16);
            this.label8.TabIndex = 4;
            this.label8.Text = "Profile Active";
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(936, 595);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FormMain";
            this.Text = "Sync";
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabsync.ResumeLayout(false);
            this.tabsync.PerformLayout();
            this.grpBox1.ResumeLayout(false);
            this.grpBox1.PerformLayout();
            this.grpBox1_1.ResumeLayout(false);
            this.grpBox1_1.PerformLayout();
            this.tabPending.ResumeLayout(false);
            this.tabPending.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPendingQueue)).EndInit();
            this.tablog.ResumeLayout(false);
            this.tablog.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvlLogData)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem configurationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem passwordToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportProfilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importProfilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem schedulerToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabsync;
        private System.Windows.Forms.TabPage tabPending;
        private System.Windows.Forms.TabPage tablog;

        internal System.Windows.Forms.CheckBox chkSelectAllPending;
        private System.Windows.Forms.Label lblPendingFilterInfo;
        internal System.Windows.Forms.TextBox txtSearchPending;
        internal System.Windows.Forms.Button btnRefreshPending;
        internal System.Windows.Forms.Button btnSyncSelected;
        internal System.Windows.Forms.DataGridView dgvPendingQueue;

        internal System.Windows.Forms.TextBox txtSearchLog;
        internal System.Windows.Forms.Button btnExportExcel;
        internal System.Windows.Forms.ComboBox cbFunction;

        internal System.Windows.Forms.Button btnViewLog;
        internal System.Windows.Forms.Button btnRetryFailed;
        internal System.Windows.Forms.ComboBox cbLogLevel;
        private System.Windows.Forms.DateTimePicker dtpStgTo;
        private System.Windows.Forms.Label lblStgTo;
        private System.Windows.Forms.DateTimePicker dtpStgFrom;
        private System.Windows.Forms.Label lblStgFrom;
        internal System.Windows.Forms.DataGridView dgvlLogData;
        internal System.Windows.Forms.GroupBox grpBox1;
        internal System.Windows.Forms.CheckBox chkDryRun;
        internal System.Windows.Forms.Button btnSync;
        private System.Windows.Forms.ProgressBar pBar;
        internal System.Windows.Forms.GroupBox grpBox1_1;
        private System.Windows.Forms.CheckBox chkSL;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox chkSO;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox chkPO;
        private System.Windows.Forms.Label labelPO;
        private System.Windows.Forms.CheckBox chkLogData;
        private System.Windows.Forms.Label label7;
        internal System.Windows.Forms.CheckBox chkAll1;
        private System.Windows.Forms.Label label8;
    }
}