using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SOLTIUS_Scheduler_Add_On.Services
{
    /// <summary>
    /// Service pengirim Webhook Callback Asynchronous ke Web App Laravel.
    /// Mengirimkan notifikasi saat DocEntry SAP terbit, heartbeat beacon,
    /// dan pembaruan rekonsiliasi status dokumen (Closed/Canceled).
    /// </summary>
    public static class WebhookCallbackService
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        /// <summary>
        /// Mengirim webhook callback saat dokumen SAP (PO/GRPO/StockTransfer) berhasil dibuat.
        /// </summary>
        public static void SendDocEntryCallbackAsync(string webhookUrl, string webhookSecret, string docType, string docEntry, string docNum, string webTxNumber, long? webTxId, string status = "synced_to_sap")
        {
            if (string.IsNullOrWhiteSpace(webhookUrl)) return;

            Task.Run(async () =>
            {
                try
                {
                    var payload = new
                    {
                        event_type = "document_created",
                        payload = new
                        {
                            doc_type = docType,
                            sap_doc_entry = docEntry,
                            sap_doc_num = string.IsNullOrWhiteSpace(docNum) ? docEntry : docNum,
                            transaction_number = webTxNumber ?? "",
                            transaction_id = webTxId,
                            status = status,
                            processed_at = DateTime.UtcNow.ToString("o")
                        }
                    };

                    await PostWebhookAsync(webhookUrl, webhookSecret, payload);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[WebhookCallback] SendDocEntryCallback failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Mengirimkan heartbeat beacon status hidup scheduler ke Web Laravel.
        /// </summary>
        public static void SendHeartbeatAsync(string webhookUrl, string webhookSecret, string profileName, bool isRunning, int lastFailedCount)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl)) return;

            Task.Run(async () =>
            {
                try
                {
                    var payload = new
                    {
                        event_type = "scheduler_heartbeat",
                        payload = new
                        {
                            service_name = "IBT SAP B1 Scheduler Add-On",
                            machine_name = Environment.MachineName,
                            status = isRunning ? "online" : "idle",
                            is_running = isRunning,
                            active_profile = profileName ?? "N/A",
                            last_failed_count = lastFailedCount,
                            heartbeat_at = DateTime.UtcNow.ToString("o")
                        }
                    };

                    await PostWebhookAsync(webhookUrl, webhookSecret, payload);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[WebhookCallback] SendHeartbeat failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Mengirimkan notifikasi perubahan status dokumen SAP (Downsync / Reconciliation).
        /// </summary>
        public static void SendReconciliationUpdateAsync(string webhookUrl, string webhookSecret, string docType, string docEntry, string docNum, string status)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl)) return;

            Task.Run(async () =>
            {
                try
                {
                    var payload = new
                    {
                        event_type = "document_updated",
                        payload = new
                        {
                            DocType = docType,
                            DocEntry = docEntry,
                            DocNum = string.IsNullOrWhiteSpace(docNum) ? docEntry : docNum,
                            Status = status,
                            synced_at = DateTime.UtcNow.ToString("o")
                        }
                    };

                    await PostWebhookAsync(webhookUrl, webhookSecret, payload);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[WebhookCallback] SendReconciliationUpdate failed: {ex.Message}");
                }
            });
        }

        private static async Task PostWebhookAsync(string webhookUrl, string webhookSecret, object data)
        {
            string jsonBody = JsonConvert.SerializeObject(data);
            using (var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl))
            {
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                if (!string.IsNullOrEmpty(webhookSecret))
                {
                    string signature = ComputeHmacSha256(jsonBody, webhookSecret);
                    request.Headers.Add("X-SAP-Signature", signature);
                }

                HttpResponseMessage response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[WebhookCallback] Endpoint responded with {response.StatusCode}");
                }
            }
        }

        private static string ComputeHmacSha256(string rawData, string secret)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
