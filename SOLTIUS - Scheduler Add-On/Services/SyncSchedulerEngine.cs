using System;
using System.IO;
using System.Threading;
using SOLTIUS_Scheduler_Add_On.Model;

namespace SOLTIUS_Scheduler_Add_On.Services
{
    /// <summary>
    /// Engine scheduler headless — tanpa ketergantungan WinForms/UI.
    /// Dipakai oleh Windows Service (service mode) dan bisa di-start dari form.
    /// Menjalankan SalesOrderSyncRunner.RunPendingSync sesuai interval / real-time
    /// dari SchedulerConfig, dan menulis log ke file txt di folder aplikasi.
    /// </summary>
    public class SyncSchedulerEngine
    {
        private readonly object _syncLock = new object();
        private System.Threading.Timer _timer;
        private bool _running;
        private bool _isExecuting; // cegah race saat cycle sebelumnya masih jalan
        private DateTime _lastRunAtUtc = DateTime.MinValue;
        private int _lastFailedCount = 0;

        public bool IsRunning { get { return _running; } }
        public DateTime LastRunAtUtc { get { return _lastRunAtUtc; } }
        public int LastFailedCount { get { return _lastFailedCount; } }
        public bool IsExecuting { get { return _isExecuting; } }

        public event Action<string> LogMessage;

        public static string LogFilePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SchedulerLog.txt"); }
        }

        private void WriteLog(string message)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            try
            {
                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
            catch
            {
                // Gagal tulis log tidak boleh mematikan scheduler
            }
            LogMessage?.Invoke(line);
        }

        /// <summary>
        /// (Re)start loop dengan interval dari config. Membaca ulang config setiap mulai.
        /// Throw bila sudah jalan; panggil Stop() dulu untuk ganti interval.
        /// </summary>
        public void Start(SchedulerConfig config)
        {
            lock (_syncLock)
            {
                if (_running) throw new InvalidOperationException("Scheduler sudah berjalan. Stop dulu sebelum mengubah interval.");
                _running = true;
            }

            int intervalMs = config.ActiveIntervalSeconds * 1000;
            WriteLog($"Scheduler dimulai. Mode={config.Mode}, interval={config.ActiveIntervalSeconds}s.");

            // Timer periodik. DueTime = interval (tidak langsung, agar cycle pertama sesuai jadwal),
            // lalu jarak antar-tick = interval.
            _timer = new System.Threading.Timer(OnTick, null, intervalMs, intervalMs);
        }

        public void Stop()
        {
            lock (_syncLock)
            {
                if (!_running) return;
                _running = false;
            }

            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
            WriteLog("Scheduler dihentikan.");
        }

        /// <summary>Jalankan sinkronisasi sekali, tidak peduli interval.</summary>
        public void RunNow()
        {
            OnTick(null);
        }

        private void OnTick(object state)
        {
            if (_isExecuting) return; // masih jalan dari cycle sebelumnya — lewati
            ExecuteSyncCycle();
        }

        private void ExecuteSyncCycle()
        {
            _isExecuting = true;
            _lastRunAtUtc = DateTime.UtcNow;

            try
            {
                SchedulerConfig config = SchedulerConfig.Load();
                WriteLog($"Cycle dimulai. Profil aktif: {new ConfigService().GetActiveProfileName() ?? "-"}");

                if (!config.SyncSalesOrder && !config.SyncPurchaseOrder && !config.SyncServiceLayer)
                {
                    WriteLog("Semua fungsi mati (Sales Order, Purchase Order & Service Layer nonaktif) — cycle dilewati.");
                    return;
                }

                var runnerConfig = new ConfigService().GetActiveConfiguration();
                if (runnerConfig == null)
                {
                    WriteLog("Tidak ada profil aktif (DefaultConfig.xml kosong) — cycle dilewati.");
                    return;
                }

                int failed = 0;
                if (config.SyncSalesOrder)
                {
                    int soFailed = SalesOrderSyncRunner.RunPendingSync(runnerConfig, isDryRun: false);
                    failed += soFailed;
                    WriteLog(soFailed == 0
                        ? "Sync Sales Order selesai — semua dokumen pending tersinkronisasi."
                        : $"Sync Sales Order selesai — {soFailed} dokumen gagal.");
                }

                if (config.SyncPurchaseOrder)
                {
                    int poFailed = PurchaseOrderSyncRunner.RunPendingSync(runnerConfig, isDryRun: false);
                    failed += poFailed;
                    WriteLog(poFailed == 0
                        ? "Sync Purchase Order selesai — semua dokumen pending tersinkronisasi."
                        : $"Sync Purchase Order selesai — {poFailed} dokumen gagal.");
                }

                _lastFailedCount = failed;
            }
            catch (Exception ex)
            {
                WriteLog("ERROR: " + ex.Message);
            }
            finally
            {
                _isExecuting = false;
            }
        }
    }
}