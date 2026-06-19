//using Alter_Parts.Models;
//using System.Text;
//using System.Text.Json;
//public class MouserService
//{
//    private readonly HttpClient _httpClient;
//    private readonly IConfiguration _config;

//    public MouserService(HttpClient httpClient, IConfiguration config)
//    {
//        _httpClient = httpClient;
//        _config = config;
//    }

//    public async Task<PartDetails> GetPartDetails(string mpn)
//    {
//        var apiKey = _config["Mouser:ApiKey"];
//        var url = $"https://api.mouser.com/api/v1/search/keyword?apiKey={apiKey}";

//        var requestBody = new
//        {
//            SearchByKeywordRequest = new
//            {
//                keyword = mpn.Trim(),
//                records = 1,
//                startingRecord = 0,
//                searchOptions = "Exact"
//            }
//        };

//        var json = JsonSerializer.Serialize(requestBody);
//        var content = new StringContent(json, Encoding.UTF8, "application/json");

//        var response = await _httpClient.PostAsync(url, content);
//        var responseContent = await response.Content.ReadAsStringAsync();

//        System.Diagnostics.Debug.WriteLine("=== MOUSER RAW RESPONSE ===");
//        System.Diagnostics.Debug.WriteLine(responseContent);
//        System.Diagnostics.Debug.WriteLine("=== END ===");

//        if (!response.IsSuccessStatusCode)
//            throw new Exception($"Mouser API error: {response.StatusCode}. Body: {responseContent}");

//        using var doc = JsonDocument.Parse(responseContent);
//        var root = doc.RootElement;

//        // Check for API errors
//        if (root.TryGetProperty("Errors", out var errors) && errors.GetArrayLength() > 0)
//            throw new Exception($"Mouser error: {errors[0].GetProperty("Message").GetString()}");

//        var parts = root
//            .GetProperty("SearchResults")
//            .GetProperty("Parts");

//        if (parts.ValueKind == JsonValueKind.Null || parts.GetArrayLength() == 0)
//            return null;

//        var part = parts[0];

//        // Extract specs from ProductAttributes
//        var specs = new Dictionary<string, string>();
//        if (part.TryGetProperty("ProductAttributes", out var attrs) &&
//            attrs.ValueKind != JsonValueKind.Null)
//        {
//            foreach (var attr in attrs.EnumerateArray())
//            {
//                var attrName = attr.TryGetProperty("AttributeName", out var n) ? n.GetString() : null;
//                var attrValue = attr.TryGetProperty("AttributeValue", out var v) ? v.GetString() : null;
//                if (!string.IsNullOrEmpty(attrName) && !string.IsNullOrEmpty(attrValue))
//                    specs[attrName] = attrValue;
//            }
//        }

//        return new PartDetails
//        {
//            Mpn = part.TryGetProperty("ManufacturerPartNumber", out var m) ? m.GetString() : mpn,
//            Description = part.TryGetProperty("Description", out var d) ? d.GetString() : "",
//            Manufacturer = part.TryGetProperty("Manufacturer", out var mfr) ? mfr.GetString() : "",
//            DatasheetUrl = part.TryGetProperty("DataSheetUrl", out var ds) ? ds.GetString() : "",
//            ProductUrl = part.TryGetProperty("ProductDetailUrl", out var pu) ? pu.GetString() : "",
//            Stock = part.TryGetProperty("Availability", out var av) ? av.GetString() : "",
//            Category = part.TryGetProperty("Category", out var cat) ? cat.GetString() : "",
//            Specs = specs,
//            Source = "Mouser"
//        };
//    }

//    public async Task<List<PartDetails>> SearchByDescription(
//    string description, int limit = 10)
//    {
//        var apiKey = _config["Mouser:ApiKey"];
//        var url = $"https://api.mouser.com/api/v1/search/keyword" +
//                     $"?apiKey={apiKey}";

//        var requestBody = new
//        {
//            SearchByKeywordRequest = new
//            {
//                keyword = description.Trim(),
//                records = limit,
//                startingRecord = 0,
//                searchOptions = "string",
//                searchWithYourSignUpLanguage = "false"
//            }
//        };

//        var json = JsonSerializer.Serialize(requestBody);
//        var content = new StringContent(
//            json, Encoding.UTF8, "application/json");

