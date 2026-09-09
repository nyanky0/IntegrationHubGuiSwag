using SOLTIUS_Scheduler_Add_On.Model;
using SOLTIUS_Scheduler_Add_On.Services;
using SOLTIUS_Scheduler_Add_On.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClosedXML.Excel;

namespace SOLTIUS_Scheduler_Add_On.UI
{
    public partial class FormMain : Form
    {
        private List<DocumentHeaderLogModel> masterLogList;
        private List<PendingQueueDocModel> masterPendingList;
        private readonly ConfigService _configService;

        // Fallback URL Web API (dipakai bila profil tidak memiliki 'Web API URL').
        // URL profil yang diisi lewat Configuration -> Manage Profile selalu diutamakan.
        private readonly string webEndpointUrl = "http://localhost:5006";

        public FormMain()
        {
            InitializeComponent();

            UITheme.ApplyForm(this); // StartPosition diproses saat CreateHandle — harus sebelum Show()

            _configService = new ConfigService();
            masterLogList = new List<DocumentHeaderLogModel>();
            masterPendingList = new List<PendingQueueDocModel>();

            SetupUI();
            WireEvents();
        }

        private void SetupUI()
        {
            cbLogLevel.Items.Clear();
            cbLogLevel.Items.AddRange(new string[] { "All", "Success", "Failed", "Pending", "Cancelled" });
            cbLogLevel.SelectedIndex = 0;

            cbFunction.Items.Clear();
            cbFunction.Items.AddRange(new string[] { "All", "Purchase Order", "Sales Order" });
            cbFunction.SelectedIndex = 0;

            SetupLogGridColumns();
            SetupPendingGridColumns();
            RefreshGrid();
            RefreshPendingGrid();
            RefreshDebugSummary();
            UpdateActiveProfileLabel();
        }

