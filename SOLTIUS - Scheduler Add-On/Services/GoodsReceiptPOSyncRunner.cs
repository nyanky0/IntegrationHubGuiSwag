using SOLTIUS_Scheduler_Add_On.Model;
using SOLTIUS_Scheduler_Add_On.Services;
using SOLTIUS_Scheduler_Add_On.UI;
using System;
using System.Collections.Generic;

namespace SOLTIUS_Scheduler_Add_On.Services
{
    /// <summary>
    /// Engine sinkronisasi Goods Receipt PO (GRPO - PurchaseDeliveryNotes) dari staging ke SAP via Service Layer.
    /// Mematuhi larangan DI API & direct SQL, pemetaan vendor VL, dan validasi BPL 3 ↔ WH-IBT.
    /// </summary>
    public static class GoodsReceiptPOSyncRunner
    {
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
            using (var slClient = isDryRun ? null : new SapServiceLayerClient(config))
            {
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
                        // 1. Resolve vendor code mapping
                        string resolvedCardCode = dbService.ResolveVendorCardCode(order.CardCode);

                        // 2. Validate branch and warehouse pairing
                        int bplId = 3;
                        foreach (var line in order.Lines)
                        {
                            SapServiceLayerClient.ValidateBranchAndWarehouse(bplId, line.Warehouse);
                        }

                        if (isDryRun)
                        {
                            LogSync(dbService, order, "Success", "DRY-RUN", "Validasi GRPO berhasil (Mode Simulasi)");
                        }
                        else
                        {
                            // 3. Build Service Layer payload
                            var linesPayload = new List<object>();
                            foreach (var line in order.Lines)
                            {
                                var lineObj = new Dictionary<string, object>
                                {
                                    { "ItemCode", line.ItemCode },
                                    { "Quantity", (double)line.Quantity },
                                    { "UnitPrice", (double)line.Price },
                                    { "WarehouseCode", line.Warehouse }
                                };
                                if (!string.IsNullOrWhiteSpace(line.VatGroup))
                                {
                                    lineObj["VatGroup"] = line.VatGroup;
                                }
                                if (line.WebLineId.HasValue)
                                {
                                    lineObj["U_SOL_WebLineId"] = line.WebLineId.Value.ToString();
                                }
                                linesPayload.Add(lineObj);
                            }

                            var grpoPayload = new Dictionary<string, object>
                            {
                                { "CardCode", resolvedCardCode },
                                { "DocDate", order.DocDate.ToString("yyyy-MM-dd") },
                                { "DocDueDate", (order.DocDueDate == DateTime.MinValue ? DateTime.Now.AddDays(7) : order.DocDueDate).ToString("yyyy-MM-dd") },
                                { "TaxDate", order.TaxDate.ToString("yyyy-MM-dd") },
                                { "BPL_IDAssignedToInvoice", bplId },
                                { "Comments", string.IsNullOrWhiteSpace(order.Remarks) ? $"GRPO Sync via SOLTIUS Scheduler ({order.WebTxNumber})" : order.Remarks },
                                { "DocumentLines", linesPayload }
                            };

                            if (!string.IsNullOrEmpty(order.WebTxNumber))
                            {
                                grpoPayload["U_SOL_WebTxNumber"] = order.WebTxNumber;
                            }
                            if (order.WebTxId.HasValue)
                            {
                                grpoPayload["U_SOL_WebTxId"] = order.WebTxId.Value.ToString();
                            }

                            // 4. POST to Service Layer
                            string docEntry = slClient.PostDocument("PurchaseDeliveryNotes", grpoPayload);

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
                    ErrorSource = status == "Failed" ? "SAP Service Layer" : "-",
                    ErrorMessage = errorMessage ?? "-",
                    CreatedAt = DateTime.Now
                };

                if (dbService != null)
                    dbService.SaveLogToDatabase(log);
            }
            catch
            {
            }
        }
    }
}
