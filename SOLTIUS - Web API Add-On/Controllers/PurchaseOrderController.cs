using Microsoft.AspNetCore.Mvc;
using SOLTIUS_Web_API_Add_On.Models.Transaction;
using SOLTIUS_Web_API_Add_On.Repositories;
using SOLTIUS_Web_API_Add_On.Services;
using SOLTIUS_Web_API_Add_On.Services.AuditLog;
using System.Text.RegularExpressions;

namespace SOLTIUS_Web_API_Add_On.Controllers
{
    [Route("api/[controller]")]
    public class PurchaseOrderController : CustomApiControllerBase
    {
        private readonly IPurchaseOrderService _service;
        private readonly IPurchaseOrderRepository _repository;
        private readonly IAuditLogService _auditLog;

        public PurchaseOrderController(
            IPurchaseOrderService service,
            IPurchaseOrderRepository repository,
            IAuditLogService auditLog)
        {
            _service = service;
            _repository = repository;
            _auditLog = auditLog;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePurchaseOrder([FromBody] PurchaseOrderHeader purchaseOrder)
        {
            if (purchaseOrder == null)
                return BadRequest(new { success = false, message = "Invalid payload." });

            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState });

            if (purchaseOrder.DocumentLines == null || purchaseOrder.DocumentLines.Count == 0)
                return BadRequest(new { success = false, message = "DocumentLines is Required." });

            try
            {
                // Inisialisasi UDF & mapping fallback field jika ada
                purchaseOrder.GetUdfJson();

                await _service.SavePurchaseOrderAsync(purchaseOrder);

                _auditLog.LogApiRequest(
                    "POST", "/api/PurchaseOrder", 200,
                    HttpContext.Connection?.RemoteIpAddress?.ToString() ?? "",
                    "",
                    $"webTx={purchaseOrder.WebTxNumber ?? "-"} cardCode={purchaseOrder.CardCode} lines={purchaseOrder.DocumentLines.Count}");

                return Ok(new
                {
                    success = true,
                    message = "Purchase Order Received",
                    webTxNumber = purchaseOrder.WebTxNumber
                });
            }
            catch (Exception ex)
            {
                // Idempotent rejection: Jika dokumen sudah ter-posting ke SAP
                if (ex.Message.Contains("Dokumen sudah ter posting ke SAP dengan nomor SAP"))
                {
                    string docNum = "";
                    var match = Regex.Match(ex.Message, @"nomor SAP\s+(\S+)");
                    if (match.Success) docNum = match.Groups[1].Value;

                    return StatusCode(409, new
                    {
                        success = false,
                        alreadyPostedToSap = true,
                        sapDocNum = docNum,
                        webTxNumber = purchaseOrder.WebTxNumber,
                        message = ex.Message
                    });
                }

                // Idempotent rejection: Jika dokumen masih berada di antrean staging Hub
                if (ex.Message.Contains("sudah ada di antrean staging Hub"))
                {
                    return StatusCode(409, new
                    {
                        success = false,
                        alreadyPostedToSap = false,
                        webTxNumber = purchaseOrder.WebTxNumber,
                        message = ex.Message
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("status/{webTxNumber}")]
        public async Task<IActionResult> GetStatus(string webTxNumber)
        {
            if (string.IsNullOrWhiteSpace(webTxNumber))
                return BadRequest(new { success = false, message = "webTxNumber is required." });

            var existing = await _repository.GetStatusByWebTxNumberAsync(webTxNumber);
            if (existing == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Dokumen {webTxNumber} tidak ditemukan di staging Hub."
                });
            }

            bool isPosted = existing.ProcessStatus == 1 || !string.IsNullOrWhiteSpace(existing.DocEntry);

            return Ok(new
            {
                success = true,
                webTxNumber = existing.WebTxNumber,
                processStatus = existing.ProcessStatus,
                statusText = existing.ProcessStatus switch
                {
                    0 => "Sync to Hub (Pending SAP)",
                    1 => "Sync to SAP (Success)",
                    2 => "Failed",
                    _ => "Unknown"
                },
                isPostedToSap = isPosted,
                sapDocNum = existing.DocEntry,
                processedAt = existing.ProcessedAt,
                errorMessage = existing.ErrorMessage,
                message = isPosted
                    ? $"Dokumen sudah ter posting ke SAP dengan nomor SAP {existing.DocEntry}"
                    : (existing.ProcessStatus == 0 ? "Dokumen berada di antrean Hub" : existing.ErrorMessage)
            });
        }

        [HttpGet("sync-status")]
        public async Task<IActionResult> GetSyncStatus([FromQuery] DateTime? since)
        {
            var list = await _repository.GetSyncStatusListAsync(since);
            return Ok(new
            {
                success = true,
                count = list.Count(),
                data = list
            });
        }
[HttpDelete("{webTxNumber}")]
        public async Task<IActionResult> DeleteFromStaging(string webTxNumber)
        {
            if (string.IsNullOrWhiteSpace(webTxNumber))
                return BadRequest(new { success = false, message = "webTxNumber is required." });

            var existing = await _repository.GetStatusByWebTxNumberAsync(webTxNumber);
            if (existing == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Dokumen {webTxNumber} tidak ditemukan di staging Hub."
                });
            }

            if (existing.ProcessStatus == 1 || !string.IsNullOrWhiteSpace(existing.DocEntry))
            {
                return StatusCode(409, new
                {
                    success = false,
                    message = $"Dokumen {webTxNumber} sudah ter-posting ke SAP dengan nomor {existing.DocEntry}. Gunakan POST /api/PurchaseOrder/cancel untuk membatalkan di SAP."
                });
            }

            bool deleted = await _repository.DeleteFromStagingAsync(webTxNumber);
            return Ok(new
            {
                success = deleted,
                webTxNumber = webTxNumber,
                message = $"Dokumen {webTxNumber} berhasil dihapus dari antrean Integration Hub."
            });
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelPurchaseOrder([FromBody] CancelPurchaseOrderDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.WebTxNumber))
                return BadRequest(new { success = false, message = "webTxNumber is required." });

            var existing = await _repository.GetStatusByWebTxNumberAsync(request.WebTxNumber);
            if (existing == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Dokumen {request.WebTxNumber} tidak ditemukan di Integration Hub."
                });
            }

            await _repository.RequestCancelInSapAsync(request.WebTxNumber, request.SapDocNum ?? existing.DocEntry, request.Reason);

            return Ok(new
            {
                success = true,
                webTxNumber = request.WebTxNumber,
                sapDocNum = request.SapDocNum ?? existing.DocEntry,
                message = $"Permintaan pembatalan dokumen {request.WebTxNumber} berhasil diterima oleh Integration Hub untuk diproses ke SAP."
            });
        }
    }

    public class CancelPurchaseOrderDto
    {
        public string WebTxNumber { get; set; }
        public string SapDocNum { get; set; }
        public string Reason { get; set; }
    }
}
