using System;
using System.IO;
using System.Xml.Serialization;

namespace SOLTIUS_Scheduler_Add_On.Model
{
    /// <summary>
    /// Konfigurasi scheduler: mode waktu (interval / real-time) dan fungsi yang dijalankan.
    /// Disimpan ke SchedulerSettings.xml di folder aplikasi. Dibaca oleh Windows Service
    /// dan juga dipakai untuk membuat file service saat user klik "Install Service".
    /// </summary>
    [Serializable]
    public class SchedulerConfig
    {
        /// <summary>"Interval" = jalankan tiap X menit. "Realtime" = polling tiap X detik.</summary>
        public string Mode { get; set; } = "Interval";

        /// <summary>Jarak antar-run dalam menit bila Mode == Interval. Min 1.</summary>
        public int IntervalMinutes { get; set; } = 5;

        /// <summary>Jarak antar-run dalam detik bila Mode == Realtime. Min 5.</summary>
        public int RealtimeSeconds { get; set; } = 10;

        /// <summary>Jalankan sinkronisasi Purchase Order (OPOR).</summary>
        public bool SyncPurchaseOrder { get; set; } = true;

        /// <summary>Jalankan sinkronisasi Goods Receipt PO (OPDN).</summary>
        public bool SyncGoodsReceiptPO { get; set; } = true;

        /// <summary>Jalankan sinkronisasi Stock Transfer (OWTR).</summary>
        public bool SyncStockTransfer { get; set; } = true;

        /// <summary>Jalankan sinkronisasi Service Layer. (Belum diimplementasi di engine.)</summary>
        public bool SyncServiceLayer { get; set; } = false;

        /// <summary>Nama service Windows yang dibuat otomatis oleh form.</summary>
        public string ServiceName { get; set; } = "SOLTIUSSchedulerService";

        /// <summary>Path exe aplikasi ini (target binPath service). Diisi saat install.</summary>
        public string ExecutablePath { get; set; } = "";

        /// <summary>URL Endpoint Webhook Web Laravel untuk callback DocEntry, Heartbeat, dan Reconcile.</summary>
        public string WebhookUrl { get; set; } = "http://localhost:8000/api/v1/sap/webhooks";

        /// <summary>Secret key HMAC-SHA256 untuk verifikasi webhook.</summary>
        public string WebhookSecret { get; set; } = "";

        /// <summary>Aktifkan asynchronous webhook callback saat DocEntry SAP terbit.</summary>
        public bool EnableWebhookCallback { get; set; } = true;

        /// <summary>Aktifkan periodic heartbeat beacon ke Web Laravel.</summary>
        public bool EnableHeartbeat { get; set; } = true;

        /// <summary>Interval pengiriman heartbeat beacon (menit).</summary>
        public int HeartbeatIntervalMinutes { get; set; } = 5;

        /// <summary>Aktifkan Two-Way Status Reconciliation (Downsync Closed/Canceled dari SAP ke Web).</summary>
        public bool EnableReconciliation { get; set; } = true;

        /// <summary>Batas maksimal percobaan retry sebelum dipindahkan ke Dead-Letter Queue (status 3).</summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// Berapa detik interval yang aktif ditentukan mode aktif.
        /// Interval menit dikonversi ke detik.
        /// </summary>
        [XmlIgnore]
        public int ActiveIntervalSeconds
        {
            get
            {
                bool realtime = string.Equals(Mode, "Realtime", StringComparison.OrdinalIgnoreCase);
                return realtime
                    ? Math.Max(5, RealtimeSeconds)
                    : Math.Max(60, IntervalMinutes * 60);
            }
        }

        public static string FilePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SchedulerSettings.xml"); }
        }

        public static SchedulerConfig Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var serializer = new XmlSerializer(typeof(SchedulerConfig));
                    using (var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        return (SchedulerConfig)serializer.Deserialize(fs);
                    }
                }
            }
            catch
            {
                // Rusak/tidak terbaca -> pakai default
            }
            return new SchedulerConfig();
        }

        public void Save()
        {
            var serializer = new XmlSerializer(typeof(SchedulerConfig));
            using (var writer = new StreamWriter(FilePath))
            {
                serializer.Serialize(writer, this);
            }
        }
    }
}