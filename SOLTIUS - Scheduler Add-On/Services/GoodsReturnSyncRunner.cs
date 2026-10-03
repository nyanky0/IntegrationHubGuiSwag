using SOLTIUS_Scheduler_Add_On.Model;
using SOLTIUS_Scheduler_Add_On.Services;
using SOLTIUS_Scheduler_Add_On.UI;
using System;
using System.Collections.Generic;

namespace SOLTIUS_Scheduler_Add_On.Services
{
    /// <summary>
    /// Engine sinkronisasi Goods Return (ORPD - PurchaseReturns) dari staging ke SAP via Service Layer.
    /// Mematuhi larangan DI API & direct SQL, pemetaan vendor VL, dan validasi BPL 3 ↔ WH-IBT.
    /// </summary>
    public static class GoodsReturnSyncRunner
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
            List<PendingPurchaseOrder> returns = dbService.LoadPendingGoodsReturns();

            if (returns.Count == 0) return 0;

            int failedCount = 0;
            using (var slClient = isDryRun ? null : new SapServiceLayerClient(config))
            {
                foreach (var ret in returns)
                {
                    // Skip if retry limit exceeded
                    if (dbService.IsGoodsReturnRetryLimitExceeded(ret.HeaderId))
                    {
                        dbService.MarkGoodsReturnAsExceededRetryLimit(ret.HeaderId);
                        LogSync(dbService, ret, "Failed", null, "Skipped: max retry limit exceeded");
                        continue;
                    }

                    try
                    {
                        // 1. Resolve vendor code mapping
                        string resolvedCardCode = dbService.ResolveVendorCardCode(ret.CardCode);

                        // 2. Validate branch and warehouse pairing
                        int bplId = 3;
                        foreach (var line in ret.Lines)
                        {
                            SapServiceLayerClient.ValidateBranchAndWarehouse(bplId, line.Warehouse);
                        }

                        if (isDryRun)
                        {
                            LogSync(dbService, ret, "Success", "DRY-RUN", "Validasi Goods Return berhasil (Mode Simulasi)");
                        }
                        else
                        {
                            // 3. Build Service Layer payload
                            var linesPayload = new List<object>();
                            foreach (var line in ret.Lines)
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

                            var returnPayload = new Dictionary<string, object>
                            {
                                { "CardCode", resolvedCardCode },
                                { "DocDate", ret.DocDate.ToString("yyyy-MM-dd") },
                                { "DocDueDate", (ret.DocDueDate == DateTime.MinValue ? DateTime.Now.AddDays(7) : ret.DocDueDate).ToString("yyyy-MM-dd") },
                                { "TaxDate", ret.TaxDate.ToString("yyyy-MM-dd") },
                                { "BPL_IDAssignedToInvoice", bplId },
                                { "Comments", string.IsNullOrWhiteSpace(ret.Remarks) ? $"Goods Return Sync via SOLTIUS Scheduler ({ret.WebTxNumber})" : ret.Remarks },
                                { "DocumentLines", linesPayload }
                            };

                            if (!string.IsNullOrEmpty(ret.WebTxNumber))
                            {
                                returnPayload["U_SOL_WebTxNumber"] = ret.WebTxNumber;
                            }
                            if (ret.WebTxId.HasValue)
                            {
                                returnPayload["U_SOL_WebTxId"] = ret.WebTxId.Value.ToString();
                            }

                            // 4. POST to Service Layer
                            string docEntry = slClient.PostDocument("PurchaseReturns", returnPayload);

                            LogSync(dbService, ret, "Success", docEntry, "-");
                            dbService.UpdateGoodsReturnStatus(ret.HeaderId, 1, null, docEntry);

                            // Asynchronous Webhook Callback ke Web Laravel
                            try
                            {
                                var schedConfig = SchedulerConfig.Load();
                                if (schedConfig.EnableWebhookCallback)
                                {
                                    WebhookCallbackService.SendDocEntryCallbackAsync(
                                        schedConfig.WebhookUrl,
                                        schedConfig.WebhookSecret,
                                        "Goods Return",
                                        docEntry,
                                        docEntry,
                                        ret.WebTxNumber,
                                        ret.WebTxId
                                    );
                                }
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;

                        dbService.UpdateGoodsReturnStatus(ret.HeaderId, 2, ex.Message);

                        string errMsg = ex.Message;
                        if (dbService.IsGoodsReturnRetryLimitExceeded(ret.HeaderId))
                        {
                            errMsg = "[DEAD-LETTER] " + ex.Message;
                            dbService.MarkGoodsReturnAsExceededRetryLimit(ret.HeaderId);
                        }

                        LogSync(dbService, ret, "Failed", null, errMsg);
                    }
                }
            }

            return failedCount;
        }

        private static void LogSync(DatabaseService dbService, PendingPurchaseOrder ret, string status, string docEntry, string errorMessage)
        {
            try
            {
                var log = new SyncLogModel
                {
                    DocType = "Goods Return",
                    DocEntry = docEntry ?? "",
                    CardCode = ret.CardCode ?? "",
                    ItemCode = ret.Lines.Count > 0 ? ret.Lines[0].ItemCode : "",
                    Quantity = ret.Lines.Count > 0 ? (double)ret.Lines[0].Quantity : 0,
                    Price = ret.Lines.Count > 0 ? (double)ret.Lines[0].Price : 0,
                    WarehouseCode = ret.Lines.Count > 0 ? ret.Lines[0].Warehouse : "",
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
