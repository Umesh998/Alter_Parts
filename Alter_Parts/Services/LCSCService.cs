using Alter_Parts.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Alter_Parts.Services
{
    public class LCSCService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private const string BaseUrl = "https://bom.lcsc.com/api/search";

        public LCSCService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        // ── Generate auth params ──────────────────────────────────
        private (string key, string nonce, string timestamp, string signature) GenerateAuthParams()
        {
            var key = _config["LCSC:Key"];
            var secret = _config["LCSC:Secret"];

            // 🚨 SAFETY CHECK #1: Prevent crash if config is missing
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(secret))
            {
                throw new Exception("CRITICAL ERROR: LCSC Key or Secret is missing from appsettings.json.");
            }

            var nonce = Guid.NewGuid().ToString("N")[..16];
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            // signature = sha1(key=x&nonce=x&secret=x&timestamp=x)
            var raw = $"key={key}&nonce={nonce}&secret={secret}&timestamp={timestamp}";

            using var sha1 = SHA1.Create();
            var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(raw));
            var signature = Convert.ToHexString(hashBytes).ToLower();

            return (key, nonce, timestamp, signature);
        }

        // ── Search by keyword/description ─────────────────────────
        public async Task<List<LCSCPartResult>> SearchByKeyword(string keyword, int limit = 10)
        {
            // 🚨 SAFETY CHECK #2: Don't search for blank Excel cells
            if (string.IsNullOrWhiteSpace(keyword))
                return new List<LCSCPartResult>();

            var (key, nonce, timestamp, signature) = GenerateAuthParams();

            // 🚨 SAFETY CHECK #3: The '?? ""' guarantees it never crashes on URL escape
            var url = $"{BaseUrl}/keyword?" +
                      $"key={Uri.EscapeDataString(key ?? "")}" +
                      $"&nonce={nonce}" +
                      $"&timestamp={timestamp}" +
                      $"&signature={signature}" +
                      $"&keyword={Uri.EscapeDataString(keyword ?? "")}" +
                      $"&currentPage=1" +
                      $"&pageSize={limit}";

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"LCSC API error: {response.StatusCode}. Body: {content}");

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("success", out var success) && !success.GetBoolean())
            {
                var msg = root.TryGetProperty("message", out var m) ? m.GetString() : "Unknown error";
                throw new Exception($"LCSC error: {msg}");
            }

            var results = new List<LCSCPartResult>();

            if (!root.TryGetProperty("result", out var result) || result.ValueKind == JsonValueKind.Null)
                return results;

            JsonElement list;
            if (result.ValueKind == JsonValueKind.Array)
                list = result;
            else if (result.TryGetProperty("list", out var l))
                list = l;
            else if (result.TryGetProperty("data", out var d))
                list = d;
            else
                return results;

            foreach (var item in list.EnumerateArray())
            {
                results.Add(new LCSCPartResult
                {
                    LcscPartNumber = GetStr(item, "lcscPart", "productCode", "partNumber"),
                    MpnNumber = GetStr(item, "mfcPart", "mfcPartNumber", "mpn"),
                    Description = GetStr(item, "productDescEn", "description", "desc"),
                    Manufacturer = GetStr(item, "brandNameEn", "manufacturer", "brand"),
                    Category = GetStr(item, "catalogName", "category", "categoryName"),
                    Stock = GetStr(item, "stockNumber", "stock", "qty"),
                    Package = GetStr(item, "encapStandard", "package", "casePackage"),
                    Price = GetLowestPrice(item),
                    DatasheetUrl = GetStr(item, "pdfUrl", "datasheet", "datasheetUrl"),
                    ProductUrl = BuildProductUrl(GetStr(item, "lcscPart", "productCode", "partNumber")),
                    MatchScore = CalculateMatch(keyword, GetStr(item, "productDescEn", "description", "desc"))
                });
            }

            return results.OrderByDescending(r => r.MatchScore).ToList();
        }

        // ── Get part details by MPN ───────────────────────────────
        public async Task<LCSCPartResult> GetPartByMpn(string mpn)
        {
            // The blank check in SearchByKeyword protects this method now too!
            var results = await SearchByKeyword(mpn, limit: 1);
            return results.FirstOrDefault();
        }

        // ── Helpers ───────────────────────────────────────────────
        private static string GetStr(JsonElement el, params string[] keys)
        {
            foreach (var key in keys)
                if (el.TryGetProperty(key, out var val) && val.ValueKind != JsonValueKind.Null)
                    return val.GetString() ?? "";
            return "";
        }

        private static string GetLowestPrice(JsonElement item)
        {
            if (item.TryGetProperty("productPriceList", out var priceList) &&
                priceList.ValueKind == JsonValueKind.Array &&
                priceList.GetArrayLength() > 0)
            {
                var first = priceList[0];
                if (first.TryGetProperty("usdPrice", out var usd))
                    return $"${usd.GetDecimal():0.0000}";
                if (first.TryGetProperty("price", out var price))
                    return $"${price.GetDecimal():0.0000}";
            }

            if (item.TryGetProperty("price", out var p) && p.ValueKind != JsonValueKind.Null)
                return $"${p.GetDecimal():0.0000}";

            return "N/A";
        }

        private static string BuildProductUrl(string lcscPart)
        {
            if (string.IsNullOrEmpty(lcscPart)) return "";
            return $"https://www.lcsc.com/product-detail/{lcscPart}.html";
        }

        private static double CalculateMatch(string keyword, string description)
        {
            if (string.IsNullOrWhiteSpace(description)) return 0;

            var words = keyword.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var descLow = description.ToLower();
            int matched = words.Count(w => descLow.Contains(w));

            return Math.Round((double)matched / words.Length * 100, 1);
        }
    }
}