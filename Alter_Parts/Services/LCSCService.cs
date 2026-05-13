//using Alter_Parts.Models;
//using System.Security.Cryptography;
//using System.Text;
//using System.Text.Json;

//namespace Alter_Parts.Services
//{
//    public class LCSCService
//    {
//        private readonly HttpClient _httpClient;
//        private readonly IConfiguration _config;
//        private const string BaseUrl = "https://bom.lcsc.com/api/search";

//        public LCSCService(HttpClient httpClient, IConfiguration config)
//        {
//            _httpClient = httpClient;
//            _config = config;
//        }

//        // ── Generate auth params ──────────────────────────────────
//        private (string key, string nonce, string timestamp, string signature) GenerateAuthParams()
//        {
//            var key = _config["LCSC:Key"];
//            var secret = _config["LCSC:Secret"];

//            // 🚨 SAFETY CHECK #1: Prevent crash if config is missing
//            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(secret))
//            {
//                throw new Exception("CRITICAL ERROR: LCSC Key or Secret is missing from appsettings.json.");
//            }

//            var nonce = Guid.NewGuid().ToString("N")[..16];
//            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

//            // signature = sha1(key=x&nonce=x&secret=x&timestamp=x)
//            var raw = $"key={key}&nonce={nonce}&secret={secret}&timestamp={timestamp}";

//            using var sha1 = SHA1.Create();
//            var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(raw));
//            var signature = Convert.ToHexString(hashBytes).ToLower();

//            return (key, nonce, timestamp, signature);
//        }

//        // ── Search by keyword/description ─────────────────────────
//        public async Task<List<LCSCPartResult>> SearchByKeyword(string keyword, int limit = 10)
//        {
//            // 🚨 SAFETY CHECK #2: Don't search for blank Excel cells
//            if (string.IsNullOrWhiteSpace(keyword))
//                return new List<LCSCPartResult>();

//            var (key, nonce, timestamp, signature) = GenerateAuthParams();

//            // 🚨 SAFETY CHECK #3: The '?? ""' guarantees it never crashes on URL escape
//            var url = $"{BaseUrl}/keyword?" +
//                      $"key={Uri.EscapeDataString(key ?? "")}" +
//                      $"&nonce={nonce}" +
//                      $"&timestamp={timestamp}" +
//                      $"&signature={signature}" +
//                      $"&keyword={Uri.EscapeDataString(keyword ?? "")}" +
//                      $"&currentPage=1" +
//                      $"&pageSize={limit}";

//            var response = await _httpClient.GetAsync(url);
//            var content = await response.Content.ReadAsStringAsync();

//            if (!response.IsSuccessStatusCode)
//                throw new Exception($"LCSC API error: {response.StatusCode}. Body: {content}");

//            using var doc = JsonDocument.Parse(content);
//            var root = doc.RootElement;

//            if (root.TryGetProperty("success", out var success) && !success.GetBoolean())
//            {
//                var msg = root.TryGetProperty("message", out var m) ? m.GetString() : "Unknown error";
//                throw new Exception($"LCSC error: {msg}");
//            }

//            var results = new List<LCSCPartResult>();

//            if (!root.TryGetProperty("result", out var result) || result.ValueKind == JsonValueKind.Null)
//                return results;

//            JsonElement list;
//            if (result.ValueKind == JsonValueKind.Array)
//                list = result;
//            else if (result.TryGetProperty("list", out var l))
//                list = l;
//            else if (result.TryGetProperty("data", out var d))
//                list = d;
//            else
//                return results;

//            foreach (var item in list.EnumerateArray())
//            {
//                results.Add(new LCSCPartResult
//                {
//                    LcscPartNumber = GetStr(item, "lcscPart", "productCode", "partNumber"),
//                    MpnNumber = GetStr(item, "mfcPart", "mfcPartNumber", "mpn"),
//                    Description = GetStr(item, "productDescEn", "description", "desc"),
//                    Manufacturer = GetStr(item, "brandNameEn", "manufacturer", "brand"),
//                    Category = GetStr(item, "catalogName", "category", "categoryName"),
//                    Stock = GetStr(item, "stockNumber", "stock", "qty"),
//                    Package = GetStr(item, "encapStandard", "package", "casePackage"),
//                    Price = GetLowestPrice(item),
//                    DatasheetUrl = GetStr(item, "pdfUrl", "datasheet", "datasheetUrl"),
//                    ProductUrl = BuildProductUrl(GetStr(item, "lcscPart", "productCode", "partNumber")),
//                    MatchScore = CalculateMatch(keyword, GetStr(item, "productDescEn", "description", "desc"))
//                });
//            }

//            return results.OrderByDescending(r => r.MatchScore).ToList();
//        }

//        // ── Get part details by MPN ───────────────────────────────
//        public async Task<LCSCPartResult> GetPartByMpn(string mpn)
//        {
//            // The blank check in SearchByKeyword protects this method now too!
//            var results = await SearchByKeyword(mpn, limit: 1);
//            return results.FirstOrDefault();
//        }

//        // ── Helpers ───────────────────────────────────────────────
//        private static string GetStr(JsonElement el, params string[] keys)
//        {
//            foreach (var key in keys)
//                if (el.TryGetProperty(key, out var val) && val.ValueKind != JsonValueKind.Null)
//                    return val.GetString() ?? "";
//            return "";
//        }

