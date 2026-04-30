using Alter_Parts.Models;
using Alter_Parts.Services;
using Microsoft.AspNetCore.Mvc;

namespace Alter_Parts.Controllers
{
    public class LCSCController : Controller
    {
        private readonly LCSCService _lcsc;

        public LCSCController(LCSCService lcsc)
            => _lcsc = lcsc;

        // GET: /LCSC
        [HttpGet]
        public IActionResult Index()
            => View(new LCSCSearchViewModel());

        // POST: /LCSC/Search
        [HttpPost]
        public async Task<IActionResult> Search(
            LCSCSearchRequest request)
        {
            var vm = new LCSCSearchViewModel
            {
                Keyword = request.Keyword
            };

            if (string.IsNullOrWhiteSpace(request.Keyword))
            {
                vm.Error = "Please enter a description to search.";
                return View("Index", vm);
            }

            try
            {
                vm.Results = await _lcsc.SearchByKeyword(
                    request.Keyword, request.Limit);
                vm.TotalFound = vm.Results.Count;

                if (!vm.Results.Any())
                    vm.Error =
                        "No parts found for this description. " +
                        "Try different keywords.";
            }
            catch (Exception ex)
            {
                vm.Error = $"Search failed: {ex.Message}";
            }

            return View("Index", vm);
        }

        // GET: /LCSC/Debug?keyword=capacitor
        // Temporary debug action to see raw response
        [HttpGet]
        public async Task<IActionResult> Debug(string keyword)
        {
            try
            {
                var results = await _lcsc
                    .SearchByKeyword(keyword ?? "capacitor", 3);
                return Json(results);
            }
            catch (Exception ex)
            {
                return Content($"ERROR: {ex.Message}");
            }
        }
    }
}