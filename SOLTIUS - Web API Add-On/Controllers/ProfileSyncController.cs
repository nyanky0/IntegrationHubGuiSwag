using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOLTIUS_Web_API_Add_On.Services.AuditLog;
using SOLTIUS_Web_API_Add_On.Services.Configuration;

namespace SOLTIUS_Web_API_Add_On.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class ProfileSyncController : CustomApiControllerBase
    {
        private readonly IConfigurationService _configService;
        private readonly IAuditLogService _auditLog;

        public ProfileSyncController(IConfigurationService configService, IAuditLogService auditLog)
        {
            _configService = configService;
            _auditLog = auditLog;
        }

        [HttpPost]
        [Consumes("application/xml")]
        public async Task<IActionResult> SyncProfile()
        {
            using var reader = new StreamReader(Request.Body);
            string xml = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(xml))
                return BadRequest(new { message = "Configuration XML is empty." });

            try
            {
                await _configService.ConfigureAsync(xml);

                _auditLog.LogApiRequest(
                    "POST", "/api/ProfileSync", 200,
                    HttpContext.Connection?.RemoteIpAddress?.ToString() ?? "",
                    "",
                    "profile sync ok");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = $"Database staging tidak dapat diakses: {ex.Message}"
                });
            }

            return Ok(new
            {
                message = "Profile configuration synchronized successfully."
            });
        }
    }
}
