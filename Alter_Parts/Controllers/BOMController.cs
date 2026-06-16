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

        // GET: /Bom/Upload
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
                var sheet = package.Workbook.Worksheets[0];

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

                if (colOriginal == -1 || colAlternate == -1)
                {
                    ViewBag.Error = $"Could not find required columns. Found: {string.Join(", ", headers.Keys)}. " +
                                    $"Need at minimum: Original Part, Alternate Part columns.";
                    return View();
                }

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

                    if (string.IsNullOrWhiteSpace(origPart) || string.IsNullOrWhiteSpace(altPart))
                        continue;

                    bool isAlternate =
                        AlternateKeywords.Any(k => remark.Contains(k, StringComparison.OrdinalIgnoreCase))
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
                    ViewBag.Error = "No rows found with alternate suggestions. " +
                                    "Make sure both Original Part and Alternate Part columns are filled.";
                    return View();
                }

                // --- 2-Step comparison for each row ---
                foreach (var bomRow in rowsToProcess)
                {
                    try
                    {
                        // Fetch both parts in parallel
                        var origTask = _lookupService.GetPartDetails(bomRow.OriginalPart);
                        var alterTask = _lookupService.GetPartDetails(bomRow.AlternatePart);
                        await Task.WhenAll(origTask, alterTask);

                        var origDetails = await origTask;
                        var alterDetails = await alterTask;

                        // Store source info
                        bomRow.Source = $"{origDetails.Source}/{alterDetails.Source}";

                        // ✅ STEP 1 — Excel description vs online fetched description
                        bomRow.OriginalDescription = origDetails.Description ?? "";
                        var (verdict, score) = _lookupService.GetMatchVerdict(
                            bomRow.Description, origDetails.Description);
                        bomRow.OverallVerdict = verdict;
                        bomRow.BestMatchPercent = $"{score:0}%";

                        // ✅ STEP 2 — Original Part vs Alternate Part specs
                        bomRow.ComparisonResult = _lookupService
                            .ComparePartDetails(origDetails, alterDetails);

                        bomRow.Status = "Done";
                    }
                    catch (Exception ex)
                    {
                        bomRow.OverallVerdict = $"API Error: {ex.Message}";
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

        // POST: /Bom/ExportResults
        [HttpPost]
        public IActionResult ExportResults(string resultsJson)
        {
            var rows = JsonSerializer.Deserialize<List<BomRow>>(resultsJson);
            if (rows == null || rows.Count == 0)
                return BadRequest("No data to export.");

            ExcelPackage.License.SetNonCommercialPersonal("Alter_Parts");

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("BOM Results");

            // --- Header row ---
            string[] headers = {
                "Row #", "Part Number", "Alternate Part",
                "Your Description", "Best Match %", "Overall Verdict",
                "Online Description (Orig)",
                "Step 2: Alt Match Result", "Status"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[1, i + 1].Value = headers[i];
                sheet.Cells[1, i + 1].Style.Font.Bold = true;
                sheet.Cells[1, i + 1].Style.Fill.PatternType =
                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[1, i + 1].Style.Fill.BackgroundColor
                    .SetColor(System.Drawing.Color.FromArgb(28, 57, 107));
                sheet.Cells[1, i + 1].Style.Font.Color
                    .SetColor(System.Drawing.Color.White);
            }

            // --- Data rows ---
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                int excelRow = i + 2;

                sheet.Cells[excelRow, 1].Value = row.RowNumber;
                sheet.Cells[excelRow, 2].Value = row.OriginalPart;
                sheet.Cells[excelRow, 3].Value = row.AlternatePart;
                sheet.Cells[excelRow, 4].Value = row.Description;
                sheet.Cells[excelRow, 5].Value = row.BestMatchPercent;
                sheet.Cells[excelRow, 6].Value = row.OverallVerdict;
                sheet.Cells[excelRow, 7].Value = row.OriginalDescription;
                sheet.Cells[excelRow, 8].Value = row.ComparisonResult;
                sheet.Cells[excelRow, 9].Value = row.Status;

                // Color: Overall Verdict (col 6) — Step 1
                var c1 = row.OverallVerdict?.StartsWith("✅") == true
                    ? System.Drawing.Color.FromArgb(198, 239, 206)
                    : row.OverallVerdict?.StartsWith("❌") == true
                    ? System.Drawing.Color.FromArgb(255, 199, 206)
                    : System.Drawing.Color.FromArgb(255, 235, 156);
                sheet.Cells[excelRow, 6].Style.Fill.PatternType =
                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[excelRow, 6].Style.Fill.BackgroundColor.SetColor(c1);

                // Color: Alt Match Result (col 8) — Step 2
                var c2 = row.ComparisonResult?.StartsWith("✅") == true
                    ? System.Drawing.Color.FromArgb(198, 239, 206)
                    : row.ComparisonResult?.StartsWith("❌") == true
                    ? System.Drawing.Color.FromArgb(255, 199, 206)
                    : System.Drawing.Color.FromArgb(255, 235, 156);
                sheet.Cells[excelRow, 8].Style.Fill.PatternType =
                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[excelRow, 8].Style.Fill.BackgroundColor.SetColor(c2);
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

            var fileBytes = package.GetAsByteArray();
            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"BOM_Results_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // --- Helper: find column by multiple possible names ---
        private static int FindColumn(Dictionary<string, int> headers, params string[] possibleNames)
        {
            foreach (var name in possibleNames)
                if (headers.TryGetValue(name, out int col))
                    return col;
            return -1;
        }
    }
}