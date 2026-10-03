using SOLTIUS_Scheduler_Add_On.Model;
using SOLTIUS_Scheduler_Add_On.Services;
using SOLTIUS_Scheduler_Add_On.UI;
using System;
using System.Collections.Generic;

namespace SOLTIUS_Scheduler_Add_On.Services
{
    /// <summary>
    /// Engine sinkronisasi Stock Transfer / Packing List (OWTR - StockTransfers) dari staging ke SAP via Service Layer.
    /// Mematuhi larangan DI API & direct SQL, serta validasi integritas gudang dan BPLID.
    /// </summary>
    public static class StockTransferSyncRunner
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
            List<PendingPurchaseOrder> orders = dbService.LoadPendingStockTransfers();

            if (orders.Count == 0) return 0;

            int failedCount = 0;
            using (var slClient = isDryRun ? null : new SapServiceLayerClient(config))
            {
                foreach (var order in orders)
                {
                    // Skip if retry limit exceeded
                    if (dbService.IsStockTransferRetryLimitExceeded(order.HeaderId))
                    {
                        dbService.MarkStockTransferAsExceededRetryLimit(order.HeaderId);
                        LogSync(dbService, order, "Failed", null, "Skipped: max retry limit exceeded");
                        continue;
                    }

                    try
                    {
                        if (isDryRun)
                        {
                            LogSync(dbService, order, "Success", "DRY-RUN", "Validasi Stock Transfer berhasil (Mode Simulasi)");
                        }
                        else
                        {
                            // 1. Determine from and to warehouses
                            string fromWhs = order.Lines.Count > 0 && !string.IsNullOrEmpty(order.Lines[0].FromWarehouse)
                                ? order.Lines[0].FromWarehouse
                                : "WH-IBT";
                            string toWhs = order.Lines.Count > 0 && !string.IsNullOrEmpty(order.Lines[0].Warehouse)
                                ? order.Lines[0].Warehouse
                                : "WH-IBT";

                            int bplId = 3; // PT Indobaruna Bulk Transport
                            SapServiceLayerClient.ValidateBranchAndWarehouse(bplId, fromWhs);
                            SapServiceLayerClient.ValidateBranchAndWarehouse(bplId, toWhs);

                            // 2. Build Service Layer payload
                            var linesPayload = new List<object>();
                            foreach (var line in order.Lines)
                            {
                                string lineFrom = !string.IsNullOrEmpty(line.FromWarehouse) ? line.FromWarehouse : fromWhs;
                                string lineTo = !string.IsNullOrEmpty(line.Warehouse) ? line.Warehouse : toWhs;

                                var lineObj = new Dictionary<string, object>
                                {
                                    { "ItemCode", line.ItemCode },
                                    { "Quantity", (double)line.Quantity },
                                    { "FromWarehouseCode", lineFrom },
                                    { "WarehouseCode", lineTo }
                                };
                                if (line.WebLineId.HasValue)
                                {
                                    lineObj["U_SOL_WebLineId"] = line.WebLineId.Value.ToString();
                                }
                                linesPayload.Add(lineObj);
                            }

                            var transferPayload = new Dictionary<string, object>
                            {
                                { "DocDate", order.DocDate.ToString("yyyy-MM-dd") },
                                { "DueDate", (order.DocDueDate == DateTime.MinValue ? DateTime.Now : order.DocDueDate).ToString("yyyy-MM-dd") },
                                { "TaxDate", order.TaxDate.ToString("yyyy-MM-dd") },
                                { "FromWarehouse", fromWhs },
                                { "ToWarehouse", toWhs },
                                { "BPLID", bplId },
                                { "Comments", string.IsNullOrWhiteSpace(order.Remarks) ? $"Stock Transfer Sync via SOLTIUS Scheduler ({order.WebTxNumber})" : order.Remarks },
                                { "StockTransferLines", linesPayload }
                            };

                            if (!string.IsNullOrEmpty(order.WebTxNumber))
                            {
                                transferPayload["U_SOL_WebTxNumber"] = order.WebTxNumber;
                            }
                            if (order.WebTxId.HasValue)
                            {
                                transferPayload["U_SOL_WebTxId"] = order.WebTxId.Value.ToString();
                            }

                            // 3. POST to Service Layer
                            string docEntry = slClient.PostDocument("StockTransfers", transferPayload);

                            LogSync(dbService, order, "Success", docEntry, "-");
                            dbService.UpdateStockTransferStatus(order.HeaderId, 1, null, docEntry);

                            // Asynchronous Webhook Callback ke Web Laravel
                            try
                            {
                                var schedConfig = SchedulerConfig.Load();
                                if (schedConfig.EnableWebhookCallback)
                                {
                                    WebhookCallbackService.SendDocEntryCallbackAsync(
                                        schedConfig.WebhookUrl,
                                        schedConfig.WebhookSecret,
                                        "Stock Transfer",
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

                        dbService.UpdateStockTransferStatus(order.HeaderId, 2, ex.Message);

                        string errMsg = ex.Message;
                        if (dbService.IsStockTransferRetryLimitExceeded(order.HeaderId))
                        {
                            errMsg = "[DEAD-LETTER] " + ex.Message;
                            dbService.MarkStockTransferAsExceededRetryLimit(order.HeaderId);
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
                    DocType = "Stock Transfer",
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
