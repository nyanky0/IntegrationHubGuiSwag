using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SOLTIUS_Web_API_Add_On.Models.Transaction
{
    public class PurchaseOrderHeader
    {
        [Required(ErrorMessage = "CardCode is required.")]
        [StringLength(30, MinimumLength = 1, ErrorMessage = "CardCode must be 1-30 characters.")]
        [JsonPropertyName("cardCode")]
        public string CardCode { get; set; } = "";

        [StringLength(200, ErrorMessage = "CardName max 200 characters.")]
        [JsonPropertyName("cardName")]
        public string CardName { get; set; } = "";

        [JsonPropertyName("docDate")]
        public DateTime DocDate { get; set; } = DateTime.MinValue;

        [JsonPropertyName("docDueDate")]
        public DateTime DocDueDate { get; set; } = DateTime.MinValue;

        [JsonPropertyName("taxDate")]
        public DateTime TaxDate { get; set; } = DateTime.MinValue;

        [StringLength(254, ErrorMessage = "Remarks max 254 characters.")]
        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = "";

        [StringLength(50, ErrorMessage = "WebTxNumber max 50 characters.")]
        [JsonPropertyName("webTxNumber")]
        public string? WebTxNumber { get; set; }

        [JsonPropertyName("webTxId")]
        public long? WebTxId { get; set; }

        [JsonPropertyName("documentLines")]
        [MinLength(1, ErrorMessage = "At least one DocumentLine is required.")]
        [MaxLength(100, ErrorMessage = "Maximum 100 DocumentLines per order.")]
        public List<PurchaseOrderDetail> DocumentLines { get; set; } = new();

        /// <summary>
        /// Menangkap field tambahan atau UDF SAP (misal U_SOL_...) secara dinamis.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalData { get; set; }

        /// <summary>
        /// Mengompilasi semua UDF menjadi string JSON untuk disimpan di tabel staging SOL_PURCHASE_ORDER_HEADER.
        /// </summary>
        public string GetUdfJson()
        {
            var udfs = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            if (AdditionalData != null)
            {
                foreach (var kvp in AdditionalData)
                {
                    if (string.Equals(kvp.Key, "U_SOL_WebTxNumber", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(WebTxNumber))
                    {
                        WebTxNumber = kvp.Value.GetString();
                    }
                    if (string.Equals(kvp.Key, "U_SOL_WebTxId", StringComparison.OrdinalIgnoreCase) && !WebTxId.HasValue && kvp.Value.TryGetInt64(out long idVal))
                    {
                        WebTxId = idVal;
                    }
                    if ((string.Equals(kvp.Key, "comments", StringComparison.OrdinalIgnoreCase) || string.Equals(kvp.Key, "Comments", StringComparison.OrdinalIgnoreCase)) && string.IsNullOrEmpty(Remarks))
                    {
                        Remarks = kvp.Value.GetString() ?? "";
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
