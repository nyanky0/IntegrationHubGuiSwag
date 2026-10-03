using Dapper;
using Microsoft.AspNetCore.Mvc;
using SOLTIUS_Web_API_Add_On.Database.Interfaces;
using SOLTIUS_Web_API_Add_On.Models.Configuration;
using SOLTIUS_Web_API_Add_On.Models.MasterData;
using SOLTIUS_Web_API_Add_On.Services.Configuration;
using SOLTIUS_Web_API_Add_On.Services.Sap;
using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SOLTIUS_Web_API_Add_On.Controllers
{
    [Route("api/[controller]")]
    public class MasterDataController : CustomApiControllerBase
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly IConfigurationService _configurationService;
        private readonly ISapMasterDataService _sapMasterDataService;

        public MasterDataController(
            IDatabaseConnectionFactory connectionFactory, 
            IConfigurationService configurationService,
            ISapMasterDataService sapMasterDataService)
        {
            _connectionFactory = connectionFactory;
            _configurationService = configurationService;
            _sapMasterDataService = sapMasterDataService;
        }

        private DBConfig GetSapDatabaseConfig()
        {
            DBConfig config = _configurationService.GetDatabaseConfig();
            string sapDbName = Environment.GetEnvironmentVariable("SAP_B1_COMPANY_DB") ?? "IBTWEBAPP";

            // Pastikan pembacaan master data SAP mengarah ke database SAP (IBTWEBAPP)
            return new DBConfig
            {
                DBType = config.DBType,
                Server = config.Server,
                Port = config.Port,
                DatabaseName = !string.IsNullOrWhiteSpace(sapDbName) ? sapDbName : config.DatabaseName,
                UserName = config.UserName,
                Password = config.Password
            };
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetItems([FromQuery] string? search, [FromQuery] DateTime? since, [FromQuery] int top = 100, [FromQuery] int skip = 0)
        {
            try
            {
                int topCount = Math.Clamp(top, 1, 1000);
                var filterParts = new List<string>();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string safeSearch = search.Trim().Replace("'", "''");
                    filterParts.Add($"(contains(ItemCode, '{safeSearch}') or contains(ItemName, '{safeSearch}'))");
                }

                if (since.HasValue)
                {
                    string sinceIso = since.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
                    filterParts.Add($"UpdateDate ge '{sinceIso}'");
                }

                var queryParams = new List<string>
                {
                    "$select=ItemCode,ItemName,ItemsGroupCode,InventoryUOM,SalesUnit,UpdateDate,CreateDate",
                    $"$top={topCount}"
                };

                if (skip > 0)
                {
                    queryParams.Add($"$skip={skip}");
                }

                if (filterParts.Count > 0)
                {
                    queryParams.Add($"$filter={string.Join(" and ", filterParts)}");
                }

                string queryString = string.Join("&", queryParams);
                var jsonItems = await _sapMasterDataService.GetODataCollectionAsync("Items", queryString);

                var items = jsonItems.Select(n =>
                {
                    var obj = n?.AsObject();
                    return new
                    {
                        ItemCode = obj?["ItemCode"]?.ToString() ?? "",
                        ItemName = obj?["ItemName"]?.ToString() ?? "",
                        GroupCode = obj?["ItemsGroupCode"] != null ? Convert.ToInt32(obj["ItemsGroupCode"]!.ToString()) : 100,
                        UomCode = obj?["InventoryUOM"]?.ToString(),
                        SalesUom = obj?["SalesUnit"]?.ToString(),
                        UpdateDate = obj?["UpdateDate"]?.ToString(),
                        CreateDate = obj?["CreateDate"]?.ToString()
                    };
                }).ToList();

                return Ok(new { success = true, count = items.Count, data = items });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "SAP Service Layer query failed: " + ex.Message
                });
            }
        }

        [HttpPost("items")]
        public async Task<IActionResult> CreateItem([FromBody] MasterDataItemRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ItemCode))
            {
                return BadRequest(new { success = false, message = "ItemCode is required." });
            }

            try
            {
                var created = await _sapMasterDataService.CreateItemAsync(request);
                return StatusCode(201, new
                {
                    success = true,
                    message = "Item successfully created in SAP Business One.",
                    data = created
                });
            }
            catch (SapServiceLayerException ex)
            {
                int statusCode = ex.StatusCode >= 400 && ex.StatusCode < 500 ? ex.StatusCode : 400;
                return StatusCode(statusCode, new
                {
                    success = false,
                    errorCode = ex.ErrorCode,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("business-partners")]
        public async Task<IActionResult> GetBusinessPartners([FromQuery] string? cardType = "S", [FromQuery] string? search = null)
        {
            try
            {
                var filterParts = new List<string>();

                if (!string.IsNullOrWhiteSpace(cardType))
                {
                    string sapCardType = cardType.Trim().ToUpper() switch
                    {
                        "C" => "cCustomer",
                        "S" => "cSupplier",
                        "L" => "cLid",
                        _ => "cSupplier"
                    };
                    filterParts.Add($"CardType eq '{sapCardType}'");
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string safeSearch = search.Trim().Replace("'", "''");
                    filterParts.Add($"(contains(CardCode, '{safeSearch}') or contains(CardName, '{safeSearch}'))");
                }

                var queryParams = new List<string>
                {
                    "$select=CardCode,CardName,CardType,GroupCode,Currency,Valid",
                    "$top=200"
                };

                if (filterParts.Count > 0)
                {
                    queryParams.Add($"$filter={string.Join(" and ", filterParts)}");
                }

                string queryString = string.Join("&", queryParams);
                var jsonBps = await _sapMasterDataService.GetODataCollectionAsync("BusinessPartners", queryString);

                var bps = jsonBps.Select(n =>
                {
                    var obj = n?.AsObject();
                    string ct = obj?["CardType"]?.ToString() ?? "";
                    string shortType = ct.Contains("Supplier", StringComparison.OrdinalIgnoreCase) ? "S" :
                                       ct.Contains("Customer", StringComparison.OrdinalIgnoreCase) ? "C" : "L";

                    return new
                    {
                        CardCode = obj?["CardCode"]?.ToString() ?? "",
                        CardName = obj?["CardName"]?.ToString() ?? "",
                        CardType = shortType,
                        GroupCode = obj?["GroupCode"] != null ? Convert.ToInt32(obj["GroupCode"]!.ToString()) : 0,
                        Currency = obj?["Currency"]?.ToString() ?? "IDR",
                        ValidFor = obj?["Valid"]?.ToString() ?? "tYES"
                    };
                }).ToList();

                return Ok(new { success = true, count = bps.Count, data = bps });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "SAP Service Layer query failed: " + ex.Message
                });
            }
        }

        [HttpPost("business-partners")]
        public async Task<IActionResult> CreateBusinessPartner([FromBody] MasterDataBusinessPartnerRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CardName))
            {
                return BadRequest(new { success = false, message = "CardName is required." });
            }

            try
            {
                var created = await _sapMasterDataService.CreateBusinessPartnerAsync(request);
                return StatusCode(201, new
                {
                    success = true,
                    message = "Business partner successfully created in SAP Business One.",
                    data = created
                });
            }
            catch (SapServiceLayerException ex)
            {
                int statusCode = ex.StatusCode >= 400 && ex.StatusCode < 500 ? ex.StatusCode : 400;
                return StatusCode(statusCode, new
                {
                    success = false,
                    errorCode = ex.ErrorCode,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("warehouses")]
        public async Task<IActionResult> GetWarehouses()
        {
            try
            {
                string queryString = "$select=WarehouseCode,WarehouseName,BusinessPlaceID,Inactive&$orderby=WarehouseCode";
                var jsonWhs = await _sapMasterDataService.GetODataCollectionAsync("Warehouses", queryString);

                var whs = jsonWhs.Select(n =>
                {
                    var obj = n?.AsObject();
                    int? bplId = null;
                    if (obj?["BusinessPlaceID"] != null && int.TryParse(obj["BusinessPlaceID"]!.ToString(), out int parsedBpl))
                    {
                        bplId = parsedBpl;
                    }

                    return new
                    {
                        WhsCode = obj?["WarehouseCode"]?.ToString() ?? "",
                        WhsName = obj?["WarehouseName"]?.ToString() ?? "",
                        BPLid = bplId,
                        Inactive = obj?["Inactive"]?.ToString() ?? "tNO"
                    };
                }).ToList();

                return Ok(new { success = true, count = whs.Count, data = whs });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "SAP Service Layer query failed: " + ex.Message
                });
            }
        }

        [HttpPost("warehouses")]
        public async Task<IActionResult> CreateWarehouse([FromBody] MasterDataWarehouseRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.WhsCode))
            {
                return BadRequest(new { success = false, message = "WhsCode is required." });
            }

            try
            {
                var created = await _sapMasterDataService.CreateWarehouseAsync(request);
                return StatusCode(201, new
                {
                    success = true,
                    message = "Warehouse successfully created in SAP Business One.",
                    data = created
                });
            }
            catch (SapServiceLayerException ex)
            {
                int statusCode = ex.StatusCode >= 400 && ex.StatusCode < 500 ? ex.StatusCode : 400;
                return StatusCode(statusCode, new
                {
                    success = false,
                    errorCode = ex.ErrorCode,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("item-hierarchy")]
        public async Task<IActionResult> GetItemHierarchy([FromQuery] string? search, [FromQuery] int top = 100, [FromQuery] int skip = 0)
        {
            try

            {
                DBConfig config = _configurationService.GetDatabaseConfig();
                using DbConnection conn = _connectionFactory.CreateConnection(config);
                await conn.OpenAsync();

                bool isHanaOrMysql = config.DBType == DatabaseType.MySql || 
                                     (!string.IsNullOrEmpty(config.Server) && config.Server.IndexOf("HANA", StringComparison.OrdinalIgnoreCase) >= 0);

                int topCount = Math.Clamp(top, 1, 1000);
                int skipCount = Math.Max(0, skip);
                string query;
                if (isHanaOrMysql)
                {
                    query = @"
                        SELECT 
                            U_SOL_Code AS Code,
                            U_SOL_Name AS Name,
                            NULLIF(NULLIF(U_SOL_Parent, '-'), '') AS Parent,
                            NULLIF(NULLIF(U_SOL_CostCenter, '-'), '') AS CostCenter,
                            NULLIF(NULLIF(U_SOL_Ownership, '-'), '') AS Ownership,
                            COALESCE(U_SOL_Level, 1) AS Level
                        FROM ""@SOL_ITEMHIER""
                        WHERE (@Search IS NULL OR U_SOL_Code LIKE CONCAT('%', @Search, '%') OR U_SOL_Name LIKE CONCAT('%', @Search, '%'))
                        ORDER BY U_SOL_Level, U_SOL_Code
                        LIMIT " + topCount + @" OFFSET " + skipCount + @";";
                }
                else
                {
                    query = @"
                        SELECT TOP (" + (topCount + skipCount) + @")
                            U_SOL_Code AS Code,
                            U_SOL_Name AS Name,
                            NULLIF(NULLIF(U_SOL_Parent, '-'), '') AS Parent,
                            NULLIF(NULLIF(U_SOL_CostCenter, '-'), '') AS CostCenter,
                            NULLIF(NULLIF(U_SOL_Ownership, '-'), '') AS Ownership,
                            COALESCE(U_SOL_Level, 1) AS Level
                        FROM [@SOL_ITEMHIER]
                        WHERE (@Search IS NULL OR U_SOL_Code LIKE '%' + @Search + '%' OR U_SOL_Name LIKE '%' + @Search + '%')
                        ORDER BY U_SOL_Level, U_SOL_Code;";
                }

                var rawRows = await conn.QueryAsync(query, new { Search = search });
                var mappedRows = rawRows.Select(r =>
                {
                    string rawCode = ((string?)r.Code ?? string.Empty).Trim();
                    string rawName = ((string?)r.Name ?? string.Empty).Trim();

                    string? rawParent = (string?)r.Parent;
                    string? parent = string.IsNullOrWhiteSpace(rawParent) || rawParent.Trim() == "-" ? null : rawParent.Trim();

                    string? rawCostCenter = (string?)r.CostCenter;
                    string? costCenter = string.IsNullOrWhiteSpace(rawCostCenter) || rawCostCenter.Trim() == "-" ? null : rawCostCenter.Trim();

                    string? rawOwnership = (string?)r.Ownership;
                    string? ownership = string.IsNullOrWhiteSpace(rawOwnership) || rawOwnership.Trim() == "-" ? null : rawOwnership.Trim();

                    int level = r.Level != null ? Convert.ToInt32(r.Level) : 1;

                    return new
                    {
                        code = rawCode,
                        name = rawName,
                        parent = parent,
                        costCenter = costCenter,
                        ownership = ownership,
                        level = level
                    };
                });

                if (!isHanaOrMysql && skipCount > 0)
                {
                    mappedRows = mappedRows.Skip(skipCount).Take(topCount);
                }

                var rows = mappedRows.ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                // Fallback mock mode apabila tabel @SOL_ITEMHIER belum didaftarkan di SAP DB
                var fallback = new[]
                {
                    new { code = "engine", name = "engine", parent = (string?)null, costCenter = (string?)null, ownership = (string?)null, level = 1 },
                    new { code = "main engine", name = "main engine", parent = (string?)"engine", costCenter = (string?)"Vessel", ownership = (string?)"Operation", level = 2 }
                };
                return Ok(new
                {
                    success = true,
                    isFallback = true,
                    message = "Integration Hub Staging Data Mode: " + ex.Message,
                    data = fallback,
                    value = fallback
                });
            }
        }

        [HttpGet("taxes")]
        [HttpGet("vat-groups")]
        public async Task<IActionResult> GetTaxes()
        {
            try
            {
                string queryString = "$select=Code,Name,Inactive,Category,VatGroups_Lines&$orderby=Code";
                var jsonRows = await _sapMasterDataService.GetODataCollectionAsync("VatGroups", queryString);

                var rows = jsonRows.Select(n =>
                {
                    var obj = n?.AsObject();
                    decimal rate = 0m;
                    if (obj?["VatGroups_Lines"] is JsonArray lines && lines.Count > 0)
                    {
                        var firstLine = lines[0]?.AsObject();
                        if (firstLine?["Rate"] != null && decimal.TryParse(firstLine["Rate"]!.ToString(), out decimal parsedRate))
                        {
                            rate = parsedRate;
                        }
                    }

                    string category = obj?["Category"]?.ToString() ?? "";
                    string shortCat = category.Contains("Input", StringComparison.OrdinalIgnoreCase) ? "I" : "O";

                    return new
                    {
                        Code = obj?["Code"]?.ToString() ?? "",
                        Name = obj?["Name"]?.ToString() ?? "",
                        Rate = rate,
                        Inactive = obj?["Inactive"]?.ToString() ?? "tNO",
                        Category = shortCat
                    };
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "SAP Service Layer query failed: " + ex.Message });
            }
        }

        [HttpGet("uoms")]
        [HttpGet("unit-of-measurements")]
        public async Task<IActionResult> GetUoms()
        {
            try
            {
                string queryString = "$select=AbsEntry,Code,Name&$orderby=Code";
                var jsonRows = await _sapMasterDataService.GetODataCollectionAsync("UnitOfMeasurements", queryString);

                var rows = jsonRows.Select(n =>
                {
                    var obj = n?.AsObject();
                    int absEntry = obj?["AbsEntry"] != null ? Convert.ToInt32(obj["AbsEntry"]!.ToString()) : 0;
                    return new
                    {
                        AbsEntry = absEntry,
                        Code = obj?["Code"]?.ToString() ?? "",
                        Name = obj?["Name"]?.ToString() ?? "",
                        Locked = "tNO"
                    };
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "SAP Service Layer query failed: " + ex.Message });
            }
        }

        [HttpGet("uom-groups")]
        [HttpGet("unit-of-measurement-groups")]
        public async Task<IActionResult> GetUomGroups()
        {
            try
            {
                string queryString = "$select=AbsEntry,Code,Name,BaseUoM&$orderby=Code";
                var jsonRows = await _sapMasterDataService.GetODataCollectionAsync("UnitOfMeasurementGroups", queryString);

                var rows = jsonRows.Select(n =>
                {
                    var obj = n?.AsObject();
                    int absEntry = obj?["AbsEntry"] != null ? Convert.ToInt32(obj["AbsEntry"]!.ToString()) : 0;
                    int baseUom = obj?["BaseUoM"] != null ? Convert.ToInt32(obj["BaseUoM"]!.ToString()) : 0;

                    return new
                    {
                        AbsEntry = absEntry,
                        Code = obj?["Code"]?.ToString() ?? "",
                        Name = obj?["Name"]?.ToString() ?? "",
                        BaseUom = baseUom
                    };
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "SAP Service Layer query failed: " + ex.Message });
            }
        }

        [HttpGet("bin-locations")]
        public async Task<IActionResult> GetBinLocations([FromQuery] string? warehouse = null)
        {
            try
            {
                string queryParams = "$select=AbsEntry,BinCode,Warehouse,Inactive&$orderby=BinCode";
                if (!string.IsNullOrWhiteSpace(warehouse))
                {
                    string safeWhs = warehouse.Trim().Replace("'", "''");
                    queryParams += $"&$filter=Warehouse eq '{safeWhs}'";
                }

                var jsonRows = await _sapMasterDataService.GetODataCollectionAsync("BinLocations", queryParams);

                var rows = jsonRows.Select(n =>
                {
                    var obj = n?.AsObject();
                    int absEntry = obj?["AbsEntry"] != null ? Convert.ToInt32(obj["AbsEntry"]!.ToString()) : 0;
                    return new
                    {
                        AbsEntry = absEntry,
                        BinCode = obj?["BinCode"]?.ToString() ?? "",
                        Warehouse = obj?["Warehouse"]?.ToString() ?? "",
                        Inactive = obj?["Inactive"]?.ToString() ?? "tNO"
                    };
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "SAP Service Layer query failed: " + ex.Message });
            }
        }

        [HttpGet("cost-centers")]
        [HttpGet("profit-centers")]
        public async Task<IActionResult> GetCostCenters([FromQuery] int? dimension = null)
        {
            try
            {
                string queryParams = "$select=CenterCode,CenterName,InWhichDimension,Active&$orderby=CenterCode";
                if (dimension.HasValue)
                {
                    queryParams += $"&$filter=InWhichDimension eq {dimension.Value}";
                }

                var jsonRows = await _sapMasterDataService.GetODataCollectionAsync("ProfitCenters", queryParams);

                var rows = jsonRows.Select(n =>
                {
                    var obj = n?.AsObject();
                    int dim = obj?["InWhichDimension"] != null ? Convert.ToInt32(obj["InWhichDimension"]!.ToString()) : 1;
                    return new
                    {
                        CenterCode = obj?["CenterCode"]?.ToString() ?? "",
                        CenterName = obj?["CenterName"]?.ToString() ?? "",
                        InWhichDimension = dim,
                        Active = obj?["Active"]?.ToString() ?? "tYES"
                    };
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "SAP Service Layer query failed: " + ex.Message });
            }
        }

        [HttpGet("payment-terms")]
        [HttpGet("payment-terms-types")]
        public async Task<IActionResult> GetPaymentTerms()
        {
            try
            {
                string queryString = "$select=GroupNumber,PaymentTermsGroupName,NumberOfAdditionalDays,NumberOfAdditionalMonths,GeneralDiscount&$orderby=GroupNumber";
                var jsonRows = await _sapMasterDataService.GetODataCollectionAsync("PaymentTermsTypes", queryString);

                var rows = jsonRows.Select(n =>
                {
                    var obj = n?.AsObject();
                    int grpNum = obj?["GroupNumber"] != null ? Convert.ToInt32(obj["GroupNumber"]!.ToString()) : 0;
                    int extraDays = obj?["NumberOfAdditionalDays"] != null ? Convert.ToInt32(obj["NumberOfAdditionalDays"]!.ToString()) : 0;
                    int extraMonths = obj?["NumberOfAdditionalMonths"] != null ? Convert.ToInt32(obj["NumberOfAdditionalMonths"]!.ToString()) : 0;
                    decimal discount = 0m;
                    if (obj?["GeneralDiscount"] != null && decimal.TryParse(obj["GeneralDiscount"]!.ToString(), out decimal parsedDisc))
                    {
                        discount = parsedDisc;
                    }

                    return new
                    {
                        GroupNumber = grpNum,
                        PaymentTermsGroupName = obj?["PaymentTermsGroupName"]?.ToString() ?? $"Term #{grpNum}",
                        ExtraDays = extraDays,
                        ExtraMonths = extraMonths,
                        DiscountPercent = discount
                    };
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "SAP Service Layer query failed: " + ex.Message });
            }
        }

        [HttpGet("freight")]
        [HttpGet("additional-expenses")]
        public async Task<IActionResult> GetFreight()
        {
            try
            {
                string queryString = "$select=ExpensCode,Name,RevenuesAccount,ExpenseAccount,OutputVATGroup,InputVATGroup,TaxLiable&$orderby=ExpensCode";
                var jsonRows = await _sapMasterDataService.GetODataCollectionAsync("AdditionalExpenses", queryString);

                var rows = jsonRows.Select(n =>
                {
                    var obj = n?.AsObject();
                    int code = obj?["ExpensCode"] != null ? Convert.ToInt32(obj["ExpensCode"]!.ToString()) : 0;
                    return new
                    {
                        ExpensCode = code,
                        Name = obj?["Name"]?.ToString() ?? $"Freight #{code}",
                        RevenuesAccount = obj?["RevenuesAccount"]?.ToString(),
                        ExpenseAccount = obj?["ExpenseAccount"]?.ToString(),
                        OutputVATGroup = obj?["OutputVATGroup"]?.ToString(),
                        InputVATGroup = obj?["InputVATGroup"]?.ToString(),
                        TaxLiable = obj?["TaxLiable"]?.ToString() ?? "tNO"
                    };
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "SAP Service Layer query failed: " + ex.Message });
            }
        }

        [HttpGet("projects")]
        public async Task<IActionResult> GetProjects([FromQuery] string? search = null)
        {
            try
            {
                string queryParams = "$select=Code,Name,ValidFrom,ValidTo,Active&$orderby=Code";
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string safeSearch = search.Trim().Replace("'", "''");
                    queryParams += $"&$filter=(contains(Code, '{safeSearch}') or contains(Name, '{safeSearch}'))";
                }

                var jsonRows = await _sapMasterDataService.GetODataCollectionAsync("Projects", queryParams);

                var rows = jsonRows.Select(n =>
                {
                    var obj = n?.AsObject();
                    string? validFrom = obj?["ValidFrom"]?.ToString();
                    string? validTo = obj?["ValidTo"]?.ToString();

                    return new
                    {
                        Code = obj?["Code"]?.ToString() ?? "",
                        Name = obj?["Name"]?.ToString() ?? "",
                        ValidFrom = !string.IsNullOrEmpty(validFrom) ? validFrom.Split('T')[0] : null,
                        ValidTo = !string.IsNullOrEmpty(validTo) ? validTo.Split('T')[0] : null,
                        Active = obj?["Active"]?.ToString() ?? "tYES"
                    };
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "SAP Service Layer query failed: " + ex.Message });
            }
        }

        [HttpGet("item-groups")]
        public async Task<IActionResult> GetItemGroups()
        {
            try
            {
                string queryString = "$select=Number,GroupName&$orderby=Number";
                var jsonRows = await _sapMasterDataService.GetODataCollectionAsync("ItemGroups", queryString);

                var rows = jsonRows.Select(n =>
                {
                    var obj = n?.AsObject();
                    int num = obj?["Number"] != null ? Convert.ToInt32(obj["Number"]!.ToString()) : 0;
                    return new
                    {
                        Number = num,
                        GroupName = obj?["GroupName"]?.ToString() ?? ""
                    };
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "SAP Service Layer query failed: " + ex.Message });
            }
        }

        [HttpGet("hs-codes")]
        public async Task<IActionResult> GetHsCodes([FromQuery] string? search = null)
        {
            try
            {
                var jsonRows = await _sapMasterDataService.GetODataCollectionAsync("CustomsGroups", "$select=Code,Name,Number&$orderby=Code");

                var mappedRows = jsonRows.Select(n =>
                {
                    var obj = n?.AsObject();
                    string hsCode = obj?["Number"]?.ToString() ?? obj?["Code"]?.ToString() ?? "";
                    string name = obj?["Name"]?.ToString() ?? "";
                    return new
                    {
                        HsCode = hsCode,
                        Desc = name,
                        Lartas = "tidak_lartas"
                    };
                });

                if (!string.IsNullOrWhiteSpace(search))
                {
                    mappedRows = mappedRows.Where(r =>
                        r.HsCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        r.Desc.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                var rows = mappedRows.ToList();
                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "SAP Service Layer query failed: " + ex.Message });
            }
        }

        [HttpGet("part-numbers")]
        public async Task<IActionResult> GetPartNumbers([FromQuery] string? search = null, [FromQuery] int top = 100, [FromQuery] int skip = 0)
        {
            try
            {
                var jsonRows = await _sapMasterDataService.GetODataCollectionAsync("SOL_PNUM_H");

                var mappedRows = jsonRows.Select(n =>
                {
                    var obj = n?.AsObject();
                    int docEntry = obj?["DocEntry"] != null ? Convert.ToInt32(obj["DocEntry"]!.ToString()) : 0;
                    string partNumber = obj?["U_SOL_PartNumber"]?.ToString() ?? "";
                    string description = obj?["U_SOL_Description"]?.ToString() ?? "";
                    string? itemCode = null;
                    if (obj?["SOL_PNUM_DLines"] is JsonArray lines && lines.Count > 0)
                    {
                        itemCode = lines[0]?["U_SOL_ItemCode"]?.ToString();
                    }

                    return new
                    {
                        DocEntry = docEntry,
                        PartNumber = partNumber,
                        Description = description,
                        ItemCode = itemCode
                    };
                });

                if (!string.IsNullOrWhiteSpace(search))
                {
                    mappedRows = mappedRows.Where(r =>
                        r.PartNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        r.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                int topCount = Math.Clamp(top, 1, 1000);
                int skipCount = Math.Max(0, skip);
                var rows = mappedRows.Skip(skipCount).Take(topCount).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "SAP Service Layer query failed: " + ex.Message });
            }
        }
    }
}