//        var response = await _httpClient.PostAsync(url, content);
//        var responseContent = await response.Content.ReadAsStringAsync();

//        System.Diagnostics.Debug.WriteLine("=== MOUSER RAW RESPONSE ===");
//        System.Diagnostics.Debug.WriteLine(responseContent);
//        System.Diagnostics.Debug.WriteLine("=== END ===");

//        if (!response.IsSuccessStatusCode)
//            throw new Exception(
//                $"Mouser API error: {response.StatusCode}. " +
//                $"Body: {responseContent}");

//        using var doc = JsonDocument.Parse(responseContent);
//        var root = doc.RootElement;

//        if (root.TryGetProperty("Errors", out var errors) &&
//            errors.GetArrayLength() > 0)
//            throw new Exception(
//                $"Mouser error: " +
//                $"{errors[0].GetProperty("Message").GetString()}");

//        var results = new List<PartDetails>();

//        var parts = root
//            .GetProperty("SearchResults")
//            .GetProperty("Parts");

//        if (parts.ValueKind == JsonValueKind.Null ||
//            parts.GetArrayLength() == 0)
//            return results;

//        foreach (var part in parts.EnumerateArray())
//        {
//            // Extract specs from ProductAttributes
//            var specs = new Dictionary<string, string>();
//            if (part.TryGetProperty(
//                "ProductAttributes", out var attrs) &&
//                attrs.ValueKind != JsonValueKind.Null)
//            {
//                foreach (var attr in attrs.EnumerateArray())
//                {
//                    var attrName = attr.TryGetProperty(
//                        "AttributeName", out var n)
//                        ? n.GetString() : null;
//                    var attrValue = attr.TryGetProperty(
//                        "AttributeValue", out var v)
//                        ? v.GetString() : null;
//                    if (!string.IsNullOrEmpty(attrName) &&
//                        !string.IsNullOrEmpty(attrValue))
//                        specs[attrName] = attrValue;
//                }
//            }

//            // Get price
//            string unitPrice = "N/A";
//            if (part.TryGetProperty(
//                "PriceBreaks", out var priceBreaks) &&
//                priceBreaks.ValueKind != JsonValueKind.Null &&
//                priceBreaks.GetArrayLength() > 0)
//            {
//                var firstBreak = priceBreaks[0];
//                if (firstBreak.TryGetProperty("Price", out var p))
//                    unitPrice = p.GetString() ?? "N/A";
//            }

//            var fetchedDesc = part.TryGetProperty(
//                "Description", out var d)
//                ? d.GetString() : "";

//            var matchScore = CalculateMatchScore(
//                description, fetchedDesc);

//            specs["Unit Price"] = unitPrice;
//            specs["Match Score"] = $"{matchScore}%";
//            specs["Mouser PN"] = part.TryGetProperty(
//                "MouserPartNumber", out var mpn)
//                ? mpn.GetString() : "";

//            results.Add(new PartDetails
//            {
//                Mpn = part.TryGetProperty(
//                    "ManufacturerPartNumber", out var m)
//                    ? m.GetString() : "",
//                Description = fetchedDesc,
//                Manufacturer = part.TryGetProperty(
//                    "Manufacturer", out var mfr)
//                    ? mfr.GetString() : "",
//                Category = part.TryGetProperty(
//                    "Category", out var cat)
//                    ? cat.GetString() : "",
//                DatasheetUrl = part.TryGetProperty(
//                    "DataSheetUrl", out var ds)
//                    ? ds.GetString() : "",
//                ProductUrl = part.TryGetProperty(
//                    "ProductDetailUrl", out var pu)
//                    ? pu.GetString() : "",
//                Stock = part.TryGetProperty(
//                    "Availability", out var av)
//                    ? av.GetString() : "0",
//                Specs = specs,
//                Source = "Mouser"
//            });
//        }

//        return results
//            .OrderByDescending(r =>
//                double.TryParse(
//                    r.Specs.GetValueOrDefault(
//                        "Match Score", "0").Replace("%", ""),
//                    out var score) ? score : 0)
//            .ToList();
//    }

//    private static double CalculateMatchScore(
//        string keyword, string description)
//    {
//        if (string.IsNullOrWhiteSpace(description)) return 0;
//        var words = keyword.ToLower().Split(' ',
//            StringSplitOptions.RemoveEmptyEntries);
//        var descLow = description.ToLower();
//        int matched = words.Count(w => descLow.Contains(w));
//        return Math.Round(
//            (double)matched / words.Length * 100, 1);
//    }
//}











