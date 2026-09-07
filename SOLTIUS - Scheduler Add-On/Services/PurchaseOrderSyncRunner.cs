using SOLTIUS_Scheduler_Add_On.Model;
using SOLTIUS_Scheduler_Add_On.Services;
using SOLTIUS_Scheduler_Add_On.UI;
using System;
using System.Collections.Generic;

namespace SOLTIUS_Scheduler_Add_On.Services
{
    /// <summary>
    /// Engine sinkronisasi Purchase Order dari staging ke SAP, tanpa ketergantungan UI.
    /// </summary>
    public static class PurchaseOrderSyncRunner
    {
        /// <summary>
        /// Menjalankan sinkronisasi semua PO pending (SOL_PROCESS_STATUS = 0).
        /// </summary>
        /// <returns>Jumlah dokumen yang gagal.</returns>
        public static int RunPendingSync(AppConfig config, bool isDryRun)
        {
            if (config == null)
                throw new ArgumentNullException("config", "Profil tidak ditemukan.");

            string connString = ConfigService.BuildStagingConnectionString(config);
            if (string.IsNullOrEmpty(connString))
                throw new InvalidOperationException(
                    "Staging hanya mendukung SQL Server. Profil aktif memakai tipe '" + config.ExternalDBType + "'.");

            var dbService = new DatabaseService(connString);
            List<PendingPurchaseOrder> orders = dbService.LoadPendingPurchaseOrders();

            if (orders.Count == 0) return 0;

            int failedCount = 0;
            using (var sapService = new SapSyncService())
            {
                if (!isDryRun)
                    sapService.ConnectToDIAPI(config);

                foreach (var order in orders)
                {
                    // --- Skip if retry limit exceeded ---
                    if (dbService.IsPurchaseOrderRetryLimitExceeded(order.HeaderId))
                    {
                        dbService.MarkPurchaseOrderAsExceededRetryLimit(order.HeaderId);
                        LogSync(dbService, order, "Failed", null, "Skipped: max retry limit exceeded");
                        continue;
                    }

                    try
                    {
                        if (isDryRun)
                        {
                            LogSync(dbService, order, "Success", "DRY-RUN", "Validasi berhasil (Mode Simulasi)");
                        }
                        else
                        {
                            string docEntry = sapService.ExecutePurchaseOrderSync(order);
                            LogSync(dbService, order, "Success", docEntry, "-");
                            dbService.UpdatePurchaseOrderStatus(order.HeaderId, 1, null, docEntry);
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;

                        // Catat kegagalan. UpdatePurchaseOrderStatus(status=2) sekaligus
                        // menaikkan retrycount di tabel staging.
                        dbService.UpdatePurchaseOrderStatus(order.HeaderId, 2, ex.Message);

                        // Setelah retrycount naik, cek apakah sudah tembus batas maksimal.
                        string errMsg = ex.Message;
                        if (dbService.IsPurchaseOrderRetryLimitExceeded(order.HeaderId))
                        {
                            errMsg = "[DEAD-LETTER] " + ex.Message;
                            dbService.MarkPurchaseOrderAsExceededRetryLimit(order.HeaderId);
                        }

                        LogSync(dbService, order, "Failed", null, errMsg);
                    }
                }
            }

            return failedCount;
        }

        /// <summary>
        /// Log sync result with UID. Multi-line: logs once per order (not per line).
        /// </summary>
        private static void LogSync(DatabaseService dbService, PendingPurchaseOrder order, string status, string docEntry, string errorMessage)
        {
            try
            {
                var log = new SyncLogModel
                {
                    DocType = "Purchase Order",
                    DocEntry = docEntry ?? "",
                    CardCode = order.CardCode ?? "",
                    ItemCode = order.Lines.Count > 0 ? order.Lines[0].ItemCode : "",
                    Quantity = order.Lines.Count > 0 ? (double)order.Lines[0].Quantity : 0,
                    Price = order.Lines.Count > 0 ? (double)order.Lines[0].Price : 0,
                    WarehouseCode = order.Lines.Count > 0 ? order.Lines[0].Warehouse : "",
                    Status = status,
                    ErrorSource = status == "Failed" ? "SAP Validation" : "-",
                    ErrorMessage = errorMessage ?? "-",
                    CreatedAt = DateTime.Now
                };

                if (dbService != null)
                    dbService.SaveLogToDatabase(log);
            }
            catch
            {
                // Logging failure must not kill the sync
            }
        }
    }
}
