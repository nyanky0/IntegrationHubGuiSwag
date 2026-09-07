using System;
using System.Drawing;
using System.IO;
using System.ServiceProcess;
using System.Windows.Forms;
using SOLTIUS_Scheduler_Add_On.Model;
using SOLTIUS_Scheduler_Add_On.Services;

namespace SOLTIUS_Scheduler_Add_On.UI
{
    /// <summary>
    /// Form kelola scheduler: atur mode waktu (interval menit / real-time polling),
    /// pilih fungsi yang dijalankan, lalu install/start/stop Windows Service otomatis.
    /// Service dibuat dari exe ini sendiri (dual-mode exe) via sc.exe (butuh admin/UAC).
    /// </summary>
    public class FormSettingScheduler : Form
    {
        private RadioButton rbInterval;
        private RadioButton rbRealtime;
        private NumericUpDown numIntervalMin;
        private NumericUpDown numRealtimeSec;
        private CheckBox chkSO;
        private CheckBox chkPO;
        private CheckBox chkSL;
        private TextBox txtServiceName;
        private Label lblServiceStatus;
        private Label lblEngineStatus;
        private Label lblLastRun;
        private Button btnSave;
        private Button btnInstallService;
        private Button btnUninstall;
        private Button btnStartService;
        private Button btnStopService;
        private Button btnRefreshStatus;
        private Button btnRunNow;
        private System.Windows.Forms.Timer statusTimer;

        private SchedulerConfig _config;
        private readonly SyncSchedulerEngine _engine;

        public FormSettingScheduler()
        {
            _engine = new SyncSchedulerEngine();
            _config = SchedulerConfig.Load();
            BuildUI();
            LoadConfigToUI();
            RefreshServiceStatus();
        }

