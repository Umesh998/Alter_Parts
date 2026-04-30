using Alter_Parts.Models;
using Alter_Parts.Services;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Text.Json;

namespace Alter_Parts.Controllers
{
    public class BomController : Controller
    {
        private readonly PartLookupService _lookupService;

        private static readonly string[] AlternateKeywords = new[]
  {
        "alternate", "alt", "alternative", "alternate suggested",
        "alt suggested", "use alternate", "replace", "substitute"
    };
        public BomController(PartLookupService lookupService)
        {
            _lookupService = lookupService;
            ExcelPackage.License.SetNonCommercialPersonal("Alter_Parts");
        }
        //public BomController(NexarService nexarService)
        //{
        //    _nexarService = nexarService;
        //    // ✅ NEW - EPPlus 8 and above
        //    ExcelPackage.License.SetNonCommercialPersonal("Alter_Parts");
        //}

        //GET: /Bom/Upload
        [HttpGet]
        public IActionResult Upload() => View();

        // POST: /Bom/Upload
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile bomFile)
        {
            if (bomFile == null || bomFile.Length == 0)
            {
                ViewBag.Error = "Please select a valid Excel file.";
                return View();
            }

            var extension = Path.GetExtension(bomFile.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls")
            {
                ViewBag.Error = "Only .xlsx or .xls files are supported.";
                return View();
            }

            try
            {
                using var stream = new MemoryStream();
                await bomFile.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);
                var sheet = package.Workbook.Worksheets[0]; // First sheet

                if (sheet.Dimension == null)
                {
                    ViewBag.Error = "The Excel sheet is empty.";
                    return View();
                }

                // --- Detect column positions from header row ---
                var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int col = 1; col <= sheet.Dimension.End.Column; col++)
                {
                    var header = sheet.Cells[1, col].Text?.Trim();
                    if (!string.IsNullOrEmpty(header))
                        headers[header] = col;
                }

                // --- Map columns (flexible naming) ---
                int colOriginal = FindColumn(headers, "original part", "original", "mpn", "part number", "part no", "bom part");
                int colAlternate = FindColumn(headers, "alternate part", "alternate", "alt part", "alternative", "replacement");
                int colRemark = FindColumn(headers, "remark", "remarks", "note", "notes", "comment", "comments");
                int colDesc = FindColumn(headers, "description", "desc", "part description");
                int colMfr = FindColumn(headers, "manufacturer", "mfr", "make", "brand");

                // ✅ NEW - colRemark is optional
                if (colOriginal == -1 || colAlternate == -1)
                {
                    ViewBag.Error = $"Could not find required columns. Found: {string.Join(", ", headers.Keys)}. " +
                                    $"Need at minimum: Original Part, Alternate Part columns.";
                    return View();
                }

                // --- Read rows where Remark = alternate keyword ---
                var result = new BomUploadResult
                {
                    FileName = bomFile.FileName,
                    TotalRows = sheet.Dimension.End.Row - 1
                };

                var rowsToProcess = new List<BomRow>();

                for (int row = 2; row <= sheet.Dimension.End.Row; row++)
                {
                    var remark = colRemark > 0 ? sheet.Cells[row, colRemark].Text?.Trim() ?? "" : "";
                    var origPart = sheet.Cells[row, colOriginal].Text?.Trim() ?? "";
                    var altPart = sheet.Cells[row, colAlternate].Text?.Trim() ?? "";

                    // Skip if original or alternate is empty
                    if (string.IsNullOrWhiteSpace(origPart) || string.IsNullOrWhiteSpace(altPart))
                        continue;

                    // ✅ NEW - if both Original and Alternate are filled, that IS the alternate suggestion
                    // Remark column is optional — if empty, still process the row
                    bool isAlternate =
                        // Either remark contains a keyword
                        AlternateKeywords.Any(k => remark.Contains(k, StringComparison.OrdinalIgnoreCase))
                        // OR remark is empty but both parts are filled (your case)
                        || string.IsNullOrWhiteSpace(remark);

                    if (!isAlternate) continue;

                    rowsToProcess.Add(new BomRow
                    {
                        RowNumber = row,
                        OriginalPart = origPart,
                        AlternatePart = altPart,
                        Remark = remark,
                        Description = colDesc > 0 ? sheet.Cells[row, colDesc].Text?.Trim() : "",
                        Manufacturer = colMfr > 0 ? sheet.Cells[row, colMfr].Text?.Trim() : "",
                        Status = "Pending"
                    });
                }

                if (rowsToProcess.Count == 0)
                {
                    ViewBag.Error = "No rows found with alternate suggestions in the Remark column. " +
                                    "Make sure remark contains words like 'Alternate', 'Alt', 'Substitute' etc.";
                    return View();
                }

                // --- Run Nexar comparison for each row ---
                foreach (var bomRow in rowsToProcess)
                {
                    try
                    {
                        var origDetails = await _lookupService.GetPartDetails(bomRow.OriginalPart);
                        var alterDetails = await _lookupService.GetPartDetails(bomRow.AlternatePart);
                        bomRow.ComparisonResult = _lookupService.ComparePartDetails(origDetails, alterDetails);
                        bomRow.Status = "Done";
                    }
                    catch (Exception ex)
                    {
                        bomRow.ComparisonResult = $"API Error: {ex.Message}";
                        bomRow.Status = "Error";
                    }
                }
                // --- Count results ---
                result.Rows = rowsToProcess;
                result.ProcessedRows = rowsToProcess.Count(r => r.Status == "Done");
                result.CompatibleCount = rowsToProcess.Count(r => r.ComparisonResult?.StartsWith("✅") == true);
                result.IncompatibleCount = rowsToProcess.Count(r => r.ComparisonResult?.StartsWith("❌") == true);
                result.ManualCheckCount = rowsToProcess.Count(r => r.ComparisonResult?.StartsWith("⚠️") == true);

                return View("Results", result);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Failed to process file: " + ex.Message;
                return View();
            }
        }

        // POST: /Bom/ExportResults — download results as Excel
        [HttpPost]
        public IActionResult ExportResults(string resultsJson)
        {
            var rows = JsonSerializer.Deserialize<List<BomRow>>(resultsJson);
            if (rows == null || rows.Count == 0)
                return BadRequest("No data to export.");

            // Use the EPPlus 8+ API to set license info (avoid obsolete LicenseContext)
            ExcelPackage.License.SetNonCommercialPersonal("Alter_Parts");

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("BOM Comparison Results");

            // Header row
            string[] headers = { "Row #", "Original Part", "Alternate Part", "Remark", "Description", "Manufacturer", "Comparison Result", "Status" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[1, i + 1].Value = headers[i];
                sheet.Cells[1, i + 1].Style.Font.Bold = true;
                sheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(68, 114, 196));
                sheet.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            // Data rows
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                int excelRow = i + 2;

                sheet.Cells[excelRow, 1].Value = row.RowNumber;
                sheet.Cells[excelRow, 2].Value = row.OriginalPart;
                sheet.Cells[excelRow, 3].Value = row.AlternatePart;
                sheet.Cells[excelRow, 4].Value = row.Remark;
                sheet.Cells[excelRow, 5].Value = row.Description;
                sheet.Cells[excelRow, 6].Value = row.Manufacturer;
                sheet.Cells[excelRow, 7].Value = row.ComparisonResult;
                sheet.Cells[excelRow, 8].Value = row.Status;

                // Color code result column
                var color = row.ComparisonResult?.StartsWith("✅") == true
                    ? System.Drawing.Color.FromArgb(198, 239, 206)   // Green
                    : row.ComparisonResult?.StartsWith("❌") == true
                    ? System.Drawing.Color.FromArgb(255, 199, 206)   // Red
                    : System.Drawing.Color.FromArgb(255, 235, 156);  // Yellow

                sheet.Cells[excelRow, 7].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[excelRow, 7].Style.Fill.BackgroundColor.SetColor(color);
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

            var fileBytes = package.GetAsByteArray();
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"BOM_Comparison_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // --- Helper: find column by multiple possible names ---
        private static int FindColumn(Dictionary<string, int> headers, params string[] possibleNames)
        {
            foreach (var name in possibleNames)
                if (headers.TryGetValue(name, out int col))
                    return col;
            return -1;
        }

        // --- Same comparison logic from ComponentController ---
        private string PerformDetailedComparison(string json1, string json2)
        {
            try
            {
                using var doc1 = JsonDocument.Parse(json1);
                using var doc2 = JsonDocument.Parse(json2);

                var data1 = doc1.RootElement.GetProperty("data");
                var data2 = doc2.RootElement.GetProperty("data");

                if (!data1.TryGetProperty("supSearchMpn", out var s1) || s1.ValueKind == JsonValueKind.Null ||
                    !data2.TryGetProperty("supSearchMpn", out var s2) || s2.ValueKind == JsonValueKind.Null)
                    return "⚠️ Error: Empty API response";

                var r1 = s1.GetProperty("results");
                var r2 = s2.GetProperty("results");

                if (r1.ValueKind == JsonValueKind.Null || r1.GetArrayLength() == 0 ||
                    r2.ValueKind == JsonValueKind.Null || r2.GetArrayLength() == 0)
                    return "⚠️ Part not found in API";

                var p1 = r1[0].GetProperty("part");
                var p2 = r2[0].GetProperty("part");

                static string GetSpec(JsonElement part, string shortname)
                {
                    if (!part.TryGetProperty("specs", out var specs) || specs.ValueKind == JsonValueKind.Null)
                        return null;
                    foreach (var spec in specs.EnumerateArray())
                    {
                        if (!spec.TryGetProperty("attribute", out var attr)) continue;
                        if (!attr.TryGetProperty("shortname", out var sn)) continue;
                        if (sn.GetString()?.ToLower() == shortname.ToLower())
                            return spec.TryGetProperty("displayValue", out var val) ? val.GetString() : null;
                    }
                    return null;
                }

                string man1 = p1.TryGetProperty("manufacturer", out var m1) && m1.ValueKind != JsonValueKind.Null ? m1.GetProperty("name").GetString() : "Unknown";
                string man2 = p2.TryGetProperty("manufacturer", out var m2) && m2.ValueKind != JsonValueKind.Null ? m2.GetProperty("name").GetString() : "Unknown";
                string cat1 = p1.TryGetProperty("category", out var c1) && c1.ValueKind != JsonValueKind.Null ? c1.GetProperty("name").GetString() : "";
                string cat2 = p2.TryGetProperty("category", out var c2) && c2.ValueKind != JsonValueKind.Null ? c2.GetProperty("name").GetString() : "";

                string pkg1 = GetSpec(p1, "case_package");
                string pkg2 = GetSpec(p2, "case_package");
                string voltage1 = GetSpec(p1, "supply_voltage");
                string voltage2 = GetSpec(p2, "supply_voltage");
                string pins1 = GetSpec(p1, "number_of_pins");
                string pins2 = GetSpec(p2, "number_of_pins");
                string temp1 = GetSpec(p1, "operating_temperature");
                string temp2 = GetSpec(p2, "operating_temperature");

                var mismatches = new List<string>();
                var warnings = new List<string>();
                var matches = new List<string>();

                if (!string.IsNullOrEmpty(cat1) && !string.IsNullOrEmpty(cat2) && cat1 != cat2)
                    mismatches.Add($"Category: {cat1} vs {cat2}");

                if (!string.IsNullOrEmpty(pkg1) && !string.IsNullOrEmpty(pkg2))
                {
                    if (pkg1.ToLower() == pkg2.ToLower()) matches.Add($"Package: {pkg1}");
                    else mismatches.Add($"Package: {pkg1} vs {pkg2}");
                }

                if (!string.IsNullOrEmpty(pins1) && !string.IsNullOrEmpty(pins2))
                {
                    if (pins1 == pins2) matches.Add($"Pins: {pins1}");
                    else mismatches.Add($"Pins: {pins1} vs {pins2}");
                }

                if (!string.IsNullOrEmpty(voltage1) && !string.IsNullOrEmpty(voltage2))
                {
                    if (voltage1 == voltage2) matches.Add($"Voltage: {voltage1}");
                    else warnings.Add($"Voltage: {voltage1} vs {voltage2}");
                }

                if (!string.IsNullOrEmpty(temp1) && !string.IsNullOrEmpty(temp2))
                {
                    if (temp1 == temp2) matches.Add($"Temp: {temp1}");
                    else warnings.Add($"Temp: {temp1} vs {temp2}");
                }

                string manNote = man1 != man2 ? $" [Diff Mfr: {man2}]" : "";

                if (mismatches.Count > 0) return $"❌ Not Compatible - {string.Join(", ", mismatches)}{manNote}";
                if (warnings.Count > 0 && matches.Count == 0) return $"⚠️ Check Manually - {string.Join(", ", warnings)}{manNote}";
                if (warnings.Count > 0) return $"⚠️ Likely Compatible (verify: {string.Join(", ", warnings)}){manNote}";
                if (matches.Count > 0) return $"✅ Compatible - {string.Join(", ", matches)}{manNote}";

                return $"⚠️ Check Manually (no specs available){manNote}";
            }
            catch (Exception ex)
            {
                return "⚠️ Logic Error: " + ex.Message;
            }
        }
    }
}