//using Alter_Parts.Models;
//using System.Text;
//using System.Text.Json;

//public class MouserService
//{
//    private readonly HttpClient _httpClient;
//    private readonly IConfiguration _config;

//    public MouserService(HttpClient httpClient, IConfiguration config)
//    {
//        _httpClient = httpClient;
//        _config = config;
//    }

//    // ── SHARED: EXTRACT SPECS FROM ProductAttributes ──────────────
//    private static Dictionary<string, string> ExtractSpecs(JsonElement part)
//    {
//        var specs = new Dictionary<string, string>();
//        if (part.TryGetProperty("ProductAttributes", out var attrs) &&
//            attrs.ValueKind != JsonValueKind.Null)
//        {
//            foreach (var attr in attrs.EnumerateArray())
//            {
//                var name = attr.TryGetProperty("AttributeName", out var n) ? n.GetString() : null;
//                var value = attr.TryGetProperty("AttributeValue", out var v) ? v.GetString() : null;
//                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
//                    specs[name] = value;
//            }
//        }
//        return specs;
//    }

//    // ── SHARED: EXTRACT PRICE BREAKS ─────────────────────────────
//    // Mouser PriceBreaks structure:
//    // { "Quantity": "1", "Price": "$0.1000", "Currency": "USD" }
//    private static List<PriceBreak> ExtractPriceBreaks(JsonElement part)
//    {
//        var priceBreaks = new List<PriceBreak>();

//        if (!part.TryGetProperty("PriceBreaks", out var pb) ||
//            pb.ValueKind == JsonValueKind.Null ||
//            pb.GetArrayLength() == 0)
//            return priceBreaks;

//        foreach (var breakItem in pb.EnumerateArray())
//        {
//            // Quantity comes as a string from Mouser e.g. "1", "10", "100"
//            var qtyStr = breakItem.TryGetProperty("Quantity", out var qty)
//                ? qty.GetString() : null;

//            // Price comes as a formatted string e.g. "$0.1000" or "0.1000"
//            var priceStr = breakItem.TryGetProperty("Price", out var price)
//                ? price.GetString() : null;

//            if (string.IsNullOrEmpty(qtyStr) || string.IsNullOrEmpty(priceStr))
//                continue;

//            // Strip currency symbol and parse
//            var cleanPrice = priceStr.Replace("$", "")
//                                     .Replace("£", "")
//                                     .Replace("€", "")
//                                     .Replace(",", "")
//                                     .Trim();

//            if (int.TryParse(qtyStr, out var quantity) &&
//                decimal.TryParse(cleanPrice,
//                    System.Globalization.NumberStyles.Any,
//                    System.Globalization.CultureInfo.InvariantCulture,
//                    out var unitPrice))
//            {
//                priceBreaks.Add(new PriceBreak
//                {
//                    BreakQuantity = quantity,
//                    UnitPrice = unitPrice
//                });
//            }
//        }

//        return priceBreaks;
//    }

//    // ── SHARED: CALCULATE MATCH SCORE ────────────────────────────
//    private static double CalculateMatchScore(string keyword, string description)
//    {
//        if (string.IsNullOrWhiteSpace(description)) return 0;
//        var words = keyword.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
//        var descLow = description.ToLower();
//        int matched = words.Count(w => descLow.Contains(w));
//        return Math.Round((double)matched / words.Length * 100, 1);
//    }

//    // ── GET PART DETAILS (single MPN lookup) ─────────────────────
//    public async Task<PartDetails> GetPartDetails(string mpn)
//    {
//        var apiKey = _config["Mouser:ApiKey"];
//        var url = $"https://api.mouser.com/api/v1/search/keyword?apiKey={apiKey}";

//        var requestBody = new
//        {
//            SearchByKeywordRequest = new
//            {
//                keyword = mpn.Trim(),
//                records = 1,
//                startingRecord = 0,
//                searchOptions = "Exact"
//            }
//        };

//        var response = await _httpClient.PostAsync(url,
//            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
//        var responseContent = await response.Content.ReadAsStringAsync();

