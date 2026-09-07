using System.Text.Json.Serialization;

namespace SOLTIUS_Web_API_Add_On.Models.Auth
{
    public class TokenRequest
    {
        private string _grantType = "client_credentials";
        private string _clientId = "";
        private string _clientSecret = "";

        [JsonPropertyName("grant_type")]
        public string GrantType
        {
            get => _grantType;
            set => _grantType = value;
        }

        [JsonPropertyName("grantType")]
        public string GrantTypeCamel
        {
            get => _grantType;
            set { if (!string.IsNullOrEmpty(value)) _grantType = value; }
        }

        [JsonPropertyName("client_id")]
        public string ClientId
        {
            get => _clientId;
            set => _clientId = value;
        }

        [JsonPropertyName("clientId")]
        public string ClientIdCamel
        {
            get => _clientId;
            set { if (!string.IsNullOrEmpty(value)) _clientId = value; }
        }

        [JsonPropertyName("client_secret")]
        public string ClientSecret
        {
            get => _clientSecret;
            set => _clientSecret = value;
        }

        [JsonPropertyName("clientSecret")]
        public string ClientSecretCamel
        {
            get => _clientSecret;
            set { if (!string.IsNullOrEmpty(value)) _clientSecret = value; }
        }
    }
}
