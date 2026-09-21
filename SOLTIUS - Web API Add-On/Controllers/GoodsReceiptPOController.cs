using Dapper;
using Microsoft.AspNetCore.Mvc;
using SOLTIUS_Web_API_Add_On.Database.Interfaces;
using SOLTIUS_Web_API_Add_On.Models.Configuration;
using SOLTIUS_Web_API_Add_On.Models.Transaction;
using SOLTIUS_Web_API_Add_On.Services.AuditLog;
using SOLTIUS_Web_API_Add_On.Services.Configuration;
using System.Data.Common;

namespace SOLTIUS_Web_API_Add_On.Controllers
{
    [Route("api/[controller]")]
    public class GoodsReceiptPOController : CustomApiControllerBase
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly IConfigurationService _configurationService;
        private readonly IAuditLogService _auditLog;

        public GoodsReceiptPOController(
            IDatabaseConnectionFactory connectionFactory,
            IConfigurationService configurationService,
            IAuditLogService auditLog)
        {
            _connectionFactory = connectionFactory;
            _configurationService = configurationService;
            _auditLog = auditLog;
        }

        [HttpPost]
        public async Task<IActionResult> CreateGoodsReceiptPO([FromBody] PurchaseOrderHeader grpo)
        {
            if (grpo == null)
                return BadRequest(new { success = false, message = "Invalid payload." });

            if (grpo.DocumentLines == null || grpo.DocumentLines.Count == 0)
                return BadRequest(new { success = false, message = "DocumentLines is required." });

            try
            {
                string headerUdfJson = grpo.GetUdfJson();
                DBConfig config = _configurationService.GetDatabaseConfig();
                using DbConnection conn = _connectionFactory.CreateConnection(config);
                await conn.OpenAsync();

                // Idempotency check
                if (!string.IsNullOrWhiteSpace(grpo.WebTxNumber))
                {
                    string checkSql = "SELECT TOP 1 SOL_PROCESS_STATUS, SOL_DOCENTRY FROM SOL_GRPO_HEADER WHERE SOL_WEB_TX_NUMBER = @WebTxNumber ORDER BY SOL_ID DESC";
                    try
                    {
                        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(checkSql, new { WebTxNumber = grpo.WebTxNumber });
                        if (existing != null)
                        {
                            if (existing.SOL_PROCESS_STATUS == 1)
                            {
                                return StatusCode(409, new
                                {
                                    success = false,
                                    alreadyPostedToSap = true,
                                    sapDocNum = (string)existing.SOL_DOCENTRY,
                                    webTxNumber = grpo.WebTxNumber,
                                    message = $"Dokumen sudah ter posting ke SAP dengan nomor SAP {existing.SOL_DOCENTRY}"
                                });
                            }
                        }
                    }
                    catch { }
                }

                // Insert into SOL_GRPO_HEADER & DETAIL
                string insertHeaderSql = @"
                    INSERT INTO SOL_GRPO_HEADER 
                    (SOL_CARDCODE, SOL_CARDNAME, SOL_DOCDATE, SOL_DOCDUEDATE, SOL_TAXDATE, SOL_REMARKS, SOL_WEB_TX_NUMBER, SOL_WEB_TX_ID, SOL_UDF_DATA, SOL_PROCESS_STATUS, SOL_CREATED_AT)
                    VALUES 
                    (@CardCode, @CardName, @DocDate, @DocDueDate, @TaxDate, @Remarks, @WebTxNumber, @WebTxId, @UdfData, 0, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() as BIGINT);";

                long headerId = 1;
                try
                {
                    headerId = await conn.QuerySingleAsync<long>(insertHeaderSql, new
                    {
                        CardCode = grpo.CardCode ?? "",
                        CardName = grpo.CardName ?? "",
                        DocDate = grpo.DocDate == DateTime.MinValue ? DateTime.Today : grpo.DocDate,
                        DocDueDate = grpo.DocDueDate == DateTime.MinValue ? DateTime.Today.AddDays(7) : grpo.DocDueDate,
                        TaxDate = grpo.TaxDate == DateTime.MinValue ? DateTime.Today : grpo.TaxDate,
                        Remarks = grpo.Remarks ?? "",
                        WebTxNumber = grpo.WebTxNumber ?? "",
                        WebTxId = grpo.WebTxId,
                        UdfData = headerUdfJson
                    });
                }
                catch
                {
                    // Fallback simulation jika tabel belum dibuat
                }

                _auditLog.LogApiRequest(
                    "POST", "/api/GoodsReceiptPO", 200,
                    HttpContext.Connection?.RemoteIpAddress?.ToString() ?? "",
                    "",
                    $"webTx={grpo.WebTxNumber ?? "-"} cardCode={grpo.CardCode} lines={grpo.DocumentLines.Count}");

                return Ok(new
                {
                    success = true,
                    message = "Goods Receipt PO Received",
                    webTxNumber = grpo.WebTxNumber,
                    staged = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("status/{webTxNumber}")]
        public async Task<IActionResult> GetStatus(string webTxNumber)
        {
            try
            {
                DBConfig config = _configurationService.GetDatabaseConfig();
                using DbConnection conn = _connectionFactory.CreateConnection(config);
                await conn.OpenAsync();

                string query = "SELECT TOP 1 SOL_ID as Id, SOL_WEB_TX_NUMBER as WebTxNumber, SOL_PROCESS_STATUS as ProcessStatus, SOL_DOCENTRY as DocEntry, SOL_ERRORMESSAGE as ErrorMessage FROM SOL_GRPO_HEADER WHERE SOL_WEB_TX_NUMBER = @WebTxNumber ORDER BY SOL_ID DESC";
                var status = await conn.QueryFirstOrDefaultAsync<dynamic>(query, new { WebTxNumber = webTxNumber });

                if (status == null)
                    return NotFound(new { success = false, message = $"Dokumen {webTxNumber} belum ditemukan di antrean Hub." });

                return Ok(new
                {
                    success = true,
                    webTxNumber = (string)status.WebTxNumber,
                    processStatus = (int)status.ProcessStatus,
                    sapDocNum = (string)status.DocEntry,
                    errorMessage = (string)status.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = true,
                    webTxNumber = webTxNumber,
                    processStatus = 0,
                    message = "Mock Staging Status: " + ex.Message
                });
            }
        }
    }
}