//        if (!response.IsSuccessStatusCode)
//            throw new Exception($"Mouser API error: {response.StatusCode}. Body: {responseContent}");

//        using var doc = JsonDocument.Parse(responseContent);
//        var root = doc.RootElement;

//        if (root.TryGetProperty("Errors", out var errors) && errors.GetArrayLength() > 0)
//            throw new Exception($"Mouser error: {errors[0].GetProperty("Message").GetString()}");

//        var parts = root.GetProperty("SearchResults").GetProperty("Parts");

//        if (parts.ValueKind == JsonValueKind.Null || parts.GetArrayLength() == 0)
//            return null;

//        var part = parts[0];
//        var specs = ExtractSpecs(part);
//        var priceBreaks = ExtractPriceBreaks(part);

//        // Unit price from first price break
//        string unitPrice = "N/A";
//        if (priceBreaks.Any())
//            unitPrice = $"${priceBreaks.First().UnitPrice:0.0000}";

//        specs["Unit Price"] = unitPrice;
//        specs["Mouser PN"] = part.TryGetProperty("MouserPartNumber", out var mouserPn)
//            ? mouserPn.GetString() ?? "" : "";

//        return new PartDetails
//        {
//            Mpn = part.TryGetProperty("ManufacturerPartNumber", out var m) ? m.GetString() : mpn,
//            Description = part.TryGetProperty("Description", out var d) ? d.GetString() : "",
//            Manufacturer = part.TryGetProperty("Manufacturer", out var mfr) ? mfr.GetString() : "",
//            DatasheetUrl = part.TryGetProperty("DataSheetUrl", out var ds) ? ds.GetString() : "",
//            ProductUrl = part.TryGetProperty("ProductDetailUrl", out var pu) ? pu.GetString() : "",
//            Stock = part.TryGetProperty("Availability", out var av) ? av.GetString() : "",
//            Category = part.TryGetProperty("Category", out var cat) ? cat.GetString() : "",
//            Specs = specs,
//            PriceBreaks = priceBreaks,
//            Source = "Mouser"
//        };
//    }

//    // ── SEARCH BY DESCRIPTION ─────────────────────────────────────
//    public async Task<List<PartDetails>> SearchByDescription(string description, int limit = 10)
//    {
//        var apiKey = _config["Mouser:ApiKey"];
//        var url = $"https://api.mouser.com/api/v1/search/keyword?apiKey={apiKey}";

//        var requestBody = new
//        {
//            SearchByKeywordRequest = new
//            {
//                keyword = description.Trim(),
//                records = limit,
//                startingRecord = 0,
//                searchOptions = "string",
//                searchWithYourSignUpLanguage = "false"
//            }
//        };

//        var response = await _httpClient.PostAsync(url,
//            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
//        var responseContent = await response.Content.ReadAsStringAsync();

//        if (!response.IsSuccessStatusCode)
//            throw new Exception($"Mouser API error: {response.StatusCode}. Body: {responseContent}");

//        using var doc = JsonDocument.Parse(responseContent);
//        var root = doc.RootElement;

//        if (root.TryGetProperty("Errors", out var errors) && errors.GetArrayLength() > 0)
//            throw new Exception($"Mouser error: {errors[0].GetProperty("Message").GetString()}");

//        var results = new List<PartDetails>();
//        var parts = root.GetProperty("SearchResults").GetProperty("Parts");

//        if (parts.ValueKind == JsonValueKind.Null || parts.GetArrayLength() == 0)
//            return results;

//        foreach (var part in parts.EnumerateArray())
//        {
//            var specs = ExtractSpecs(part);
//            var priceBreaks = ExtractPriceBreaks(part);
//            var fetchedDesc = part.TryGetProperty("Description", out var d) ? d.GetString() : "";
//            var matchScore = CalculateMatchScore(description, fetchedDesc);

//            // Unit price from first price break
//            string unitPrice = "N/A";
//            if (priceBreaks.Any())
//                unitPrice = $"${priceBreaks.First().UnitPrice:0.0000}";

//            // Store display values in Specs
//            specs["Unit Price"] = unitPrice;
//            specs["Match Score"] = $"{matchScore}%";
//            specs["Mouser PN"] = part.TryGetProperty("MouserPartNumber", out var mpnEl)
//                ? mpnEl.GetString() ?? "" : "";

