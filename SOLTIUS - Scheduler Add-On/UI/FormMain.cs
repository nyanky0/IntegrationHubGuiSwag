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
            RefreshGrid();
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

            this.tabControl1.SelectedIndexChanged += tabControl1_SelectedIndexChanged;

            if (txtSearchLog != null) this.txtSearchLog.TextChanged += (s, e) => RefreshGrid();
            if (btnExportExcel != null) this.btnExportExcel.Click += btnExportExcel_Click;
            if (dgvlLogData != null) this.dgvlLogData.CellDoubleClick += dgvlLogData_CellDoubleClick;
            if (exportProfilesToolStripMenuItem != null) this.exportProfilesToolStripMenuItem.Click += ExportProfiles_Click;
            if (importProfilesToolStripMenuItem != null) this.importProfilesToolStripMenuItem.Click += ImportProfiles_Click;
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
        }
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
            // btnRetryFailed.Enabled = isEnabled; // Uncomment jika menggunakan retry
            if (isEnabled) pBar.Value = 0;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            // Terapkan theme konsisten
            ApplyTheme();

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

            UITheme.ApplyGrid(dgvlLogData);
            UITheme.ApplyProgress(pBar);
            UITheme.ApplyGroup(grpBox1);
            UITheme.ApplyGroup(grpBox1_1);
            UITheme.ApplyCombo(cbLogLevel);
            UITheme.ApplyCombo(cbFunction);
            UITheme.ApplyTextBox(txtSearchLog);
            UITheme.ApplyLabel(label8, muted: true);

            UITheme.ApplyCheck(chkDryRun);
            UITheme.ApplyCheck(chkSO);
            UITheme.ApplyCheck(chkSL);
            UITheme.ApplyCheck(chkLogData);
            UITheme.ApplyCheck(chkAll1);

            tabControl1.SelectedTab = tabsync;
        }
    }
}