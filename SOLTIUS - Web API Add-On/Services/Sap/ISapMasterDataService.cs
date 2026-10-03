using SOLTIUS_Web_API_Add_On.Models.MasterData;
using System.Text.Json.Nodes;

namespace SOLTIUS_Web_API_Add_On.Services.Sap
{
    public interface ISapMasterDataService
    {
        Task<JsonObject> CreateItemAsync(MasterDataItemRequest request);
        Task<JsonObject?> GetItemAsync(string itemCode);

        Task<JsonObject> CreateBusinessPartnerAsync(MasterDataBusinessPartnerRequest request);
        Task<JsonObject?> GetBusinessPartnerAsync(string cardCode);

        Task<JsonObject> CreateWarehouseAsync(MasterDataWarehouseRequest request);
        Task<JsonObject?> GetWarehouseAsync(string whsCode);

        Task<JsonArray> GetODataCollectionAsync(string entitySet, string? queryOptions = null);
    }
}
