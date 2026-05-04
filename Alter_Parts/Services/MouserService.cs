using Alter_Parts.Models;
using System.Text;
using System.Text.Json;
public class MouserService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public MouserService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<PartDetails> GetPartDetails(string mpn)
    {
        var apiKey = _config["Mouser:ApiKey"];
        var url = $"https://api.mouser.com/api/v1/search/keyword?apiKey={apiKey}";

        var requestBody = new
        {
            SearchByKeywordRequest = new
            {
                keyword = mpn.Trim(),
                records = 1,
                startingRecord = 0,
                searchOptions = "Exact"
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Mouser API error: {response.StatusCode}. Body: {responseContent}");

        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        // Check for API errors
        if (root.TryGetProperty("Errors", out var errors) && errors.GetArrayLength() > 0)
            throw new Exception($"Mouser error: {errors[0].GetProperty("Message").GetString()}");

        var parts = root
            .GetProperty("SearchResults")
            .GetProperty("Parts");

        if (parts.ValueKind == JsonValueKind.Null || parts.GetArrayLength() == 0)
            return null;

        var part = parts[0];

        // Extract specs from ProductAttributes
        var specs = new Dictionary<string, string>();
        if (part.TryGetProperty("ProductAttributes", out var attrs) &&
            attrs.ValueKind != JsonValueKind.Null)
        {
            foreach (var attr in attrs.EnumerateArray())
            {
                var attrName = attr.TryGetProperty("AttributeName", out var n) ? n.GetString() : null;
                var attrValue = attr.TryGetProperty("AttributeValue", out var v) ? v.GetString() : null;
                if (!string.IsNullOrEmpty(attrName) && !string.IsNullOrEmpty(attrValue))
                    specs[attrName] = attrValue;
            }
        }

        return new PartDetails
        {
            Mpn = part.TryGetProperty("ManufacturerPartNumber", out var m) ? m.GetString() : mpn,
            Description = part.TryGetProperty("Description", out var d) ? d.GetString() : "",
            Manufacturer = part.TryGetProperty("Manufacturer", out var mfr) ? mfr.GetString() : "",
            DatasheetUrl = part.TryGetProperty("DataSheetUrl", out var ds) ? ds.GetString() : "",
            ProductUrl = part.TryGetProperty("ProductDetailUrl", out var pu) ? pu.GetString() : "",
            Stock = part.TryGetProperty("Availability", out var av) ? av.GetString() : "",
            Category = part.TryGetProperty("Category", out var cat) ? cat.GetString() : "",
            Specs = specs,
            Source = "Mouser"
        };
    }

    public async Task<List<PartDetails>> SearchByDescription(
    string description, int limit = 10)
    {
        var apiKey = _config["Mouser:ApiKey"];
        var url = $"https://api.mouser.com/api/v1/search/keyword" +
                     $"?apiKey={apiKey}";

        var requestBody = new
        {
            SearchByKeywordRequest = new
            {
                keyword = description.Trim(),
                records = limit,
                startingRecord = 0,
                searchOptions = "string",
                searchWithYourSignUpLanguage = "false"
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(
            json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        var responseContent =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception(
                $"Mouser API error: {response.StatusCode}. " +
                $"Body: {responseContent}");

        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        if (root.TryGetProperty("Errors", out var errors) &&
            errors.GetArrayLength() > 0)
            throw new Exception(
                $"Mouser error: " +
                $"{errors[0].GetProperty("Message").GetString()}");

        var results = new List<PartDetails>();

        var parts = root
            .GetProperty("SearchResults")
            .GetProperty("Parts");

        if (parts.ValueKind == JsonValueKind.Null ||
            parts.GetArrayLength() == 0)
            return results;

        foreach (var part in parts.EnumerateArray())
        {
            // Extract specs from ProductAttributes
            var specs = new Dictionary<string, string>();
            if (part.TryGetProperty(
                "ProductAttributes", out var attrs) &&
                attrs.ValueKind != JsonValueKind.Null)
            {
                foreach (var attr in attrs.EnumerateArray())
                {
                    var attrName = attr.TryGetProperty(
                        "AttributeName", out var n)
                        ? n.GetString() : null;
                    var attrValue = attr.TryGetProperty(
                        "AttributeValue", out var v)
                        ? v.GetString() : null;
                    if (!string.IsNullOrEmpty(attrName) &&
                        !string.IsNullOrEmpty(attrValue))
                        specs[attrName] = attrValue;
                }
            }

            // Get price
            string unitPrice = "N/A";
            if (part.TryGetProperty(
                "PriceBreaks", out var priceBreaks) &&
                priceBreaks.ValueKind != JsonValueKind.Null &&
                priceBreaks.GetArrayLength() > 0)
            {
                var firstBreak = priceBreaks[0];
                if (firstBreak.TryGetProperty("Price", out var p))
                    unitPrice = p.GetString() ?? "N/A";
            }

            var fetchedDesc = part.TryGetProperty(
                "Description", out var d)
                ? d.GetString() : "";

            var matchScore = CalculateMatchScore(
                description, fetchedDesc);

            specs["Unit Price"] = unitPrice;
            specs["Match Score"] = $"{matchScore}%";
            specs["Mouser PN"] = part.TryGetProperty(
                "MouserPartNumber", out var mpn)
                ? mpn.GetString() : "";

            results.Add(new PartDetails
            {
                Mpn = part.TryGetProperty(
                    "ManufacturerPartNumber", out var m)
                    ? m.GetString() : "",
                Description = fetchedDesc,
                Manufacturer = part.TryGetProperty(
                    "Manufacturer", out var mfr)
                    ? mfr.GetString() : "",
                Category = part.TryGetProperty(
                    "Category", out var cat)
                    ? cat.GetString() : "",
                DatasheetUrl = part.TryGetProperty(
                    "DataSheetUrl", out var ds)
                    ? ds.GetString() : "",
                ProductUrl = part.TryGetProperty(
                    "ProductDetailUrl", out var pu)
                    ? pu.GetString() : "",
                Stock = part.TryGetProperty(
                    "Availability", out var av)
                    ? av.GetString() : "0",
                Specs = specs,
                Source = "Mouser"
            });
        }

        return results
            .OrderByDescending(r =>
                double.TryParse(
                    r.Specs.GetValueOrDefault(
                        "Match Score", "0").Replace("%", ""),
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
