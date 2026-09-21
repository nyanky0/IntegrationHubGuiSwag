using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SOLTIUS_Scheduler_Add_On.Model;
using SOLTIUS_Scheduler_Add_On.Services;

namespace SOLTIUS_Scheduler_Add_On.UI
{
    public partial class FormDocumentDetail : Form
    {
        private readonly DocumentHeaderLogModel _header;
        private readonly DatabaseService _dbService;

        public FormDocumentDetail(DocumentHeaderLogModel header, DatabaseService dbService)
        {
            InitializeComponent();

            _header = header ?? throw new ArgumentNullException(nameof(header));
            _dbService = dbService;

            UITheme.ApplyForm(this);
            UITheme.ApplyGrid(dgvLines);
            UITheme.ApplySecondary(btnClose);

            // Terapkan styling teks jelas pada semua input detail dokumen
            foreach (Control c in grpHeaderInfo.Controls)
            {
                if (c is TextBox tb)
                {
                    tb.BackColor = Color.White;
                    tb.ForeColor = UITheme.Text;
                }
            }
            txtUdfInfo.BackColor = Color.White;
            txtUdfInfo.ForeColor = UITheme.Text;

            this.Load += FormDocumentDetail_Load;
            this.btnClose.Click += (s, e) => this.Close();
        }

        private void FormDocumentDetail_Load(object sender, EventArgs e)
        {
            BindHeaderInfo();
            LoadLineItems();
        }

        private void BindHeaderInfo()
        {
            txtDocType.Text = _header.DocType;
            txtWebTxNum.Text = string.IsNullOrEmpty(_header.WebTxNumber) ? "-" : _header.WebTxNumber;
            txtCardCode.Text = _header.CardCode;
            txtCardName.Text = _header.CardName;
            txtDocDate.Text = _header.DocDate == DateTime.MinValue ? "-" : _header.DocDate.ToString("yyyy-MM-dd");
            txtDocDueDate.Text = _header.DocDueDate == DateTime.MinValue ? "-" : _header.DocDueDate.ToString("yyyy-MM-dd");
            txtStatus.Text = _header.Status;
            txtDocEntry.Text = string.IsNullOrEmpty(_header.DocEntry) ? "-" : _header.DocEntry;
            txtProcessedAt.Text = _header.ProcessedAt.HasValue ? _header.ProcessedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "-";
            txtRemarks.Text = string.IsNullOrEmpty(_header.Remarks) ? "-" : _header.Remarks;
            txtErrorMessage.Text = string.IsNullOrEmpty(_header.ErrorMessage) ? "-" : _header.ErrorMessage;

            // Status color highlight
            if (_header.Status == "Success")
            {
                txtStatus.ForeColor = Color.DarkGreen;
            }
            else if (_header.Status == "Failed" || _header.Status == "Dead-Letter")
            {
                txtStatus.ForeColor = Color.DarkRed;
            }
            else
            {
                txtStatus.ForeColor = Color.DarkOrange;
            }

            // Parse & Format UDF JSON
            FormatUdfJson(_header.UdfDataJson);
        }

        private void FormatUdfJson(string udfJson)
        {
            if (string.IsNullOrWhiteSpace(udfJson))
            {
                txtUdfInfo.Text = "Tidak ada User Defined Fields (UDF).";
                return;
            }

            try
            {
                var parsed = JToken.Parse(udfJson);
                txtUdfInfo.Text = parsed.ToString(Formatting.Indented);
            }
            catch
            {
                txtUdfInfo.Text = udfJson;
            }
        }

        private void LoadLineItems()
        {
            List<DocumentDetailLineModel> lines = new List<DocumentDetailLineModel>();

            if (_dbService != null)
            {
                if (_header.DocType == "Purchase Order")
                {
                    lines = _dbService.LoadPurchaseOrderLineDetails(_header.HeaderId);
                }
                else if (_header.DocType == "Goods Receipt PO")
                {
                    lines = _dbService.LoadGoodsReceiptPOLineDetails(_header.HeaderId);
                }
                else if (_header.DocType == "Stock Transfer")
                {
                    lines = _dbService.LoadStockTransferLineDetails(_header.HeaderId);
                }
            }

            SetupGridColumns();
            dgvLines.DataSource = lines;
            lblTotalLines.Text = $"Total Items: {lines.Count}";
        }

        private void SetupGridColumns()
        {
            dgvLines.AutoGenerateColumns = false;
            dgvLines.Columns.Clear();

            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LineNum",
                HeaderText = "No",
                Width = 45,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ItemCode",
                HeaderText = "Item Code",
                Width = 140
            });

            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ItemName",
                HeaderText = "Item Description",
                Width = 240
            });

            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Warehouse",
                HeaderText = "Whs",
                Width = 60,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Quantity",
                HeaderText = "Qty",
                Width = 75,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" }
            });

            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Price",
                HeaderText = "Price",
                Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" }
            });

            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LineTotal",
                HeaderText = "Total",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" }
            });

            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "VatGroup",
                HeaderText = "Tax Code",
                Width = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Status",
                HeaderText = "Status",
                Width = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            dgvLines.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ErrorMessage",
                HeaderText = "Line Error",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
        }
    }
}
