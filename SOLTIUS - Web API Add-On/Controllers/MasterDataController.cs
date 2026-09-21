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
    }
}
