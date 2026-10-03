using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dapper;
using SOLTIUS_Web_API_Add_On.Database.Interfaces;
using SOLTIUS_Web_API_Add_On.Models.MasterData;
using SOLTIUS_Web_API_Add_On.Services.Configuration;

namespace SOLTIUS_Web_API_Add_On.Services.Sap
{
    public class SapMasterDataService : ISapMasterDataService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SapMasterDataService> _logger;
        private readonly IDatabaseConnectionFactory? _connectionFactory;
        private readonly IConfigurationService? _configurationService;

        private string? _b1Session;
        private string? _routeId;
        private DateTime _sessionExpiryUtc = DateTime.MinValue;
        private readonly SemaphoreSlim _loginLock = new SemaphoreSlim(1, 1);

        public SapMasterDataService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ILogger<SapMasterDataService> logger,
            IDatabaseConnectionFactory? connectionFactory = null,
            IConfigurationService? configurationService = null)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _connectionFactory = connectionFactory;
            _configurationService = configurationService;

            // Pastikan Expect: 100-continue dimatikan agar Apache SAP Service Layer tidak mengembalikan 400 Bad Request
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;
        }

        private string GetBaseUrl()
        {
            string url = Environment.GetEnvironmentVariable("SAP_B1_URL") 
                         ?? _configuration["SapServiceLayer:Url"] 
                         ?? "http://localhost:50001/b1s/v2";
            return url.TrimEnd('/');
        }

        private string GetCompanyDb()
        {
            return Environment.GetEnvironmentVariable("SAP_B1_COMPANY_DB") 
                   ?? _configuration["SapServiceLayer:CompanyDB"] 
                   ?? "IBTWEBAPP";
        }

        private string GetUsername()
        {
            return Environment.GetEnvironmentVariable("SAP_B1_USERNAME") 
                   ?? _configuration["SapServiceLayer:UserName"] 
                   ?? "manager";
        }

        private string GetPassword()
        {
            string? pwd = Environment.GetEnvironmentVariable("SAP_B1_PASSWORD") 
                          ?? _configuration["SapServiceLayer:Password"];
            if (string.IsNullOrWhiteSpace(pwd))
            {
                throw new InvalidOperationException("SAP Service Layer password is not configured. Please set the 'SAP_B1_PASSWORD' environment variable or 'SapServiceLayer:Password' configuration key.");
            }
            return pwd;
        }

        private async Task EnsureSessionAsync()
        {
            if (!string.IsNullOrEmpty(_b1Session) && DateTime.UtcNow < _sessionExpiryUtc)
            {
                return;
            }

            await _loginLock.WaitAsync();
            try
            {
                if (!string.IsNullOrEmpty(_b1Session) && DateTime.UtcNow < _sessionExpiryUtc)
                {
                    return;
                }

                await LoginAsync();
            }
            finally
            {
                _loginLock.Release();
            }
        }

        private async Task LoginAsync()
        {
            string loginUrl = $"{GetBaseUrl()}/Login";
            var loginPayload = new
            {
                CompanyDB = GetCompanyDb(),
                UserName = GetUsername(),
                Password = GetPassword()
            };

            string json = JsonSerializer.Serialize(loginPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, loginUrl)
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SAP Service Layer Login failed with status {StatusCode}: {Body}", response.StatusCode, responseBody);
                ParseAndThrowSapError((int)response.StatusCode, responseBody, "SAP Service Layer authentication failed.");
            }

            // Extract B1SESSION and ROUTEID from Set-Cookie headers
            if (response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
            {
                foreach (var header in cookieHeaders)
                {
                    var cookies = header.Split(';');
                    foreach (var cookie in cookies)
                    {
                        var trimmed = cookie.Trim();
                        if (trimmed.StartsWith("B1SESSION=", StringComparison.OrdinalIgnoreCase))
                        {
                            _b1Session = trimmed.Substring("B1SESSION=".Length);
                        }
                        else if (trimmed.StartsWith("ROUTEID=", StringComparison.OrdinalIgnoreCase))
                        {
                            _routeId = trimmed.Substring("ROUTEID=".Length);
                        }
                    }
                }
            }

            // Fallback: cek SessionId di response body jika cookie tidak terbaca
            if (string.IsNullOrEmpty(_b1Session))
            {
                try
                {
                    var jsonNode = JsonNode.Parse(responseBody);
                    _b1Session = jsonNode?["SessionId"]?.ToString();
                }
                catch { }
            }

            if (string.IsNullOrEmpty(_b1Session))
            {
                throw new SapServiceLayerException(500, "-1", "Failed to retrieve B1SESSION from Service Layer login response.");
            }

            // Service Layer default session timeout adalah 30 menit; set expiry lokal ke 25 menit
            _sessionExpiryUtc = DateTime.UtcNow.AddMinutes(25);
            _logger.LogInformation("SAP Service Layer Login successful. Session active until {Expiry}", _sessionExpiryUtc);
        }

        private void ApplySessionCookies(HttpRequestMessage request)
        {
            var cookieParts = new List<string>();
            if (!string.IsNullOrEmpty(_b1Session))
            {
                cookieParts.Add($"B1SESSION={_b1Session}");
            }
            if (!string.IsNullOrEmpty(_routeId))
            {
                cookieParts.Add($"ROUTEID={_routeId}");
            }

            if (cookieParts.Count > 0)
            {
                request.Headers.Add("Cookie", string.Join("; ", cookieParts));
            }
        }

        private async Task<HttpResponseMessage> SendWithRetryAsync(Func<HttpRequestMessage> requestFactory)
        {
            await EnsureSessionAsync();

            var req = requestFactory();
            ApplySessionCookies(req);

            var resp = await _httpClient.SendAsync(req);

            // Jika session expired atau unauthorized (401), re-login sekali lalu retry
            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("SAP Service Layer session expired (401). Attempting re-login...");
                await _loginLock.WaitAsync();
                try
                {
                    _b1Session = null;
                    await LoginAsync();
                }
                finally
                {
                    _loginLock.Release();
                }

                var retryReq = requestFactory();
                ApplySessionCookies(retryReq);
                resp = await _httpClient.SendAsync(retryReq);
            }

            return resp;
        }

        private void ParseAndThrowSapError(int statusCode, string responseBody, string defaultMessage)
        {
            string errCode = statusCode.ToString();
            string errMsg = defaultMessage;

            try
            {
                var doc = JsonNode.Parse(responseBody);
                var errorObj = doc?["error"];
                if (errorObj != null)
                {
                    var codeNode = errorObj["code"];
                    if (codeNode != null)
                    {
                        errCode = codeNode.ToString();
                    }

                    var msgNode = errorObj["message"];
                    if (msgNode != null)
                    {
                        if (msgNode is JsonObject msgObj && msgObj["value"] != null)
                        {
                            errMsg = msgObj["value"]!.ToString();
                        }
                        else
                        {
                            errMsg = msgNode.ToString();
                        }
                    }
                }
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    errMsg = responseBody;
                }
            }

            throw new SapServiceLayerException(statusCode, errCode, errMsg, responseBody);
        }

        // =========================================================================
        // ITEMS
        // =========================================================================
        public async Task<JsonObject> CreateItemAsync(MasterDataItemRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ItemCode))
                throw new ArgumentException("ItemCode is required.", nameof(request.ItemCode));

            string url = $"{GetBaseUrl()}/Items";

            var payload = new JsonObject
            {
                ["ItemCode"] = request.ItemCode.Trim(),
                ["ItemName"] = string.IsNullOrWhiteSpace(request.ItemName) ? request.ItemCode.Trim() : request.ItemName.Trim(),
                ["ItemType"] = "itItems",
                ["ItemsGroupCode"] = request.GroupCode ?? 100,
                ["InventoryUOM"] = string.IsNullOrWhiteSpace(request.UomCode) ? "PCS" : request.UomCode.Trim(),
                ["PurchaseItem"] = string.IsNullOrWhiteSpace(request.PurchaseItem) ? "tYES" : request.PurchaseItem,
                ["SalesItem"] = string.IsNullOrWhiteSpace(request.SalesItem) ? "tYES" : request.SalesItem,
                ["InventoryItem"] = string.IsNullOrWhiteSpace(request.InventoryItem) ? "tYES" : request.InventoryItem
            };

            if (!string.IsNullOrWhiteSpace(request.SalesUom))
            {
                payload["SalesUnit"] = request.SalesUom.Trim();
            }

            string json = payload.ToJsonString();

            var response = await SendWithRetryAsync(() =>
            {
                return new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SAP SL CreateItem failed: {StatusCode} {Body}", response.StatusCode, responseBody);
                ParseAndThrowSapError((int)response.StatusCode, responseBody, $"Failed to create Item '{request.ItemCode}'.");
            }

            // Read back item immediately from SAP to confirm state
            var readBack = await GetItemAsync(request.ItemCode.Trim());
            return readBack ?? (JsonNode.Parse(responseBody) as JsonObject ?? new JsonObject());
        }

        public async Task<JsonObject?> GetItemAsync(string itemCode)
        {
            if (string.IsNullOrWhiteSpace(itemCode)) return null;

            string escapedCode = Uri.EscapeDataString(itemCode.Trim());
            string url = $"{GetBaseUrl()}/Items('{escapedCode}')?$select=ItemCode,ItemName,ItemsGroupCode,InventoryUOM,SalesUnit,PurchaseItem,SalesItem,InventoryItem,CreateDate,UpdateDate";

            var response = await SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Get, url));
            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                ParseAndThrowSapError((int)response.StatusCode, responseBody, $"Failed to get Item '{itemCode}'.");
            }

            return JsonNode.Parse(responseBody) as JsonObject;
        }

        // =========================================================================
        // BUSINESS PARTNERS (SUPPLIER VL-00xxx NUMBERING PRESERVATION)
        // =========================================================================
        public async Task<JsonObject> CreateBusinessPartnerAsync(MasterDataBusinessPartnerRequest request)
        {
            string url = $"{GetBaseUrl()}/BusinessPartners";

            string cardName = string.IsNullOrWhiteSpace(request.CardName) ? "New Vendor" : request.CardName.Trim();
            string incomingCardCode = (request.CardCode ?? "").Trim();

            // Aturan penomoran supplier:
            // SAP IBTWEBAPP mewajibkan Series 73 (V.Lokal dengan prefix VL-00xxx).
            // Jika user mengirimkan kode temporary web seperti "V-MARINDO-01" atau string non-VL,
            // kita gunakan Series 73 otomatis dan biarkan SAP menerbitkan kode resmi VL-00xxx.
            // Jika user mengirimkan kode yang sudah ada (misal VL-00001), kita cek apakah sudah ada untuk duplicate rejection.
            if (!string.IsNullOrWhiteSpace(incomingCardCode) && incomingCardCode.StartsWith("VL-", StringComparison.OrdinalIgnoreCase))
            {
                var existing = await GetBusinessPartnerAsync(incomingCardCode);
                if (existing != null)
                {
                    throw new SapServiceLayerException(400, "-10", $"Business partner code '{incomingCardCode}' already exists in SAP Business One.");
                }
            }

            var payload = new JsonObject
            {
                ["CardName"] = cardName,
                ["CardType"] = "cSupplier",
                ["Series"] = request.Series ?? 73, // 73 = V.Lokal
                ["Currency"] = string.IsNullOrWhiteSpace(request.Currency) ? "IDR" : request.Currency.Trim()
            };

            string json = payload.ToJsonString();

            var response = await SendWithRetryAsync(() =>
            {
                return new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SAP SL CreateBusinessPartner failed: {StatusCode} {Body}", response.StatusCode, responseBody);
                ParseAndThrowSapError((int)response.StatusCode, responseBody, "Failed to create Business Partner.");
            }

            var createdJson = JsonNode.Parse(responseBody) as JsonObject;
            string createdCardCode = createdJson?["CardCode"]?.ToString() ?? "";

            // Simpan mapping web code (misal V-MARINDO-01) -> SAP CardCode (misal VL-00017)
            if (!string.IsNullOrEmpty(createdCardCode) && !string.IsNullOrWhiteSpace(incomingCardCode))
            {
                await SaveVendorMappingAsync(incomingCardCode, createdCardCode, cardName);
            }

            if (!string.IsNullOrEmpty(createdCardCode))
            {
                var readBack = await GetBusinessPartnerAsync(createdCardCode);
                if (readBack != null) return readBack;
            }

            return createdJson ?? new JsonObject();
        }

        private async Task SaveVendorMappingAsync(string webCode, string sapCode, string cardName)
        {
            if (_connectionFactory == null || _configurationService == null) return;
            try
            {
                var dbConfig = _configurationService.GetDatabaseConfig();
                using var conn = _connectionFactory.CreateConnection(dbConfig);
                await conn.OpenAsync();

                string upsertSql = @"
                    IF NOT EXISTS (SELECT 1 FROM sysobjects WHERE name='TBL_VENDOR_MAPPING' AND xtype='U')
                    CREATE TABLE TBL_VENDOR_MAPPING (
                        WebCardCode VARCHAR(100) PRIMARY KEY,
                        SapCardCode VARCHAR(100) NOT NULL,
                        SapCardName NVARCHAR(200),
                        CreatedAt DATETIME DEFAULT GETDATE()
                    );

                    IF EXISTS (SELECT 1 FROM TBL_VENDOR_MAPPING WHERE WebCardCode = @WebCode)
                    BEGIN
                        UPDATE TBL_VENDOR_MAPPING 
                        SET SapCardCode = @SapCode, SapCardName = @CardName 
                        WHERE WebCardCode = @WebCode;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO TBL_VENDOR_MAPPING (WebCardCode, SapCardCode, SapCardName) 
                        VALUES (@WebCode, @SapCode, @CardName);
                    END";

                await conn.ExecuteAsync(upsertSql, new { WebCode = webCode.Trim(), SapCode = sapCode.Trim(), CardName = cardName.Trim() });
                _logger.LogInformation("Saved vendor mapping: {WebCode} -> {SapCode} ({CardName})", webCode, sapCode, cardName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist vendor mapping for {WebCode} -> {SapCode}", webCode, sapCode);
            }
        }

        public async Task<JsonObject?> GetBusinessPartnerAsync(string cardCode)
        {
            if (string.IsNullOrWhiteSpace(cardCode)) return null;

            string escaped = Uri.EscapeDataString(cardCode.Trim());
            string url = $"{GetBaseUrl()}/BusinessPartners('{escaped}')?$select=CardCode,CardName,CardType,GroupCode,Currency,Valid";

            var response = await SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Get, url));
            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                // Cek jika error body menyatakan entity does not exist (-2028)
                try
                {
                    var doc = JsonNode.Parse(responseBody);
                    string? code = doc?["error"]?["code"]?.ToString();
                    if (code == "-2028") return null;
                }
                catch { }

                ParseAndThrowSapError((int)response.StatusCode, responseBody, $"Failed to get Business Partner '{cardCode}'.");
            }

            return JsonNode.Parse(responseBody) as JsonObject;
        }

        // =========================================================================
        // WAREHOUSES
        // =========================================================================
        public async Task<JsonObject> CreateWarehouseAsync(MasterDataWarehouseRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.WhsCode))
                throw new ArgumentException("WhsCode is required.", nameof(request.WhsCode));

            string whsCode = request.WhsCode.Trim();
            if (whsCode.Length > 8)
            {
                throw new SapServiceLayerException(400, "-8112", $"Warehouse code '{whsCode}' exceeds the maximum allowed length of 8 characters in SAP Business One.");
            }

            string url = $"{GetBaseUrl()}/Warehouses";

            var payload = new JsonObject
            {
                ["WarehouseCode"] = whsCode,
                ["WarehouseName"] = string.IsNullOrWhiteSpace(request.WhsName) ? whsCode : request.WhsName.Trim()
            };

            if (request.BPLid.HasValue && request.BPLid.Value > 0)
            {
                payload["BusinessPlaceID"] = request.BPLid.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Inactive))
            {
                payload["Inactive"] = request.Inactive.Trim();
            }

            string json = payload.ToJsonString();

            var response = await SendWithRetryAsync(() =>
            {
                return new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SAP SL CreateWarehouse failed: {StatusCode} {Body}", response.StatusCode, responseBody);
                ParseAndThrowSapError((int)response.StatusCode, responseBody, $"Failed to create Warehouse '{whsCode}'.");
            }

            var readBack = await GetWarehouseAsync(whsCode);
            return readBack ?? (JsonNode.Parse(responseBody) as JsonObject ?? new JsonObject());
        }

        public async Task<JsonObject?> GetWarehouseAsync(string whsCode)
        {
            if (string.IsNullOrWhiteSpace(whsCode)) return null;

            string escaped = Uri.EscapeDataString(whsCode.Trim());
            string url = $"{GetBaseUrl()}/Warehouses('{escaped}')?$select=WarehouseCode,WarehouseName,BusinessPlaceID,Inactive";

            var response = await SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Get, url));
            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    var doc = JsonNode.Parse(responseBody);
                    string? code = doc?["error"]?["code"]?.ToString();
                    if (code == "-2028") return null;
                }
                catch { }

                ParseAndThrowSapError((int)response.StatusCode, responseBody, $"Failed to get Warehouse '{whsCode}'.");
            }

            return JsonNode.Parse(responseBody) as JsonObject;
        }

        public async Task<JsonArray> GetODataCollectionAsync(string entitySet, string? queryOptions = null)
        {
            if (string.IsNullOrWhiteSpace(entitySet))
                throw new ArgumentException("entitySet is required", nameof(entitySet));

            string url = $"{GetBaseUrl()}/{entitySet}";
            if (!string.IsNullOrWhiteSpace(queryOptions))
            {
                url += queryOptions.StartsWith("?") ? queryOptions : $"?{queryOptions}";
            }

            var response = await SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Get, url));
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                ParseAndThrowSapError((int)response.StatusCode, responseBody, $"Failed to query Service Layer entitySet '{entitySet}'.");
            }

            var node = JsonNode.Parse(responseBody);
            if (node is JsonObject obj && obj["value"] is JsonArray arr)
            {
                return arr;
            }

            return new JsonArray();
        }
    }
}
