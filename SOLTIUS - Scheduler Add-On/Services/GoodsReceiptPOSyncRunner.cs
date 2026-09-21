using SOLTIUS_Scheduler_Add_On.Model;
using SOLTIUS_Scheduler_Add_On.Services;
using SOLTIUS_Scheduler_Add_On.UI;
using System;
using System.Collections.Generic;

namespace SOLTIUS_Scheduler_Add_On.Services
{
    /// <summary>
    /// Engine sinkronisasi Goods Receipt PO (GRPO - OPDN) dari staging ke SAP B1, tanpa ketergantungan UI.
    /// Mengikuti alur procurement resmi Web App IBT.
    /// </summary>
    public static class GoodsReceiptPOSyncRunner
    {
        /// <summary>
        /// Menjalankan sinkronisasi semua GRPO pending (SOL_PROCESS_STATUS = 0).
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
            List<PendingPurchaseOrder> orders = dbService.LoadPendingGoodsReceiptPOs();

            if (orders.Count == 0) return 0;

            int failedCount = 0;
            using (var sapService = new SapSyncService())
            {
                if (!isDryRun)
                    sapService.ConnectToDIAPI(config);

                foreach (var order in orders)
                {
                    // Skip if retry limit exceeded
                    if (dbService.IsGoodsReceiptPORetryLimitExceeded(order.HeaderId))
                    {
                        dbService.MarkGoodsReceiptPOAsExceededRetryLimit(order.HeaderId);
                        LogSync(dbService, order, "Failed", null, "Skipped: max retry limit exceeded");
                        continue;
                    }

                    try
                    {
                        if (isDryRun)
                        {
                            LogSync(dbService, order, "Success", "DRY-RUN", "Validasi GRPO berhasil (Mode Simulasi)");
                        }
                        else
                        {
                            string docEntry = sapService.ExecuteGoodsReceiptPOSync(order);
                            LogSync(dbService, order, "Success", docEntry, "-");
                            dbService.UpdateGoodsReceiptPOStatus(order.HeaderId, 1, null, docEntry);

                            // Asynchronous Webhook Callback ke Web Laravel
                            try
                            {
                                var schedConfig = SchedulerConfig.Load();
                                if (schedConfig.EnableWebhookCallback)
                                {
                                    WebhookCallbackService.SendDocEntryCallbackAsync(
                                        schedConfig.WebhookUrl,
                                        schedConfig.WebhookSecret,
                                        "Goods Receipt PO",
                                        docEntry,
                                        docEntry,
                                        order.WebTxNumber,
                                        order.WebTxId
                                    );
                                }
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;

                        dbService.UpdateGoodsReceiptPOStatus(order.HeaderId, 2, ex.Message);

                        string errMsg = ex.Message;
                        if (dbService.IsGoodsReceiptPORetryLimitExceeded(order.HeaderId))
                        {
                            errMsg = "[DEAD-LETTER] " + ex.Message;
                            dbService.MarkGoodsReceiptPOAsExceededRetryLimit(order.HeaderId);
                        }

                        LogSync(dbService, order, "Failed", null, errMsg);
                    }
                }
            }

            return failedCount;
        }

        private static void LogSync(DatabaseService dbService, PendingPurchaseOrder order, string status, string docEntry, string errorMessage)
        {
            try
            {
                var log = new SyncLogModel
                {
                    DocType = "Goods Receipt PO",
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
