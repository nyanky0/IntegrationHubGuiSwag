using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SOLTIUS_Scheduler_Add_On.Model;
using SOLTIUS_Scheduler_Add_On.UI;

namespace SOLTIUS_Scheduler_Add_On.Services
{
    public class SapServiceLayerClient : IDisposable
    {
        private static readonly HttpClient _httpClient;
        private string _baseUrl;
        private string _companyDb;
        private string _username;
        private string _password;
        private string _b1Session;
        private string _routeId;
        private DateTime _sessionExpiryUtc = DateTime.MinValue;
        private static readonly object _lock = new object();

        static SapServiceLayerClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (req, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;
        }

        public SapServiceLayerClient(AppConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            string url = Environment.GetEnvironmentVariable("SAP_B1_URL");
            if (string.IsNullOrWhiteSpace(url))
            {
                string port = !string.IsNullOrWhiteSpace(config.SAPDBPort) && config.SAPDBPort != "0" ? config.SAPDBPort : "50001";
                string server = !string.IsNullOrWhiteSpace(config.SAPDBServer) ? config.SAPDBServer : "localhost";
                if (server.Equals("localhost", StringComparison.OrdinalIgnoreCase) || server == "127.0.0.1")
                {
                    server = "localhost";
                }
                url = $"http://{server}:{port}/b1s/v2";
            }

            _baseUrl = url.TrimEnd('/');
            _companyDb = Environment.GetEnvironmentVariable("SAP_B1_COMPANY_DB") ?? config.SAPDatabase ?? "IBTWEBAPP";
            _username = Environment.GetEnvironmentVariable("SAP_B1_USERNAME") ?? config.SAPUser ?? "manager";
            _password = Environment.GetEnvironmentVariable("SAP_B1_PASSWORD") ?? config.SAPPass;

            if (string.IsNullOrWhiteSpace(_password))
            {
                throw new InvalidOperationException("SAP Service Layer password is not configured. Please supply SAPPass in AppConfig or set SAP_B1_PASSWORD environment variable.");
            }
        }

        public void EnsureLogin()
        {
            if (!string.IsNullOrEmpty(_b1Session) && DateTime.UtcNow < _sessionExpiryUtc)
            {
                return;
            }

            lock (_lock)
            {
                if (!string.IsNullOrEmpty(_b1Session) && DateTime.UtcNow < _sessionExpiryUtc)
                {
                    return;
                }

                string loginUrl = $"{_baseUrl}/Login";
                var body = new
                {
                    CompanyDB = _companyDb,
                    UserName = _username,
                    Password = _password
                };

                var request = new HttpRequestMessage(HttpMethod.Post, loginUrl)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
                };

                HttpResponseMessage response;
                try
                {
                    response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Cannot connect to SAP Service Layer at {_baseUrl}: {ex.Message}", ex);
                }

                string respContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                {
                    string errDetail = ParseErrorMessage(respContent);
                    throw new Exception($"SAP Service Layer Login failed ({(int)response.StatusCode}): {errDetail}");
                }

                var jObj = JObject.Parse(respContent);
                _b1Session = jObj["SessionId"]?.ToString();
                int timeoutMinutes = 30;
                if (jObj["SessionTimeout"] != null && int.TryParse(jObj["SessionTimeout"].ToString(), out int parsedMins))
                {
                    timeoutMinutes = Math.Max(5, parsedMins - 2);
                }
                _sessionExpiryUtc = DateTime.UtcNow.AddMinutes(timeoutMinutes);

                if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                {
                    foreach (var c in cookies)
                    {
                        if (c.Contains("ROUTEID="))
                        {
                            int start = c.IndexOf("ROUTEID=");
                            int end = c.IndexOf(';', start);
                            _routeId = end > start ? c.Substring(start, end - start) : c.Substring(start);
                        }
                    }
                }
            }
        }

        public string PostDocument(string entitySet, object payload)
        {
            EnsureLogin();

            string url = $"{_baseUrl}/{entitySet}";
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            };

            string cookieHeader = $"B1SESSION={_b1Session}";
            if (!string.IsNullOrEmpty(_routeId))
            {
                cookieHeader += $"; {_routeId}";
            }
            request.Headers.Add("Cookie", cookieHeader);

