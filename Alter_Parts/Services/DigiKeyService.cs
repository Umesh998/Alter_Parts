using Alter_Parts.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class DigiKeyService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private string _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public DigiKeyService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    //private async Task<string> GetAccessToken()
    //{
    //    // Return cached token if still valid
    //    if (!string.IsNullOrEmpty(_cachedToken) && DateTime.Now < _tokenExpiry)
    //        return _cachedToken;

    //    var clientId = _config["DigiKey:ClientId"];
    //    var clientSecret = _config["DigiKey:ClientSecret"];

    //    var request = new HttpRequestMessage(HttpMethod.Post,
    //        "https://api.digikey.com/v1/oauth2/token");

    //    var contentData = new List<KeyValuePair<string, string>>
    //    {
    //        new("grant_type",    "client_credentials"),
    //        new("client_id",     clientId),
    //        new("client_secret", clientSecret)
    //    };

    //    request.Content = new FormUrlEncodedContent(contentData);

    private async Task<string> GetAccessToken()
    {
        // Return cached token if still valid
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.Now < _tokenExpiry)
            return _cachedToken;

        var clientId = _config["DigiKey:ClientId"];
        var clientSecret = _config["DigiKey:ClientSecret"];

        // 🚨 ADD THIS SAFETY CHECK 🚨
        // This stops the crash and tells you EXACTLY what is wrong!
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new Exception("CRITICAL ERROR: DigiKey ClientId or ClientSecret is NULL. Please check the spelling in your appsettings.json file!");
        }

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://api.digikey.com/v1/oauth2/token");

        var contentData = new List<KeyValuePair<string, string>>
    {
        new("grant_type",    "client_credentials"),
        new("client_id",     clientId),
        new("client_secret", clientSecret)
    };

        // Because of the check above, 'clientId' will never be null here, so it won't crash!
        request.Content = new FormUrlEncodedContent(contentData);

        // ... the rest of your method ...

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"DigiKey auth failed: {response.StatusCode}. Body: {content}");

        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        if (!root.TryGetProperty("access_token", out var tokenEl))
            throw new Exception($"No access_token in DigiKey response: {content}");

        _cachedToken = tokenEl.GetString();
        _tokenExpiry = DateTime.Now.AddSeconds(
            root.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() - 60 : 3540);

        return _cachedToken;
    }

    public async Task<PartDetails> GetPartDetails(string mpn)
    {
        var token = await GetAccessToken();

        var requestBody = new
        {
            keywords = mpn.Trim(),
            limit = 1,
            offset = 0,
            filterOptionsRequest = new
            {
                manufacturerFilter = Array.Empty<object>()
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://api.digikey.com/products/v4/search/keyword");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-DIGIKEY-Client-Id", _config["DigiKey:ClientId"]);
        request.Headers.Add("X-DIGIKEY-Locale-Site", "US");
        request.Headers.Add("X-DIGIKEY-Locale-Language", "en");
        request.Headers.Add("X-DIGIKEY-Locale-Currency", "USD");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"DigiKey API error: {response.StatusCode}. Body: {responseContent}");

        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        if (!root.TryGetProperty("Products", out var products) ||
            products.ValueKind == JsonValueKind.Null ||
            products.GetArrayLength() == 0)
            return null;

        var part = products[0];

        // Extract specs
        var specs = new Dictionary<string, string>();
        if (part.TryGetProperty("Parameters", out var parameters) &&
            parameters.ValueKind != JsonValueKind.Null)
        {
            foreach (var param in parameters.EnumerateArray())
            {
                var paramName = param.TryGetProperty("ParameterText", out var pn) ? pn.GetString() : null;
                var paramValue = param.TryGetProperty("ValueText", out var pv) ? pv.GetString() : null;
                if (!string.IsNullOrEmpty(paramName) && !string.IsNullOrEmpty(paramValue))
                    specs[paramName] = paramValue;
            }
        }

        string GetNested(JsonElement el, string prop, string subprop)
        {
            if (el.TryGetProperty(prop, out var outer) && outer.ValueKind != JsonValueKind.Null)
                if (outer.TryGetProperty(subprop, out var inner))
                    return inner.GetString() ?? "";
            return "";
        }

        return new PartDetails
        {
            Mpn = part.TryGetProperty("ManufacturerProductNumber", out var m) ? m.GetString() : mpn,
            Description = part.TryGetProperty("Description", out var desc) ?
                           GetNested(part, "Description", "ProductDescription") : "",
            Manufacturer = GetNested(part, "Manufacturer", "Name"),
            DatasheetUrl = part.TryGetProperty("DatasheetUrl", out var ds) ? ds.GetString() : "",
            ProductUrl = part.TryGetProperty("ProductUrl", out var pu) ? pu.GetString() : "",
            Stock = part.TryGetProperty("QuantityAvailable", out var qty) ?
                           qty.GetInt64().ToString() : "0",
            Category = GetNested(part, "Category", "Name"),
            Specs = specs,
            Source = "DigiKey"
        };
    }

        public async Task<List<PartDetails>> SearchByDescription(
    string description, int limit = 10)
    {
        var token = await GetAccessToken();

        var requestBody = new
        {
            keywords = description.Trim(),
            limit = limit,
            offset = 0,
            filterOptionsRequest = new
            {
                manufacturerFilter = Array.Empty<object>()
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://api.digikey.com/products/v4/search/keyword");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-DIGIKEY-Client-Id",
            _config["DigiKey:ClientId"]);
        request.Headers.Add("X-DIGIKEY-Locale-Site", "US");
        request.Headers.Add("X-DIGIKEY-Locale-Language", "en");
        request.Headers.Add("X-DIGIKEY-Locale-Currency", "USD");
        request.Content = new StringContent(
            json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var responseContent =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception(
                $"DigiKey API error: {response.StatusCode}. " +
                $"Body: {responseContent}");

        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;
        var results = new List<PartDetails>();

        if (!root.TryGetProperty("Products", out var products) ||
            products.ValueKind == JsonValueKind.Null ||
            products.GetArrayLength() == 0)
            return results;

        foreach (var product in products.EnumerateArray())
        {
            // Extract specs
            var specs = new Dictionary<string, string>();
            if (product.TryGetProperty("Parameters", out var parameters) &&
                parameters.ValueKind != JsonValueKind.Null)
            {
                foreach (var param in parameters.EnumerateArray())
                {
                    var paramName = param.TryGetProperty(
                        "ParameterText", out var pn)
                        ? pn.GetString() : null;
                    var paramValue = param.TryGetProperty(
                        "ValueText", out var pv)
                        ? pv.GetString() : null;
                    if (!string.IsNullOrEmpty(paramName) &&
                        !string.IsNullOrEmpty(paramValue))
                        specs[paramName] = paramValue;
                }
            }

            string GetNested(JsonElement el,
                string prop, string subprop)
            {
                if (el.TryGetProperty(prop, out var outer) &&
                    outer.ValueKind != JsonValueKind.Null)
                    if (outer.TryGetProperty(subprop, out var inner))
                        return inner.GetString() ?? "";
                return "";
            }

            // Get unit price
            string unitPrice = "N/A";
            if (product.TryGetProperty(
                "UnitPrice", out var up))
                unitPrice = $"${up.GetDecimal():0.0000}";

            // Calculate match score
            var fetchedDesc = GetNested(product,
                "Description", "ProductDescription");
            var matchScore = CalculateMatchScore(
                description, fetchedDesc);

            results.Add(new PartDetails
            {
                Mpn = product.TryGetProperty(
                    "ManufacturerProductNumber", out var m)
                    ? m.GetString() : "",
                Description = fetchedDesc,
                Manufacturer = GetNested(product, "Manufacturer", "Name"),
                Category = GetNested(product, "Category", "Name"),
                DatasheetUrl = product.TryGetProperty(
                    "DatasheetUrl", out var ds)
                    ? ds.GetString() : "",
                ProductUrl = product.TryGetProperty(
                    "ProductUrl", out var pu)
                    ? pu.GetString() : "",
                Stock = product.TryGetProperty(
                    "QuantityAvailable", out var qty)
                    ? qty.GetInt64().ToString() : "0",
                Specs = specs,
                Source = "DigiKey",
                // Store unit price and match score in Specs
                // for easy access in view
            });

            // Add price and match score to specs for display
            results.Last().Specs["Unit Price"] = unitPrice;
            results.Last().Specs["Match Score"] =
                $"{matchScore}%";
            results.Last().Specs["DigiKey PN"] =
                product.TryGetProperty(
                    "DigiKeyPartNumber", out var dkpn)
                ? dkpn.GetString() : "";
        }

        // Sort by best match score
        return results
            .OrderByDescending(r =>
                double.TryParse(
                    r.Specs.GetValueOrDefault("Match Score", "0")
                     .Replace("%", ""),
                    out var score) ? score : 0)
            .ToList();
    }

    private static double CalculateMatchScore(
        string keyword, string description)
    {
        if (string.IsNullOrWhiteSpace(description)) return 0;
        var words = keyword.ToLower().Split(' ',
            StringSplitOptions.RemoveEmptyEntries);
        var descLow = description.ToLower();
        int matched = words.Count(w => descLow.Contains(w));
        return Math.Round(
            (double)matched / words.Length * 100, 1);
    }
}