        #region UI CONSTRUCTION
        private void BuildUI()
        {
            UITheme.ApplyForm(this);
            this.Text = "Setting Scheduler";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new Size(520, 600);

            // ===== Judul =====
            var lblTitle = new Label
            {
                Text = "Scheduler Setting",
                AutoSize = true,
                Location = new Point(24, 18),
                Font = UITheme.FontTitle,
                ForeColor = UITheme.Primary
            };

            // ===== Group: Mode Waktu =====
            var grpMode = new GroupBox { Text = "Mode Waktu", Location = new Point(24, 60), Size = new Size(472, 130) };
            UITheme.ApplyGroup(grpMode);

            rbInterval = new RadioButton { Text = "Interval — jalankan tiap", Location = new Point(16, 30), AutoSize = true };
            numIntervalMin = new NumericUpDown { Location = new Point(236, 28), Width = 70, Minimum = 1, Maximum = 1440, Value = 5 };
            var lblMin = new Label { Text = "menit", AutoSize = true, Location = new Point(312, 31) };

            rbRealtime = new RadioButton { Text = "Real-time — polling tiap", Location = new Point(16, 62), AutoSize = true };
            numRealtimeSec = new NumericUpDown { Location = new Point(236, 60), Width = 70, Minimum = 5, Maximum = 3600, Value = 10 };
            var lblSec = new Label { Text = "detik", AutoSize = true, Location = new Point(312, 63) };

            var lblModeNote = new Label
            {
                Text = "Real-time memeriksa staging DB secara terus-menerus sampai menemukan data pending.",
                AutoSize = false, Size = new Size(440, 30), Location = new Point(16, 92),
                ForeColor = UITheme.TextMuted, Font = UITheme.FontSmall
            };

            UITheme.ApplyRadio(rbInterval);
            UITheme.ApplyRadio(rbRealtime);
            UITheme.ApplyLabel(lblMin, muted: true);
            UITheme.ApplyLabel(lblSec, muted: true);
            UITheme.ApplyLabel(lblModeNote, muted: true);

            grpMode.Controls.AddRange(new Control[] { rbInterval, numIntervalMin, lblMin, rbRealtime, numRealtimeSec, lblSec, lblModeNote });

            rbInterval.CheckedChanged += (s, e) => UpdateNumericEnable();

            // ===== Group: Fungsi =====
            var grpFunc = new GroupBox { Text = "Fungsi", Location = new Point(24, 200), Size = new Size(472, 115) };
            UITheme.ApplyGroup(grpFunc);

            chkSO = new CheckBox { Text = "Sync Sales Order (ORDR)", Location = new Point(16, 25), AutoSize = true, Checked = true };
            chkPO = new CheckBox { Text = "Sync Purchase Order (OPOR)", Location = new Point(16, 52), AutoSize = true, Checked = true };
            chkSL = new CheckBox { Text = "Sync Service Layer (OSL) — belum diimplementasikan", Location = new Point(16, 79), AutoSize = true, Enabled = false };

            UITheme.ApplyCheck(chkSO);
            UITheme.ApplyCheck(chkPO);
            UITheme.ApplyCheck(chkSL);

            grpFunc.Controls.AddRange(new Control[] { chkSO, chkPO, chkSL });

            // ===== Group: Windows Service =====
            var grpSvc = new GroupBox { Text = "Windows Service", Location = new Point(24, 325), Size = new Size(472, 190) };
            UITheme.ApplyGroup(grpSvc);

            var lblSvcName = new Label { Text = "Nama Service", AutoSize = true, Location = new Point(16, 32) };
            txtServiceName = new TextBox { Text = _config.ServiceName, Location = new Point(150, 28), Width = 300 };
            UITheme.ApplyLabel(lblSvcName, muted: true);
            UITheme.ApplyTextBox(txtServiceName);

            lblServiceStatus = new Label { Text = "Status: —", Location = new Point(16, 60), AutoSize = true };
            UITheme.ApplyLabel(lblServiceStatus);

            btnInstallService = new Button { Text = "Install Service", Location = new Point(16, 88), Size = new Size(140, 30) };
            btnUninstall = new Button { Text = "Uninstall", Location = new Point(164, 88), Size = new Size(140, 30) };
            btnStartService = new Button { Text = "Start Service", Location = new Point(16, 124), Size = new Size(140, 30) };
            btnStopService = new Button { Text = "Stop Service", Location = new Point(164, 124), Size = new Size(140, 30) };
            btnRefreshStatus = new Button { Text = "Refresh Status", Location = new Point(312, 124), Size = new Size(136, 30) };

            UITheme.ApplyPrimary(btnInstallService);
            UITheme.ApplySecondary(btnUninstall);
            UITheme.ApplyPrimary(btnStartService);
            UITheme.ApplySecondary(btnStopService);
            UITheme.ApplySecondary(btnRefreshStatus);

            grpSvc.Controls.AddRange(new Control[]
            {
                lblSvcName, txtServiceName, lblServiceStatus,
                btnInstallService, btnUninstall, btnStartService, btnStopService, btnRefreshStatus
            });

            // ===== Status engine + aksi =====
            lblEngineStatus = new Label { Text = "Engine: idle", Location = new Point(24, 522), AutoSize = true };
            lblLastRun = new Label { Text = "", Location = new Point(24, 542), AutoSize = true };
            UITheme.ApplyLabel(lblEngineStatus);
            UITheme.ApplyLabel(lblLastRun, muted: true);

            btnRunNow = new Button { Text = "Run Now", Location = new Point(360, 518), Size = new Size(136, 30) };
            btnSave = new Button { Text = "Simpan", Location = new Point(360, 556), Size = new Size(136, 30) };
            UITheme.ApplySecondary(btnRunNow);
            UITheme.ApplyPrimary(btnSave);

            this.Controls.AddRange(new Control[]
            {
                lblTitle, grpMode, grpFunc, grpSvc,
                lblEngineStatus, lblLastRun, btnRunNow, btnSave
            });

            // ===== Events =====
            btnSave.Click += (s, e) => SaveConfig();
            btnRunNow.Click += (s, e) => RunNow();
            btnInstallService.Click += (s, e) => InstallService();
            btnUninstall.Click += (s, e) => UninstallService();
            btnStartService.Click += (s, e) => ControlService(false);
            btnStopService.Click += (s, e) => ControlService(true);
            btnRefreshStatus.Click += (s, e) => RefreshServiceStatus();

            // Timer refresh status service + engine tiap 3 detik
            statusTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            statusTimer.Tick += (s, e) => { RefreshEngineStatus(); RefreshServiceStatusQuiet(); };
            statusTimer.Start();

            this.FormClosing += (s, e) => statusTimer.Stop();
        }

