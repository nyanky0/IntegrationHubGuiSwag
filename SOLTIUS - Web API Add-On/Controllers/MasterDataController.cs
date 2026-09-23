using Dapper;
using Microsoft.AspNetCore.Mvc;
using SOLTIUS_Web_API_Add_On.Database.Interfaces;
using SOLTIUS_Web_API_Add_On.Models.Configuration;
using SOLTIUS_Web_API_Add_On.Services.Configuration;
using System.Data.Common;

namespace SOLTIUS_Web_API_Add_On.Controllers
{
    [Route("api/[controller]")]
    public class MasterDataController : CustomApiControllerBase
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly IConfigurationService _configurationService;

        public MasterDataController(IDatabaseConnectionFactory connectionFactory, IConfigurationService configurationService)
        {
            _connectionFactory = connectionFactory;
            _configurationService = configurationService;
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetItems([FromQuery] string? search, [FromQuery] DateTime? since, [FromQuery] int top = 100, [FromQuery] int skip = 0)
        {
            try
            {
                DBConfig config = _configurationService.GetDatabaseConfig();
                using DbConnection conn = _connectionFactory.CreateConnection(config);
                await conn.OpenAsync();

                bool isHanaOrMysql = config.DBType == DatabaseType.MySql || 
                                     (!string.IsNullOrEmpty(config.Server) && config.Server.IndexOf("HANA", StringComparison.OrdinalIgnoreCase) >= 0);

                int topCount = Math.Min(top, 1000);
                string query;
                if (isHanaOrMysql)
                {
                    query = @"
                        SELECT 
                            ItemCode, ItemName, ItmsGrpCod as GroupCode, 
                            InvntryUom as UomCode, SalUnitMsr as SalesUom,
                            UpdateDate, CreateDate
                        FROM OITM
                        WHERE (@Search IS NULL OR ItemCode LIKE CONCAT('%', @Search, '%') OR ItemName LIKE CONCAT('%', @Search, '%'))
                          AND (@Since IS NULL OR UpdateDate >= @Since)
                        ORDER BY ItemCode
                        LIMIT " + topCount + ";";
                }
                else
                {
                    query = @"
                        SELECT TOP (" + topCount + @") 
                            ItemCode, ItemName, ItmsGrpCod as GroupCode, 
                            InvntryUom as UomCode, SalUnitMsr as SalesUom,
                            UpdateDate, CreateDate
                        FROM OITM
                        WHERE (@Search IS NULL OR ItemCode LIKE '%' + @Search + '%' OR ItemName LIKE '%' + @Search + '%')
                          AND (@Since IS NULL OR UpdateDate >= @Since)
                        ORDER BY ItemCode;";
                }

                var items = await conn.QueryAsync(query, new { Search = search, Since = since });
                return Ok(new { success = true, count = items.Count(), data = items });
            }
            catch (Exception ex)
            {
                // Fallback mock/sample response jika tabel OITM belum ada di db staging
                return Ok(new
                {
                    success = true,
                    isFallback = true,
                    message = "Integration Hub Staging Data Mode: " + ex.Message,
                    data = new[]
                    {
                        new { ItemCode = "ITM-001", ItemName = "Sparepart Engine Filter", UomCode = "PCS", GroupCode = 101 },
                        new { ItemCode = "ITM-002", ItemName = "Marine Lubricant Oil 20L", UomCode = "CAN", GroupCode = 102 },
                        new { ItemCode = "ITM-003", ItemName = "Gasket Cylinder Head", UomCode = "SET", GroupCode = 101 }
                    }
                });
            }
        }

        [HttpGet("business-partners")]
        public async Task<IActionResult> GetBusinessPartners([FromQuery] string? cardType = "S", [FromQuery] string? search = null)
        {
            try
            {
                DBConfig config = _configurationService.GetDatabaseConfig();
                using DbConnection conn = _connectionFactory.CreateConnection(config);
                await conn.OpenAsync();

                bool isHanaOrMysql = config.DBType == DatabaseType.MySql || 
                                     (!string.IsNullOrEmpty(config.Server) && config.Server.IndexOf("HANA", StringComparison.OrdinalIgnoreCase) >= 0);

                string query;
                if (isHanaOrMysql)
                {
                    query = @"
                        SELECT 
                            CardCode, CardName, CardType, GroupCode, Currency, ValidFor
                        FROM OCRD
                        WHERE (@CardType IS NULL OR CardType = @CardType)
                          AND (@Search IS NULL OR CardCode LIKE CONCAT('%', @Search, '%') OR CardName LIKE CONCAT('%', @Search, '%'))
                        ORDER BY CardName
                        LIMIT 200;";
                }
                else
                {
                    query = @"
                        SELECT TOP 200 
                            CardCode, CardName, CardType, GroupCode, Currency, ValidFor
                        FROM OCRD
                        WHERE (@CardType IS NULL OR CardType = @CardType)
                          AND (@Search IS NULL OR CardCode LIKE '%' + @Search + '%' OR CardName LIKE '%' + @Search + '%')
                        ORDER BY CardName;";
                }

                var bps = await conn.QueryAsync(query, new { CardType = cardType, Search = search });
                return Ok(new { success = true, count = bps.Count(), data = bps });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = true,
                    isFallback = true,
                    message = "Integration Hub Staging Data Mode: " + ex.Message,
                    data = new[]
                    {
                        new { CardCode = "VL-00001", CardName = "PT CATERPILLAR INDONESIA", CardType = "S", Currency = "IDR" },
                        new { CardCode = "VL-00002", CardName = "PT KOMATSU MARKETING", CardType = "S", Currency = "IDR" },
                        new { CardCode = "VL-00004", CardName = "Acme Associates (Vendor Utama)", CardType = "S", Currency = "IDR" }
                    }
                });
            }
        }

        [HttpGet("warehouses")]
        public async Task<IActionResult> GetWarehouses()
        {
            try
            {
                DBConfig config = _configurationService.GetDatabaseConfig();
                using DbConnection conn = _connectionFactory.CreateConnection(config);
                await conn.OpenAsync();

                string query = @"
                    SELECT WhsCode, WhsName, BPLid, Inactive
                    FROM OWHS
                    ORDER BY WhsCode;";

                var whs = await conn.QueryAsync(query);
                return Ok(new { success = true, count = whs.Count(), data = whs });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = true,
                    isFallback = true,
                    message = "Integration Hub Staging Data Mode: " + ex.Message,
                    data = new[]
                    {
                        new { WhsCode = "WH-IBT", WhsName = "Gudang Utama IBT", BPLid = 1 },
                        new { WhsCode = "WH-ISL", WhsName = "Gudang Logistik ISL", BPLid = 2 },
                        new { WhsCode = "WHS-D1", WhsName = "Gudang Dermaga Balikpapan", BPLid = 1 }
                    }
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
                DBConfig config = _configurationService.GetDatabaseConfig();
                using DbConnection conn = _connectionFactory.CreateConnection(config);
                await conn.OpenAsync();

                bool isHanaOrMysql = config.DBType == DatabaseType.MySql || 
                                     (!string.IsNullOrEmpty(config.Server) && config.Server.IndexOf("HANA", StringComparison.OrdinalIgnoreCase) >= 0);

                string query = isHanaOrMysql
                    ? @"SELECT ""Code"", ""Name"", ""Rate"", ""Inactive"", ""Category"" FROM OVTG ORDER BY ""Code"";"
                    : @"SELECT Code, Name, Rate, Inactive, Category FROM OVTG ORDER BY Code;";

                var rawRows = await conn.QueryAsync(query);
                var rows = rawRows.Select(r => new
                {
                    Code = (string)r.Code,
                    Name = (string)(r.Name ?? r.Code),
                    Rate = r.Rate != null ? Convert.ToDecimal(r.Rate) : 0m,
                    Inactive = (string)(r.Inactive ?? "tNO"),
                    Category = (string)(r.Category ?? string.Empty)
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                var fallback = new[]
                {
                    new { Code = "PPN11", Name = "PPN 11%", Rate = 11.0m, Inactive = "tNO", Category = "O" },
                    new { Code = "PPN12", Name = "PPN 12%", Rate = 12.0m, Inactive = "tNO", Category = "O" },
                    new { Code = "NON_PPN", Name = "Non PPN / Bebas Pajak", Rate = 0.0m, Inactive = "tNO", Category = "O" }
                };
                return Ok(new { success = true, isFallback = true, message = "Integration Hub Staging Data Mode: " + ex.Message, data = fallback, value = fallback });
            }
        }

        [HttpGet("uoms")]
        [HttpGet("unit-of-measurements")]
        public async Task<IActionResult> GetUoms()
        {
            try
            {
                DBConfig config = _configurationService.GetDatabaseConfig();
                using DbConnection conn = _connectionFactory.CreateConnection(config);
                await conn.OpenAsync();

                bool isHanaOrMysql = config.DBType == DatabaseType.MySql || 
                                     (!string.IsNullOrEmpty(config.Server) && config.Server.IndexOf("HANA", StringComparison.OrdinalIgnoreCase) >= 0);

                string query = isHanaOrMysql
                    ? @"SELECT ""UomEntry"" AS ""AbsEntry"", ""UomCode"" AS ""Code"", ""UomName"" AS ""Name"", ""Locked"" FROM OUOM ORDER BY ""UomCode"";"
                    : @"SELECT UomEntry AS AbsEntry, UomCode AS Code, UomName AS Name, Locked FROM OUOM ORDER BY UomCode;";

                var rawRows = await conn.QueryAsync(query);
                var rows = rawRows.Select(r => new
                {
                    AbsEntry = Convert.ToInt32(r.AbsEntry),
                    Code = (string)r.Code,
                    Name = (string)(r.Name ?? r.Code),
                    Locked = (string)(r.Locked ?? "tNO")
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                var fallback = new[]
                {
                    new { AbsEntry = 1, Code = "PCS", Name = "Pieces", Locked = "tNO" },
                    new { AbsEntry = 2, Code = "SET", Name = "Set", Locked = "tNO" },
                    new { AbsEntry = 3, Code = "CAN", Name = "Can", Locked = "tNO" },
                    new { AbsEntry = 4, Code = "LTR", Name = "Liter", Locked = "tNO" },
                    new { AbsEntry = 5, Code = "MTR", Name = "Meter", Locked = "tNO" },
                    new { AbsEntry = 6, Code = "BOX", Name = "Box", Locked = "tNO" },
                    new { AbsEntry = 7, Code = "ROLL", Name = "Roll", Locked = "tNO" },
                    new { AbsEntry = 8, Code = "DRUM", Name = "Drum 200L", Locked = "tNO" }
                };
                return Ok(new { success = true, isFallback = true, message = "Integration Hub Staging Data Mode: " + ex.Message, data = fallback, value = fallback });
            }
        }

        [HttpGet("uom-groups")]
        [HttpGet("unit-of-measurement-groups")]
        public async Task<IActionResult> GetUomGroups()
        {
            try
            {
                DBConfig config = _configurationService.GetDatabaseConfig();
                using DbConnection conn = _connectionFactory.CreateConnection(config);
                await conn.OpenAsync();

                bool isHanaOrMysql = config.DBType == DatabaseType.MySql || 
                                     (!string.IsNullOrEmpty(config.Server) && config.Server.IndexOf("HANA", StringComparison.OrdinalIgnoreCase) >= 0);

                string query = isHanaOrMysql
                    ? @"SELECT ""UgpEntry"" AS ""AbsEntry"", ""UgpCode"" AS ""Code"", ""UgpName"" AS ""Name"", ""BaseUom"" FROM OUGP ORDER BY ""UgpCode"";"
                    : @"SELECT UgpEntry AS AbsEntry, UgpCode AS Code, UgpName AS Name, BaseUom FROM OUGP ORDER BY UgpCode;";

                var rawRows = await conn.QueryAsync(query);
                var rows = rawRows.Select(r => new
                {
                    AbsEntry = Convert.ToInt32(r.AbsEntry),
                    Code = (string)r.Code,
                    Name = (string)(r.Name ?? r.Code),
                    BaseUom = r.BaseUom != null ? Convert.ToInt32(r.BaseUom) : 0
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                var fallback = new[]
                {
                    new { AbsEntry = -1, Code = "Manual", Name = "Manual UoM Group", BaseUom = -1 },
                    new { AbsEntry = 1, Code = "GRP-PCS", Name = "Group Pieces", BaseUom = 1 }
                };
                return Ok(new { success = true, isFallback = true, message = "Integration Hub Staging Data Mode: " + ex.Message, data = fallback, value = fallback });
            }
        }

        [HttpGet("bin-locations")]
        public async Task<IActionResult> GetBinLocations([FromQuery] string? warehouse = null)
        {
            try
            {
                DBConfig config = _configurationService.GetDatabaseConfig();
                using DbConnection conn = _connectionFactory.CreateConnection(config);
                await conn.OpenAsync();

                bool isHanaOrMysql = config.DBType == DatabaseType.MySql || 
                                     (!string.IsNullOrEmpty(config.Server) && config.Server.IndexOf("HANA", StringComparison.OrdinalIgnoreCase) >= 0);

                string query = isHanaOrMysql
                    ? @"SELECT ""AbsEntry"", ""BinCode"", ""WhsCode"" AS ""Warehouse"", ""Disabled"" AS ""Inactive"" 
                        FROM OBIN 
                        WHERE (@Warehouse IS NULL OR ""WhsCode"" = @Warehouse)
                        ORDER BY ""BinCode"";"
                    : @"SELECT AbsEntry, BinCode, WhsCode AS Warehouse, Disabled AS Inactive 
                        FROM OBIN 
                        WHERE (@Warehouse IS NULL OR WhsCode = @Warehouse)
                        ORDER BY BinCode;";

                var rawRows = await conn.QueryAsync(query, new { Warehouse = warehouse });
                var rows = rawRows.Select(r => new
                {
                    AbsEntry = Convert.ToInt32(r.AbsEntry),
                    BinCode = (string)r.BinCode,
                    Warehouse = (string)r.Warehouse,
                    Inactive = (string)(r.Inactive ?? "tNO")
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                var fallback = new[]
                {
                    new { AbsEntry = 101, BinCode = "HO-A01-R01-S01", Warehouse = "WHS-HO", Inactive = "tNO" },
                    new { AbsEntry = 102, BinCode = "HO-A01-R01-S02", Warehouse = "WHS-HO", Inactive = "tNO" },
                    new { AbsEntry = 103, BinCode = "HO-A02-R01-S01", Warehouse = "WHS-HO", Inactive = "tNO" },
                    new { AbsEntry = 201, BinCode = "IBT-SP-01-A", Warehouse = "WH-IBT", Inactive = "tNO" }
                };
                return Ok(new { success = true, isFallback = true, message = "Integration Hub Staging Data Mode: " + ex.Message, data = fallback, value = fallback });
            }
        }

        [HttpGet("cost-centers")]
        [HttpGet("profit-centers")]
        public async Task<IActionResult> GetCostCenters([FromQuery] int? dimension = null)
        {
            try
            {
                DBConfig config = _configurationService.GetDatabaseConfig();
                using DbConnection conn = _connectionFactory.CreateConnection(config);
                await conn.OpenAsync();

                bool isHanaOrMysql = config.DBType == DatabaseType.MySql || 
                                     (!string.IsNullOrEmpty(config.Server) && config.Server.IndexOf("HANA", StringComparison.OrdinalIgnoreCase) >= 0);

                string query = isHanaOrMysql
                    ? @"SELECT ""PrcCode"" AS ""CenterCode"", ""PrcName"" AS ""CenterName"", ""DimCode"" AS ""InWhichDimension"", ""Active"" 
                        FROM OPRC 
                        WHERE LOWER(""PrcCode"") NOT LIKE 'centr_z%'
                          AND (@Dimension IS NULL OR ""DimCode"" = @Dimension)
                        ORDER BY ""PrcCode"";"
                    : @"SELECT PrcCode AS CenterCode, PrcName AS CenterName, DimCode AS InWhichDimension, Active 
                        FROM OPRC 
                        WHERE PrcCode NOT LIKE 'Centr_z%'
                          AND (@Dimension IS NULL OR DimCode = @Dimension)
                        ORDER BY PrcCode;";

                var rawRows = await conn.QueryAsync(query, new { Dimension = dimension });
                var rows = rawRows.Select(r => new
                {
                    CenterCode = (string)r.CenterCode,
                    CenterName = (string)(r.CenterName ?? r.CenterCode),
                    InWhichDimension = Convert.ToInt32(r.InWhichDimension),
                    Active = (string)(r.Active ?? "tYES")
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                var fallback = new[]
                {
                    new { CenterCode = "CC-OPS-01", CenterName = "Operasional Armada Laut", InWhichDimension = 1, Active = "tYES" },
                    new { CenterCode = "CC-ENG-02", CenterName = "Engineering & Workshop Maintenance", InWhichDimension = 1, Active = "tYES" },
                    new { CenterCode = "CC-LOG-03", CenterName = "Logistik & Gudang Transit", InWhichDimension = 1, Active = "tYES" },
                    new { CenterCode = "CC-VSL-01", CenterName = "Tugboat IBT Pioneer", InWhichDimension = 2, Active = "tYES" },
                    new { CenterCode = "CC-DOCK-04", CenterName = "Galangan Perbaikan Kapal", InWhichDimension = 2, Active = "tNO" }
                };
                return Ok(new { success = true, isFallback = true, message = "Integration Hub Staging Data Mode: " + ex.Message, data = fallback, value = fallback });
            }
        }

        [HttpGet("payment-terms")]
        [HttpGet("payment-terms-types")]
        public async Task<IActionResult> GetPaymentTerms()
        {
            try
            {
                DBConfig config = _configurationService.GetDatabaseConfig();
                using DbConnection conn = _connectionFactory.CreateConnection(config);
                await conn.OpenAsync();

                bool isHanaOrMysql = config.DBType == DatabaseType.MySql || 
                                     (!string.IsNullOrEmpty(config.Server) && config.Server.IndexOf("HANA", StringComparison.OrdinalIgnoreCase) >= 0);

                string query = isHanaOrMysql
                    ? @"SELECT ""GroupNum"" AS ""GroupNumber"", ""PymntGroup"" AS ""PaymentTermsGroupName"", ""ExtraDays"", ""ExtraMonth"" AS ""ExtraMonths"", ""DiscPrcnt"" AS ""DiscountPercent"" 
                        FROM OCTG ORDER BY ""GroupNum"";"
                    : @"SELECT GroupNum AS GroupNumber, PymntGroup AS PaymentTermsGroupName, ExtraDays, ExtraMonth AS ExtraMonths, DiscPrcnt AS DiscountPercent 
                        FROM OCTG ORDER BY GroupNum;";

                var rawRows = await conn.QueryAsync(query);
                var rows = rawRows.Select(r => new
                {
                    GroupNumber = Convert.ToInt32(r.GroupNumber),
                    PaymentTermsGroupName = (string)(r.PaymentTermsGroupName ?? $"Term #{r.GroupNumber}"),
                    ExtraDays = r.ExtraDays != null ? Convert.ToInt32(r.ExtraDays) : 0,
                    ExtraMonths = r.ExtraMonths != null ? Convert.ToInt32(r.ExtraMonths) : 0,
                    DiscountPercent = r.DiscountPercent != null ? Convert.ToDecimal(r.DiscountPercent) : 0m
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                var fallback = new[]
                {
                    new { GroupNumber = 1, PaymentTermsGroupName = "COD / Tunai Langsung", ExtraDays = 0, ExtraMonths = 0, DiscountPercent = 0.0m },
                    new { GroupNumber = 2, PaymentTermsGroupName = "Net 30 Hari", ExtraDays = 30, ExtraMonths = 0, DiscountPercent = 0.0m },
                    new { GroupNumber = 3, PaymentTermsGroupName = "Net 60 Hari", ExtraDays = 60, ExtraMonths = 0, DiscountPercent = 0.0m },
                    new { GroupNumber = 4, PaymentTermsGroupName = "2/10 Net 30", ExtraDays = 30, ExtraMonths = 0, DiscountPercent = 2.0m }
                };
                return Ok(new { success = true, isFallback = true, message = "Integration Hub Staging Data Mode: " + ex.Message, data = fallback, value = fallback });
            }
        }

        [HttpGet("freight")]
        [HttpGet("additional-expenses")]
        public async Task<IActionResult> GetFreight()
        {
            try
            {
                DBConfig config = _configurationService.GetDatabaseConfig();
                using DbConnection conn = _connectionFactory.CreateConnection(config);
                await conn.OpenAsync();

                bool isHanaOrMysql = config.DBType == DatabaseType.MySql || 
                                     (!string.IsNullOrEmpty(config.Server) && config.Server.IndexOf("HANA", StringComparison.OrdinalIgnoreCase) >= 0);

                string query = isHanaOrMysql
                    ? @"SELECT ""ExpnsCode"" AS ""ExpensCode"", ""ExpnsName"" AS ""Name"", ""RevnAcct"" AS ""RevenuesAccount"", ""ExpnsAcct"" AS ""ExpenseAccount"", ""VatGroup"" AS ""OutputVATGroup"", ""VatGroupI"" AS ""InputVATGroup"", ""TaxLiable"" 
                        FROM OEXD ORDER BY ""ExpnsCode"";"
                    : @"SELECT ExpnsCode AS ExpensCode, ExpnsName AS Name, RevnAcct AS RevenuesAccount, ExpnsAcct AS ExpenseAccount, VatGroup AS OutputVATGroup, VatGroupI AS InputVATGroup, TaxLiable 
                        FROM OEXD ORDER BY ExpnsCode;";

                var rawRows = await conn.QueryAsync(query);
                var rows = rawRows.Select(r => new
                {
                    ExpensCode = Convert.ToInt32(r.ExpensCode),
                    Name = (string)(r.Name ?? $"Freight #{r.ExpensCode}"),
                    RevenuesAccount = (string?)r.RevenuesAccount,
                    ExpenseAccount = (string?)r.ExpenseAccount,
                    OutputVATGroup = (string?)r.OutputVATGroup,
                    InputVATGroup = (string?)r.InputVATGroup,
                    TaxLiable = (string)(r.TaxLiable ?? "tNO")
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                var fallback = new[]
                {
                    new { ExpensCode = 1, Name = "Ongkos Angkut Truk & Ekspedisi Darat (Trucking)", RevenuesAccount = "410101", ExpenseAccount = "510201", OutputVATGroup = "PPN11", InputVATGroup = "PPN11", TaxLiable = "tYES" },
                    new { ExpensCode = 2, Name = "Freight Pelayaran Kapal & Tongkang (Barge/Tug)", RevenuesAccount = "410102", ExpenseAccount = "510202", OutputVATGroup = "NON_PPN", InputVATGroup = "NON_PPN", TaxLiable = "tNO" },
                    new { ExpensCode = 3, Name = "Asuransi Pengangkutan Kargo Laut & Darat", RevenuesAccount = "410103", ExpenseAccount = "510203", OutputVATGroup = "PPN11", InputVATGroup = "PPN11", TaxLiable = "tYES" },
                    new { ExpensCode = 4, Name = "Biaya Bongkar Muat Pelabuhan (Stevedoring & Handling)", RevenuesAccount = "410104", ExpenseAccount = "510204", OutputVATGroup = "PPN11", InputVATGroup = "PPN11", TaxLiable = "tYES" },
                    new { ExpensCode = 5, Name = "Demurrage & Biaya Penumpukan Kontainer (Storage)", RevenuesAccount = "410105", ExpenseAccount = "510205", OutputVATGroup = "PPN11", InputVATGroup = "PPN11", TaxLiable = "tYES" }
                };
                return Ok(new { success = true, isFallback = true, message = "Integration Hub Staging Data Mode: " + ex.Message, data = fallback, value = fallback });
            }
        }

        [HttpGet("projects")]
        public async Task<IActionResult> GetProjects([FromQuery] string? search = null)
        {
            try
            {
                DBConfig config = _configurationService.GetDatabaseConfig();
                using DbConnection conn = _connectionFactory.CreateConnection(config);
                await conn.OpenAsync();

                bool isHanaOrMysql = config.DBType == DatabaseType.MySql || 
                                     (!string.IsNullOrEmpty(config.Server) && config.Server.IndexOf("HANA", StringComparison.OrdinalIgnoreCase) >= 0);

                string query = isHanaOrMysql
                    ? @"SELECT ""PrjCode"" AS ""Code"", ""PrjName"" AS ""Name"", ""ValidFrom"", ""ValidTo"", ""Active"" 
                        FROM OPRJ 
                        WHERE (@Search IS NULL OR ""PrjCode"" LIKE CONCAT('%', @Search, '%') OR ""PrjName"" LIKE CONCAT('%', @Search, '%'))
                        ORDER BY ""PrjCode"";"
                    : @"SELECT PrjCode AS Code, PrjName AS Name, ValidFrom, ValidTo, Active 
                        FROM OPRJ 
                        WHERE (@Search IS NULL OR PrjCode LIKE '%' + @Search + '%' OR PrjName LIKE '%' + @Search + '%')
                        ORDER BY PrjCode;";

                var rawRows = await conn.QueryAsync(query, new { Search = search });
                var rows = rawRows.Select(r => new
                {
                    Code = (string)r.Code,
                    Name = (string)(r.Name ?? r.Code),
                    ValidFrom = r.ValidFrom != null ? Convert.ToDateTime(r.ValidFrom).ToString("yyyy-MM-dd") : null,
                    ValidTo = r.ValidTo != null ? Convert.ToDateTime(r.ValidTo).ToString("yyyy-MM-dd") : null,
                    Active = (string)(r.Active ?? "tYES")
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                var fallback = new[]
                {
                    new { Code = "PRJ-2026-001", Name = "Overhaul Mesin Utama Tugboat Pioneer", ValidFrom = "2026-01-01", ValidTo = "2026-12-31", Active = "tYES" },
                    new { Code = "PRJ-2026-002", Name = "Pengadaan Fasilitas Terminal Mahakam", ValidFrom = "2026-02-01", ValidTo = "2026-11-30", Active = "tYES" },
                    new { Code = "PRJ-2026-003", Name = "Drydocking Tongkang Barge Mega 08", ValidFrom = "2026-03-15", ValidTo = "2026-09-30", Active = "tYES" },
                    new { Code = "PRJ-2026-004", Name = "Modernisasi Sistem Navigasi & IT Kapal", ValidFrom = "2026-01-10", ValidTo = "2026-08-31", Active = "tYES" },
                    new { Code = "PRJ-2025-099", Name = "Pembangunan Dermaga Jetty 2 (Selesai)", ValidFrom = "2025-01-01", ValidTo = "2025-12-31", Active = "tNO" }
                };
                return Ok(new { success = true, isFallback = true, message = "Integration Hub Staging Data Mode: " + ex.Message, data = fallback, value = fallback });
            }
        }

        [HttpGet("item-groups")]
        public async Task<IActionResult> GetItemGroups()
        {
            try
            {
                DBConfig config = _configurationService.GetDatabaseConfig();
                using DbConnection conn = _connectionFactory.CreateConnection(config);
                await conn.OpenAsync();

                bool isHanaOrMysql = config.DBType == DatabaseType.MySql || 
                                     (!string.IsNullOrEmpty(config.Server) && config.Server.IndexOf("HANA", StringComparison.OrdinalIgnoreCase) >= 0);

                string query = isHanaOrMysql
                    ? @"SELECT ""ItmsGrpCod"" AS ""Number"", ""ItmsGrpNam"" AS ""GroupName"" FROM OITB ORDER BY ""ItmsGrpCod"";"
                    : @"SELECT ItmsGrpCod AS Number, ItmsGrpNam AS GroupName FROM OITB ORDER BY ItmsGrpCod;";

                var rawRows = await conn.QueryAsync(query);
                var rows = rawRows.Select(r => new
                {
                    Number = Convert.ToInt32(r.Number),
                    GroupName = (string)r.GroupName
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                var fallback = new[]
                {
                    new { Number = 101, GroupName = "Mechanical & Engine Parts" },
                    new { Number = 102, GroupName = "Electrical & Navigation Equipment" },
                    new { Number = 103, GroupName = "Deck Consumables & Paints" },
                    new { Number = 104, GroupName = "Safety & Lifesaving Appliances" }
                };
                return Ok(new { success = true, isFallback = true, message = "Integration Hub Staging Data Mode: " + ex.Message, data = fallback, value = fallback });
            }
        }

        [HttpGet("hs-codes")]
        public async Task<IActionResult> GetHsCodes([FromQuery] string? search = null)
        {
            try
            {
                DBConfig config = _configurationService.GetDatabaseConfig();
                using DbConnection conn = _connectionFactory.CreateConnection(config);
                await conn.OpenAsync();

                bool isHanaOrMysql = config.DBType == DatabaseType.MySql || 
                                     (!string.IsNullOrEmpty(config.Server) && config.Server.IndexOf("HANA", StringComparison.OrdinalIgnoreCase) >= 0);

                string query = isHanaOrMysql
                    ? @"SELECT ""Chapter"" AS ""HsCode"", ""Dscription"" AS ""Desc"" 
                        FROM OCHS 
                        WHERE (@Search IS NULL OR ""Chapter"" LIKE CONCAT('%', @Search, '%') OR ""Dscription"" LIKE CONCAT('%', @Search, '%'))
                        ORDER BY ""Chapter"";"
                    : @"SELECT Chapter AS HsCode, Dscription AS [Desc] 
                        FROM OCHS 
                        WHERE (@Search IS NULL OR Chapter LIKE '%' + @Search + '%' OR Dscription LIKE '%' + @Search + '%')
                        ORDER BY Chapter;";

                var rawRows = await conn.QueryAsync(query, new { Search = search });
                var rows = rawRows.Select(r => new
                {
                    HsCode = (string)r.HsCode,
                    Desc = (string)(r.Desc ?? string.Empty),
                    Lartas = "tidak_lartas"
                }).ToList();

                return Ok(new { success = true, count = rows.Count, data = rows, value = rows });
            }
            catch (Exception ex)
            {
                var fallback = new[]
                {
                    new { HsCode = "8481.80.90", Desc = "Kran & Katup Pipa Kapal", Lartas = "tidak_lartas" },
                    new { HsCode = "8501.52.00", Desc = "Motor Listrik AC 3-Phase", Lartas = "lartas" },
                    new { HsCode = "8409.99.00", Desc = "Suku Cadang Mesin Diesel Kapal", Lartas = "tidak_lartas" }
                };
                return Ok(new { success = true, isFallback = true, message = "Integration Hub Staging Data Mode: " + ex.Message, data = fallback, value = fallback });
            }
        }

        [HttpGet("part-numbers")]
        public async Task<IActionResult> GetPartNumbers([FromQuery] string? search = null, [FromQuery] int top = 100, [FromQuery] int skip = 0)
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
                            H.""DocEntry"", 
                            H.""U_SOL_PartNumber"" AS ""PartNumber"", 
                            H.""U_SOL_Description"" AS ""Description"",
                            D.""U_SOL_ItemCode"" AS ""ItemCode""
                        FROM ""@SOL_PNUM_H"" H
                        LEFT JOIN ""@SOL_PNUM_D"" D ON H.""DocEntry"" = D.""DocEntry""
                        WHERE (@Search IS NULL OR H.""U_SOL_PartNumber"" LIKE CONCAT('%', @Search, '%') OR H.""U_SOL_Description"" LIKE CONCAT('%', @Search, '%'))
                        ORDER BY H.""DocEntry""
                        LIMIT " + topCount + @" OFFSET " + skipCount + @";";
                }
                else
                {
                    query = @"
                        SELECT TOP (" + (topCount + skipCount) + @")
                            H.DocEntry, 
                            H.U_SOL_PartNumber AS PartNumber, 
                            H.U_SOL_Description AS Description,
                            D.U_SOL_ItemCode AS ItemCode
                        FROM [@SOL_PNUM_H] H
                        LEFT JOIN [@SOL_PNUM_D] D ON H.DocEntry = D.DocEntry
                        WHERE (@Search IS NULL OR H.U_SOL_PartNumber LIKE '%' + @Search + '%' OR H.U_SOL_Description LIKE '%' + @Search + '%')
                        ORDER BY H.DocEntry;";
                }

                var rawRows = await conn.QueryAsync(query, new { Search = search });
                var mappedRows = rawRows.Select(r => new
                {
                    DocEntry = Convert.ToInt32(r.DocEntry),
                    PartNumber = (string)(r.PartNumber ?? string.Empty),
                    Description = (string)(r.Description ?? string.Empty),
                    ItemCode = (string?)r.ItemCode
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
                var fallback = new[]
                {
                    new { DocEntry = 1, PartNumber = "PN-WARTSILA-001", Description = "Main Engine Cylinder Head Gasket", ItemCode = (string?)"ITM-003" },
                    new { DocEntry = 2, PartNumber = "PN-CAT-3512B-01", Description = "Fuel Injector Nozzle Caterpillar", ItemCode = (string?)"ITM-001" },
                    new { DocEntry = 3, PartNumber = "PN-YANMAR-6EY-01", Description = "Oil Filter Cartridge Yanmar", ItemCode = (string?)"ITM-002" }
                };
                return Ok(new { success = true, isFallback = true, message = "Integration Hub Staging Data Mode: " + ex.Message, data = fallback, value = fallback });
            }
        }
    }
}

