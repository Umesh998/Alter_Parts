using Alter_Parts.Models;
using Alter_Parts.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alter_Parts.Controllers
{
    public class ComponentController : Controller
    {
        private readonly PartLookupService _lookupService;
        private readonly Data.DB _ctx;

        // 💡 Nexar commented out
        // private readonly NexarService _nexar;

        public ComponentController(PartLookupService lookupService,
                                   Data.DB ctx)
        {
            _lookupService = lookupService;
            _ctx = ctx;
            // _nexar = nexar;
        }

        // ── SEARCH ───────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Search(string mpn)
        {
            if (string.IsNullOrEmpty(mpn))
                return View(new Search());

            try
            {
                // ✅ Uses DigiKey → Mouser → LCSC
                var details = await _lookupService
                    .GetPartDetails(mpn);

                if (details == null ||
                    details.Source == "Not Found")
                {
                    ViewBag.Error =
                        "No results found for this part.";
                    return View(new Search());
                }

                var model = new Search
                {
                    Mpn = details.Mpn ?? "N/A",
                    Description = details.Description
                                   ?? "No description",
                    Manufacturer = details.Manufacturer
                                   ?? "Unknown"
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Data format error: " + ex.Message;
                return View(new Search());
            }
        }

        // ── RUN COMPARISON ────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> RunComparison(int id)
        {
            var part = await _ctx.More_Fruits.FindAsync(id);
            if (part == null) return NotFound();

            try
            {
                // ✅ Uses DigiKey → Mouser → LCSC
                var origDetails = await _lookupService
                    .GetPartDetails(part.Original_Part_Number);
                var alterDetails = await _lookupService
                    .GetPartDetails(part.Alter_Part_Number);

                part.Final_Result = _lookupService
                    .ComparePartDetails(origDetails, alterDetails);

                // 💡 Nexar commented out
                // var jsonOrig  = await _nexar
                //     .GetPartData(part.Original_Part_Number);
                // var jsonAlter = await _nexar
                //     .GetPartData(part.Alter_Part_Number);
                // part.Final_Result = PerformDetailedComparison(
                //     jsonOrig, jsonAlter);
            }
            catch (Exception ex)
            {
                part.Final_Result = $"Error: {ex.Message}";
            }

            part.Status = "Reviewed";
            part.LastChecked = DateTime.Now;

            await _ctx.SaveChangesAsync();
            TempData["Notify"] =
                $"Comparison complete: {part.Final_Result}";
            return RedirectToAction(nameof(More_Fruits));
        }

        // 💡 Nexar-based comparison kept for future use
        // private string PerformDetailedComparison(
        //     string json1, string json2)
        // {
        //     try
        //     {
        //         using var doc1 = JsonDocument.Parse(json1);
        //         using var doc2 = JsonDocument.Parse(json2);
        //         var data1 = doc1.RootElement.GetProperty("data");
        //         var data2 = doc2.RootElement.GetProperty("data");
        //
        //         if (!data1.TryGetProperty("supSearchMpn",
        //             out var search1) ||
        //             search1.ValueKind == JsonValueKind.Null ||
        //             !data2.TryGetProperty("supSearchMpn",
        //             out var search2) ||
        //             search2.ValueKind == JsonValueKind.Null)
        //             return "Error: API Response empty";
        //
        //         var results1 = search1.GetProperty("results");
        //         var results2 = search2.GetProperty("results");
        //
        //         if (results1.ValueKind == JsonValueKind.Null ||
        //             results1.GetArrayLength() == 0 ||
        //             results2.ValueKind == JsonValueKind.Null ||
        //             results2.GetArrayLength() == 0)
        //             return "Error: Part Not Found in API";
        //
        //         var p1 = results1[0].GetProperty("part");
        //         var p2 = results2[0].GetProperty("part");
        //
        //         static string GetSpec(JsonElement part,
        //             string shortname)
        //         {
        //             if (!part.TryGetProperty("specs", out var specs)
        //                 || specs.ValueKind == JsonValueKind.Null)
        //                 return null;
        //             foreach (var spec in specs.EnumerateArray())
        //             {
        //                 if (!spec.TryGetProperty(
        //                     "attribute", out var attr)) continue;
        //                 if (!attr.TryGetProperty(
        //                     "shortname", out var sn)) continue;
        //                 if (sn.GetString()?.ToLower() ==
        //                     shortname.ToLower())
        //                     return spec.TryGetProperty(
        //                         "displayValue", out var val)
        //                         ? val.GetString() : null;
        //             }
        //             return null;
        //         }
        //
        //         static string GetField(JsonElement part,
        //             string field) =>
        //             part.TryGetProperty(field, out var val) &&
        //             val.ValueKind != JsonValueKind.Null
        //             ? val.GetString() ?? "" : "";
        //
        //         string man1 = p1.TryGetProperty(
        //             "manufacturer", out var m1) &&
        //             m1.ValueKind != JsonValueKind.Null
        //             ? m1.GetProperty("name").GetString()
        //             : "Unknown";
        //         string man2 = p2.TryGetProperty(
        //             "manufacturer", out var m2) &&
        //             m2.ValueKind != JsonValueKind.Null
        //             ? m2.GetProperty("name").GetString()
        //             : "Unknown";
        //         string cat1 = p1.TryGetProperty(
        //             "category", out var c1) &&
        //             c1.ValueKind != JsonValueKind.Null
        //             ? c1.GetProperty("name").GetString() : "";
        //         string cat2 = p2.TryGetProperty(
        //             "category", out var c2) &&
        //             c2.ValueKind != JsonValueKind.Null
        //             ? c2.GetProperty("name").GetString() : "";
        //         string desc1 = GetField(p1,
        //             "shortDescription").ToLower();
        //         string desc2 = GetField(p2,
        //             "shortDescription").ToLower();
        //         string pkg1  = GetSpec(p1, "case_package");
        //         string pkg2  = GetSpec(p2, "case_package");
        //         string voltage1 = GetSpec(p1, "supply_voltage");
        //         string voltage2 = GetSpec(p2, "supply_voltage");
        //         string current1 = GetSpec(p1,
        //             "continuous_collector_current");
        //         string current2 = GetSpec(p2,
        //             "continuous_collector_current");
        //         string temp1 = GetSpec(p1,
        //             "operating_temperature");
        //         string temp2 = GetSpec(p2,
        //             "operating_temperature");
        //         string pins1 = GetSpec(p1, "number_of_pins");
        //         string pins2 = GetSpec(p2, "number_of_pins");
        //
        //         var mismatches = new List<string>();
        //         var warnings   = new List<string>();
        //         var matches    = new List<string>();
        //
        //         if (!string.IsNullOrEmpty(cat1) &&
        //             !string.IsNullOrEmpty(cat2) && cat1 != cat2)
        //             mismatches.Add($"Category: {cat1} vs {cat2}");
        //
        //         if (!string.IsNullOrEmpty(pkg1) &&
        //             !string.IsNullOrEmpty(pkg2))
        //         {
        //             if (pkg1.ToLower() == pkg2.ToLower())
        //                 matches.Add($"Package: {pkg1}");
        //             else
        //                 mismatches.Add(
        //                     $"Package: {pkg1} vs {pkg2}");
        //         }
        //
        //         if (!string.IsNullOrEmpty(pins1) &&
        //             !string.IsNullOrEmpty(pins2))
        //         {
        //             if (pins1 == pins2)
        //                 matches.Add($"Pins: {pins1}");
        //             else
        //                 mismatches.Add(
        //                     $"Pin Count: {pins1} vs {pins2}");
        //         }
        //
        //         if (!string.IsNullOrEmpty(voltage1) &&
        //             !string.IsNullOrEmpty(voltage2))
        //         {
        //             if (voltage1 == voltage2)
        //                 matches.Add($"Voltage: {voltage1}");
        //             else
        //                 warnings.Add(
        //                     $"Voltage: {voltage1} vs {voltage2}");
        //         }
        //
        //         if (!string.IsNullOrEmpty(current1) &&
        //             !string.IsNullOrEmpty(current2))
        //         {
        //             if (current1 == current2)
        //                 matches.Add($"Current: {current1}");
        //             else
        //                 warnings.Add(
        //                     $"Current: {current1} vs {current2}");
        //         }
        //
        //         if (!string.IsNullOrEmpty(temp1) &&
        //             !string.IsNullOrEmpty(temp2))
        //         {
        //             if (temp1 == temp2)
        //                 matches.Add($"Temp: {temp1}");
        //             else
        //                 warnings.Add(
        //                     $"Temp Range: {temp1} vs {temp2}");
        //         }
        //
        //         string manNote = man1 == man2 ? ""
        //             : $" [Diff Manufacturer: {man2}]";
        //
        //         if (mismatches.Count > 0)
        //             return $"❌ Not Compatible - " +
        //                    $"{string.Join(", ", mismatches)}" +
        //                    $"{manNote}";
        //         if (warnings.Count > 0 && matches.Count == 0)
        //             return $"⚠️ Check Manually - " +
        //                    $"{string.Join(", ", warnings)}" +
        //                    $"{manNote}";
        //         if (warnings.Count > 0)
        //             return $"⚠️ Likely Compatible (verify: " +
        //                    $"{string.Join(", ", warnings)})" +
        //                    $"{manNote}";
        //         if (matches.Count > 0)
        //             return $"✅ Compatible - " +
        //                    $"{string.Join(", ", matches)}" +
        //                    $"{manNote}";
        //         if (desc1.Contains(desc2) ||
        //             desc2.Contains(desc1))
        //             return $"✅ Compatible (description match)" +
        //                    $"{manNote}";
        //         return $"⚠️ Check Manually (no specs){manNote}";
        //     }
        //     catch (Exception ex)
        //     {
        //         return "Logic Error: " + ex.Message;
        //     }
        // }

        // ── INVENTORY CRUD ────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> More_Fruits(
            string term = "", string orderBy = "",
            int currentPage = 1)
        {
            var vm = new Part_View_Model
            {
                Term = term,
                PageSize = 5,
                CurrentPage = currentPage
            };

            IQueryable<Alter> query =
                _ctx.More_Fruits.AsQueryable();

            if (!string.IsNullOrEmpty(term))
                query = query.Where(f =>
                    f.Original_Part_Number.Contains(term) ||
                    f.Alter_Part_Number.Contains(term));

            vm.TotalPages = (int)System.Math.Ceiling(
                (double)await query.CountAsync() / vm.PageSize);
            vm.Fruits = await query
                .Skip((currentPage - 1) * vm.PageSize)
                .Take(vm.PageSize)
                .ToListAsync();

            return View(vm);
        }

        [HttpGet]
        public IActionResult Create() => View(new Alter());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Alter xData)
        {
            if (!ModelState.IsValid) return View(xData);

            xData.Status = "Pending";
            xData.LastChecked = DateTime.Now;
            xData.Final_Result = "Pending";

            _ctx.More_Fruits.Add(xData);
            await _ctx.SaveChangesAsync();
            return RedirectToAction("More_Fruits", "Component");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
            => View(await _ctx.More_Fruits.FindAsync(id));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Alter obj)
        {
            if (!ModelState.IsValid) return View(obj);
            obj.LastChecked = DateTime.Now;
            _ctx.More_Fruits.Update(obj);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(More_Fruits));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var obj = await _ctx.More_Fruits.FindAsync(id);
            if (obj == null) return NotFound();
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            var obj = await _ctx.More_Fruits.FindAsync(id);
            if (obj != null)
            {
                _ctx.More_Fruits.Remove(obj);
                await _ctx.SaveChangesAsync();
            }
            return RedirectToAction(nameof(More_Fruits));
        }
    }
}