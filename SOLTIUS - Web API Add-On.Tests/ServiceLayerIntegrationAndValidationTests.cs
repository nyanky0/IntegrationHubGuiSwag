using System;
using System.Net.Http;
using System.Text.Json.Nodes;
using SOLTIUS_Scheduler_Add_On.Services;
using SOLTIUS_Web_API_Add_On.Models.MasterData;
using SOLTIUS_Web_API_Add_On.Services.Sap;
using Xunit;

namespace SOLTIUS_Web_API_Add_On.Tests
{
    public class ServiceLayerIntegrationAndValidationTests
    {
        // =========================================================================
        // 1. UNIT TEST: Branch & Warehouse Pairing Validation
        // =========================================================================

        [Fact]
        public void ValidateBranchAndWarehouse_Branch3WithWhIbt_PassesValidation()
        {
            // Branch 3 (PT Indobaruna Bulk Transport) must pair with WH-IBT
            var exception = Record.Exception(() => 
                SapServiceLayerClient.ValidateBranchAndWarehouse(3, "WH-IBT"));

            Assert.Null(exception);
        }

        [Theory]
        [InlineData("DC")]
        [InlineData("JK-MAIN")]
        [InlineData("JK-QC")]
        [InlineData("WH-ISL")]
        [InlineData("WH-SIP")]
        public void ValidateBranchAndWarehouse_Branch3WithCrossBranchWarehouse_ThrowsInvalidOperationException(string crossWarehouse)
        {
            // Fallback to cross-branch warehouses must be strictly forbidden
            var ex = Assert.Throws<InvalidOperationException>(() => 
                SapServiceLayerClient.ValidateBranchAndWarehouse(3, crossWarehouse));

            Assert.Contains("Incompatible branch and warehouse", ex.Message);
            Assert.Contains("WH-IBT", ex.Message);
        }

        [Fact]
        public void ValidateBranchAndWarehouse_WhIbtWithOtherBranch_ThrowsInvalidOperationException()
        {
            // WH-IBT must not be used with Branch 1 or 4
            var ex = Assert.Throws<InvalidOperationException>(() => 
                SapServiceLayerClient.ValidateBranchAndWarehouse(1, "WH-IBT"));

            Assert.Contains("Incompatible branch and warehouse", ex.Message);
        }

        // =========================================================================
        // 2. UNIT TEST: Vendor Mapping Resolution
        // =========================================================================

        [Fact]
        public void VendorMapping_ResolveVendorCardCode_ResolvesMappedCodeOrPreserves()
        {
            // Testing the DatabaseService fallback logic when connection string is empty
            var dbService = new DatabaseService("");
            string result = dbService.ResolveVendorCardCode("VL-00001");
            Assert.Equal("VL-00001", result);
        }

        // =========================================================================
        // 3. UNIT TEST: SAP Error Parsing & Status Propagation
        // =========================================================================

        [Fact]
        public void SapServiceLayerException_CapturesHttpAndSapErrorCode()
        {
            var ex = new SapServiceLayerException(400, "-10", "Business Partner already exists.");

            Assert.Equal(400, ex.StatusCode);
            Assert.Equal("-10", ex.ErrorCode);
            Assert.Equal("Business Partner already exists.", ex.Message);
        }

        // =========================================================================
        // 4. OPT-IN INTEGRATION TEST: Readback from Live SAP Service Layer
        // =========================================================================

        [Fact]
        public async Task LiveSapServiceLayer_Readback_WhenEnabled()
        {
            // Opt-in flag: only runs when RUN_SAP_INTEGRATION_TESTS=true
            string? runIntegration = Environment.GetEnvironmentVariable("RUN_SAP_INTEGRATION_TESTS");
            if (!string.Equals(runIntegration, "true", StringComparison.OrdinalIgnoreCase))
            {
                // Skip gracefully if opt-in flag is not set
                return;
            }

            using var httpClient = new HttpClient();
            var configMock = new Moq.Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            configMock.Setup(c => c["SapServiceLayer:Url"]).Returns("http://localhost:50001/b1s/v2");
            configMock.Setup(c => c["SapServiceLayer:CompanyDB"]).Returns("IBTWEBAPP");
            configMock.Setup(c => c["SapServiceLayer:UserName"]).Returns("manager");
            configMock.Setup(c => c["SapServiceLayer:Password"]).Returns("P@ssw0rd");

            var loggerMock = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<SapMasterDataService>>();
            var masterDataService = new SapMasterDataService(httpClient, configMock.Object, loggerMock.Object);

            // Read back Items collection from live SAP SL
            var items = await masterDataService.GetODataCollectionAsync("Items", "$top=2");
            Assert.NotNull(items);
            Assert.NotEmpty(items);

            // Read back BusinessPartners collection from live SAP SL
            var bps = await masterDataService.GetODataCollectionAsync("BusinessPartners", "$top=2");
            Assert.NotNull(bps);
            Assert.NotEmpty(bps);
        }
    }
}