//            results.Add(new PartDetails
//            {
//                Mpn = part.TryGetProperty("ManufacturerPartNumber", out var m) ? m.GetString() : "",
//                Description = fetchedDesc,
//                Manufacturer = part.TryGetProperty("Manufacturer", out var mfr) ? mfr.GetString() : "",
//                Category = part.TryGetProperty("Category", out var cat) ? cat.GetString() : "",
//                DatasheetUrl = part.TryGetProperty("DataSheetUrl", out var ds) ? ds.GetString() : "",
//                ProductUrl = part.TryGetProperty("ProductDetailUrl", out var pu) ? pu.GetString() : "",
//                Stock = part.TryGetProperty("Availability", out var av) ? av.GetString() : "0",
//                Specs = specs,
//                PriceBreaks = priceBreaks,
//                Source = "Mouser"
//            });
//        }

//        return results
//            .OrderByDescending(r =>
//                double.TryParse(
//                    r.Specs.GetValueOrDefault("Match Score", "0").Replace("%", ""),
//                    out var score) ? score : 0)
//            .ToList();
//    }
//}














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

    // ── SHARED: EXTRACT SPECS FROM ProductAttributes ──────────────
    private static Dictionary<string, string> ExtractSpecs(JsonElement part)
    {
        var specs = new Dictionary<string, string>();
        if (part.TryGetProperty("ProductAttributes", out var attrs) &&
            attrs.ValueKind != JsonValueKind.Null)
        {
            foreach (var attr in attrs.EnumerateArray())
            {
                var name = attr.TryGetProperty("AttributeName", out var n) ? n.GetString() : null;
                var value = attr.TryGetProperty("AttributeValue", out var v) ? v.GetString() : null;
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                    specs[name] = value;
            }
        }
        return specs;
    }

    // ── SHARED: EXTRACT PRICE BREAKS ─────────────────────────────
    // Mouser structure: { "Quantity": 1, "Price": "₹9.32", "Currency": "INR" }
    private static List<PriceBreak> ExtractPriceBreaks(JsonElement part)
    {
        var priceBreaks = new List<PriceBreak>();

        if (!part.TryGetProperty("PriceBreaks", out var pb) ||
            pb.ValueKind == JsonValueKind.Null ||
            pb.GetArrayLength() == 0)
            return priceBreaks;

        foreach (var breakItem in pb.EnumerateArray())
        {
            // Quantity is an integer in Mouser response
            if (!breakItem.TryGetProperty("Quantity", out var qty))
                continue;

            // Price is a string like "₹9.32" or "$0.10"
            if (!breakItem.TryGetProperty("Price", out var priceEl))
                continue;

            var priceStr = priceEl.GetString() ?? "";

            // Strip all currency symbols and whitespace, keep digits and decimal
            var cleanPrice = new string(priceStr
                .Where(c => char.IsDigit(c) || c == '.' || c == ',')
                .ToArray())
                .Replace(",", "");

            if (string.IsNullOrEmpty(cleanPrice)) continue;

            if (decimal.TryParse(cleanPrice,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var unitPrice))
            {
                priceBreaks.Add(new PriceBreak
                {
                    BreakQuantity = qty.GetInt32(),
                    UnitPrice = unitPrice
                });
            }
        }

        return priceBreaks;
    }

    // ── SHARED: GET UNIT PRICE STRING ────────────────────────────
    // Returns the raw price string from first break e.g. "₹9.32"
    private static string GetUnitPriceString(JsonElement part)
    {
        if (part.TryGetProperty("PriceBreaks", out var pb) &&
            pb.ValueKind != JsonValueKind.Null &&
            pb.GetArrayLength() > 0)
        {
            var firstBreak = pb[0];
            if (firstBreak.TryGetProperty("Price", out var p))
                return p.GetString() ?? "N/A";
        }
        return "N/A";
    }

    // ── SHARED: CALCULATE MATCH SCORE ────────────────────────────
    private static double CalculateMatchScore(string keyword, string description)
    {
        if (string.IsNullOrWhiteSpace(description)) return 0;
        var words = keyword.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var descLow = description.ToLower();
        int matched = words.Count(w => descLow.Contains(w));
        return Math.Round((double)matched / words.Length * 100, 1);
    }

    // ── GET PART DETAILS (single MPN lookup) ─────────────────────
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

        var response = await _httpClient.PostAsync(url,
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Mouser API error: {response.StatusCode}. Body: {responseContent}");

        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        if (root.TryGetProperty("Errors", out var errors) && errors.GetArrayLength() > 0)
            throw new Exception($"Mouser error: {errors[0].GetProperty("Message").GetString()}");

        var parts = root.GetProperty("SearchResults").GetProperty("Parts");

        if (parts.ValueKind == JsonValueKind.Null || parts.GetArrayLength() == 0)
            return null;

        var part = parts[0];
        var specs = ExtractSpecs(part);
        var priceBreaks = ExtractPriceBreaks(part);

        specs["Unit Price"] = GetUnitPriceString(part);
        specs["Mouser PN"] = part.TryGetProperty("MouserPartNumber", out var mouserPn)
            ? mouserPn.GetString() ?? "" : "";

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
            PriceBreaks = priceBreaks,
            Source = "Mouser"
        };
    }

    // ── SEARCH BY DESCRIPTION ─────────────────────────────────────
    public async Task<List<PartDetails>> SearchByDescription(string description, int limit = 10)
    {
        var apiKey = _config["Mouser:ApiKey"];
        var url = $"https://api.mouser.com/api/v1/search/keyword?apiKey={apiKey}";

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

        var response = await _httpClient.PostAsync(url,
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Mouser API error: {response.StatusCode}. Body: {responseContent}");

        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        if (root.TryGetProperty("Errors", out var errors) && errors.GetArrayLength() > 0)
            throw new Exception($"Mouser error: {errors[0].GetProperty("Message").GetString()}");

        var results = new List<PartDetails>();
        var parts = root.GetProperty("SearchResults").GetProperty("Parts");

        if (parts.ValueKind == JsonValueKind.Null || parts.GetArrayLength() == 0)
            return results;

        foreach (var part in parts.EnumerateArray())
        {
            var specs = ExtractSpecs(part);
            var priceBreaks = ExtractPriceBreaks(part);
            var fetchedDesc = part.TryGetProperty("Description", out var d) ? d.GetString() : "";
            var matchScore = CalculateMatchScore(description, fetchedDesc);

            specs["Unit Price"] = GetUnitPriceString(part);
            specs["Match Score"] = $"{matchScore}%";
            specs["Mouser PN"] = part.TryGetProperty("MouserPartNumber", out var mpnEl)
                ? mpnEl.GetString() ?? "" : "";

            // Extract lifecycle status from Mouser
            string lifecycleStatus = "Active";
            if (part.TryGetProperty("LifecycleStatus", out var lc) &&
                lc.ValueKind != JsonValueKind.Null)
            {
                var lcStr = lc.GetString() ?? "";
                if (lcStr.Contains("Obsolete", StringComparison.OrdinalIgnoreCase))
                    lifecycleStatus = "EndOfLife";
                else if (lcStr.Contains("Discontinued", StringComparison.OrdinalIgnoreCase))
                    lifecycleStatus = "Discontinued";
                else if (lcStr.Contains("NRND", StringComparison.OrdinalIgnoreCase) ||
                         lcStr.Contains("Not Recommended", StringComparison.OrdinalIgnoreCase))
                    lifecycleStatus = "NRND";
            }

            results.Add(new PartDetails
            {
                Mpn = part.TryGetProperty("ManufacturerPartNumber", out var m) ? m.GetString() : "",
                Description = fetchedDesc,
                Manufacturer = part.TryGetProperty("Manufacturer", out var mfr) ? mfr.GetString() : "",
                Category = part.TryGetProperty("Category", out var cat) ? cat.GetString() : "",
                DatasheetUrl = part.TryGetProperty("DataSheetUrl", out var ds) ? ds.GetString() : "",
                ProductUrl = part.TryGetProperty("ProductDetailUrl", out var pu) ? pu.GetString() : "",
                Stock = part.TryGetProperty("Availability", out var av) ? av.GetString() : "0",
                Specs = specs,
                PriceBreaks = priceBreaks,
                Source = "Mouser"
            });
        }

        return results
            .OrderByDescending(r =>
                double.TryParse(
                    r.Specs.GetValueOrDefault("Match Score", "0").Replace("%", ""),
                    out var score) ? score : 0)
            .ToList();
    }
}
