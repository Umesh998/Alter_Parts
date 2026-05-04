using Alter_Parts.Models;
using Alter_Parts.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OfficeOpenXml;

namespace Alter_Parts.Controllers
{
    public class SourcingController : Controller
    {
        private const int TopResultsPerDistributor = 3;

        private readonly MouserService _mouser;
        private readonly DigiKeyService _digiKey;
        private readonly LCSCService _lcsc;
        private readonly ExcelExportService _excelExport;
        private readonly IMemoryCache _cache;

        public SourcingController(
            MouserService mouserService,
            DigiKeyService digiKeyService,
            LCSCService lcscService,
            ExcelExportService excelExportService,
            IMemoryCache memoryCache)
        {
            _mouser = mouserService;
            _digiKey = digiKeyService;
            _lcsc = lcscService;
            _excelExport = excelExportService;
            _cache = memoryCache;
        }

        // GET /Sourcing/BulkUpload
        [HttpGet]
        public IActionResult BulkUpload()
        {
            // Only a small key lives in TempData — actual data is in IMemoryCache
            if (TempData["ResultKey"] is string resultKey)
            {
                if (_cache.TryGetValue(resultKey, out List<BulkResultGroup> groups))
                    ViewBag.Groups = groups;

                if (_cache.TryGetValue(resultKey + "_file", out string fileName))
                    ViewBag.FileName = fileName;

                ViewBag.ResultKey = resultKey;

                // Keep the key alive so DownloadReport can still use it
                TempData.Keep("ResultKey");
            }

            return View();
        }

        // POST /Sourcing/BulkUpload
        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> BulkUpload(IFormFile uploadedFile, CancellationToken ct)
        {
            // ── 1. Validate ──────────────────────────────────────────────────────
            if (uploadedFile is null || uploadedFile.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Please upload a valid .xlsx file.");
                return View();
            }

            if (!Path.GetExtension(uploadedFile.FileName)
                    .Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Only .xlsx files are supported.");
                return View();
            }

            // ── 2. Parse Column A ────────────────────────────────────────────────
            List<string> descriptions;
            try
            {
                descriptions = ParseDescriptions(uploadedFile);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Could not read the file: {ex.Message}");
                return View();
            }

            if (descriptions.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "No component descriptions found in Column A.");
                return View();
            }

            // ── 3. Query all distributors ────────────────────────────────────────
            var groups = await FetchAllGroupsAsync(descriptions, ct);

            // ── 4. Generate Excel ────────────────────────────────────────────────
            var excelBytes = _excelExport.GenerateReport(groups);
            var fileName = $"BulkSourcing_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            // ── 5. Store everything server-side in IMemoryCache (expires in 30 min)
            var cacheKey = Guid.NewGuid().ToString("N");
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

            _cache.Set(cacheKey, groups, cacheOptions); // result groups
            _cache.Set(cacheKey + "_excel", excelBytes, cacheOptions); // raw Excel bytes
            _cache.Set(cacheKey + "_file", fileName, cacheOptions); // file name

            // Only the tiny key goes in TempData (cookie-safe — just 32 chars)
            TempData["ResultKey"] = cacheKey;

            return RedirectToAction("BulkUpload");
        }

        // GET /Sourcing/DownloadReport
        [HttpGet]
        public IActionResult DownloadReport(string key)
        {
            if (string.IsNullOrWhiteSpace(key) ||
                !_cache.TryGetValue(key + "_excel", out byte[] excelBytes))
                return RedirectToAction("BulkUpload");

            var fileName = _cache.TryGetValue(key + "_file", out string fn)
                ? fn : "BulkSourcing.xlsx";

            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // ── Private Helpers ──────────────────────────────────────────────────────

        private static List<string> ParseDescriptions(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);

            var sheet = package.Workbook.Worksheets[0];
            var items = new List<string>();

            if (sheet.Dimension == null) return items;

            for (int row = 1; row <= sheet.Dimension.End.Row; row++)
            {
                var value = sheet.Cells[row, 1].Text?.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                    items.Add(value);
            }

            return items;
        }

        private async Task<List<BulkResultGroup>> FetchAllGroupsAsync(
            List<string> descriptions, CancellationToken ct)
        {
            using var throttle = new SemaphoreSlim(5, 5);

            var tasks = descriptions.Select(async desc =>
            {
                await throttle.WaitAsync(ct);
                try { return await FetchSingleGroupAsync(desc); }
                finally { throttle.Release(); }
            });

            return (await Task.WhenAll(tasks)).ToList();
        }

        private async Task<BulkResultGroup> FetchSingleGroupAsync(string description)
        {
            var group = new BulkResultGroup { RequestedDescription = description };

            var mouserTask = SafeFetch(() => _mouser.SearchByDescription(description, TopResultsPerDistributor), "Mouser", group);
            var digiKeyTask = SafeFetch(() => _digiKey.SearchByDescription(description, TopResultsPerDistributor), "DigiKey", group);
            var lcscTask = SafeFetchLcsc(description, group);

            await Task.WhenAll(mouserTask, digiKeyTask, lcscTask);

            group.Results.AddRange((mouserTask.Result ?? Enumerable.Empty<PartDetails>()).Take(TopResultsPerDistributor));
            group.Results.AddRange((digiKeyTask.Result ?? Enumerable.Empty<PartDetails>()).Take(TopResultsPerDistributor));
            group.Results.AddRange((lcscTask.Result ?? Enumerable.Empty<PartDetails>()).Take(TopResultsPerDistributor));

            return group;
        }

        private static async Task<IEnumerable<PartDetails>?> SafeFetch(
            Func<Task<List<PartDetails>>> fetch,
            string distributor,
            BulkResultGroup group)
        {
            try { return await fetch(); }
            catch (Exception ex)
            {
                group.Errors[distributor] = ex.Message;
                return null;
            }
        }

        private async Task<IEnumerable<PartDetails>?> SafeFetchLcsc(
            string description,
            BulkResultGroup group)
        {
            try
            {
                var lcscResults = await _lcsc.SearchByKeyword(
                    description, TopResultsPerDistributor);

                return lcscResults.Select(r => new PartDetails
                {
                    Mpn = r.MpnNumber,
                    Manufacturer = r.Manufacturer,
                    Description = r.Description,
                    Price = r.Price,
                    Stock = r.Stock,
                    ProductUrl = r.ProductUrl,
                    DatasheetUrl = r.DatasheetUrl,
                    Category = r.Category,
                    Source = "LCSC"
                });
            }
            catch (Exception ex)
            {
                group.Errors["LCSC"] = ex.Message;
                return null;
            }
        }
    }
}