            HttpResponseMessage response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
            string responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                string errMsg = ParseErrorMessage(responseText);
                throw new Exception($"SAP Service Layer POST {entitySet} failed ({(int)response.StatusCode}): {errMsg}");
            }

            var jsonResult = JObject.Parse(responseText);
            string docEntry = jsonResult["DocEntry"]?.ToString();
            if (string.IsNullOrEmpty(docEntry))
            {
                docEntry = jsonResult["DocNum"]?.ToString() ?? "0";
            }

            // Readback verification from SAP Service Layer
            VerifyReadback(entitySet, docEntry);

            return docEntry;
        }

        private void VerifyReadback(string entitySet, string docEntry)
        {
            try
            {
                string url = $"{_baseUrl}/{entitySet}({docEntry})";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                string cookieHeader = $"B1SESSION={_b1Session}";
                if (!string.IsNullOrEmpty(_routeId)) cookieHeader += $"; {_routeId}";
                request.Headers.Add("Cookie", cookieHeader);

                HttpResponseMessage response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    Console.WriteLine($"[SapServiceLayerClient] Readback verification warning: {response.StatusCode} - {body}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SapServiceLayerClient] Readback exception: {ex.Message}");
            }
        }

        public static void ValidateBranchAndWarehouse(int? bplId, string warehouseCode)
        {
            if (string.IsNullOrWhiteSpace(warehouseCode))
            {
                throw new ArgumentException("Gudang (Warehouse) tidak boleh kosong.");
            }

            string whs = warehouseCode.Trim().ToUpperInvariant();

            // Aturan spesifik sistem IBT:
            // Branch 3 (PT Indobaruna Bulk Transport) berpasangan eksklusif dengan WH-IBT
            if (bplId.HasValue && bplId.Value == 3)
            {
                if (whs != "WH-IBT")
                {
                    throw new InvalidOperationException($"Incompatible branch and warehouse: Cabang BPLID 3 (PT Indobaruna Bulk Transport) hanya boleh menggunakan gudang 'WH-IBT', bukan '{warehouseCode}'. Fallback ke gudang cabang lain dilarang!");
                }
            }
            else if (whs == "WH-IBT" && bplId.HasValue && bplId.Value != 3)
            {
                throw new InvalidOperationException($"Incompatible branch and warehouse: Gudang 'WH-IBT' hanya valid untuk Cabang BPLID 3, bukan cabang {bplId.Value}.");
            }
        }

        public (bool Exists, string Status, string DocNum) GetDocumentStatus(string docType, string docEntry)
        {
            EnsureLogin();
            string entitySet = "PurchaseOrders";
            if (docType.Equals("Goods Receipt PO", StringComparison.OrdinalIgnoreCase)) entitySet = "PurchaseDeliveryNotes";
            else if (docType.Equals("Stock Transfer", StringComparison.OrdinalIgnoreCase)) entitySet = "StockTransfers";
            else if (docType.Equals("Goods Return", StringComparison.OrdinalIgnoreCase)) entitySet = "PurchaseReturns";

            string url = $"{_baseUrl}/{entitySet}({docEntry})";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            string cookieHeader = $"B1SESSION={_b1Session}";
            if (!string.IsNullOrEmpty(_routeId)) cookieHeader += $"; {_routeId}";
            request.Headers.Add("Cookie", cookieHeader);

            HttpResponseMessage response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                return (false, "Not Found", "");
            }

            string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var jObj = JObject.Parse(content);
            string docNum = jObj["DocNum"]?.ToString() ?? "";
            string docStatus = jObj["DocumentStatus"]?.ToString() ?? "";
            string cancelled = jObj["Cancelled"]?.ToString() ?? "tNO";

            string finalStatus = "Open";
            if (cancelled.Equals("tYES", StringComparison.OrdinalIgnoreCase))
            {
                finalStatus = "Canceled";
            }
            else if (docStatus.Equals("bost_Close", StringComparison.OrdinalIgnoreCase) || docStatus.Equals("Closed", StringComparison.OrdinalIgnoreCase))
            {
                finalStatus = "Closed";
            }

            return (true, finalStatus, docNum);
        }

        private static string ParseErrorMessage(string jsonResponse)
        {
            if (string.IsNullOrWhiteSpace(jsonResponse)) return "Empty error response from SAP";
            try
            {
                var parsed = JObject.Parse(jsonResponse);
                var errObj = parsed["error"];
                if (errObj != null)
                {
                    var msg = errObj["message"];
                    if (msg is JValue jv) return jv.ToString();
                    if (msg?["value"] != null) return msg["value"].ToString();
                }
            }
            catch { }
            return jsonResponse;
        }

        public void Dispose()
        {
            // HttpClient is shared static, no unmanaged resources
        }
    }
}
