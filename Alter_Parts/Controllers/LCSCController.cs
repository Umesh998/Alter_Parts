//using Alter_Parts.Models;
//using Alter_Parts.Services;
//using Microsoft.AspNetCore.Mvc;

//namespace Alter_Parts.Controllers
//{
//    public class LCSCController : Controller
//    {
//        private readonly LCSCService _lcsc;

//        public LCSCController(LCSCService lcsc)
//            => _lcsc = lcsc;

//        // GET: /LCSC
//        [HttpGet]
//        public IActionResult Index()
//            => View(new LCSCSearchViewModel());

//        // POST: /LCSC/Search
//        [HttpPost]
//        public async Task<IActionResult> Search(
//            LCSCSearchRequest request)
//        {
//            var vm = new LCSCSearchViewModel
//            {
//                Keyword = request.Keyword
//            };

//            if (string.IsNullOrWhiteSpace(request.Keyword))
//            {
//                vm.Error = "Please enter a description to search.";
//                return View("Index", vm);
//            }

//            try
//            {
//                vm.Results = await _lcsc.SearchByKeyword(
//                    request.Keyword, request.Limit);
//                vm.TotalFound = vm.Results.Count;

//                if (!vm.Results.Any())
//                    vm.Error =
//                        "No parts found for this description. " +
//                        "Try different keywords.";
//            }
//            catch (Exception ex)
//            {
//                vm.Error = $"Search failed: {ex.Message}";
//            }

//            return View("Index", vm);
//        }

//        // GET: /LCSC/Debug?keyword=capacitor
//        // Temporary debug action to see raw response
//        [HttpGet]
//        public async Task<IActionResult> Debug(string keyword)
//        {
//            try
//            {
//                var results = await _lcsc
//                    .SearchByKeyword(keyword ?? "capacitor", 3);
//                return Json(results);
//            }
//            catch (Exception ex)
//            {
//                return Content($"ERROR: {ex.Message}");
//            }
//        }
//    }
//}













using Alter_Parts.Models;
using Alter_Parts.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace Alter_Parts.Controllers
{
    public class LCSCController : Controller
    {
        private readonly LCSCService _lcsc;
        private readonly IConfiguration _config;

        public LCSCController(LCSCService lcsc, IConfiguration config)
        {
            _lcsc = lcsc;
            _config = config;
        }

        [HttpGet]
        public IActionResult Index()
            => View(new LCSCSearchViewModel());

        [HttpPost]
        public async Task<IActionResult> Search(LCSCSearchRequest request)
        {
            var vm = new LCSCSearchViewModel { Keyword = request.Keyword };

            if (string.IsNullOrWhiteSpace(request.Keyword))
            {
                vm.Error = "Please enter a description to search.";
                return View("Index", vm);
            }

            try
            {
                vm.Results = await _lcsc.SearchByKeyword(request.Keyword, request.Limit);
                vm.TotalFound = vm.Results.Count;

                if (!vm.Results.Any())
                    vm.Error = "No parts found. Try different keywords.";
            }
            catch (Exception ex)
            {
                vm.Error = $"Search failed: {ex.Message}";
            }

            return View("Index", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Debug(string keyword)
        {
            try
            {
                var results = await _lcsc.SearchByKeyword(keyword ?? "capacitor", 3);
                return Json(results);
            }
            catch (Exception ex)
            {
                return Content($"ERROR: {ex.Message}");
            }
        }

        // ── TEMPORARY TEST ACTION ────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> TestLCSC()
        {
            var key = _config["LCSC:ApiKey"];
            var secret = _config["LCSC:ApiSecret"];

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret))
                return Content("ERROR: Keys are null — check appsettings.json");

            var nonce = Guid.NewGuid().ToString("N")[..16];
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

            var rawSignature = $"key={key}&nonce={nonce}&secret={secret}&timestamp={timestamp}";

            using var sha1 = SHA1.Create();
            var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(rawSignature));
            var signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            var url = "https://lcsc.com/api/global/search/product?" +
                      $"key={Uri.EscapeDataString(key)}" +
                      $"&nonce={nonce}" +
                      $"&timestamp={timestamp}" +
                      $"&sign={signature}" +
                      $"&keyword=LM358" +
                      $"&page_size=1";

            Console.WriteLine($"[TEST] Raw signature: {rawSignature}");
            Console.WriteLine($"[TEST] Signature: {signature}");
            Console.WriteLine($"[TEST] URL: {url}");

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            client.DefaultRequestHeaders.Add("Referer", "https://www.lcsc.com/");

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[TEST] Status: {response.StatusCode}");
            Console.WriteLine($"[TEST] Response: {content}");

            // Show everything in browser
            var output = $"KEY: {key}\n" +
                         $"NONCE: {nonce}\n" +
                         $"TIMESTAMP: {timestamp}\n" +
                         $"RAW SIGNATURE: {rawSignature}\n" +
                         $"SIGNATURE: {signature}\n\n" +
                         $"URL: {url}\n\n" +
                         $"STATUS: {response.StatusCode}\n\n" +
                         $"RESPONSE:\n{content}";

            return Content(output);
        }
    }
}