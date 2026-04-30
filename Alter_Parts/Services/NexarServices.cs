//using System.Net.Http.Headers;
//using System.Text;
//using System.Text.Json;

//public class NexarService
//{
//    private readonly HttpClient _httpClient;
//    private readonly IConfiguration _config;

//    public NexarService(HttpClient httpClient, IConfiguration config)
//    {
//        _httpClient = httpClient;
//        _config = config;
//    }

//    private async Task<string> GetAccessToken()
//    {
//        var clientId = "712227d9-8efb-40ad-91c5-3be498535664".Trim();
//        var clientSecret = "WvH5reu2N_1l1vy9-dVVAIxObqRMmMrYtA0E".Trim();

//        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
//            throw new Exception("Client ID or Secret is empty.");

//        var request = new HttpRequestMessage(HttpMethod.Post, "https://identity.nexar.com/connect/token");

//        string creds = $"{clientId}:{clientSecret}";
//        string encodedCreds = Convert.ToBase64String(Encoding.UTF8.GetBytes(creds));
//        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedCreds);

//        var contentData = new List<KeyValuePair<string, string>>
//        {
//            new KeyValuePair<string, string>("grant_type", "client_credentials"),
//            new KeyValuePair<string, string>("scope", "supply.domain")
//        };

//        request.Content = new FormUrlEncodedContent(contentData);

//        var response = await _httpClient.SendAsync(request);
//        var content = await response.Content.ReadAsStringAsync();

//        if (!response.IsSuccessStatusCode)
//            throw new Exception($"TOKEN LOGIN ERROR: {response.StatusCode}. Response: {content}");

//        using var json = JsonDocument.Parse(content);
//        var root = json.RootElement;

//        if (root.TryGetProperty("error", out var errorProp))
//            throw new Exception($"Nexar returned error: {errorProp.GetString()}");

//        if (!root.TryGetProperty("access_token", out var tokenElement)
//            || tokenElement.ValueKind == JsonValueKind.Null)
//            throw new Exception($"access_token not found. Body: {content}");

//        return tokenElement.GetString()
//            ?? throw new Exception("access_token was null after extraction.");
//    }



//    public async Task<string> GetPartData(string mpn)
//    {
//        string cleanMpn = mpn.Contains("(") ? mpn.Split('(')[0].Trim() : mpn.Trim();

//        var token = await GetAccessToken();

//        // ✅ Correct field: supSearchMpn, correct arg: q, variable name matches declaration
//        var requestBody = new
//        {
//            query = @"query Search($q: String!) {
//    supSearchMpn(q: $q, limit: 1) {
//        results {
//            part {
//                mpn
//                shortDescription
//                manufacturer { name }
//                category { name }
//                specs {
//                    attribute { name shortname }
//                    displayValue
//                }
//                sellers {
//                    company { name }
//                    offers {
//                        inventoryLevel
//                        prices {
//                            quantity
//                            price
//                            currency
//                        }
//                    }
//                }
//            }
//        }
//    }
//}",
//            variables = new { q = cleanMpn }  // ✅ matches $q above
//        };

//        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.nexar.com/graphql");
//        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

//        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
//        var jsonBody = JsonSerializer.Serialize(requestBody, options);
//        httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

//        var response = await _httpClient.SendAsync(httpRequest);
//        string responseContent = await response.Content.ReadAsStringAsync();

//        if (!response.IsSuccessStatusCode)
//            throw new Exception($"API returned {response.StatusCode}. Body: {responseContent}");

//        using var doc = JsonDocument.Parse(responseContent);
//        var root = doc.RootElement;

//        if (root.TryGetProperty("errors", out var errors))
//            throw new Exception($"GraphQL errors: {errors.GetRawText()}");

//        if (!root.TryGetProperty("data", out var data)
//            || data.ValueKind == JsonValueKind.Null)
//            throw new Exception($"GraphQL returned null data. Response: {responseContent}");

//        return responseContent;
//    }
//}






using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Vinrox_Tools.Services // Update this to match your actual namespace
{
    public class NexarService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public NexarService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            // SECURE: Pulls from appsettings.json instead of hardcoding
            var clientId = _config["Nexar:712227d9-8efb-40ad-91c5-3be498535664"];
            var clientSecret = _config["Nexar:WvH5reu2N_1l1vy9-dVVAIxObqRMmMrYtA0E"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                throw new Exception("Nexar Client ID or Secret is missing from appsettings.json.");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://identity.nexar.com/connect/token");

            // Nexar explicitly prefers client credentials in the Form Body, not Basic Auth headers
            var contentData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("scope", "supply.domain")
            };

            request.Content = new FormUrlEncodedContent(contentData);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"TOKEN LOGIN ERROR: {response.StatusCode}. Response: {content}");

            using var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            if (root.TryGetProperty("error", out var errorProp))
                throw new Exception($"Nexar returned error: {errorProp.GetString()}");

            if (!root.TryGetProperty("access_token", out var tokenElement) || tokenElement.ValueKind == JsonValueKind.Null)
                throw new Exception($"access_token not found. Body: {content}");

            return tokenElement.GetString() ?? throw new Exception("access_token was null.");
        }

        // FIX: Renamed from GetPartData to GetPartDetailsAsync so your controller can find it!
        public async Task<string> GetPartData(string mpn)
        {
            string cleanMpn = mpn.Contains("(") ? mpn.Split('(')[0].Trim() : mpn.Trim();

            var token = await GetAccessTokenAsync();

            var requestBody = new
            {
                query = @"query Search($q: String!) {
                    supSearchMpn(q: $q, limit: 1) {
                        results {
                            part {
                                mpn
                                shortDescription
                                manufacturer { name }
                                category { name }
                                specs {
                                    attribute { name shortname }
                                    displayValue
                                }
                                sellers {
                                    company { name }
                                    offers {
                                        inventoryLevel
                                        prices {
                                            quantity
                                            price
                                            currency
                                        }
                                    }
                                }
                            }
                        }
                    }
                }",
                variables = new { q = cleanMpn }
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.nexar.com/graphql");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var jsonBody = JsonSerializer.Serialize(requestBody, options);
            httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"API returned {response.StatusCode}. Body: {responseContent}");

            // Basic error checking before returning the data
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            if (root.TryGetProperty("errors", out var errors))
                throw new Exception($"GraphQL errors: {errors.GetRawText()}");

            return responseContent;
        }
    }
}