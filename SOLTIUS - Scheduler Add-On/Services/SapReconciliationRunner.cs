using System;
using System.Collections.Generic;
using SOLTIUS_Scheduler_Add_On.Model;
using SOLTIUS_Scheduler_Add_On.UI;

namespace SOLTIUS_Scheduler_Add_On.Services
{
    /// <summary>
    /// Runner untuk Two-Way Status Reconciliation (Downsync dari SAP B1 ke Web Laravel).
    /// Mendeteksi dokumen di SAP B1 yang di-close ('C') atau di-cancel ('Y') oleh user SAP,
    /// dan mengirimkan pembaruan status ke Web App Laravel via webhook.
    /// </summary>
    public static class SapReconciliationRunner
    {
        public static int RunReconciliation(AppConfig config, SchedulerConfig schedConfig, Action<string> logWriter = null)
        {
            if (config == null || schedConfig == null) return 0;
            if (!schedConfig.EnableReconciliation) return 0;

            string connString = ConfigService.BuildStagingConnectionString(config);
            if (string.IsNullOrEmpty(connString)) return 0;

            int updatedCount = 0;
            var dbService = new DatabaseService(connString);

            using (var sapService = new SapSyncService())
            {
                try
                {
                    sapService.ConnectToDIAPI(config);
                }
                catch (Exception ex)
                {
                    logWriter?.Invoke($"[Reconciliation] Gagal konek DI-API: {ex.Message}");
                    return 0;
                }

                // 1. Reconcile Purchase Order
                if (schedConfig.SyncPurchaseOrder)
                {
                    updatedCount += ReconcileDocType(dbService, sapService, schedConfig, "Purchase Order", logWriter);
                }

                // 2. Reconcile Goods Receipt PO
                if (schedConfig.SyncGoodsReceiptPO)
                {
                    updatedCount += ReconcileDocType(dbService, sapService, schedConfig, "Goods Receipt PO", logWriter);
                }

                // 3. Reconcile Stock Transfer
                if (schedConfig.SyncStockTransfer)
                {
                    updatedCount += ReconcileDocType(dbService, sapService, schedConfig, "Stock Transfer", logWriter);
                }
            }

            return updatedCount;
        }

        private static int ReconcileDocType(DatabaseService dbService, SapSyncService sapService, SchedulerConfig schedConfig, string docType, Action<string> logWriter)
        {
            int updated = 0;
            try
            {
                List<Tuple<long, string, string>> docs = dbService.LoadSyncedDocumentsForReconciliation(docType, 25);
                foreach (var doc in docs)
                {
                    long headerId = doc.Item1;
                    string docEntry = doc.Item2;
                    string webTxNum = doc.Item3;

                    try
                    {
                        var statusResult = sapService.GetDocumentStatus(docType, docEntry);
                        if (statusResult.Exists && (statusResult.Status == "Closed" || statusResult.Status == "Canceled"))
                        {
                            // Kirim webhook ke Laravel
                            if (schedConfig.EnableWebhookCallback)
                            {
                                WebhookCallbackService.SendReconciliationUpdateAsync(
                                    schedConfig.WebhookUrl,
                                    schedConfig.WebhookSecret,
                                    docType,
                                    docEntry,
                                    statusResult.DocNum,
                                    statusResult.Status
                                );
                            }

                            // Tandai di staging
                            dbService.MarkDocumentAsReconciled(docType, headerId, statusResult.Status);
                            updated++;
                            logWriter?.Invoke($"[Reconciliation] {docType} DocEntry={docEntry} (Tx: {webTxNum}) terdeteksi {statusResult.Status} di SAP -> sinkron ke Web.");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Reconciliation] Error checking {docType} {docEntry}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logWriter?.Invoke($"[Reconciliation] Error saat rekonsiliasi {docType}: {ex.Message}");
            }

            return updated;
        }
    }
}