//        private static string GetLowestPrice(JsonElement item)
//        {
//            if (item.TryGetProperty("productPriceList", out var priceList) &&
//                priceList.ValueKind == JsonValueKind.Array &&
//                priceList.GetArrayLength() > 0)
//            {
//                var first = priceList[0];
//                if (first.TryGetProperty("usdPrice", out var usd))
//                    return $"${usd.GetDecimal():0.0000}";
//                if (first.TryGetProperty("price", out var price))
//                    return $"${price.GetDecimal():0.0000}";
//            }

//            if (item.TryGetProperty("price", out var p) && p.ValueKind != JsonValueKind.Null)
//                return $"${p.GetDecimal():0.0000}";

//            return "N/A";
//        }

//        private static string BuildProductUrl(string lcscPart)
//        {
//            if (string.IsNullOrEmpty(lcscPart)) return "";
//            return $"https://www.lcsc.com/product-detail/{lcscPart}.html";
//        }

//        private static double CalculateMatch(string keyword, string description)
//        {
//            if (string.IsNullOrWhiteSpace(description)) return 0;

//            var words = keyword.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
//            var descLow = description.ToLower();
//            int matched = words.Count(w => descLow.Contains(w));

//            return Math.Round((double)matched / words.Length * 100, 1);
//        }
//    }
//}








using Alter_Parts.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace Alter_Parts.Services
{
    public class LCSCService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        // Official Open API Endpoint (V1 is often more stable for Keyword searches)
        private const string BaseUrl = "https://api.lcsc.com/openapi/v1/products/search";

        public LCSCService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;

            // ── ULTRA-STEALTH BROWSER SPOOFING ──
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            _httpClient.DefaultRequestHeaders.Add("Referer", "https://www.lcsc.com/");
            _httpClient.DefaultRequestHeaders.Add("Origin", "https://www.lcsc.com");
            _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Chromium\";v=\"124\", \"Google Chrome\";v=\"124\", \"Not-A.Brand\";v=\"99\"");
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
        }

        public async Task<List<LCSCPartResult>> SearchByKeyword(string keyword, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new List<LCSCPartResult>();

            var key = _config["ApiSettings:LCSC:ApiKey"];
            var secret = _config["ApiSettings:LCSC:ApiSecret"];

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(secret))
            {
                Debug.WriteLine("[LCSC] API key or secret is missing from config!");
                return new List<LCSCPartResult>();
            }

            var nonce = Guid.NewGuid().ToString("N")[..16];
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

            var rawSignature = $"key={key}&nonce={nonce}&secret={secret}&timestamp={timestamp}";

            using var sha1 = SHA1.Create();
            var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(rawSignature));
            var signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            var url = $"{BaseUrl}?" +
                      $"key={Uri.EscapeDataString(key)}" +
                      $"&nonce={nonce}" +
                      $"&timestamp={timestamp}" +
                      $"&sign={signature}" +
                      $"&keyword={Uri.EscapeDataString(keyword)}" +
                      $"&match_type=fuzzy" +
                      $"&page_size={limit}";

            Debug.WriteLine($"[LCSC DEBUG] Raw signature string: {rawSignature}");
            Debug.WriteLine($"[LCSC DEBUG] Generated signature: {signature}");
            Debug.WriteLine($"[LCSC DEBUG] URL: {url}");

            try
            {
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"[LCSC DEBUG] Status: {response.StatusCode}");
                Debug.WriteLine($"[LCSC DEBUG] Response: {content}");

                if (!response.IsSuccessStatusCode)
                    return new List<LCSCPartResult>();

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("code", out var apiCode) && apiCode.GetInt32() != 200)
                    return new List<LCSCPartResult>();

                var results = new List<LCSCPartResult>();
                if (!root.TryGetProperty("result", out var resultObj))
                    return results;

                JsonElement list = default;
                if (resultObj.TryGetProperty("productList", out var pl)) list = pl;
                else if (resultObj.TryGetProperty("list", out var l)) list = l;
                else if (resultObj.ValueKind == JsonValueKind.Array) list = resultObj;

                if (list.ValueKind != JsonValueKind.Array)
                    return results;

                foreach (var item in list.EnumerateArray())
                {
                    var desc = GetStr(item, "productDescEn", "description", "productDesc");
                    results.Add(new LCSCPartResult
                    {
                        LcscPartNumber = GetStr(item, "productCode", "lcscPart", "productNumber"),
                        MpnNumber = GetStr(item, "productModel", "mfcPart", "mpn"),
                        Description = desc,
                        Manufacturer = GetStr(item, "brandNameEn", "manufacturer", "brandName"),
                        Stock = GetStr(item, "stockNumber", "stock", "qty"),
                        Price = "Check Site",
                        MatchScore = CalculateMatch(keyword, desc)
                    });
                }

                return results; // ✅ return inside try
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LCSC] Exception: {ex.Message}");
                return new List<LCSCPartResult>(); // ✅ return inside catch
            }
            // ✅ No code after try-catch needed since both paths return
        }

        // ... rest of method stays the same

        public async Task<LCSCPartResult> GetPartByMpn(string mpn)
        {
            var results = await SearchByKeyword(mpn, limit: 1);
            return results.FirstOrDefault();
        }

        private static string GetStr(JsonElement el, params string[] keys)
        {
            foreach (var key in keys)
                if (el.TryGetProperty(key, out var val)) return val.ToString();
            return "";
        }

        private static double CalculateMatch(string k, string d)
        {
            if (string.IsNullOrWhiteSpace(d) || string.IsNullOrWhiteSpace(k)) return 0;
            var words = k.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var descLow = d.ToLower();
            int matched = words.Count(w => descLow.Contains(w));
            return Math.Round((double)matched / words.Length * 100, 1);
        }
    }
}