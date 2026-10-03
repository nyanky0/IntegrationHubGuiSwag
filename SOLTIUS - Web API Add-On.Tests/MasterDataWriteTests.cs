using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SOLTIUS_Web_API_Add_On.Controllers;
using SOLTIUS_Web_API_Add_On.Database.Interfaces;
using SOLTIUS_Web_API_Add_On.Models.Configuration;
using SOLTIUS_Web_API_Add_On.Models.MasterData;
using SOLTIUS_Web_API_Add_On.Services.Configuration;
using SOLTIUS_Web_API_Add_On.Services.Sap;
using Xunit;

namespace SOLTIUS_Web_API_Add_On.Tests
{
    public class MasterDataWriteTests
    {
        private readonly Mock<IDatabaseConnectionFactory> _dbFactoryMock;
        private readonly Mock<IConfigurationService> _configServiceMock;
        private readonly Mock<ISapMasterDataService> _sapServiceMock;
        private readonly MasterDataController _controller;

        public MasterDataWriteTests()
        {
            _dbFactoryMock = new Mock<IDatabaseConnectionFactory>();
            _configServiceMock = new Mock<IConfigurationService>();
            _sapServiceMock = new Mock<ISapMasterDataService>();

            _configServiceMock.Setup(c => c.GetDatabaseConfig()).Returns(new DBConfig
            {
                DBType = DatabaseType.SqlServer,
                Server = "localhost",
                DatabaseName = "NYANKYO",
                UserName = "sa",
                Password = "P@ssw0rd"
            });

            _controller = new MasterDataController(
                _dbFactoryMock.Object,
                _configServiceMock.Object,
                _sapServiceMock.Object
            );
        }

        // =========================================================================
        // 1. SUCCESS WRITE & READBACK TESTS
        // =========================================================================

        [Fact]
        public async Task CreateItem_Success_WritesThroughSapAndReturnsReadback()
        {
            // Arrange
            var req = new MasterDataItemRequest
            {
                ItemCode = "ITM-TEST-001",
                ItemName = "High Pressure Valve",
                GroupCode = 100,
                UomCode = "PCS"
            };

            var expectedReadback = new JsonObject
            {
                ["ItemCode"] = "ITM-TEST-001",
                ["ItemName"] = "High Pressure Valve",
                ["ItemsGroupCode"] = 100,
                ["InventoryUOM"] = "PCS"
            };

            _sapServiceMock
                .Setup(s => s.CreateItemAsync(It.Is<MasterDataItemRequest>(r => r.ItemCode == "ITM-TEST-001")))
                .ReturnsAsync(expectedReadback);

            // Act
            var result = await _controller.CreateItem(req) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);

            dynamic val = result.Value!;
            // Convert to reflection check
            var successProp = val.GetType().GetProperty("success")?.GetValue(val);
            var dataProp = val.GetType().GetProperty("data")?.GetValue(val) as JsonObject;

            Assert.True((bool)successProp);
            Assert.NotNull(dataProp);
            Assert.Equal("ITM-TEST-001", dataProp!["ItemCode"]?.ToString());
            Assert.Equal("High Pressure Valve", dataProp!["ItemName"]?.ToString());

            _sapServiceMock.Verify(s => s.CreateItemAsync(It.IsAny<MasterDataItemRequest>()), Times.Once);
        }

        [Fact]
        public async Task CreateBusinessPartner_Success_PreservesSupplierNumberingAndReturnsReadback()
        {
            // Arrange: Incoming request with temporary web vendor code "V-MARINDO-01"
            var req = new MasterDataBusinessPartnerRequest
            {
                CardCode = "V-MARINDO-01",
                CardName = "PT Marindo Jaya Perkasa",
                CardType = "S"
            };

            // SAP must assign official VL-00xxx series (not literal V-MARINDO-01 CardCode in SAP)
            var expectedReadback = new JsonObject
            {
                ["CardCode"] = "VL-00045",
                ["CardName"] = "PT Marindo Jaya Perkasa",
                ["CardType"] = "cSupplier",
                ["Series"] = 73
            };

            _sapServiceMock
                .Setup(s => s.CreateBusinessPartnerAsync(It.Is<MasterDataBusinessPartnerRequest>(r => r.CardName == "PT Marindo Jaya Perkasa")))
                .ReturnsAsync(expectedReadback);

            // Act
            var result = await _controller.CreateBusinessPartner(req) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);

            dynamic val = result.Value!;
            var dataProp = val.GetType().GetProperty("data")?.GetValue(val) as JsonObject;

