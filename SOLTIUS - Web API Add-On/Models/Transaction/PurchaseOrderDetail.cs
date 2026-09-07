using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SOLTIUS_Web_API_Add_On.Models.Transaction
{
    public class PurchaseOrderDetail
    {
        [Required(ErrorMessage = "ItemCode is required.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "ItemCode must be 1-50 characters.")]
        [JsonPropertyName("itemCode")]
        public string ItemCode { get; set; } = "";

        [StringLength(200, ErrorMessage = "ItemDescription max 200 characters.")]
        [JsonPropertyName("itemDescription")]
        public string ItemDescription { get; set; } = "";

        [StringLength(20, ErrorMessage = "WarehouseCode max 20 characters.")]
        [JsonPropertyName("warehouseCode")]
        public string WarehouseCode { get; set; } = "";

        [Range(0.001, 9999999, ErrorMessage = "Quantity must be between 0.001 and 9999999.")]
        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [Range(0, 999999999, ErrorMessage = "Price must be between 0 and 999999999.")]
        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [StringLength(20, ErrorMessage = "VatGroup max 20 characters.")]
        [JsonPropertyName("vatGroup")]
        public string? VatGroup { get; set; }

        [JsonPropertyName("webLineId")]
        public long? WebLineId { get; set; }

        /// <summary>
        /// Menangkap field baris tambahan atau UDF SAP (misal U_SOL_WebOpenQty, U_SOL_LineRemarks, dsb.)
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalData { get; set; }

        /// <summary>
        /// Mengompilasi semua UDF baris menjadi string JSON untuk disimpan di tabel staging SOL_PURCHASE_ORDER_DETAIL.
        /// </summary>
        public string GetUdfJson()
        {
            var udfs = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            if (AdditionalData != null)
            {
                foreach (var kvp in AdditionalData)
                {
                    if (string.Equals(kvp.Key, "UnitPrice", StringComparison.OrdinalIgnoreCase) && Price == 0 && kvp.Value.TryGetDecimal(out decimal unitPrice))
                    {
                        Price = unitPrice;
                    }
                    if (string.Equals(kvp.Key, "U_SOL_WebLineId", StringComparison.OrdinalIgnoreCase) && !WebLineId.HasValue && kvp.Value.TryGetInt64(out long lineId))
                    {
                        WebLineId = lineId;
                    }
                    if (string.Equals(kvp.Key, "VatGroup", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(VatGroup))
                    {
                        VatGroup = kvp.Value.GetString();
                    }

                    if (kvp.Key.StartsWith("U_", StringComparison.OrdinalIgnoreCase) || kvp.Key.StartsWith("u_", StringComparison.OrdinalIgnoreCase))
                    {
                        udfs[kvp.Key] = ConvertJsonElement(kvp.Value);
                    }
                }
            }

            return udfs.Count > 0 ? JsonSerializer.Serialize(udfs) : "";
        }

        private static object? ConvertJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out long l)) return l;
                    if (element.TryGetDecimal(out decimal d)) return d;
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                default:
                    return element.GetRawText();
            }
        }
    }
}
