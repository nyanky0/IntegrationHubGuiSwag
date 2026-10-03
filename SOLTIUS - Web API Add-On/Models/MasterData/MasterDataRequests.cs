using System.Text.Json.Serialization;

namespace SOLTIUS_Web_API_Add_On.Models.MasterData
{
    public class MasterDataItemRequest
    {
        [JsonPropertyName("itemCode")]
        public string? ItemCode { get; set; }

        [JsonPropertyName("itemName")]
        public string? ItemName { get; set; }

        [JsonPropertyName("groupCode")]
        public int? GroupCode { get; set; }

        [JsonPropertyName("uomCode")]
        public string? UomCode { get; set; }

        [JsonPropertyName("salesUom")]
        public string? SalesUom { get; set; }

        [JsonPropertyName("purchaseItem")]
        public string? PurchaseItem { get; set; }

        [JsonPropertyName("salesItem")]
        public string? SalesItem { get; set; }

        [JsonPropertyName("inventoryItem")]
        public string? InventoryItem { get; set; }
    }

    public class MasterDataBusinessPartnerRequest
    {
        [JsonPropertyName("cardCode")]
        public string? CardCode { get; set; }

        [JsonPropertyName("cardName")]
        public string? CardName { get; set; }

        [JsonPropertyName("cardType")]
        public string? CardType { get; set; } = "S";

        [JsonPropertyName("currency")]
        public string? Currency { get; set; } = "IDR";

        [JsonPropertyName("series")]
        public int? Series { get; set; }
    }

    public class MasterDataWarehouseRequest
    {
        [JsonPropertyName("whsCode")]
        public string? WhsCode { get; set; }

        [JsonPropertyName("whsName")]
        public string? WhsName { get; set; }

        [JsonPropertyName("bplId")]
        public int? BPLid { get; set; }

        [JsonPropertyName("inactive")]
        public string? Inactive { get; set; }
    }

    public class SapServiceLayerException : Exception
    {
        public int StatusCode { get; }
        public string ErrorCode { get; }
        public string RawResponse { get; }

        public SapServiceLayerException(int statusCode, string errorCode, string message, string rawResponse = "")
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            RawResponse = rawResponse;
        }
    }
}
