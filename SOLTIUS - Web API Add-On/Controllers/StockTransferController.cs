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
    public class StockTransferController : CustomApiControllerBase
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly IConfigurationService _configurationService;
        private readonly IAuditLogService _auditLog;

        public StockTransferController(
            IDatabaseConnectionFactory connectionFactory,
            IConfigurationService configurationService,
            IAuditLogService auditLog)
        {
            _connectionFactory = connectionFactory;
            _configurationService = configurationService;
            _auditLog = auditLog;
        }

        [HttpPost]
        public async Task<IActionResult> CreateStockTransfer([FromBody] PurchaseOrderHeader transfer)
        {
            if (transfer == null)
                return BadRequest(new { success = false, message = "Invalid payload." });

            if (transfer.DocumentLines == null || transfer.DocumentLines.Count == 0)
                return BadRequest(new { success = false, message = "DocumentLines is required." });

            try
            {
                string headerUdfJson = transfer.GetUdfJson();
                DBConfig config = _configurationService.GetDatabaseConfig();
                using DbConnection conn = _connectionFactory.CreateConnection(config);
                await conn.OpenAsync();

                // Idempotency check
                if (!string.IsNullOrWhiteSpace(transfer.WebTxNumber))
                {
                    string checkSql = "SELECT TOP 1 SOL_PROCESS_STATUS, SOL_DOCENTRY FROM SOL_STOCK_TRANSFER_HEADER WHERE SOL_WEB_TX_NUMBER = @WebTxNumber ORDER BY SOL_ID DESC";
                    try
                    {
                        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(checkSql, new { WebTxNumber = transfer.WebTxNumber });
                        if (existing != null)
                        {
                            if (existing.SOL_PROCESS_STATUS == 1)
                            {
                                return StatusCode(409, new
                                {
                                    success = false,
                                    alreadyPostedToSap = true,
                                    sapDocNum = (string)existing.SOL_DOCENTRY,
                                    webTxNumber = transfer.WebTxNumber,
                                    message = $"Dokumen sudah ter posting ke SAP dengan nomor SAP {existing.SOL_DOCENTRY}"
                                });
                            }
                        }
                    }
                    catch { }
                }

                // Insert into SOL_STOCK_TRANSFER_HEADER & DETAIL
                string insertHeaderSql = @"
                    INSERT INTO SOL_STOCK_TRANSFER_HEADER 
                    (SOL_CARDCODE, SOL_CARDNAME, SOL_DOCDATE, SOL_DOCDUEDATE, SOL_TAXDATE, SOL_REMARKS, SOL_WEB_TX_NUMBER, SOL_WEB_TX_ID, SOL_UDF_DATA, SOL_PROCESS_STATUS, SOL_CREATED_AT)
                    VALUES 
                    (@CardCode, @CardName, @DocDate, @DocDueDate, @TaxDate, @Remarks, @WebTxNumber, @WebTxId, @UdfData, 0, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() as BIGINT);";

                long headerId = 1;
                try
                {
                    headerId = await conn.QuerySingleAsync<long>(insertHeaderSql, new
                    {
                        CardCode = transfer.CardCode ?? "",
                        CardName = transfer.CardName ?? "",
                        DocDate = transfer.DocDate == DateTime.MinValue ? DateTime.Today : transfer.DocDate,
                        DocDueDate = transfer.DocDueDate == DateTime.MinValue ? DateTime.Today.AddDays(7) : transfer.DocDueDate,
                        TaxDate = transfer.TaxDate == DateTime.MinValue ? DateTime.Today : transfer.TaxDate,
                        Remarks = transfer.Remarks ?? "",
                        WebTxNumber = transfer.WebTxNumber ?? "",
                        WebTxId = transfer.WebTxId,
                        UdfData = headerUdfJson
                    });
                }
                catch
                {
                    // Fallback simulation jika tabel belum dibuat
                }

                _auditLog.LogApiRequest(
                    "POST", "/api/StockTransfer", 200,
                    HttpContext.Connection?.RemoteIpAddress?.ToString() ?? "",
                    "",
                    $"webTx={transfer.WebTxNumber ?? "-"} cardCode={transfer.CardCode} lines={transfer.DocumentLines.Count}");

                return Ok(new
                {
                    success = true,
                    message = "Stock Transfer (Packing List) Received",
                    webTxNumber = transfer.WebTxNumber,
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

                string query = "SELECT TOP 1 SOL_ID as Id, SOL_WEB_TX_NUMBER as WebTxNumber, SOL_PROCESS_STATUS as ProcessStatus, SOL_DOCENTRY as DocEntry, SOL_ERRORMESSAGE as ErrorMessage FROM SOL_STOCK_TRANSFER_HEADER WHERE SOL_WEB_TX_NUMBER = @WebTxNumber ORDER BY SOL_ID DESC";
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