        private void SetupLogGridColumns()
        {
            dgvlLogData.AutoGenerateColumns = false;
            dgvlLogData.Columns.Clear();
            dgvlLogData.ReadOnly = true;
            dgvlLogData.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvlLogData.MultiSelect = false;

            dgvlLogData.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDocType",
                HeaderText = "Doc Type",
                DataPropertyName = "DocType",
                Width = 120
            });
            dgvlLogData.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colWebTxNumber",
                HeaderText = "Web Tx Number",
                DataPropertyName = "WebTxNumber",
                Width = 140
            });
            dgvlLogData.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCardCode",
                HeaderText = "BP Code",
                DataPropertyName = "CardCode",
                Width = 100
            });
            dgvlLogData.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCardName",
                HeaderText = "BP Name",
                DataPropertyName = "CardName",
                Width = 160
            });
            dgvlLogData.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDocDate",
                HeaderText = "Doc Date",
                DataPropertyName = "DocDate",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" },
                Width = 95
            });
            dgvlLogData.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDocDueDate",
                HeaderText = "Due Date",
                DataPropertyName = "DocDueDate",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" },
                Width = 95
            });
            dgvlLogData.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colStatus",
                HeaderText = "Status",
                DataPropertyName = "Status",
                Width = 90
            });
            dgvlLogData.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDocEntry",
                HeaderText = "DocEntry SAP",
                DataPropertyName = "DocEntry",
                Width = 100
            });
            dgvlLogData.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colProcessedAt",
                HeaderText = "Processed At",
                DataPropertyName = "ProcessedAt",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" },
                Width = 135
            });
            dgvlLogData.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colErrorMessage",
                HeaderText = "Error / Info",
                DataPropertyName = "ErrorMessage",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
        }

        private void SetupPendingGridColumns()
        {
            dgvPendingQueue.AutoGenerateColumns = false;
            dgvPendingQueue.Columns.Clear();
            dgvPendingQueue.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPendingQueue.MultiSelect = false;

            dgvPendingQueue.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "colSelectPending",
                HeaderText = "Pilih",
                DataPropertyName = "IsSelected",
                Width = 45,
                ReadOnly = false
            });
            dgvPendingQueue.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDocTypePending",
                HeaderText = "Doc Type",
                DataPropertyName = "DocType",
                Width = 110,
                ReadOnly = true
            });
            dgvPendingQueue.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colWebTxPending",
                HeaderText = "Web Tx Number",
                DataPropertyName = "WebTxNumber",
                Width = 140,
                ReadOnly = true
            });
            dgvPendingQueue.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCardCodePending",
                HeaderText = "BP Code",
                DataPropertyName = "CardCode",
                Width = 90,
                ReadOnly = true
            });
            dgvPendingQueue.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCardNamePending",
                HeaderText = "BP Name",
                DataPropertyName = "CardName",
                Width = 160,
                ReadOnly = true
            });
            dgvPendingQueue.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDocDatePending",
                HeaderText = "Doc Date",
                DataPropertyName = "DocDate",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" },
                Width = 90,
                ReadOnly = true
            });
            dgvPendingQueue.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDocDueDatePending",
                HeaderText = "Due Date",
                DataPropertyName = "DocDueDate",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" },
                Width = 90,
                ReadOnly = true
            });
            dgvPendingQueue.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colStatusPending",
                HeaderText = "Status",
                DataPropertyName = "Status",
                Width = 80,
                ReadOnly = true
            });
            dgvPendingQueue.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colErrorPending",
                HeaderText = "Keterangan / Error Terakhir",
                DataPropertyName = "ErrorMessage",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });
            dgvPendingQueue.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCreatedAtPending",
                HeaderText = "Created At",
                DataPropertyName = "CreatedAt",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" },
                Width = 130,
                ReadOnly = true
            });
        }

        private void WireEvents()
        {
            this.Load += (s, e) => UpdateActiveProfileLabel();
            this.Activated += (s, e) => UpdateActiveProfileLabel();
            this.btnSync.Click += btnSync_Click;
            this.btnRetryFailed.Click += btnRetryFailed_Click;
          // UBAH BARIS INI: Agar tombol View Filter menerapkan filter lokal (bukan load ulang dari awal)
            if (btnViewLog != null) this.btnViewLog.Click += (s, e) => RefreshGrid();

            // TAMBAHKAN BARIS INI: Agar saat tanggal diganti, grid otomatis refresh
            if (dtpStgFrom != null) this.dtpStgFrom.ValueChanged += (s, e) => RefreshGrid();
            if (dtpStgTo != null) this.dtpStgTo.ValueChanged += (s, e) => RefreshGrid();

            // ... event combobox
            if (cbLogLevel != null) this.cbLogLevel.SelectedIndexChanged += (s, e) => RefreshGrid();
            if (cbFunction != null) this.cbFunction.SelectedIndexChanged += (s, e) => RefreshGrid();

            // ... menu strip
            this.configurationToolStripMenuItem.Click += configurationToolStripMenuItem_Click;
            this.passwordToolStripMenuItem.Click += passwordToolStripMenuItem_Click;
            this.schedulerToolStripMenuItem.Click += schedulerToolStripMenuItem_Click;
            this.label8.Click += (s, e) => configurationToolStripMenuItem_Click(null, null);
            if (this.lblStagingDb != null)
            {
                this.lblStagingDb.Cursor = Cursors.Hand;
                this.lblStagingDb.Click += (s, e) => configurationToolStripMenuItem_Click(null, null);
            }

            this.tabControl1.SelectedIndexChanged += tabControl1_SelectedIndexChanged;

            if (txtSearchLog != null) this.txtSearchLog.TextChanged += (s, e) => RefreshGrid();
            if (btnExportExcel != null) this.btnExportExcel.Click += btnExportExcel_Click;
            if (dgvlLogData != null) this.dgvlLogData.CellDoubleClick += dgvlLogData_CellDoubleClick;
            if (exportProfilesToolStripMenuItem != null) this.exportProfilesToolStripMenuItem.Click += ExportProfiles_Click;
            if (importProfilesToolStripMenuItem != null) this.importProfilesToolStripMenuItem.Click += ImportProfiles_Click;

            // Checkbox modul di Tab 1 mengubah isi antrean dokumen di Tab 2
            if (chkSO != null) this.chkSO.CheckedChanged += (s, e) => RefreshPendingGrid();
            if (chkPO != null) this.chkPO.CheckedChanged += (s, e) => RefreshPendingGrid();
            if (chkSL != null) this.chkSL.CheckedChanged += (s, e) => RefreshPendingGrid();

            // Kontrol di Tab Antrean Pending
            if (chkSelectAllPending != null) this.chkSelectAllPending.CheckedChanged += chkSelectAllPending_CheckedChanged;
            if (btnRefreshPending != null) this.btnRefreshPending.Click += (s, e) => RefreshPendingGrid();
            if (txtSearchPending != null) this.txtSearchPending.TextChanged += (s, e) => ApplyPendingFilter();
            if (btnSyncSelected != null) this.btnSyncSelected.Click += btnSyncSelected_Click;
            if (dgvPendingQueue != null)
            {
                this.dgvPendingQueue.CellDoubleClick += dgvPendingQueue_CellDoubleClick;
                this.dgvPendingQueue.CellFormatting += dgvPendingQueue_CellFormatting;
            }

            // Context Menu Klik Kanan pada Grid
            if (dgvlLogData != null) this.dgvlLogData.CellMouseDown += Grid_CellMouseDown;
            if (dgvPendingQueue != null) this.dgvPendingQueue.CellMouseDown += Grid_CellMouseDown;
            if (contextMenuStrip1 != null) this.contextMenuStrip1.Opening += contextMenuStrip1_Opening;
            if (menuItemViewDetail != null) this.menuItemViewDetail.Click += menuItemViewDetail_Click;
            if (menuItemDeleteRow != null) this.menuItemDeleteRow.Click += menuItemDeleteRow_Click;

            // Kontrol di Tab Debug
            if (btnRefreshStats != null) this.btnRefreshStats.Click += (s, e) => RefreshDebugSummary();
            if (btnDeleteAllTx != null) this.btnDeleteAllTx.Click += btnDeleteAllTx_Click;
        }

        #region PROFILE & DATABASE HELPERS
        private void UpdateActiveProfileLabel()
        {
            string profileName = _configService.GetActiveProfileName();
            if (!string.IsNullOrEmpty(profileName))
            {
                label8.Text = "Active Profile: " + profileName;
                label8.ForeColor = Color.Blue;
            }
            else
            {
                label8.Text = "Active Profile: None";
                label8.ForeColor = Color.Red;
            }
            label8.Refresh();

            UpdateStagingDbLabel();
        }

        private void UpdateStagingDbLabel()
        {
            if (lblStagingDb == null) return;

            // Posisikan lblStagingDb di sebelah kanan label8 dengan jarak 25px
            lblStagingDb.Left = label8.Right + 25;
            lblStagingDb.Top = label8.Top;

            AppConfig config = _configService.GetActiveConfiguration();
            if (config == null || string.IsNullOrWhiteSpace(config.ExternalDBName))
            {
                lblStagingDb.Text = "Staging DB: None";
                lblStagingDb.ForeColor = Color.Gray;
                lblStagingDb.Refresh();
                return;
            }

            string dbType = !string.IsNullOrWhiteSpace(config.ExternalDBType) ? config.ExternalDBType : "SQLServer";
            string dbName = config.ExternalDBName;
            string dbServer = config.ExternalDBServer ?? "localhost";
            if (!string.IsNullOrWhiteSpace(config.ExternalDBPort) && config.ExternalDBPort != "0" && config.ExternalDBPort != "1433")
                dbServer += ":" + config.ExternalDBPort;

            // Tampilkan info awal DB terlebih dahulu
            string baseInfo = $"Staging DB: {dbName} ({dbType} @ {dbServer})";
            lblStagingDb.Text = $"{baseInfo} (Connecting...)";
            lblStagingDb.ForeColor = Color.FromArgb(70, 80, 95);
            lblStagingDb.Refresh();

            // Jalankan pengecekan koneksi secara asynchronous di background
            string baseConnStr = ConfigService.BuildStagingConnectionString(config);
            Task.Run(() =>
            {
                bool isConnected = false;
                if (!string.IsNullOrEmpty(baseConnStr))
                {
                    try
                    {
                        var builder = new System.Data.SqlClient.SqlConnectionStringBuilder(baseConnStr)
                        {
                            ConnectTimeout = 3
                        };
                        using (var conn = new System.Data.SqlClient.SqlConnection(builder.ConnectionString))
                        {
                            conn.Open();
                            isConnected = (conn.State == System.Data.ConnectionState.Open);
                        }
                    }
                    catch
                    {
                        isConnected = false;
                    }
                }

                if (this.IsDisposed || !this.IsHandleCreated) return;

                this.BeginInvoke((Action)(() =>
                {
                    if (this.IsDisposed) return;
                    if (isConnected)
                    {
                        lblStagingDb.Text = $"{baseInfo} • Connected";
                        lblStagingDb.ForeColor = Color.ForestGreen;
                    }
                    else
                    {
                        lblStagingDb.Text = $"{baseInfo} • Disconnected";
                        lblStagingDb.ForeColor = Color.Crimson;
                    }
                    lblStagingDb.Refresh();
                }));
            });
        }

        private DatabaseService GetDatabaseService()
        {
            AppConfig config = _configService.GetActiveConfiguration();
            if (config == null) return null;

            // DatabaseService (log staging) hanya mendukung SQL Server.
            // Cegah pembuatan connection string SQL Server untuk DB tipe lain.
            string connString = ConfigService.BuildStagingConnectionString(config);
            if (string.IsNullOrEmpty(connString))
            {
                MessageBox.Show(
                    $"Log staging hanya mendukung SQL Server. Profil aktif memakai tipe '{config.ExternalDBType}'.",
                    "Tipe DB Tidak Didukung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            var dbService = new DatabaseService(connString);

            // Pastikan tabel log ada di DB Staging
            dbService.InitializeTables();

            return dbService;
        }
        #endregion

        #region UI ACTIONS (SYNC & LOG)
        private void LoadLogFromDatabase()
        {
            var dbService = GetDatabaseService();
            if (dbService == null) return;

            masterLogList = dbService.LoadDocumentHeaderLogs();
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            if (masterLogList == null) masterLogList = new List<DocumentHeaderLogModel>();

            // Ambil filter teks/combobox
            string filterStatus = cbLogLevel.SelectedItem?.ToString() ?? "All";
            string filterFunction = cbFunction.SelectedItem?.ToString() ?? "All";
            string searchKeyword = txtSearchLog?.Text.Trim().ToLower() ?? "";

            // Ambil filter tanggal (Gunakan .Date untuk mengabaikan jam/menit/detik)
            DateTime fromDate = dtpStgFrom.Value.Date;
            DateTime toDate = dtpStgTo.Value.Date;

            var filteredList = masterLogList.Where(x =>
                // 1. FILTER TANGGAL (berdasarkan CreatedAt jika valid, jika tidak pakai DocDate)
                ((x.CreatedAt != DateTime.MinValue ? x.CreatedAt.Date : x.DocDate.Date) >= fromDate &&
                 (x.CreatedAt != DateTime.MinValue ? x.CreatedAt.Date : x.DocDate.Date) <= toDate) &&

                // 2. FILTER STATUS & FUNGSI
                (filterStatus == "All" || x.Status.Equals(filterStatus, StringComparison.OrdinalIgnoreCase)) &&
                (filterFunction == "All" || x.DocType.Equals(filterFunction, StringComparison.OrdinalIgnoreCase)) &&

                // 3. FILTER PENCARIAN TEKS
                (string.IsNullOrEmpty(searchKeyword) ||
                 (!string.IsNullOrEmpty(x.WebTxNumber) && x.WebTxNumber.ToLower().Contains(searchKeyword)) ||
                 (!string.IsNullOrEmpty(x.CardCode) && x.CardCode.ToLower().Contains(searchKeyword)) ||
                 (!string.IsNullOrEmpty(x.CardName) && x.CardName.ToLower().Contains(searchKeyword)) ||
                 (!string.IsNullOrEmpty(x.DocEntry) && x.DocEntry.ToLower().Contains(searchKeyword)) ||
                 (!string.IsNullOrEmpty(x.Remarks) && x.Remarks.ToLower().Contains(searchKeyword)) ||
                 (!string.IsNullOrEmpty(x.ErrorMessage) && x.ErrorMessage.ToLower().Contains(searchKeyword)))
            ).ToList();

            dgvlLogData.DataSource = new System.ComponentModel.BindingList<DocumentHeaderLogModel>(filteredList);
            dgvlLogData.Refresh();
        }

        private void dgvlLogData_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvlLogData.Rows.Count) return;

            var selectedDoc = dgvlLogData.Rows[e.RowIndex].DataBoundItem as DocumentHeaderLogModel;
            if (selectedDoc == null) return;

            var dbService = GetDatabaseService();
            if (dbService == null) return;

            using (var frmDetail = new FormDocumentDetail(selectedDoc, dbService))
            {
                frmDetail.ShowDialog(this);
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tablog)
                LoadLogFromDatabase();
            else if (tabControl1.SelectedTab == tabPending)
                RefreshPendingGrid();
            else if (tabControl1.SelectedTab == tabDebug)
                RefreshDebugSummary();
        }

        #region PENDING / RETRY QUEUE ACTIONS
        private void RefreshPendingGrid()
        {
            var dbService = GetDatabaseService();
            if (dbService == null)
            {
                masterPendingList = new List<PendingQueueDocModel>();
                ApplyPendingFilter();
                return;
            }

            bool includeSO = chkSO?.Checked ?? false;
            bool includePO = chkPO?.Checked ?? false;
            bool includeSL = chkSL?.Checked ?? false;

            // Update label filter info
            var activeModules = new List<string>();
            if (includePO) activeModules.Add("Purchase Order");
            if (includeSO) activeModules.Add("Sales Order");
            if (includeSL) activeModules.Add("Service Layer");

            if (lblPendingFilterInfo != null)
            {
                if (activeModules.Count > 0)
                {
                    lblPendingFilterInfo.Text = "Filter Modul Aktif dari Tab 1: " + string.Join(", ", activeModules);
                    lblPendingFilterInfo.ForeColor = Color.DarkSlateGray;
                }
                else
                {
                    lblPendingFilterInfo.Text = "Belum ada modul dipilih pada Tab 1 (Centang Sales Order / Purchase Order / Service Layer).";
                    lblPendingFilterInfo.ForeColor = Color.DarkRed;
                }
            }

            if (!includeSO && !includePO && !includeSL)
            {
                masterPendingList = new List<PendingQueueDocModel>();
            }
            else
            {
                try
                {
                    masterPendingList = dbService.LoadUnsyncedDocuments(includePO, includeSO, includeSL);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error load unsynced: " + ex.Message);
                    masterPendingList = new List<PendingQueueDocModel>();
                }
            }

            ApplyPendingFilter();
        }

        private void ApplyPendingFilter()
        {
            if (masterPendingList == null) masterPendingList = new List<PendingQueueDocModel>();

            string searchKeyword = txtSearchPending?.Text.Trim().ToLower() ?? "";

            var filteredList = masterPendingList.Where(x =>
                string.IsNullOrEmpty(searchKeyword) ||
                (!string.IsNullOrEmpty(x.WebTxNumber) && x.WebTxNumber.ToLower().Contains(searchKeyword)) ||
                (!string.IsNullOrEmpty(x.CardCode) && x.CardCode.ToLower().Contains(searchKeyword)) ||
                (!string.IsNullOrEmpty(x.CardName) && x.CardName.ToLower().Contains(searchKeyword)) ||
                (!string.IsNullOrEmpty(x.DocType) && x.DocType.ToLower().Contains(searchKeyword)) ||
                (!string.IsNullOrEmpty(x.Status) && x.Status.ToLower().Contains(searchKeyword)) ||
                (!string.IsNullOrEmpty(x.ErrorMessage) && x.ErrorMessage.ToLower().Contains(searchKeyword))
            ).ToList();

            dgvPendingQueue.DataSource = new BindingList<PendingQueueDocModel>(filteredList);
            dgvPendingQueue.Refresh();
        }

        private void chkSelectAllPending_CheckedChanged(object sender, EventArgs e)
        {
            if (masterPendingList == null) return;
            bool isChecked = chkSelectAllPending.Checked;
            foreach (var item in masterPendingList)
            {
                item.IsSelected = isChecked;
            }
            dgvPendingQueue.Refresh();
        }

        private void dgvPendingQueue_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvPendingQueue.Rows.Count) return;

            var selectedItem = dgvPendingQueue.Rows[e.RowIndex].DataBoundItem as PendingQueueDocModel;
            if (selectedItem == null) return;

            var dbService = GetDatabaseService();
            if (dbService == null) return;

            var headerLog = new DocumentHeaderLogModel
            {
                HeaderId = selectedItem.HeaderId,
                DocType = selectedItem.DocType,
                WebTxNumber = selectedItem.WebTxNumber,
                CardCode = selectedItem.CardCode,
                CardName = selectedItem.CardName,
                DocDate = selectedItem.DocDate,
                DocDueDate = selectedItem.DocDueDate,
                Status = selectedItem.Status,
                DocEntry = "-",
                CreatedAt = selectedItem.CreatedAt,
                Remarks = selectedItem.Remarks,
                ErrorMessage = selectedItem.ErrorMessage,
                UdfDataJson = selectedItem.UdfDataJson
            };

            using (var frmDetail = new FormDocumentDetail(headerLog, dbService))
            {
                frmDetail.ShowDialog(this);
            }
        }

        private void dgvPendingQueue_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvPendingQueue.Rows.Count) return;

            var item = dgvPendingQueue.Rows[e.RowIndex].DataBoundItem as PendingQueueDocModel;
            if (item == null) return;

            if (item.Status == "Failed")
            {
                dgvPendingQueue.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.DarkRed;
                dgvPendingQueue.Rows[e.RowIndex].DefaultCellStyle.SelectionForeColor = Color.Yellow;
            }
            else if (item.Status == "Pending")
            {
                dgvPendingQueue.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.DarkOrange;
                dgvPendingQueue.Rows[e.RowIndex].DefaultCellStyle.SelectionForeColor = Color.White;
            }
        }

        private async void btnSyncSelected_Click(object sender, EventArgs e)
        {
            AppConfig activeConfig = _configService.GetActiveConfiguration();
            if (activeConfig == null)
            {
                MessageBox.Show("Profil aktif tidak ditemukan.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            dgvPendingQueue.EndEdit();

            var selectedItems = masterPendingList?.Where(x => x.IsSelected).ToList();
            if (selectedItems == null || selectedItems.Count == 0)
            {
                MessageBox.Show("Pilih minimal satu dokumen dari antrean untuk disinkronisasi.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirmResult = MessageBox.Show(
                $"Apakah Anda yakin ingin menyinkronkan {selectedItems.Count} dokumen terpilih ke SAP?",
                "Konfirmasi Sync Terpilih",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmResult != DialogResult.Yes) return;

            SetUIState(false);

            var dbService = GetDatabaseService();
            if (dbService == null)
            {
                SetUIState(true);
                return;
            }

            var poIds = selectedItems.Where(x => x.DocType == "Purchase Order").Select(x => x.HeaderId).ToList();
            var soIds = selectedItems.Where(x => x.DocType == "Sales Order").Select(x => x.HeaderId).ToList();

            var pendingPurchaseOrders = new List<PendingPurchaseOrder>();
            if (poIds.Count > 0)
            {
                try
                {
                    pendingPurchaseOrders = dbService.LoadPendingPurchaseOrdersByIds(poIds);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Gagal memuat detail Purchase Order terpilih: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetUIState(true);
                    return;
                }
            }

            var pendingOrders = new List<PendingSalesOrder>();
            if (soIds.Count > 0)
            {
                try
                {
                    pendingOrders = dbService.LoadPendingSalesOrdersByIds(soIds);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Gagal memuat detail Sales Order terpilih: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetUIState(true);
                    return;
                }
            }

            int totalDocCount = pendingOrders.Count + pendingPurchaseOrders.Count;
            if (totalDocCount == 0)
            {
                MessageBox.Show("Tidak ada dokumen yang valid untuk disinkronisasi.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetUIState(true);
                return;
            }

            bool isDryRun = chkDryRun?.Checked ?? false;
            var progress = new Progress<int>(percent => pBar.Value = percent);
            int failedCount = 0;

            try
            {
                failedCount = await Task.Run(() => ProcessSyncInSequence(activeConfig, dbService, pendingOrders, pendingPurchaseOrders, isDryRun, progress));

                RefreshPendingGrid();
                LoadLogFromDatabase();

                string modeSuffix = isDryRun ? " (Mode Simulasi)" : "";
                if (failedCount > 0)
                    MessageBox.Show($"Sync Selesai{modeSuffix}!\nBerhasil: {totalDocCount - failedCount} dokumen.\nGagal: {failedCount} dokumen.", "Hasil Sinkronisasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show($"Sinkronisasi Berhasil Sepenuhnya{modeSuffix}!\nSemua {totalDocCount} dokumen terpilih berhasil disinkronisasi ke SAP.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sync Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUIState(true);
            }
        }
        #endregion
        #endregion

        #region BUTTON SYNC LOGIC (PARALLEL & DRY RUN)
        private async void btnSync_Click(object sender, EventArgs e)
        {
            AppConfig activeConfig = _configService.GetActiveConfiguration();
            if (activeConfig == null)
            {
                MessageBox.Show("Profil aktif tidak ditemukan.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!chkSO.Checked && !chkPO.Checked && !chkSL.Checked && !chkLogData.Checked)
            {
                MessageBox.Show("Pilih minimal satu kategori sinkronisasi.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (chkSL.Checked)
            {
                MessageBox.Show("Sinkronisasi Service Layer belum diimplementasikan. Centang 'Sales Order' atau 'Purchase Order' untuk sekarang.",
                                "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            SetUIState(false);

            // =========================================================================
            // TRIGGER OTOMATIS: Kirim XML dan Profil ke Endpoint Web saat menekan Sync
            // =========================================================================
            try
            {
                await _configService.SendActiveConfigurationToWebAsync(webEndpointUrl);
            }
            catch (Exception ex)
            {
                var confirmResult = MessageBox.Show(
                    $"{ex.Message}\n\nApakah Anda ingin tetap melanjutkan proses sinkronisasi SAP?",
                    "Gagal Kirim Konfigurasi Web",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirmResult == DialogResult.No)
                {
                    SetUIState(true);
                    return;
                }
            }

            if (chkLogData.Checked) { masterLogList.Clear(); RefreshGrid(); }

            if (!chkSO.Checked && !chkPO.Checked)
            {
                MessageBox.Show("Pilih minimal satu modul untuk disinkronisasi (Sales Order atau Purchase Order).", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetUIState(true);
                return;
            }

            // =========================================================================
            // AMBIL DATA PENDING DARI TABEL STAGING (bukan data hardcoded)
            // =========================================================================
            var dbService = GetDatabaseService();
            if (dbService == null)
            {
                SetUIState(true);
                return;
            }

            var pendingOrders = new List<PendingSalesOrder>();
            if (chkSO.Checked)
            {
                try
                {
                    pendingOrders = dbService.LoadPendingSalesOrders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Gagal memuat data Sales Order pending dari staging: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetUIState(true);
                    return;
                }
            }

            var pendingPurchaseOrders = new List<PendingPurchaseOrder>();
            if (chkPO.Checked)
            {
                try
                {
                    pendingPurchaseOrders = dbService.LoadPendingPurchaseOrders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Gagal memuat data Purchase Order pending dari staging: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetUIState(true);
                    return;
                }
            }

            int totalDocCount = pendingOrders.Count + pendingPurchaseOrders.Count;
            if (totalDocCount == 0)
            {
                MessageBox.Show("Tidak ada dokumen pending di staging (process_status = 0).", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetUIState(true);
                return;
            }

            bool isDryRun = chkDryRun?.Checked ?? false;
            var progress = new Progress<int>(percent => pBar.Value = percent);
            int failedCount = 0;

            try
            {
                failedCount = await Task.Run(() => ProcessSyncInSequence(activeConfig, dbService, pendingOrders, pendingPurchaseOrders, isDryRun, progress));

                LoadLogFromDatabase();
                RefreshPendingGrid();
                string modeSuffix = isDryRun ? " (Mode Simulasi)" : "";

                if (failedCount > 0)
                    MessageBox.Show($"Sync Selesai{modeSuffix}!\nBerhasil: {totalDocCount - failedCount} dokumen.\nGagal: {failedCount} dokumen.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show($"Sinkronisasi Berhasil Sepenuhnya{modeSuffix}!\nSemua {totalDocCount} dokumen tersinkronisasi.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sync Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUIState(true);
            }
        }

        private int ProcessSyncInSequence(AppConfig config, DatabaseService dbService, List<PendingSalesOrder> orders, List<PendingPurchaseOrder> poOrders, bool isDryRun, IProgress<int> progress)
        {
            int failedCount = 0;
            int totalTasks = orders.Count + poOrders.Count;
            if (totalTasks == 0) return 0;
            int completedTasks = 0;

            progress.Report(10);

            // Membuka 1 instance service untuk dipakai bersama-sama oleh seluruh data
            using (var sapService = new SapSyncService())
            {
                try
                {
                    if (!isDryRun)
                    {
                        // LOGIN KE SAP HANYA 1 KALI DI SINI (DI LUAR LOOP)
                        sapService.ConnectToDIAPI(config);
                    }
                    progress.Report(30);

                    // 1. Sync Sales Orders
                    foreach (var order in orders)
                    {
                        try
                        {
                            if (isDryRun)
                            {
                                System.Threading.Thread.Sleep(100); // Delay simulasi singkat
                                LogSyncResult(dbService, order, "Success", "DRY-RUN", "Validasi berhasil (Mode Simulasi)");
                            }
                            else
                            {
                                string docEntry = sapService.ExecuteSalesOrderSync(order);
                                LogSyncResult(dbService, order, "Success", docEntry, "-");
                                dbService?.UpdateSalesOrderStatus(order.HeaderId, 1);
                            }
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            LogSyncResult(dbService, order, "Failed", null, ex.Message);
                            dbService?.UpdateSalesOrderStatus(order.HeaderId, 2, ex.Message);
                        }

                        completedTasks++;
                        int currentProgress = 30 + (int)((completedTasks / (float)totalTasks) * 70);
                        progress.Report(Math.Min(currentProgress, 100));
                    }

                    // 2. Sync Purchase Orders
                    foreach (var po in poOrders)
                    {
                        try
                        {
                            if (isDryRun)
                            {
                                System.Threading.Thread.Sleep(100);
                                LogSyncResult(dbService, po, "Success", "DRY-RUN", "Validasi berhasil (Mode Simulasi)");
                            }
                            else
                            {
                                string docEntry = sapService.ExecutePurchaseOrderSync(po);
                                LogSyncResult(dbService, po, "Success", docEntry, "-");
                                dbService?.UpdatePurchaseOrderStatus(po.HeaderId, 1, null, docEntry);
                            }
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            LogSyncResult(dbService, po, "Failed", null, ex.Message);
                            dbService?.UpdatePurchaseOrderStatus(po.HeaderId, 2, ex.Message);
                        }

                        completedTasks++;
                        int currentProgress = 30 + (int)((completedTasks / (float)totalTasks) * 70);
                        progress.Report(Math.Min(currentProgress, 100));
                    }
                }
                catch (Exception ex)
                {
                    // Menangkap jika kegagalan terjadi pada proses login utama di awal
                    MessageBox.Show($"Gagal inisialisasi koneksi awal ke SAP: {ex.Message}", "Koneksi Gagal", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return totalTasks;
                }
            }

            progress.Report(100);
            return failedCount;
        }

        /// <summary>
        /// Mencatat hasil sync per dokumen ke log database (TBL_SYNC_HISTORY / TBL_SYNC_ERROR)
        /// dan menampilkan satu baris di grid log.
        /// </summary>
        private void LogSyncResult(DatabaseService dbService, PendingSalesOrder order, string status, string docEntry, string errorMessage)
        {
            try
            {
                var log = new SyncLogModel
                {
                    DocType = "Sales Order",
                    DocEntry = docEntry ?? "",
                    CardCode = order.CardCode,
                    ItemCode = order.Lines.Count > 0 ? order.Lines[0].ItemCode : "",
                    Quantity = order.Lines.Count > 0 ? (double)order.Lines[0].Quantity : 0,
                    Price = order.Lines.Count > 0 ? (double)order.Lines[0].Price : 0,
                    WarehouseCode = order.Lines.Count > 0 ? order.Lines[0].Warehouse : "",
                    Status = status,
                    ErrorSource = status == "Failed" ? "SAP Validation" : "-",
                    ErrorMessage = errorMessage ?? "-",
                    CreatedAt = DateTime.Now
                };

                dbService?.SaveLogToDatabase(log);
            }
            catch
            {
                // Logging gagal tidak boleh mengganggu proses sync utama
            }
        }

        private void LogSyncResult(DatabaseService dbService, PendingPurchaseOrder order, string status, string docEntry, string errorMessage)
        {
            try
            {
                var log = new SyncLogModel
                {
                    DocType = "Purchase Order",
                    DocEntry = docEntry ?? "",
                    CardCode = order.CardCode,
                    ItemCode = order.Lines.Count > 0 ? order.Lines[0].ItemCode : "",
                    Quantity = order.Lines.Count > 0 ? (double)order.Lines[0].Quantity : 0,
                    Price = order.Lines.Count > 0 ? (double)order.Lines[0].Price : 0,
                    WarehouseCode = order.Lines.Count > 0 ? order.Lines[0].Warehouse : "",
                    Status = status,
                    ErrorSource = status == "Failed" ? "SAP Validation" : "-",
                    ErrorMessage = errorMessage ?? "-",
                    CreatedAt = DateTime.Now
                };

                dbService?.SaveLogToDatabase(log);
            }
            catch
            {
                // Logging gagal tidak boleh mengganggu proses sync utama
            }
        }

        //private int ProcessSyncInParallel(AppConfig config, List<SyncLogModel> tasks, bool isDryRun, IProgress<int> progress)
        //{
        //    int failedCount = 0;
        //    int totalTasks = tasks.Count;
        //    int completedTasks = 0;
        //    var dbService = GetDatabaseService();

        //    progress.Report(10);

        //    var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 5 };

        //    Parallel.ForEach(tasks, parallelOptions, task =>
        //    {
        //        if (task.DocType == "Sales Order")
        //        {
        //            using (var sapService = new SapSyncService())
        //            {
        //                try
        //                {
        //                    task.CreatedAt = DateTime.Now;

        //                    if (isDryRun)
        //                    {
        //                        System.Threading.Thread.Sleep(500); // Simulasi proses
        //                        if (task.CardCode == "INVALID_CARD")
        //                            throw new Exception("Simulasi Gagal: CardCode tidak valid.");

        //                        task.Status = "Success";
        //                        task.DocEntry = "DRY-RUN";
        //                        task.ErrorMessage = "Validasi berhasil (Tidak di-post ke SAP)";
        //                    }
        //                    else
        //                    {
        //                        sapService.ConnectToDIAPI(config);
        //                        sapService.ExecuteSalesOrderSync(task);
        //                        task.Status = "Success";
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    task.Status = "Failed";
        //                    task.ErrorMessage = ex.Message;
        //                    task.DocEntry = null;
        //                    System.Threading.Interlocked.Increment(ref failedCount);
        //                }
        //                finally
        //                {
        //                    this.Invoke(new Action(() => masterLogList.Add(task)));
        //                    dbService?.SaveLogToDatabase(task);

        //                    System.Threading.Interlocked.Increment(ref completedTasks);
        //                    int currentProgress = 10 + (int)((completedTasks / (float)totalTasks) * 90);
        //                    progress.Report(Math.Min(currentProgress, 100));
        //                }
        //            }
        //        }
        //    });

        //    progress.Report(100);
        //    return failedCount;
        //}
        #endregion

        #region EXPORT & BACKUP/RESTORE LOGIC
        private void btnExportExcel_Click(object sender, EventArgs e)
        {
            if (dgvlLogData.Rows.Count == 0)
            {
                MessageBox.Show("Tidak ada data log untuk diekspor.", "Informasi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "Excel Workbook|*.xlsx", FileName = $"SyncDocLog_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Log Header Dokumen");
                            var currentList = ((BindingList<DocumentHeaderLogModel>)dgvlLogData.DataSource).ToList();

                            // Header
                            worksheet.Cell(1, 1).InsertTable(currentList);
                            worksheet.Columns().AdjustToContents();

                            workbook.SaveAs(sfd.FileName);
                            MessageBox.Show("Data log berhasil diekspor!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Gagal mengekspor data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExportProfiles_Click(object sender, EventArgs e)
        {
            string sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AllConfigurations.xml");
            if (!File.Exists(sourcePath)) return;

            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "XML Configuration|*.xml", FileName = $"ConfigBackup_{DateTime.Now:yyyyMMdd}.xml" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.Copy(sourcePath, sfd.FileName, true);
                    MessageBox.Show("Backup profil berhasil diekspor!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private async void ImportProfiles_Click(object sender, EventArgs e)
        {
            string targetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AllConfigurations.xml");
            var confirm = MessageBox.Show("Mengimpor akan menimpa profil saat ini. Lanjutkan?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "XML Configuration|*.xml" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    File.Copy(ofd.FileName, targetPath, true);
                    UpdateActiveProfileLabel();

                    // =========================================================================
                    // TRIGGER OTOMATIS: Kirim XML dan Profil ke Endpoint Web setelah diimpor
                    // =========================================================================
                    try
                    {
                        await _configService.SendActiveConfigurationToWebAsync(webEndpointUrl);
                        MessageBox.Show("Profil berhasil diimpor & dikirim ke Web Server!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Profil berhasil diimpor, tetapi gagal dikirim ke Web:\n{ex.Message}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }
        #endregion

        // ... (Biarkan Region BUTTON RETRY LOGIC (ASYNC AWAIT) dan MENU EVENTS sama persis dengan yang sebelumnya) ...
        // Agar script tidak terlalu panjang, tempatkan kode btnRetryFailed_Click kamu di sini.

        #region BUTTON RETRY LOGIC (ASYNC AWAIT)
        private async void btnRetryFailed_Click(object sender, EventArgs e)
        {
            var dbService = GetDatabaseService();
            if (dbService == null) return;

            List<SyncLogModel> latestErrors;
            try
            {
                latestErrors = dbService.GetLatestUnresolvedErrors();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (latestErrors.Count == 0)
            {
                MessageBox.Show("Tidak ada data error yang perlu di-retry.", "Informasi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedToRetry = ShowRetrySelectionDialog(latestErrors);
            if (selectedToRetry == null || selectedToRetry.Count == 0) return;

            AppConfig activeConfig = _configService.GetActiveConfiguration();
            if (activeConfig == null) return;

            SetUIState(false);
            var progress = new Progress<int>(percent => pBar.Value = percent);

            try
            {
                var result = await Task.Run(() => ProcessRetryInBackground(activeConfig, dbService, selectedToRetry, progress));

                LoadLogFromDatabase();
                if (result.Failed > 0)
                    MessageBox.Show($"Retry selesai.\nBerhasil: {result.Success}\nMasih gagal: {result.Failed}\n\nData gagal akan muncul kembali di daftar retry.", "Retry Selesai", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show("Retry selesai!\nSemua data yang dipilih berhasil disinkronkan dan telah dihapus dari daftar retry.", "Retry Selesai", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Retry Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUIState(true);
            }
        }

        private (int Success, int Failed) ProcessRetryInBackground(AppConfig config, DatabaseService dbService, List<SyncLogModel> tasksToRetry, IProgress<int> progress)
        {
            int successCount = 0;
            int stillFailedCount = 0;
            int current = 0;

            using (var sapService = new SapSyncService())
            {
                sapService.ConnectToDIAPI(config);

                foreach (var task in tasksToRetry)
                {
                    try
                    {
                        if (task.DocType == "Sales Order")
                            sapService.ExecuteSalesOrderSync(task);
                        else if (task.DocType == "Purchase Order")
                            sapService.ExecutePurchaseOrderSync(task);

                        task.Status = "Success";
                        dbService.MarkErrorsAsResolved(task.CardCode, task.ItemCode);
                        dbService.SaveLogToDatabase(task);
                        successCount++;

                        this.Invoke(new Action(() => MessageBox.Show($"Berhasil menyinkronkan data:\nCardCode: {task.CardCode}, ItemCode: {task.ItemCode}", "Retry Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information)));
                    }
                    catch (Exception ex)
                    {
                        task.Status = "Failed";
                        task.ErrorMessage = ex.Message;
                        dbService.SaveLogToDatabase(task);
                        stillFailedCount++;
                    }

                    current++;
                    progress.Report((int)((current / (float)tasksToRetry.Count) * 100));
                }
            }
            return (successCount, stillFailedCount);
        }

        private List<SyncLogModel> ShowRetrySelectionDialog(List<SyncLogModel> latestErrors)
        {
            using (Form formRetry = new Form { Text = "Pilih Data", Size = new Size(900, 480), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false })
            {
                DataGridView dgvRetry = new DataGridView { Dock = DockStyle.Top, Height = 380, AllowUserToAddRows = false, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
                dgvRetry.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Pilih", Name = "colSelect", Width = 50 });
                dgvRetry.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Function", DataPropertyName = "DocType", ReadOnly = true, Width = 120 });
                dgvRetry.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CardCode", DataPropertyName = "CardCode", ReadOnly = true, Width = 100 });
                dgvRetry.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ItemCode", DataPropertyName = "ItemCode", ReadOnly = true, Width = 100 });
                dgvRetry.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Quantity", DataPropertyName = "Quantity", ReadOnly = true, Width = 70 });
                dgvRetry.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Warehouse", DataPropertyName = "WarehouseCode", ReadOnly = true, Width = 90 });
                dgvRetry.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Error Message", DataPropertyName = "ErrorMessage", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

                dgvRetry.DataSource = new System.ComponentModel.BindingList<SyncLogModel>(latestErrors);
                for (int i = 0; i < dgvRetry.Rows.Count; i++) dgvRetry.Rows[i].Cells["colSelect"].Value = true;

                Button btnProcess = new Button { Text = "Retry", Dock = DockStyle.Bottom, Height = 45, Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold), BackColor = Color.LightGreen };
                btnProcess.Click += (s, ev) => formRetry.DialogResult = DialogResult.OK;

                formRetry.Controls.Add(dgvRetry);
                formRetry.Controls.Add(btnProcess);

                if (formRetry.ShowDialog() == DialogResult.OK)
                {
                    var selected = new List<SyncLogModel>();
                    for (int i = 0; i < dgvRetry.Rows.Count; i++)
                    {
                        if (Convert.ToBoolean(dgvRetry.Rows[i].Cells["colSelect"].Value))
                            selected.Add(latestErrors[i]);
                    }

                    if (selected.Count == 0)
                        MessageBox.Show("Tidak ada data yang dicentang.", "Informasi", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    return selected;
                }
            }
            return null;
        }
        #endregion

        private async void configurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frmChoose = new FormChooseCF { StartPosition = FormStartPosition.CenterParent })
            {
                // =========================================================================
                // TRIGGER OTOMATIS: Update & Kirim data saat Profil diubah dari menu
                // =========================================================================
                if (frmChoose.ShowDialog() == DialogResult.OK)
                {
                    UpdateActiveProfileLabel();
                    string activeName = frmChoose.SelectedProfileName ?? _configService.GetActiveProfileName();
                    string profileInfo = !string.IsNullOrEmpty(activeName) ? $"Profil '{activeName}'" : "Profil";

                    try
                    {
                        await _configService.SendActiveConfigurationToWebAsync(webEndpointUrl);
                        MessageBox.Show($"{profileInfo} aktif digunakan & konfigurasi berhasil dikirim ke Web Server!",
                                        "Informasi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"{profileInfo} aktif digunakan, namun GAGAL mengirim ke Web Server:\n{ex.Message}",
                                        "Warning Endpoint", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void passwordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new FormPassword { StartPosition = FormStartPosition.CenterParent })
            {
                frm.ShowDialog();
            }
        }

        private void schedulerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new FormSettingScheduler { StartPosition = FormStartPosition.CenterParent })
            {
                frm.ShowDialog();
            }
        }

        private void SetUIState(bool isEnabled)
        {
            btnSync.Enabled = isEnabled;
            if (btnSyncSelected != null) btnSyncSelected.Enabled = isEnabled;
            if (btnRefreshPending != null) btnRefreshPending.Enabled = isEnabled;
            if (isEnabled) pBar.Value = 0;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            // Terapkan theme konsisten
            ApplyTheme();

            // Perbarui label profil & database staging saat form dimuat
            UpdateActiveProfileLabel();

            // Coba kirim ulang payload yang gagal dikirim ke Web API saat ganti profil
            try
            {
                Task.Run(() => ProfileSyncService.FlushPendingSyncs());
            }
            catch { }
        }

        /// <summary>
        /// Terapkan theme SOLTIUS ke seluruh kontrol FormMain.
        /// </summary>
        private void ApplyTheme()
        {
            UITheme.ApplyForm(this);
            this.Text = "SOLTIUS Scheduler";

            UITheme.ApplyPrimary(btnSync);
            UITheme.ApplySecondary(btnExportExcel);
            UITheme.ApplySecondary(btnViewLog);
            UITheme.ApplySecondary(btnRetryFailed);

            UITheme.ApplyPrimary(btnSyncSelected);
            UITheme.ApplySecondary(btnRefreshPending);
            UITheme.ApplyGrid(dgvPendingQueue);
            UITheme.ApplyTextBox(txtSearchPending);
            UITheme.ApplyCheck(chkSelectAllPending);

            UITheme.ApplyGrid(dgvlLogData);
            UITheme.ApplyProgress(pBar);
            UITheme.ApplyGroup(grpBox1);
            UITheme.ApplyGroup(grpBox1_1);
            UITheme.ApplyCombo(cbLogLevel);
            UITheme.ApplyCombo(cbFunction);
            UITheme.ApplyTextBox(txtSearchLog);
            UITheme.ApplyLabel(label8, muted: true);
            if (lblStagingDb != null) UITheme.ApplyLabel(lblStagingDb, muted: true);

            UITheme.ApplyCheck(chkDryRun);
            UITheme.ApplyCheck(chkSO);
            UITheme.ApplyCheck(chkPO);
            UITheme.ApplyCheck(chkSL);
            UITheme.ApplyCheck(chkLogData);
            UITheme.ApplyCheck(chkAll1);

            // Theme kontrol Tab Debug
            if (grpBoxDebug != null) UITheme.ApplyGroup(grpBoxDebug);
            if (grpBoxTxStats != null) UITheme.ApplyGroup(grpBoxTxStats);
            if (grpBoxDangerZone != null) UITheme.ApplyGroup(grpBoxDangerZone);
            if (btnRefreshStats != null) UITheme.ApplySecondary(btnRefreshStats);
            if (btnDeleteAllTx != null) UITheme.ApplyDanger(btnDeleteAllTx);
            if (chkEnableRowDelete != null) UITheme.ApplyCheck(chkEnableRowDelete);

            tabControl1.SelectedTab = tabsync;
        }

        #region DEBUG & MAINTENANCE ACTIONS
        private void RefreshDebugSummary()
        {
            var dbService = GetDatabaseService();
            if (dbService == null)
            {
                if (lblStatSO != null) lblStatSO.Text = "• Sales Order: Profil / DB belum aktif";
                if (lblStatPO != null) lblStatPO.Text = "• Purchase Order: Profil / DB belum aktif";
                if (lblStatLog != null) lblStatLog.Text = "• Riwayat Sync / Error: Profil / DB belum aktif";
                if (lblStatTotal != null)
                {
                    lblStatTotal.Text = "• Total Dokumen Transaksi: Database tidak terhubung";
                    lblStatTotal.ForeColor = Color.DarkGray;
                }
                return;
            }

            try
            {
                var summary = dbService.GetTransactionSummary();
                if (lblStatSO != null)
                    lblStatSO.Text = $"• Sales Order: {summary.SalesOrderHeaderCount} Dokumen Header, {summary.SalesOrderDetailCount} Baris Detail";
                if (lblStatPO != null)
                    lblStatPO.Text = $"• Purchase Order: {summary.PurchaseOrderHeaderCount} Dokumen Header, {summary.PurchaseOrderDetailCount} Baris Detail";
                if (lblStatLog != null)
                    lblStatLog.Text = $"• Riwayat Sync / Error: {summary.SyncHistoryCount} Riwayat History, {summary.SyncErrorCount} Riwayat Error";

                if (lblStatTotal != null)
                {
                    lblStatTotal.Text = $"• Total Dokumen Transaksi: {summary.TotalTransactions} Dokumen ({summary.TotalRecords} Total Semua Baris)";
                    if (summary.TotalTransactions == 0)
                    {
                        lblStatTotal.ForeColor = Color.FromArgb(40, 130, 70); // Green
                        lblStatTotal.Text += " (KOSONG / 0)";
                    }
                    else
                    {
                        lblStatTotal.ForeColor = Color.FromArgb(200, 60, 60); // Red
                    }
                }
            }
            catch (Exception ex)
            {
                if (lblStatTotal != null)
                {
                    lblStatTotal.Text = "• Gagal membaca status transaksi: " + ex.Message;
                    lblStatTotal.ForeColor = Color.Red;
                }
            }
        }

        private void btnDeleteAllTx_Click(object sender, EventArgs e)
        {
            var dbService = GetDatabaseService();
            if (dbService == null)
            {
                MessageBox.Show("Koneksi database staging tidak tersedia. Pastikan profil aktif telah dikonfigurasi dengan benar.", "Koneksi Tidak Tersedia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirmResult = MessageBox.Show(
                "PERINGATAN KERAS!\n\n" +
                "Apakah Anda yakin ingin melakukan PENGHAPUSAN TOTAL seluruh data transaksi di Database Staging?\n\n" +
                "Tindakan ini akan menghapus secara permanen:\n" +
                "• Seluruh data Sales Order (Header & Detail)\n" +
                "• Seluruh data Purchase Order (Header & Detail)\n" +
                "• Seluruh data Riwayat Sinkronisasi (TBL_SYNC_HISTORY & TBL_SYNC_ERROR)\n" +
                "• Mereset ID urutan penomoran transaksi menjadi 0\n\n" +
                "Semua transaksi akan hilang dan kembali menjadi 0.\nData yang dihapus TIDAK DAPAT dikembalikan!\n\n" +
                "Lanjutkan proses penghapusan?",
                "Konfirmasi Hapus Total Transaksi",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirmResult != DialogResult.Yes) return;

            var finalConfirm = MessageBox.Show(
                "KONFIRMASI TERAKHIR!\n\n" +
                "Semua transaksi di database staging akan DIHAPUS BERSIH menjadi 0.\n\n" +
                "Klik 'Yes' untuk memulai penghapusan sekarang.",
                "Konfirmasi Terakhir",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Stop,
                MessageBoxDefaultButton.Button2);

            if (finalConfirm != DialogResult.Yes) return;

            try
            {
                Cursor.Current = Cursors.WaitCursor;

                if (dbService.DeleteAllTransactions(out int totalDeleted, out string error))
                {
                    // Bersihkan cache in-memory
                    masterLogList?.Clear();
                    masterPendingList?.Clear();

                    // Refresh tampilan grid
                    RefreshGrid();
                    RefreshPendingGrid();

                    // Refresh ringkasan di tab Debug
                    RefreshDebugSummary();

                    MessageBox.Show(
                        $"Penghapusan total transaksi berhasil dilaksanakan!\n\n" +
                        $"• Total baris data yang dibersihkan: {totalDeleted} baris.\n" +
                        $"• Total transaksi saat ini: 0.\n\n" +
                        "Seluruh tabel transaksi telah dikosongkan.",
                        "Penghapusan Berhasil",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Gagal melakukan penghapusan transaksi:\n" + error,
                        "Error Penghapusan",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Terjadi kesalahan saat menghapus transaksi: " + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        #region CONTEXT MENU & DELETE PER ROW
        private void Grid_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                var grid = sender as DataGridView;
                if (grid != null)
                {
                    grid.ClearSelection();
                    grid.Rows[e.RowIndex].Selected = true;
                }
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            var grid = contextMenuStrip1.SourceControl as DataGridView;
            if (grid == null || grid.SelectedRows.Count == 0 || grid.SelectedRows[0].Index < 0)
            {
                e.Cancel = true;
                return;
            }

            // Opsi Delete hanya ditampilkan jika user telah mencentang checklist di tab Debug
            bool allowDelete = chkEnableRowDelete != null && chkEnableRowDelete.Checked;
            menuItemDeleteRow.Visible = allowDelete;
            separatorDelete.Visible = allowDelete;
        }

        private void menuItemViewDetail_Click(object sender, EventArgs e)
        {
            var grid = contextMenuStrip1.SourceControl as DataGridView;
            if (grid == null || grid.SelectedRows.Count == 0) return;

            var dbService = GetDatabaseService();
            if (dbService == null) return;

            DocumentHeaderLogModel doc = null;
            if (grid == dgvlLogData)
            {
                doc = grid.SelectedRows[0].DataBoundItem as DocumentHeaderLogModel;
            }
            else if (grid == dgvPendingQueue)
            {
                var pending = grid.SelectedRows[0].DataBoundItem as PendingQueueDocModel;
                if (pending != null)
                {
                    doc = new DocumentHeaderLogModel
                    {
                        HeaderId = pending.HeaderId,
                        DocType = pending.DocType,
                        WebTxNumber = pending.WebTxNumber,
                        CardCode = pending.CardCode,
                        CardName = pending.CardName,
                        DocDate = pending.DocDate,
                        DocDueDate = pending.DocDueDate,
                        Status = pending.Status,
                        CreatedAt = pending.CreatedAt,
                        Remarks = pending.Remarks,
                        ErrorMessage = pending.ErrorMessage,
                        UdfDataJson = pending.UdfDataJson
                    };
                }
            }

            if (doc != null)
            {
                using (var frmDetail = new FormDocumentDetail(doc, dbService))
                {
                    frmDetail.ShowDialog(this);
                }
            }
        }

        private void menuItemDeleteRow_Click(object sender, EventArgs e)
        {
            if (chkEnableRowDelete == null || !chkEnableRowDelete.Checked)
            {
                MessageBox.Show(
                    "Fungsi hapus per baris dinonaktifkan.\n\nUntuk mengaktifkannya, silakan centang opsi di tab 'Debug'.",
                    "Fitur Belum Aktif", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var grid = contextMenuStrip1.SourceControl as DataGridView;
            if (grid == null || grid.SelectedRows.Count == 0) return;

            var dbService = GetDatabaseService();
            if (dbService == null)
            {
                MessageBox.Show("Koneksi database staging tidak tersedia.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string docType = "";
            long headerId = 0;
            string webTx = "";
            string docEntry = "";
            string cardInfo = "";
            DateTime docDate = DateTime.MinValue;

            if (grid == dgvlLogData)
            {
                var logDoc = grid.SelectedRows[0].DataBoundItem as DocumentHeaderLogModel;
                if (logDoc == null) return;

                docType = logDoc.DocType;
                headerId = logDoc.HeaderId;
                webTx = logDoc.WebTxNumber;
                docEntry = logDoc.DocEntry;
                cardInfo = $"{logDoc.CardCode} - {logDoc.CardName}".Trim(' ', '-');
                docDate = logDoc.DocDate;
            }
            else if (grid == dgvPendingQueue)
            {
                var pendingDoc = grid.SelectedRows[0].DataBoundItem as PendingQueueDocModel;
                if (pendingDoc == null) return;

                docType = pendingDoc.DocType;
                headerId = pendingDoc.HeaderId;
                webTx = pendingDoc.WebTxNumber;
                docEntry = "";
                cardInfo = $"{pendingDoc.CardCode} - {pendingDoc.CardName}".Trim(' ', '-');
                docDate = pendingDoc.DocDate;
            }

            if (string.IsNullOrEmpty(docType) || headerId <= 0) return;

            var confirm = MessageBox.Show(
                $"Apakah Anda yakin ingin menghapus transaksi {docType} terpilih ini?\n\n" +
                $"• Header ID: {headerId}\n" +
                $"• Web Tx Number: {(string.IsNullOrEmpty(webTx) ? "-" : webTx)}\n" +
                $"• Partner: {(string.IsNullOrEmpty(cardInfo) ? "-" : cardInfo)}\n" +
                $"• Doc Date: {(docDate != DateTime.MinValue ? docDate.ToString("yyyy-MM-dd") : "-")}\n\n" +
                "Data transaksi ini akan dihapus permanen dari database staging!\nLanjutkan?",
                "Konfirmasi Hapus Transaksi",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes) return;

            try
            {
                Cursor.Current = Cursors.WaitCursor;

                if (dbService.DeleteSingleTransaction(docType, headerId, webTx, docEntry, out string err))
                {
                    // Refresh data list & grid
                    LoadLogFromDatabase();
                    RefreshPendingGrid();
                    RefreshDebugSummary();

                    MessageBox.Show(
                        $"Transaksi {docType} (ID: {headerId}) berhasil dihapus dari staging database.",
                        "Berhasil Dihapus",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Gagal menghapus transaksi: " + err, "Error Penghapusan", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Terjadi error saat menghapus transaksi: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }
        #endregion
        #endregion
    }
}