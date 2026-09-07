namespace SOLTIUS_Scheduler_Add_On.UI
{
    partial class FormDocumentDetail
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
            this.pnlTop = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubTitle = new System.Windows.Forms.Label();
            this.grpHeaderInfo = new System.Windows.Forms.GroupBox();
            this.lblDocType = new System.Windows.Forms.Label();
            this.lblWebTxNum = new System.Windows.Forms.Label();
            this.lblDocEntry = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblCardCode = new System.Windows.Forms.Label();
            this.lblCardName = new System.Windows.Forms.Label();
            this.lblDocDate = new System.Windows.Forms.Label();
            this.lblDocDueDate = new System.Windows.Forms.Label();
            this.lblProcessedAt = new System.Windows.Forms.Label();
            this.lblRemarks = new System.Windows.Forms.Label();
            this.lblErrorMessage = new System.Windows.Forms.Label();
            this.txtDocType = new System.Windows.Forms.TextBox();
            this.txtWebTxNum = new System.Windows.Forms.TextBox();
            this.txtDocEntry = new System.Windows.Forms.TextBox();
            this.txtStatus = new System.Windows.Forms.TextBox();
            this.txtCardCode = new System.Windows.Forms.TextBox();
            this.txtCardName = new System.Windows.Forms.TextBox();
            this.txtDocDate = new System.Windows.Forms.TextBox();
            this.txtDocDueDate = new System.Windows.Forms.TextBox();
            this.txtProcessedAt = new System.Windows.Forms.TextBox();
            this.txtRemarks = new System.Windows.Forms.TextBox();
            this.txtErrorMessage = new System.Windows.Forms.TextBox();
            this.grpUdfInfo = new System.Windows.Forms.GroupBox();
            this.txtUdfInfo = new System.Windows.Forms.TextBox();
            this.grpLines = new System.Windows.Forms.GroupBox();
            this.dgvLines = new System.Windows.Forms.DataGridView();
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.btnClose = new System.Windows.Forms.Button();
            this.lblTotalLines = new System.Windows.Forms.Label();
            this.pnlTop.SuspendLayout();
            this.grpHeaderInfo.SuspendLayout();
            this.grpUdfInfo.SuspendLayout();
            this.grpLines.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLines)).BeginInit();
            this.pnlBottom.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlTop
            // 
            this.pnlTop.BackColor = System.Drawing.Color.White;
            this.pnlTop.Controls.Add(this.lblTitle);
            this.pnlTop.Controls.Add(this.lblSubTitle);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(0, 0);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(984, 60);
            this.pnlTop.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(104)))), ((int)(((byte)(168)))));
            this.lblTitle.Location = new System.Drawing.Point(16, 10);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(183, 21);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Detail Dokumen Staging";
            // 
            // lblSubTitle
            // 
            this.lblSubTitle.AutoSize = true;
            this.lblSubTitle.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSubTitle.ForeColor = System.Drawing.Color.Gray;
            this.lblSubTitle.Location = new System.Drawing.Point(17, 34);
            this.lblSubTitle.Name = "lblSubTitle";
            this.lblSubTitle.Size = new System.Drawing.Size(374, 13);
            this.lblSubTitle.TabIndex = 1;
            this.lblSubTitle.Text = "Informasi lengkap header transaksi, UDF, status sinkronisasi, dan line items.";
            // 
            // grpHeaderInfo
            // 
            this.grpHeaderInfo.Controls.Add(this.lblDocType);
            this.grpHeaderInfo.Controls.Add(this.txtDocType);
            this.grpHeaderInfo.Controls.Add(this.lblWebTxNum);
            this.grpHeaderInfo.Controls.Add(this.txtWebTxNum);
            this.grpHeaderInfo.Controls.Add(this.lblDocEntry);
            this.grpHeaderInfo.Controls.Add(this.txtDocEntry);
            this.grpHeaderInfo.Controls.Add(this.lblStatus);
            this.grpHeaderInfo.Controls.Add(this.txtStatus);
            this.grpHeaderInfo.Controls.Add(this.lblCardCode);
            this.grpHeaderInfo.Controls.Add(this.txtCardCode);
            this.grpHeaderInfo.Controls.Add(this.lblCardName);
            this.grpHeaderInfo.Controls.Add(this.txtCardName);
            this.grpHeaderInfo.Controls.Add(this.lblDocDate);
            this.grpHeaderInfo.Controls.Add(this.txtDocDate);
            this.grpHeaderInfo.Controls.Add(this.lblDocDueDate);
            this.grpHeaderInfo.Controls.Add(this.txtDocDueDate);
            this.grpHeaderInfo.Controls.Add(this.lblProcessedAt);
            this.grpHeaderInfo.Controls.Add(this.txtProcessedAt);
            this.grpHeaderInfo.Controls.Add(this.lblRemarks);
            this.grpHeaderInfo.Controls.Add(this.txtRemarks);
            this.grpHeaderInfo.Controls.Add(this.lblErrorMessage);
            this.grpHeaderInfo.Controls.Add(this.txtErrorMessage);
            this.grpHeaderInfo.Location = new System.Drawing.Point(12, 68);
            this.grpHeaderInfo.Name = "grpHeaderInfo";
            this.grpHeaderInfo.Size = new System.Drawing.Size(630, 230);
            this.grpHeaderInfo.TabIndex = 1;
            this.grpHeaderInfo.TabStop = false;
            this.grpHeaderInfo.Text = "Informasi Header";
            // 
            // lblDocType
            // 
            this.lblDocType.Location = new System.Drawing.Point(12, 24);
            this.lblDocType.Name = "lblDocType";
            this.lblDocType.Size = new System.Drawing.Size(85, 20);
            this.lblDocType.TabIndex = 0;
            this.lblDocType.Text = "Doc Type:";
            // 
            // txtDocType
            // 
            this.txtDocType.Location = new System.Drawing.Point(100, 21);
            this.txtDocType.Name = "txtDocType";
            this.txtDocType.ReadOnly = true;
            this.txtDocType.Size = new System.Drawing.Size(190, 22);
            this.txtDocType.TabIndex = 1;
            // 
            // lblWebTxNum
            // 
            this.lblWebTxNum.Location = new System.Drawing.Point(310, 24);
            this.lblWebTxNum.Name = "lblWebTxNum";
            this.lblWebTxNum.Size = new System.Drawing.Size(85, 20);
            this.lblWebTxNum.TabIndex = 2;
            this.lblWebTxNum.Text = "Web Tx Num:";
            // 
            // txtWebTxNum
            // 
            this.txtWebTxNum.Location = new System.Drawing.Point(400, 21);
            this.txtWebTxNum.Name = "txtWebTxNum";
            this.txtWebTxNum.ReadOnly = true;
            this.txtWebTxNum.Size = new System.Drawing.Size(215, 22);
            this.txtWebTxNum.TabIndex = 3;
            // 
            // lblCardCode
            // 
            this.lblCardCode.Location = new System.Drawing.Point(12, 52);
            this.lblCardCode.Name = "lblCardCode";
            this.lblCardCode.Size = new System.Drawing.Size(85, 20);
            this.lblCardCode.TabIndex = 4;
            this.lblCardCode.Text = "BP Code:";
            // 
            // txtCardCode
            // 
            this.txtCardCode.Location = new System.Drawing.Point(100, 49);
            this.txtCardCode.Name = "txtCardCode";
            this.txtCardCode.ReadOnly = true;
            this.txtCardCode.Size = new System.Drawing.Size(190, 22);
            this.txtCardCode.TabIndex = 5;
            // 
            // lblCardName
            // 
            this.lblCardName.Location = new System.Drawing.Point(310, 52);
            this.lblCardName.Name = "lblCardName";
            this.lblCardName.Size = new System.Drawing.Size(85, 20);
            this.lblCardName.TabIndex = 6;
            this.lblCardName.Text = "BP Name:";
            // 
            // txtCardName
            // 
            this.txtCardName.Location = new System.Drawing.Point(400, 49);
            this.txtCardName.Name = "txtCardName";
            this.txtCardName.ReadOnly = true;
            this.txtCardName.Size = new System.Drawing.Size(215, 22);
            this.txtCardName.TabIndex = 7;
            // 
            // lblDocDate
            // 
            this.lblDocDate.Location = new System.Drawing.Point(12, 80);
            this.lblDocDate.Name = "lblDocDate";
            this.lblDocDate.Size = new System.Drawing.Size(85, 20);
            this.lblDocDate.TabIndex = 8;
            this.lblDocDate.Text = "Doc Date:";
            // 
            // txtDocDate
            // 
            this.txtDocDate.Location = new System.Drawing.Point(100, 77);
            this.txtDocDate.Name = "txtDocDate";
            this.txtDocDate.ReadOnly = true;
            this.txtDocDate.Size = new System.Drawing.Size(190, 22);
            this.txtDocDate.TabIndex = 9;
            // 
            // lblDocDueDate
            // 
            this.lblDocDueDate.Location = new System.Drawing.Point(310, 80);
            this.lblDocDueDate.Name = "lblDocDueDate";
            this.lblDocDueDate.Size = new System.Drawing.Size(85, 20);
            this.lblDocDueDate.TabIndex = 10;
            this.lblDocDueDate.Text = "Due Date:";
            // 
            // txtDocDueDate
            // 
            this.txtDocDueDate.Location = new System.Drawing.Point(400, 77);
            this.txtDocDueDate.Name = "txtDocDueDate";
            this.txtDocDueDate.ReadOnly = true;
            this.txtDocDueDate.Size = new System.Drawing.Size(215, 22);
            this.txtDocDueDate.TabIndex = 11;
            // 
            // lblStatus
            // 
            this.lblStatus.Location = new System.Drawing.Point(12, 108);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(85, 20);
            this.lblStatus.TabIndex = 12;
            this.lblStatus.Text = "Sync Status:";
            // 
            // txtStatus
            // 
            this.txtStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.txtStatus.Location = new System.Drawing.Point(100, 105);
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.ReadOnly = true;
            this.txtStatus.Size = new System.Drawing.Size(190, 23);
            this.txtStatus.TabIndex = 13;
            // 
            // lblDocEntry
            // 
            this.lblDocEntry.Location = new System.Drawing.Point(310, 108);
            this.lblDocEntry.Name = "lblDocEntry";
            this.lblDocEntry.Size = new System.Drawing.Size(85, 20);
            this.lblDocEntry.TabIndex = 14;
            this.lblDocEntry.Text = "DocEntry SAP:";
            // 
            // txtDocEntry
            // 
            this.txtDocEntry.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.txtDocEntry.Location = new System.Drawing.Point(400, 105);
            this.txtDocEntry.Name = "txtDocEntry";
            this.txtDocEntry.ReadOnly = true;
            this.txtDocEntry.Size = new System.Drawing.Size(215, 23);
            this.txtDocEntry.TabIndex = 15;
            // 
            // lblProcessedAt
            // 
            this.lblProcessedAt.Location = new System.Drawing.Point(12, 136);
            this.lblProcessedAt.Name = "lblProcessedAt";
            this.lblProcessedAt.Size = new System.Drawing.Size(85, 20);
            this.lblProcessedAt.TabIndex = 16;
            this.lblProcessedAt.Text = "Processed At:";
            // 
            // txtProcessedAt
            // 
            this.txtProcessedAt.Location = new System.Drawing.Point(100, 133);
            this.txtProcessedAt.Name = "txtProcessedAt";
            this.txtProcessedAt.ReadOnly = true;
            this.txtProcessedAt.Size = new System.Drawing.Size(190, 22);
            this.txtProcessedAt.TabIndex = 17;
            // 
            // lblRemarks
            // 
            this.lblRemarks.Location = new System.Drawing.Point(310, 136);
            this.lblRemarks.Name = "lblRemarks";
            this.lblRemarks.Size = new System.Drawing.Size(85, 20);
            this.lblRemarks.TabIndex = 18;
            this.lblRemarks.Text = "Remarks:";
            // 
            // txtRemarks
            // 
            this.txtRemarks.Location = new System.Drawing.Point(400, 133);
            this.txtRemarks.Name = "txtRemarks";
            this.txtRemarks.ReadOnly = true;
            this.txtRemarks.Size = new System.Drawing.Size(215, 22);
            this.txtRemarks.TabIndex = 19;
            // 
            // lblErrorMessage
            // 
            this.lblErrorMessage.Location = new System.Drawing.Point(12, 164);
            this.lblErrorMessage.Name = "lblErrorMessage";
            this.lblErrorMessage.Size = new System.Drawing.Size(85, 20);
            this.lblErrorMessage.TabIndex = 20;
            this.lblErrorMessage.Text = "Error Message:";
            // 
            // txtErrorMessage
            // 
            this.txtErrorMessage.ForeColor = System.Drawing.Color.DarkRed;
            this.txtErrorMessage.Location = new System.Drawing.Point(100, 161);
            this.txtErrorMessage.Multiline = true;
            this.txtErrorMessage.Name = "txtErrorMessage";
            this.txtErrorMessage.ReadOnly = true;
            this.txtErrorMessage.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtErrorMessage.Size = new System.Drawing.Size(515, 56);
            this.txtErrorMessage.TabIndex = 21;
            // 
            // grpUdfInfo
            // 
            this.grpUdfInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpUdfInfo.Controls.Add(this.txtUdfInfo);
            this.grpUdfInfo.Location = new System.Drawing.Point(648, 68);
            this.grpUdfInfo.Name = "grpUdfInfo";
            this.grpUdfInfo.Size = new System.Drawing.Size(324, 230);
            this.grpUdfInfo.TabIndex = 2;
            this.grpUdfInfo.TabStop = false;
            this.grpUdfInfo.Text = "Custom Fields / UDF (Kapal, Pengaju, dll)";
            // 
            // txtUdfInfo
            // 
            this.txtUdfInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtUdfInfo.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtUdfInfo.Location = new System.Drawing.Point(3, 18);
            this.txtUdfInfo.Multiline = true;
            this.txtUdfInfo.Name = "txtUdfInfo";
            this.txtUdfInfo.ReadOnly = true;
            this.txtUdfInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtUdfInfo.Size = new System.Drawing.Size(318, 209);
            this.txtUdfInfo.TabIndex = 0;
            // 
            // grpLines
            // 
            this.grpLines.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpLines.Controls.Add(this.dgvLines);
            this.grpLines.Location = new System.Drawing.Point(12, 304);
            this.grpLines.Name = "grpLines";
            this.grpLines.Size = new System.Drawing.Size(960, 246);
            this.grpLines.TabIndex = 3;
            this.grpLines.TabStop = false;
            this.grpLines.Text = "Line Items";
            // 
            // dgvLines
            // 
            this.dgvLines.AllowUserToAddRows = false;
            this.dgvLines.AllowUserToDeleteRows = false;
            this.dgvLines.BackgroundColor = System.Drawing.Color.White;
            this.dgvLines.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvLines.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvLines.Location = new System.Drawing.Point(3, 18);
            this.dgvLines.Name = "dgvLines";
            this.dgvLines.ReadOnly = true;
            this.dgvLines.RowHeadersVisible = false;
            this.dgvLines.RowTemplate.Height = 24;
            this.dgvLines.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvLines.Size = new System.Drawing.Size(954, 225);
            this.dgvLines.TabIndex = 0;
            // 
            // pnlBottom
            // 
            this.pnlBottom.Controls.Add(this.lblTotalLines);
            this.pnlBottom.Controls.Add(this.btnClose);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 556);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Size = new System.Drawing.Size(984, 50);
            this.pnlBottom.TabIndex = 4;
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Location = new System.Drawing.Point(862, 10);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(110, 30);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "Tutup";
            this.btnClose.UseVisualStyleBackColor = true;
            // 
            // lblTotalLines
            // 
            this.lblTotalLines.AutoSize = true;
            this.lblTotalLines.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblTotalLines.Location = new System.Drawing.Point(15, 18);
            this.lblTotalLines.Name = "lblTotalLines";
            this.lblTotalLines.Size = new System.Drawing.Size(78, 15);
            this.lblTotalLines.TabIndex = 1;
            this.lblTotalLines.Text = "Total Items: 0";
            // 
            // FormDocumentDetail
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 606);
            this.Controls.Add(this.pnlBottom);
            this.Controls.Add(this.grpLines);
            this.Controls.Add(this.grpUdfInfo);
            this.Controls.Add(this.grpHeaderInfo);
            this.Controls.Add(this.pnlTop);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(900, 550);
            this.Name = "FormDocumentDetail";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Detail Dokumen";
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.grpHeaderInfo.ResumeLayout(false);
            this.grpHeaderInfo.PerformLayout();
            this.grpUdfInfo.ResumeLayout(false);
            this.grpUdfInfo.PerformLayout();
            this.grpLines.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvLines)).EndInit();
            this.pnlBottom.ResumeLayout(false);
            this.pnlBottom.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubTitle;
        private System.Windows.Forms.GroupBox grpHeaderInfo;
        private System.Windows.Forms.Label lblDocType;
        private System.Windows.Forms.Label lblWebTxNum;
        private System.Windows.Forms.Label lblDocEntry;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblCardCode;
        private System.Windows.Forms.Label lblCardName;
        private System.Windows.Forms.Label lblDocDate;
        private System.Windows.Forms.Label lblDocDueDate;
        private System.Windows.Forms.Label lblProcessedAt;
        private System.Windows.Forms.Label lblRemarks;
        private System.Windows.Forms.Label lblErrorMessage;
        private System.Windows.Forms.TextBox txtDocType;
        private System.Windows.Forms.TextBox txtWebTxNum;
        private System.Windows.Forms.TextBox txtDocEntry;
        private System.Windows.Forms.TextBox txtStatus;
        private System.Windows.Forms.TextBox txtCardCode;
        private System.Windows.Forms.TextBox txtCardName;
        private System.Windows.Forms.TextBox txtDocDate;
        private System.Windows.Forms.TextBox txtDocDueDate;
        private System.Windows.Forms.TextBox txtProcessedAt;
        private System.Windows.Forms.TextBox txtRemarks;
        private System.Windows.Forms.TextBox txtErrorMessage;
        private System.Windows.Forms.GroupBox grpUdfInfo;
        private System.Windows.Forms.TextBox txtUdfInfo;
        private System.Windows.Forms.GroupBox grpLines;
        private System.Windows.Forms.DataGridView dgvLines;
        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblTotalLines;
    }
}