        private void UpdateNumericEnable()
        {
            numIntervalMin.Enabled = rbInterval.Checked;
            numRealtimeSec.Enabled = rbRealtime.Checked;
        }
        #endregion

        #region CONFIG LOAD/SAVE
        private void LoadConfigToUI()
        {
            bool realtime = string.Equals(_config.Mode, "Realtime", StringComparison.OrdinalIgnoreCase);
            rbRealtime.Checked = realtime;
            rbInterval.Checked = !realtime;
            numIntervalMin.Value = Math.Max(1, _config.IntervalMinutes);
            numRealtimeSec.Value = Math.Max(5, _config.RealtimeSeconds);
            chkSO.Checked = _config.SyncSalesOrder;
            chkPO.Checked = _config.SyncPurchaseOrder;
            chkSL.Checked = _config.SyncServiceLayer;
            txtServiceName.Text = string.IsNullOrWhiteSpace(_config.ServiceName) ? "SOLTIUSSchedulerService" : _config.ServiceName;
            UpdateNumericEnable();
            RefreshEngineStatus();
        }

        private void SaveConfig()
        {
            _config.Mode = rbRealtime.Checked ? "Realtime" : "Interval";
            _config.IntervalMinutes = (int)numIntervalMin.Value;
            _config.RealtimeSeconds = (int)numRealtimeSec.Value;
            _config.SyncSalesOrder = chkSO.Checked;
            _config.SyncPurchaseOrder = chkPO.Checked;
            _config.SyncServiceLayer = chkSL.Checked;
            _config.ServiceName = txtServiceName.Text.Trim();
            if (string.IsNullOrWhiteSpace(_config.ServiceName))
            {
                MessageBox.Show("Nama service tidak boleh kosong.", "Setting Scheduler",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _config.Save();

            string intervalDesc = _config.Mode == "Realtime"
                ? $"setiap {_config.RealtimeSeconds} detik"
                : $"setiap {_config.IntervalMinutes} menit";
            MessageBox.Show($"Setting tersimpan.\n\nMode: {_config.Mode}\nInterval: {intervalDesc}\nSales Order: {(chkSO.Checked ? "Aktif" : "Nonaktif")}\nPurchase Order: {(chkPO.Checked ? "Aktif" : "Nonaktif")}\n\n" +
                "Perubahan interval berlaku saat service di-restart (Stop → Start).",
                "Setting Scheduler", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion

        #region ENGINE (IN-APP)
        private void RefreshEngineStatus()
        {
            lblEngineStatus.Text = _engine.IsExecuting
                ? "Engine: sedang menjalankan sync..."
                : (_engine.IsRunning ? "Engine: berjalan (background)" : "Engine: idle");
            lblLastRun.Text = _engine.LastRunAtUtc == DateTime.MinValue
                ? "Belum pernah jalan."
                : $"Terakhir jalan: {_engine.LastRunAtUtc.ToLocalTime():HH:mm:ss} — dokumen gagal: {_engine.LastFailedCount}";
        }

        private void RunNow()
        {
            try
            {
                lblEngineStatus.Text = "Engine: menjalankan sync manual...";
                System.Threading.Tasks.Task.Run(() =>
                {
                    _engine.RunNow();
                    this.BeginInvoke(new Action(RefreshEngineStatus));
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menjalankan sync: " + ex.Message, "Run Now",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region SERVICE MANAGEMENT
        /// <summary>Status service: 0=tidak terinstal, 1=stopped, 2=running, 3=lainnya.</summary>
        private int GetServiceStatusCode(out string detail)
        {
            detail = "";
            try
            {
                using (var sc = new ServiceController(txtServiceName.Text.Trim()))
                {
                    sc.Refresh();
                    detail = "Status: " + sc.Status;
                    switch (sc.Status)
                    {
                        case ServiceControllerStatus.Running: return 2;
                        case ServiceControllerStatus.Stopped: return 1;
                        default: return 3;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                detail = "Status: belum terinstal";
                return 0;
            }
            catch (Exception ex)
            {
                detail = "Status: error (" + ex.Message + ")";
                return 3;
            }
        }

        private void RefreshServiceStatus()
        {
            int code = GetServiceStatusCode(out string detail);
            lblServiceStatus.Text = detail;
            lblServiceStatus.ForeColor = code == 2 ? UITheme.Success : (code == 0 ? UITheme.Danger : UITheme.Text);
        }

        private void RefreshServiceStatusQuiet()
        {
            // Update status service tanpa mengganti warna — hanya saat form aktif
            if (!this.Visible) return;
            GetServiceStatusCode(out string detail);
            lblServiceStatus.Text = detail;
        }

        /// <summary>
        /// Install service via sc.exe (butuh admin — UAC). Exe ini dual-mode:
        /// saat dijalankan SCM, Environment.UserInteractive=false → ServiceBase.Run.
        /// </summary>
        private void InstallService()
        {
            // Simpan config dulu + pastikan ServiceName dipakai
            _config.ServiceName = txtServiceName.Text.Trim();
            _config.Save();

            string exePath = Application.ExecutablePath;
            string serviceName = _config.ServiceName;

            var confirm = MessageBox.Show(
                $"Akan membuat Windows Service:\n\nNama : {serviceName}\nEXE  : {exePath}\n\n" +
                "Proses ini butuh hak Administrator (UAC akan muncul). Lanjutkan?",
                "Install Service", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                // sc create <name> binPath= "<exe>" start= auto
                string args = $"create \"{serviceName}\" binPath= \"\\\"{exePath}\\\"\" start= auto DisplayName= \"SOLTIUS Scheduler\"";
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = args,
                    UseShellExecute = true,
                    Verb = "runas", // elevate → UAC
                    CreateNoWindow = false
                };
                using (var proc = System.Diagnostics.Process.Start(psi))
                {
                    proc.WaitForExit();
                }
                RefreshServiceStatus();
                MessageBox.Show("Service berhasil dibuat. Klik 'Start Service' untuk menjalankannya.",
                    "Install Service", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Install gagal: " + ex.Message + "\n\nPastikan aplikasi dijalankan sebagai Administrator.",
                    "Install Service", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UninstallService()
        {
            var confirm = MessageBox.Show(
                $"Hapus Windows Service '{txtServiceName.Text.Trim()}'? Service harus dalam keadaan berhenti.",
                "Uninstall Service", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            try
            {
                string args = $"delete \"{txtServiceName.Text.Trim()}\"";
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = args,
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = false
                };
                using (var proc = System.Diagnostics.Process.Start(psi))
                {
                    proc.WaitForExit();
                }
                RefreshServiceStatus();
                MessageBox.Show("Service dihapus.", "Uninstall Service", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Uninstall gagal: " + ex.Message + "\n\nPastikan aplikasi dijalankan sebagai Administrator.",
                    "Uninstall Service", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>stop=true → stop service; stop=false → start service. Butuh admin (UAC).</summary>
        private void ControlService(bool stop)
        {
            string serviceName = txtServiceName.Text.Trim();
            try
            {
                string action = stop ? "stop" : "start";
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"{action} \"{serviceName}\"",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = false
                };
                using (var proc = System.Diagnostics.Process.Start(psi))
                {
                    proc.WaitForExit();
                }
                RefreshServiceStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show((stop ? "Stop" : "Start") + " service gagal: " + ex.Message,
                    "Service", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion
    }
}