            Assert.NotNull(dataProp);
            Assert.Equal("VL-00045", dataProp!["CardCode"]?.ToString());
            Assert.NotEqual("V-MARINDO-01", dataProp!["CardCode"]?.ToString());
            Assert.Equal("PT Marindo Jaya Perkasa", dataProp!["CardName"]?.ToString());
        }

        [Fact]
        public async Task CreateWarehouse_Success_WritesThroughSapAndReturnsReadback()
        {
            // Arrange
            var req = new MasterDataWarehouseRequest
            {
                WhsCode = "WHTEST01",
                WhsName = "Gudang Transit Baru"
            };

            var expectedReadback = new JsonObject
            {
                ["WarehouseCode"] = "WHTEST01",
                ["WarehouseName"] = "Gudang Transit Baru",
                ["Inactive"] = "tNO"
            };

            _sapServiceMock
                .Setup(s => s.CreateWarehouseAsync(It.Is<MasterDataWarehouseRequest>(r => r.WhsCode == "WHTEST01")))
                .ReturnsAsync(expectedReadback);

            // Act
            var result = await _controller.CreateWarehouse(req) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);

            dynamic val = result.Value!;
            var dataProp = val.GetType().GetProperty("data")?.GetValue(val) as JsonObject;

            Assert.NotNull(dataProp);
            Assert.Equal("WHTEST01", dataProp!["WarehouseCode"]?.ToString());
            Assert.Equal("Gudang Transit Baru", dataProp!["WarehouseName"]?.ToString());
        }

        // =========================================================================
        // 2. SAP REJECTION (ERROR PROPAGATION) TESTS
        // =========================================================================

        [Fact]
        public async Task CreateItem_SapRejection_PropagatesSapErrorWithoutSwallowing()
        {
            // Arrange: SAP Service Layer returns validation error
            var req = new MasterDataItemRequest
            {
                ItemCode = "INVALID-ITEM",
                ItemName = "Broken Payload"
            };

            _sapServiceMock
                .Setup(s => s.CreateItemAsync(It.IsAny<MasterDataItemRequest>()))
                .ThrowsAsync(new SapServiceLayerException(400, "-1", "Field 'InventoryUOM' is mandatory in SAP Business One."));

            // Act
            var result = await _controller.CreateItem(req) as ObjectResult;

            // Assert: Error must NOT be swallowed or converted to fake success
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);

            dynamic val = result.Value!;
            var success = (bool)val.GetType().GetProperty("success")?.GetValue(val);
            var errorCode = (string)val.GetType().GetProperty("errorCode")?.GetValue(val);
            var message = (string)val.GetType().GetProperty("message")?.GetValue(val);

            Assert.False(success);
            Assert.Equal("-1", errorCode);
            Assert.Contains("InventoryUOM", message);
        }

        [Fact]
        public async Task CreateWarehouse_ExceedsLength_PropagatesSapError()
        {
            // Arrange: Code exceeds 8 chars (SAP OWHS limit)
            var req = new MasterDataWarehouseRequest
            {
                WhsCode = "WH-EXTRA-LONG-NAME",
                WhsName = "Gudang Terlalu Panjang"
            };

            _sapServiceMock
                .Setup(s => s.CreateWarehouseAsync(It.IsAny<MasterDataWarehouseRequest>()))
                .ThrowsAsync(new SapServiceLayerException(400, "-8112", "Warehouse code exceeds the maximum allowed length of 8 characters in SAP Business One."));

            // Act
            var result = await _controller.CreateWarehouse(req) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);

            dynamic val = result.Value!;
            var success = (bool)val.GetType().GetProperty("success")?.GetValue(val);
            var errorCode = (string)val.GetType().GetProperty("errorCode")?.GetValue(val);

            Assert.False(success);
            Assert.Equal("-8112", errorCode);
        }

        // =========================================================================
        // 3. DUPLICATE HANDLING TESTS
        // =========================================================================

        [Fact]
        public async Task CreateItem_DuplicateItemCode_ReturnsDuplicateError()
        {
            // Arrange
            var req = new MasterDataItemRequest
            {
                ItemCode = "ITM-00001",
                ItemName = "Existing Item"
            };

            _sapServiceMock
                .Setup(s => s.CreateItemAsync(It.Is<MasterDataItemRequest>(r => r.ItemCode == "ITM-00001")))
                .ThrowsAsync(new SapServiceLayerException(400, "-10", "Item code 'ITM-00001' already exists"));

            // Act
            var result = await _controller.CreateItem(req) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);

            dynamic val = result.Value!;
            var success = (bool)val.GetType().GetProperty("success")?.GetValue(val);
            var errorCode = (string)val.GetType().GetProperty("errorCode")?.GetValue(val);
            var message = (string)val.GetType().GetProperty("message")?.GetValue(val);

            Assert.False(success);
            Assert.Equal("-10", errorCode);
            Assert.Contains("already exists", message);
        }

        [Fact]
        public async Task CreateBusinessPartner_DuplicateCardCode_ReturnsDuplicateError()
        {
            // Arrange: Trying to create with existing VL-00001
            var req = new MasterDataBusinessPartnerRequest
            {
                CardCode = "VL-00001",
                CardName = "Duplicate Supplier"
            };

            _sapServiceMock
                .Setup(s => s.CreateBusinessPartnerAsync(It.Is<MasterDataBusinessPartnerRequest>(r => r.CardCode == "VL-00001")))
                .ThrowsAsync(new SapServiceLayerException(400, "-10", "Business partner code 'VL-00001' already exists in SAP Business One."));

            // Act
            var result = await _controller.CreateBusinessPartner(req) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);

            dynamic val = result.Value!;
            var success = (bool)val.GetType().GetProperty("success")?.GetValue(val);
            var errorCode = (string)val.GetType().GetProperty("errorCode")?.GetValue(val);
            var message = (string)val.GetType().GetProperty("message")?.GetValue(val);

            Assert.False(success);
            Assert.Equal("-10", errorCode);
            Assert.Contains("already exists", message);
        }
    }
}
