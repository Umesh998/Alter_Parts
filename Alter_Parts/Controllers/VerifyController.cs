//using Alter_Parts.Models;
//using Microsoft.AspNetCore.Mvc;
//using OfficeOpenXml;
//using System.Text.Json;
//using Vinrox_Tools.Services;

//namespace Alter_Parts.Controllers
//{
//    public class VerifyController : Controller
//    {
//        private readonly NexarService _nexar;
//        //private readonly DigiKeyService _digikey;
//        //private readonly MouserService _mouser;
//        //private readonly LCSCService _lcsc;

//        public VerifyController(NexarService nexar /*DigiKeyService digikey, MouserService mouser, LCSCService lcsc*/)
//        {
//            _nexar = nexar;
//            //_digikey = digikey;
//            //_mouser = mouser;
//            //_lcsc = lcsc;
//        }

//        // GET: /Verify
//        [HttpGet]
//        public IActionResult Index() => View(new VerifyRequest());

//        // POST: /Verify
//        [HttpPost]
//        public async Task<IActionResult> Index(VerifyRequest request)
//        {
//            if (!ModelState.IsValid) return View(request);

//            var result = new VerifyResult
//            {
//                PartNumber = request.PartNumber.Trim(),
//                UserDescription = request.Description.Trim()
//            };

//            // Fetch from DigiKey
//            //try
//            //{
//            //    var dkData = await _digikey.GetPartDetails(request.PartNumber);
//            //    if (dkData != null)
//            //    {
//            //        result.DigiKeyResult = new VerifySource
//            //        {
//            //            Source = "DigiKey",
//            //            FetchedDescription = dkData.Description,
//            //            Manufacturer = dkData.Manufacturer,
//            //            Category = dkData.Category,
//            //            Package = dkData.Specs.TryGetValue("Package / Case", out var pkg) ? pkg :
//            //                              dkData.Specs.TryGetValue("Supplier Device Package", out var spkg) ? spkg : "N/A",
//            //            DatasheetUrl = dkData.DatasheetUrl,
//            //            ProductUrl = dkData.ProductUrl,
//            //            Stock = dkData.Stock,
//            //            Specs = dkData.Specs,
//            //            MatchVerdict = GetMatchVerdict(request.Description, dkData.Description)
//            //        };
//            //    }
//            //    else
//            //    {
//            //        result.DigiKeyResult = new VerifySource
//            //        {
//            //            Source = "DigiKey",
//            //            MatchVerdict = "❌ Part not found on DigiKey"
//            //        };
//            //    }
//            //}
//            //catch (Exception ex)
//            //{
//            //    result.DigiKeyResult = new VerifySource
//            //    {
//            //        Source = "DigiKey",
//            //        MatchVerdict = $"❌ Error: {ex.Message}"
//            //    };
//            //}

//            // Fetch from Mouser
//            //try
//            //{
//            //    var mouserData = await _mouser.GetPartDetails(request.PartNumber);
//            //    if (mouserData != null)
//            //    {
//            //        result.MouserResult = new VerifySource
//            //        {
//            //            Source = "Mouser",
//            //            FetchedDescription = mouserData.Description,
//            //            Manufacturer = mouserData.Manufacturer,
//            //            Category = mouserData.Category,
//            //            Package = mouserData.Specs.TryGetValue("Package / Case", out var pkg) ? pkg :
//            //                                 mouserData.Specs.TryGetValue("Case/Package", out var cpkg) ? cpkg : "N/A",
//            //            DatasheetUrl = mouserData.DatasheetUrl,
//            //            ProductUrl = mouserData.ProductUrl,
//            //            Stock = mouserData.Stock,
//            //            Specs = mouserData.Specs,
//            //            MatchVerdict = GetMatchVerdict(request.Description, mouserData.Description)
//            //        };
//            //    }
//            //    else
//            //    {
//            //        result.MouserResult = new VerifySource
//            //        {
//            //            Source = "Mouser",
//            //            MatchVerdict = "❌ Part not found on Mouser"
//            //        };
//            //    }
//            //}
//            //catch (Exception ex)
//            //{
//            //    result.MouserResult = new VerifySource
//            //    {
//            //        Source = "Mouser",
//            //        MatchVerdict = $"❌ Error: {ex.Message}"
//            //    };
//            //}


//            // Overall verdict
//            result.OverallVerdict = GetOverallVerdict(result.DigiKeyResult, result.MouserResult);

//            return View("Result", result);
//        }

//        // --- Match Logic ---
//        private string GetMatchVerdict(string userDesc, string fetchedDesc)
//        {
//            if (string.IsNullOrWhiteSpace(fetchedDesc))
//                return "⚠️ No description available";

//            var user = userDesc.ToLower().Trim();
//            var fetched = fetchedDesc.ToLower().Trim();

//            // Exact match
//            if (user == fetched)
//                return "✅ Exact Match";

//            // Check how many words from user description exist in fetched
//            var userWords = user.Split(' ', StringSplitOptions.RemoveEmptyEntries);
//            var matchedWords = userWords.Count(w => fetched.Contains(w));
//            var matchPercent = (double)matchedWords / userWords.Length * 100;

//            if (matchPercent >= 80)
//                return $"✅ Strong Match ({matchPercent:0}% keywords matched)";
//            if (matchPercent >= 50)
//                return $"⚠️ Partial Match ({matchPercent:0}% keywords matched)";
//            if (matchPercent >= 20)
//                return $"⚠️ Weak Match ({matchPercent:0}% keywords matched)";

//            return $"❌ No Match ({matchPercent:0}% keywords matched)";
//        }

//        private string GetOverallVerdict(VerifySource dk, VerifySource mouser)
//        {
//            bool dkMatch = dk?.MatchVerdict?.StartsWith("✅") == true;
//            bool mouserMatch = mouser?.MatchVerdict?.StartsWith("✅") == true;

//            if (dkMatch && mouserMatch)
//                return "✅ Verified — Both DigiKey and Mouser confirm this description matches the part number.";
//            if (dkMatch || mouserMatch)
//                return "⚠️ Partially Verified — One source confirms the match. Check the other source manually.";

//            return "❌ Not Verified — Neither DigiKey nor Mouser description matches what you entered.";
//        }


//        // GET: /Verify/Compare
//        [HttpGet]
//        public IActionResult Compare() => View(new CompareRequest());

//        // POST: /Verify/Compare
//        [HttpPost]
//        public async Task<IActionResult> Compare(CompareRequest request)
//        {
//            if (!ModelState.IsValid) return View(request);

//            var result = new CompareResult
//            {
//                OriginalPart = request.OriginalPart.Trim(),
//                AlternatePart = request.AlternatePart.Trim(),
//                UserDescription = request.Description.Trim()
//            };

//            // --- Fetch Original Part from DigiKey then Mouser ---
//            result.OriginalDetails = await FetchBestDetails(request.OriginalPart.Trim());
//            result.AlternateDetails = await FetchBestDetails(request.AlternatePart.Trim());

//            // --- Calculate match scores against user description ---
//            result.OriginalMatchScore = CalculateMatchScore(request.Description,
//                                            result.OriginalDetails?.FetchedDescription);
//            result.AlternateMatchScore = CalculateMatchScore(request.Description,
//                                            result.AlternateDetails?.FetchedDescription);

//            // --- Build spec comparison table ---
//            result.SpecComparisons = BuildSpecComparison(
//                                        result.OriginalDetails?.Specs,
//                                        result.AlternateDetails?.Specs);

//            // --- Final verdict ---
//            DetermineVerdict(result);

//            return View("CompareResult", result);
//        }

//        // --- Fetch from DigiKey first, fallback to Mouser ---
//        private async Task<VerifySource> FetchBestDetails(string mpn)
//        {
//            // ── PRIMARY: Nexar ────────────────────────────────────────
//            try
//            {
//                var rawJson = await _nexar.GetPartData(mpn);

//                using var doc = JsonDocument.Parse(rawJson);
//                var root = doc.RootElement;

//                if (!root.TryGetProperty("data", out var data) ||
//                    !data.TryGetProperty("supSearchMpn", out var search) ||
//                    search.ValueKind == JsonValueKind.Null ||
//                    !search.TryGetProperty("results", out var results) ||
//                    results.GetArrayLength() == 0)
//                    return new VerifySource
//                    {
//                        Source = "Not Found",
//                        FetchedDescription = "",
//                        Specs = new Dictionary<string, string>()
//                    };

//                var part = results[0].GetProperty("part");

//                // Extract specs
//                var specs = new Dictionary<string, string>();
//                if (part.TryGetProperty("specs", out var specList) &&
//                    specList.ValueKind != JsonValueKind.Null)
//                {
//                    foreach (var spec in specList.EnumerateArray())
//                    {
//                        if (!spec.TryGetProperty("attribute", out var attr))
//                            continue;
//                        var name = attr.TryGetProperty("name", out var n)
//                            ? n.GetString() : null;
//                        var val = spec.TryGetProperty(
//                            "displayValue", out var v)
//                            ? v.GetString() : null;
//                        if (!string.IsNullOrEmpty(name) &&
//                            !string.IsNullOrEmpty(val))
//                            specs[name] = val;
//                    }
//                }

//                return new VerifySource
//                {
//                    Source = "Nexar",
//                    FetchedDescription = part.TryGetProperty(
//                        "shortDescription", out var d)
//                        ? d.GetString() : "",
//                    Manufacturer = part.TryGetProperty(
//                        "manufacturer", out var mfr) &&
//                        mfr.ValueKind != JsonValueKind.Null &&
//                        mfr.TryGetProperty("name", out var mn)
//                        ? mn.GetString() : "",
//                    Category = part.TryGetProperty(
//                        "category", out var cat) &&
//                        cat.ValueKind != JsonValueKind.Null &&
//                        cat.TryGetProperty("name", out var cn)
//                        ? cn.GetString() : "",
//                    Package = specs.TryGetValue(
//                        "Case/Package", out var pkg) ? pkg :
//                        specs.TryGetValue(
//                        "Mounting Style", out var ms) ? ms : "N/A",
//                    DatasheetUrl = part.TryGetProperty(
//                        "bestDatasheet", out var ds) &&
//                        ds.ValueKind != JsonValueKind.Null &&
//                        ds.TryGetProperty("url", out var du)
//                        ? du.GetString() : "",
//                    Stock = "",
//                    Specs = specs,
//                    MatchVerdict = ""
//                };
//            }
//            catch { }

//            // ── FALLBACK: DigiKey (uncomment to re-enable) ────────────
//            // try
//            // {
//            //     var data = await _digikey.GetPartDetails(mpn);
//            //     if (data != null)
//            //     {
//            //         return new VerifySource
//            //         {
//            //             Source             = "DigiKey",
//            //             FetchedDescription = data.Description,
//            //             Manufacturer       = data.Manufacturer,
//            //             Category           = data.Category,
//            //             Package = data.Specs.TryGetValue(
//            //                 "Package / Case", out var pkg) ? pkg :
//            //                 data.Specs.TryGetValue(
//            //                 "Supplier Device Package", out var spkg)
//            //                 ? spkg : "N/A",
//            //             DatasheetUrl       = data.DatasheetUrl,
//            //             ProductUrl         = data.ProductUrl,
//            //             Stock              = data.Stock,
//            //             Specs              = data.Specs,
//            //             MatchVerdict       = ""
//            //         };
//            //     }
//            // }
//            // catch { }

//            // ── FALLBACK: Mouser (uncomment to re-enable) ─────────────
//            // try
//            // {
//            //     var data = await _mouser.GetPartDetails(mpn);
//            //     if (data != null)
//            //     {
//            //         return new VerifySource
//            //         {
//            //             Source             = "Mouser",
//            //             FetchedDescription = data.Description,
//            //             Manufacturer       = data.Manufacturer,
//            //             Category           = data.Category,
//            //             Package = data.Specs.TryGetValue(
//            //                 "Case/Package", out var pkg) ? pkg : "N/A",
//            //             DatasheetUrl       = data.DatasheetUrl,
//            //             ProductUrl         = data.ProductUrl,
//            //             Stock              = data.Stock,
//            //             Specs              = data.Specs,
//            //             MatchVerdict       = ""
//            //         };
//            //     }
//            // }
//            // catch { }

//            // ── Not found in any source ───────────────────────────────
//            return new VerifySource
//            {
//                Source = "Not Found",
//                FetchedDescription = "",
//                Specs = new Dictionary<string, string>()
//            };
//        }
//        // --- Calculate how well description matches fetched description ---
//        private double CalculateMatchScore(string userDesc, string fetchedDesc)
//        {
//            if (string.IsNullOrWhiteSpace(fetchedDesc)) return 0;

//            var userWords = userDesc.ToLower()
//                                       .Split(' ', StringSplitOptions.RemoveEmptyEntries);
//            var fetchedLower = fetchedDesc.ToLower();

//            int matched = userWords.Count(w => fetchedLower.Contains(w));
//            return Math.Round((double)matched / userWords.Length * 100, 1);
//        }

//        // --- Build spec comparison table ---
//        private List<SpecComparison> BuildSpecComparison(
//            Dictionary<string, string> origSpecs,
//            Dictionary<string, string> altSpecs)
//        {
//            var comparisons = new List<SpecComparison>();

//            origSpecs ??= new Dictionary<string, string>();
//            altSpecs ??= new Dictionary<string, string>();

//            // Key specs to always show first
//            var prioritySpecs = new[]
//            {
//        "Package / Case", "Supplier Device Package",
//        "Number of Pins", "Pin Count",
//        "Voltage - Supply", "Supply Voltage",
//        "Current - Output", "Operating Temperature",
//        "Technology", "Part Status",
//        "Mounting Type", "Operating Temperature Range"
//    };

//            // All unique spec keys from both parts
//            var allKeys = origSpecs.Keys
//                                   .Union(altSpecs.Keys)
//                                   .OrderBy(k => Array.IndexOf(prioritySpecs, k) >= 0
//                                       ? Array.IndexOf(prioritySpecs, k) : 999)
//                                   .ToList();

//            foreach (var key in allKeys)
//            {
//                origSpecs.TryGetValue(key, out var origVal);
//                altSpecs.TryGetValue(key, out var altVal);

//                string status;
//                if (!string.IsNullOrEmpty(origVal) && !string.IsNullOrEmpty(altVal))
//                    status = origVal.Equals(altVal, StringComparison.OrdinalIgnoreCase)
//                             ? "Match" : "Mismatch";
//                else if (!string.IsNullOrEmpty(origVal))
//                    status = "Only Original";
//                else
//                    status = "Only Alternate";

//                comparisons.Add(new SpecComparison
//                {
//                    SpecName = key,
//                    OriginalValue = origVal ?? "—",
//                    AlternateValue = altVal ?? "—",
//                    Status = status
//                });
//            }

//            return comparisons;
//        }

//        // --- Determine final verdict ---
//        private void DetermineVerdict(CompareResult result)
//        {
//            bool origFound = result.OriginalDetails?.Source != "Not Found";
//            bool altFound = result.AlternateDetails?.Source != "Not Found";

//            // Neither found
//            if (!origFound && !altFound)
//            {
//                result.Verdict = "❌ Cannot Verify";
//                result.VerdictReason = "Neither part was found on DigiKey or Mouser.";
//                result.RecommendedPart = "Unknown";
//                return;
//            }

//            // Count spec mismatches (critical ones)
//            int criticalMismatches = result.SpecComparisons.Count(s =>
//                s.Status == "Mismatch" &&
//                (s.SpecName.Contains("Package") || s.SpecName.Contains("Pin")));

//            int totalMismatches = result.SpecComparisons.Count(s => s.Status == "Mismatch");
//            int totalMatches = result.SpecComparisons.Count(s => s.Status == "Match");

//            double origScore = result.OriginalMatchScore;
//            double altScore = result.AlternateMatchScore;

//            // Critical mismatch = must use original
//            if (criticalMismatches > 0)
//            {
//                result.Verdict = "⚠️ Use Original Part Only";
//                result.VerdictReason = $"Alternate has {criticalMismatches} critical spec mismatch(es) " +
//                                          $"(Package/Pins). It may not fit the board.";
//                result.RecommendedPart = "Original";
//                return;
//            }

//            // Both specs match well and alt description matches user desc
//            if (totalMatches > totalMismatches && altScore >= 50)
//            {
//                result.Verdict = "✅ Alternate Part is Okay to Use";
//                result.VerdictReason = $"Specs match in {totalMatches} of " +
//                                          $"{totalMatches + totalMismatches} compared parameters. " +
//                                          $"Description match: {altScore}%.";
//                result.RecommendedPart = "Alternate";
//                return;
//            }

//            // Alternate description matches better than original
//            if (altScore > origScore && altScore >= 60)
//            {
//                result.Verdict = "✅ Alternate Part is Okay to Use";
//                result.VerdictReason = $"Alternate part description matches your requirement better " +
//                                          $"({altScore}% vs {origScore}%).";
//                result.RecommendedPart = "Alternate";
//                return;
//            }

//            // Original matches better
//            if (origScore > altScore && origScore >= 60)
//            {
//                result.Verdict = "⚠️ Use Original Part";
//                result.VerdictReason = $"Original part description matches your requirement better " +
//                                          $"({origScore}% vs {altScore}%).";
//                result.RecommendedPart = "Original";
//                return;
//            }

//            // Both match reasonably
//            if (altScore >= 40 && origScore >= 40 && totalMismatches <= 2)
//            {
//                result.Verdict = "✅ Either Part Can Be Used";
//                result.VerdictReason = $"Both parts match your description reasonably well. " +
//                                          $"Original: {origScore}%, Alternate: {altScore}%.";
//                result.RecommendedPart = "Either";
//                return;
//            }

//            // Default
//            result.Verdict = "⚠️ Check Manually";
//            result.VerdictReason = $"Insufficient data to make a confident recommendation. " +
//                                      $"Original match: {origScore}%, Alternate match: {altScore}%.";
//            result.RecommendedPart = "Unknown";
//        }


//        // GET: /Verify/BomVerify
//        [HttpGet]
//        public IActionResult BomVerify() => View();

//        // POST: /Verify/BomVerify
//        [HttpPost]
//        public async Task<IActionResult> BomVerify(IFormFile bomFile)
//        {
//            if (bomFile == null || bomFile.Length == 0)
//            {
//                ViewBag.Error = "Please select a valid Excel file.";
//                return View();
//            }

//            var extension = Path.GetExtension(bomFile.FileName).ToLower();
//            if (extension != ".xlsx" && extension != ".xls")
//            {
//                ViewBag.Error = "Only .xlsx or .xls files are supported.";
//                return View();
//            }

//            try
//            {
//                using var stream = new MemoryStream();
//                await bomFile.CopyToAsync(stream);
//                stream.Position = 0;

//                OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("Alter_Parts");
//                using var package = new OfficeOpenXml.ExcelPackage(stream);
//                var sheet = package.Workbook.Worksheets[0];

//                if (sheet.Dimension == null)
//                {
//                    ViewBag.Error = "The Excel sheet is empty.";
//                    return View();
//                }

//                // Detect columns from header row
//                var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
//                for (int col = 1; col <= sheet.Dimension.End.Column; col++)
//                {
//                    var header = sheet.Cells[1, col].Text?.Trim();
//                    if (!string.IsNullOrEmpty(header))
//                        headers[header] = col;
//                }

//                // Map columns
//                int colPart = FindColumn(headers,
//                    "part number", "part no", "mpn", "part",
//                    "original part", "component");
//                int colDesc = FindColumn(headers,
//                    "description", "desc", "part description",
//                    "component description");

//                if (colPart == -1 || colDesc == -1)
//                {
//                    ViewBag.Error = $"Could not find required columns. " +
//                                    $"Found: {string.Join(", ", headers.Keys)}. " +
//                                    $"Need: Part Number and Description columns.";
//                    return View();
//                }

//                var result = new BomVerifyResult
//                {
//                    FileName = bomFile.FileName,
//                    TotalRows = sheet.Dimension.End.Row - 1
//                };

//                // Process each row
//                for (int row = 2; row <= sheet.Dimension.End.Row; row++)
//                {
//                    var partNumber = sheet.Cells[row, colPart].Text?.Trim() ?? "";
//                    var userDesc = sheet.Cells[row, colDesc].Text?.Trim() ?? "";

//                    // Skip completely empty rows
//                    if (string.IsNullOrWhiteSpace(partNumber) &&
//                        string.IsNullOrWhiteSpace(userDesc))
//                        continue;

//                    var bomRow = new BomVerifyRow
//                    {
//                        RowNumber = row,
//                        PartNumber = partNumber,
//                        UserDescription = userDesc,
//                        Status = "Pending"
//                    };

//                    // Skip if part number missing
//                    if (string.IsNullOrWhiteSpace(partNumber))
//                    {
//                        bomRow.MatchVerdict = "⚠️ No part number";
//                        bomRow.Status = "Skipped";
//                        result.Rows.Add(bomRow);
//                        continue;
//                    }

//                    // Fetch from DigiKey then Mouser
//                    try
//                    {
//                        var details = await FetchBestDetails(partNumber);

//                        bomRow.Source = details.Source;
//                        bomRow.FetchedDescription = details.FetchedDescription;
//                        bomRow.Manufacturer = details.Manufacturer;
//                        bomRow.Category = details.Category;

//                        if (details.Source == "Not Found")
//                        {
//                            bomRow.MatchVerdict = "❌ Part not found";
//                            bomRow.MatchScore = 0;
//                            bomRow.Status = "Done";
//                        }
//                        else if (string.IsNullOrWhiteSpace(userDesc))
//                        {
//                            bomRow.MatchVerdict = "⚠️ No description to verify";
//                            bomRow.MatchScore = 0;
//                            bomRow.Status = "Done";
//                        }
//                        else
//                        {
//                            bomRow.MatchScore = CalculateMatchScore(userDesc,
//                                                    details.FetchedDescription);
//                            bomRow.MatchVerdict = GetMatchVerdict(userDesc,
//                                                    details.FetchedDescription);
//                            bomRow.Status = "Done";
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        bomRow.MatchVerdict = $"❌ API Error: {ex.Message}";
//                        bomRow.Status = "Error";
//                    }

//                    result.Rows.Add(bomRow);
//                }

//                // Count results
//                result.MatchedCount = result.Rows.Count(r =>
//                    r.MatchVerdict?.StartsWith("✅") == true);
//                result.NotMatchedCount = result.Rows.Count(r =>
//                    r.MatchVerdict?.StartsWith("❌") == true);
//                result.ManualCheckCount = result.Rows.Count(r =>
//                    r.MatchVerdict?.StartsWith("⚠️") == true);
//                result.NotFoundCount = result.Rows.Count(r =>
//                    r.MatchVerdict == "❌ Part not found");

//                return View("BomVerifyResult", result);
//            }
//            catch (Exception ex)
//            {
//                ViewBag.Error = "Failed to process file: " + ex.Message;
//                return View();
//            }
//        }

//        // POST: /Verify/ExportVerifyResults
//        [HttpPost]
//        public IActionResult ExportVerifyResults(string resultsJson)
//        {
//            var rows = System.Text.Json.JsonSerializer
//                             .Deserialize<List<BomVerifyRow>>(resultsJson);
//            if (rows == null || rows.Count == 0)
//                return BadRequest("No data to export.");

//            OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("Alter_Parts");
//            using var package = new OfficeOpenXml.ExcelPackage();
//            var sheet = package.Workbook.Worksheets.Add("BOM Verify Results");

//            string[] hdrs = { "Row #", "Part Number", "Your Description",
//                      "Fetched Description", "Manufacturer",
//                      "Category", "Source", "Match Score",
//                      "Verdict", "Status" };

//            for (int i = 0; i < hdrs.Length; i++)
//            {
//                sheet.Cells[1, i + 1].Value = hdrs[i];
//                sheet.Cells[1, i + 1].Style.Font.Bold = true;
//                sheet.Cells[1, i + 1].Style.Fill.PatternType =
//                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
//                sheet.Cells[1, i + 1].Style.Fill.BackgroundColor
//                     .SetColor(System.Drawing.Color.FromArgb(68, 114, 196));
//                sheet.Cells[1, i + 1].Style.Font.Color
//                     .SetColor(System.Drawing.Color.White);
//            }

//            for (int i = 0; i < rows.Count; i++)
//            {
//                var row = rows[i];
//                int excelRow = i + 2;

//                sheet.Cells[excelRow, 1].Value = row.RowNumber;
//                sheet.Cells[excelRow, 2].Value = row.PartNumber;
//                sheet.Cells[excelRow, 3].Value = row.UserDescription;
//                sheet.Cells[excelRow, 4].Value = row.FetchedDescription;
//                sheet.Cells[excelRow, 5].Value = row.Manufacturer;
//                sheet.Cells[excelRow, 6].Value = row.Category;
//                sheet.Cells[excelRow, 7].Value = row.Source;
//                sheet.Cells[excelRow, 8].Value = $"{row.MatchScore}%";
//                sheet.Cells[excelRow, 9].Value = row.MatchVerdict;
//                sheet.Cells[excelRow, 10].Value = row.Status;

//                var color = row.MatchVerdict?.StartsWith("✅") == true
//                    ? System.Drawing.Color.FromArgb(198, 239, 206)
//                    : row.MatchVerdict?.StartsWith("❌") == true
//                    ? System.Drawing.Color.FromArgb(255, 199, 206)
//                    : System.Drawing.Color.FromArgb(255, 235, 156);

//                sheet.Cells[excelRow, 9].Style.Fill.PatternType =
//                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
//                sheet.Cells[excelRow, 9].Style.Fill.BackgroundColor.SetColor(color);
//            }

//            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

//            var fileBytes = package.GetAsByteArray();
//            return File(fileBytes,
//                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
//                $"BOM_Verify_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
//        }

//        // Helper to find column by multiple possible names
//        private static int FindColumn(Dictionary<string, int> headers,
//            params string[] names)
//        {
//            foreach (var name in names)
//                if (headers.TryGetValue(name, out int col))
//                    return col;
//            return -1;
//        }
//    }
//}












//using Alter_Parts.Models;
//using Alter_Parts.Services;
//using Microsoft.AspNetCore.Mvc;
//using OfficeOpenXml;
//using System.Text.Json;

//namespace Alter_Parts.Controllers
//{
//    public class VerifyController : Controller
//    {
//        private readonly DigiKeyService _digikey;
//        private readonly MouserService _mouser;
//        private readonly LCSCService _lcsc;

//        // 💡 Nexar — uncomment when ready
//        // private readonly NexarService _nexar;

//        public VerifyController(DigiKeyService digikey,
//                                MouserService mouser,
//                                LCSCService lcsc)
//        {
//            _digikey = digikey;
//            _mouser = mouser;
//            _lcsc = lcsc;
//            // _nexar = nexar;
//        }

//        // ── GET: /Verify ──────────────────────────────────────────
//        [HttpGet]
//        public IActionResult Index() => View(new VerifyRequest());

//        // ── POST: /Verify ─────────────────────────────────────────
//        [HttpPost]
//        public async Task<IActionResult> Index(
//            VerifyRequest request)
//        {
//            if (!ModelState.IsValid) return View(request);

//            var result = new VerifyResult
//            {
//                PartNumber = request.PartNumber.Trim(),
//                UserDescription = request.Description.Trim()
//            };

//            // ── Fetch from DigiKey ────────────────────────────────
//            try
//            {
//                var dkData = await _digikey
//                    .GetPartDetails(request.PartNumber);
//                if (dkData != null)
//                {
//                    result.DigiKeyResult = new VerifySource
//                    {
//                        Source = "DigiKey",
//                        FetchedDescription = dkData.Description,
//                        Manufacturer = dkData.Manufacturer,
//                        Category = dkData.Category,
//                        Package = dkData.Specs.TryGetValue(
//                            "Package / Case", out var pkg) ? pkg :
//                            dkData.Specs.TryGetValue(
//                            "Supplier Device Package", out var spkg)
//                            ? spkg : "N/A",
//                        DatasheetUrl = dkData.DatasheetUrl,
//                        ProductUrl = dkData.ProductUrl,
//                        Stock = dkData.Stock,
//                        Specs = dkData.Specs,
//                        MatchVerdict = GetMatchVerdict(
//                            request.Description, dkData.Description)
//                    };
//                }
//                else
//                {
//                    result.DigiKeyResult = new VerifySource
//                    {
//                        Source = "DigiKey",
//                        MatchVerdict =
//                            "❌ Part not found on DigiKey",
//                        Specs = new Dictionary<string, string>()
//                    };
//                }
//            }
//            catch (Exception ex)
//            {
//                result.DigiKeyResult = new VerifySource
//                {
//                    Source = "DigiKey",
//                    MatchVerdict = $"❌ Error: {ex.Message}",
//                    Specs = new Dictionary<string, string>()
//                };
//            }

//            // ── Fetch from Mouser ─────────────────────────────────
//            try
//            {
//                var mouserData = await _mouser
//                    .GetPartDetails(request.PartNumber);
//                if (mouserData != null)
//                {
//                    result.MouserResult = new VerifySource
//                    {
//                        Source = "Mouser",
//                        FetchedDescription = mouserData.Description,
//                        Manufacturer = mouserData.Manufacturer,
//                        Category = mouserData.Category,
//                        Package = mouserData.Specs.TryGetValue(
//                            "Package / Case", out var pkg) ? pkg :
//                            mouserData.Specs.TryGetValue(
//                            "Case/Package", out var cpkg)
//                            ? cpkg : "N/A",
//                        DatasheetUrl = mouserData.DatasheetUrl,
//                        ProductUrl = mouserData.ProductUrl,
//                        Stock = mouserData.Stock,
//                        Specs = mouserData.Specs,
//                        MatchVerdict = GetMatchVerdict(
//                            request.Description,
//                            mouserData.Description)
//                    };
//                }
//                else
//                {
//                    result.MouserResult = new VerifySource
//                    {
//                        Source = "Mouser",
//                        MatchVerdict =
//                            "❌ Part not found on Mouser",
//                        Specs = new Dictionary<string, string>()
//                    };
//                }
//            }
//            catch (Exception ex)
//            {
//                result.MouserResult = new VerifySource
//                {
//                    Source = "Mouser",
//                    MatchVerdict = $"❌ Error: {ex.Message}",
//                    Specs = new Dictionary<string, string>()
//                };
//            }

//            // ── Fetch from LCSC ───────────────────────────────────
//            try
//            {
//                var lcscResults = await _lcsc
//                    .SearchByKeyword(request.PartNumber, limit: 1);
//                var lcscData = lcscResults.FirstOrDefault();
//                if (lcscData != null)
//                {
//                    result.LCSCResult = new VerifySource
//                    {
//                        Source = "LCSC",
//                        FetchedDescription = lcscData.Description,
//                        Manufacturer = lcscData.Manufacturer,
//                        Category = lcscData.Category,
//                        Package = lcscData.Package ?? "N/A",
//                        DatasheetUrl = lcscData.DatasheetUrl,
//                        ProductUrl = lcscData.ProductUrl,
//                        Stock = lcscData.Stock,
//                        Specs = new Dictionary<string, string>
//                        {
//                            ["Package"] = lcscData.Package
//                                               ?? "",
//                            ["LCSC Part No"] = lcscData.LcscPartNumber
//                                               ?? "",
//                            ["MPN"] = lcscData.MpnNumber
//                                               ?? "",
//                            ["Price"] = lcscData.Price ?? ""
//                        },
//                        MatchVerdict = GetMatchVerdict(
//                            request.Description,
//                            lcscData.Description)
//                    };
//                }
//                else
//                {
//                    result.LCSCResult = new VerifySource
//                    {
//                        Source = "LCSC",
//                        MatchVerdict = "❌ Part not found on LCSC",
//                        Specs = new Dictionary<string, string>()
//                    };
//                }
//            }
//            catch (Exception ex)
//            {
//                result.LCSCResult = new VerifySource
//                {
//                    Source = "LCSC",
//                    MatchVerdict = $"❌ Error: {ex.Message}",
//                    Specs = new Dictionary<string, string>()
//                };
//            }

//            result.OverallVerdict = GetOverallVerdict(result);
//            return View("Result", result);
//        }

//        // ── Match Logic ───────────────────────────────────────────
//        private string GetMatchVerdict(string userDesc,
//            string fetchedDesc)
//        {
//            if (string.IsNullOrWhiteSpace(fetchedDesc))
//                return "⚠️ No description available";

//            var user = userDesc.ToLower().Trim();
//            var fetched = fetchedDesc.ToLower().Trim();

//            if (user == fetched) return "✅ Exact Match";

//            var userWords = user.Split(' ',
//                StringSplitOptions.RemoveEmptyEntries);
//            var matchedWords = userWords.Count(w =>
//                fetched.Contains(w));
//            var matchPercent =
//                (double)matchedWords / userWords.Length * 100;

//            if (matchPercent >= 80)
//                return $"✅ Strong Match " +
//                       $"({matchPercent:0}% keywords matched)";
//            if (matchPercent >= 50)
//                return $"⚠️ Partial Match " +
//                       $"({matchPercent:0}% keywords matched)";
//            if (matchPercent >= 20)
//                return $"⚠️ Weak Match " +
//                       $"({matchPercent:0}% keywords matched)";

//            return
//                $"❌ No Match ({matchPercent:0}% keywords matched)";
//        }

//        // ── Overall verdict ───────────────────────────────────────
//        private string GetOverallVerdict(VerifyResult result)
//        {
//            bool dkMatch = result.DigiKeyResult?.MatchVerdict
//                                     ?.StartsWith("✅") == true;
//            bool mouserMatch = result.MouserResult?.MatchVerdict
//                                     ?.StartsWith("✅") == true;
//            bool lcscMatch = result.LCSCResult?.MatchVerdict
//                                     ?.StartsWith("✅") == true;

//            int matchCount = new[] { dkMatch, mouserMatch, lcscMatch }
//                .Count(m => m);

//            if (matchCount == 3)
//                return "✅ Verified — DigiKey, Mouser and LCSC " +
//                       "all confirm this description matches.";
//            if (matchCount == 2)
//                return "✅ Verified — 2 out of 3 sources confirm " +
//                       "this description matches.";
//            if (matchCount == 1)
//                return "⚠️ Partially Verified — Only 1 source " +
//                       "confirms. Check the others manually.";

//            return "❌ Not Verified — No source matches " +
//                   "the description you entered.";

//            // 💡 Nexar verdict — uncomment when ready
//            // bool nexarMatch = result.NexarResult?.MatchVerdict
//            //     ?.StartsWith("✅") == true;
//        }

//        // ── GET: /Verify/Compare ──────────────────────────────────
//        [HttpGet]
//        public IActionResult Compare() => View(new CompareRequest());

//        // ── POST: /Verify/Compare ─────────────────────────────────
//        [HttpPost]
//        public async Task<IActionResult> Compare(
//            CompareRequest request)
//        {
//            if (!ModelState.IsValid) return View(request);

//            var result = new CompareResult
//            {
//                OriginalPart = request.OriginalPart.Trim(),
//                AlternatePart = request.AlternatePart.Trim(),
//                UserDescription = request.Description.Trim()
//            };

//            result.OriginalDetails =
//                await FetchBestDetails(request.OriginalPart.Trim());
//            result.AlternateDetails =
//                await FetchBestDetails(request.AlternatePart.Trim());

//            result.OriginalMatchScore = CalculateMatchScore(
//                request.Description,
//                result.OriginalDetails?.FetchedDescription);
//            result.AlternateMatchScore = CalculateMatchScore(
//                request.Description,
//                result.AlternateDetails?.FetchedDescription);

//            result.SpecComparisons = BuildSpecComparison(
//                result.OriginalDetails?.Specs,
//                result.AlternateDetails?.Specs);

//            DetermineVerdict(result);
//            return View("CompareResult", result);
//        }

//        // ── GET: /Verify/DescriptionSearch ────────────────────────
//        [HttpGet]
//        public IActionResult DescriptionSearch()
//            => View(new DescriptionSearchViewModel());

//        // ── POST: /Verify/DescriptionSearch ───────────────────────
//        [HttpPost]
//        public async Task<IActionResult> DescriptionSearch(
//            DescriptionSearchRequest request)
//        {
//            var vm = new DescriptionSearchViewModel
//            {
//                Description = request.Description
//            };

//            if (string.IsNullOrWhiteSpace(request.Description))
//            {
//                vm.Error = "Please enter a description.";
//                return View(vm);
//            }

//            // ── Run all 3 simultaneously ──────────────────────────
//            var digikeyTask = Task.Run(async () =>
//            {
//                try
//                {
//                    return await _digikey.SearchByDescription(
//                        request.Description, request.Limit);
//                }
//                catch { return new List<PartDetails>(); }
//            });

//            var mouserTask = Task.Run(async () =>
//            {
//                try
//                {
//                    return await _mouser.SearchByDescription(
//                        request.Description, request.Limit);
//                }
//                catch { return new List<PartDetails>(); }
//            });

//            var lcscTask = Task.Run(async () =>
//            {
//                try
//                {
//                    var lcscResults = await _lcsc.SearchByKeyword(
//                        request.Description, request.Limit);
//                    return lcscResults.Select(r => new PartDetails
//                    {
//                        Mpn = r.MpnNumber,
//                        Description = r.Description,
//                        Manufacturer = r.Manufacturer,
//                        Category = r.Category,
//                        DatasheetUrl = r.DatasheetUrl,
//                        ProductUrl = r.ProductUrl,
//                        Stock = r.Stock,
//                        Source = "LCSC",
//                        Specs = new Dictionary<string, string>
//                        {
//                            ["Package"] = r.Package ?? "",
//                            ["LCSC Part No"] = r.LcscPartNumber ?? "",
//                            ["Unit Price"] = r.Price ?? "N/A",
//                            ["Match Score"] = $"{r.MatchScore}%"
//                        }
//                    }).ToList();
//                }
//                catch { return new List<PartDetails>(); }
//            });

//            await Task.WhenAll(digikeyTask, mouserTask, lcscTask);

//            vm.DigiKeyResults = await digikeyTask;
//            vm.MouserResults = await mouserTask;
//            vm.LCSCResults = await lcscTask;
//            vm.DigiKeyTotal = vm.DigiKeyResults.Count;
//            vm.MouserTotal = vm.MouserResults.Count;
//            vm.LCSCTotal = vm.LCSCResults.Count;

//            if (!vm.DigiKeyResults.Any() &&
//                !vm.MouserResults.Any() &&
//                !vm.LCSCResults.Any())
//                vm.Error =
//                    "No parts found. Try different keywords.";

//            return View(vm);
//        }

//        // ── FetchBestDetails ──────────────────────────────────────
//        // DigiKey → Mouser → LCSC fallback chain
//        private async Task<VerifySource> FetchBestDetails(
//            string mpn)
//        {
//            // ── PRIMARY: DigiKey ──────────────────────────────────
//            try
//            {
//                var data = await _digikey.GetPartDetails(mpn);
//                if (data != null)
//                    return new VerifySource
//                    {
//                        Source = "DigiKey",
//                        FetchedDescription = data.Description,
//                        Manufacturer = data.Manufacturer,
//                        Category = data.Category,
//                        Package = data.Specs.TryGetValue(
//                            "Package / Case", out var pkg) ? pkg :
//                            data.Specs.TryGetValue(
//                            "Supplier Device Package", out var spkg)
//                            ? spkg : "N/A",
//                        DatasheetUrl = data.DatasheetUrl,
//                        ProductUrl = data.ProductUrl,
//                        Stock = data.Stock,
//                        Specs = data.Specs,
//                        MatchVerdict = ""
//                    };
//            }
//            catch { }

//            // ── FALLBACK: Mouser ──────────────────────────────────
//            try
//            {
//                var data = await _mouser.GetPartDetails(mpn);
//                if (data != null)
//                    return new VerifySource
//                    {
//                        Source = "Mouser",
//                        FetchedDescription = data.Description,
//                        Manufacturer = data.Manufacturer,
//                        Category = data.Category,
//                        Package = data.Specs.TryGetValue(
//                            "Case/Package", out var pkg)
//                            ? pkg : "N/A",
//                        DatasheetUrl = data.DatasheetUrl,
//                        ProductUrl = data.ProductUrl,
//                        Stock = data.Stock,
//                        Specs = data.Specs,
//                        MatchVerdict = ""
//                    };
//            }
//            catch { }

//            // ── FALLBACK: LCSC ────────────────────────────────────
//            try
//            {
//                var results = await _lcsc
//                    .SearchByKeyword(mpn, limit: 1);
//                var data = results.FirstOrDefault();
//                if (data != null)
//                    return new VerifySource
//                    {
//                        Source = "LCSC",
//                        FetchedDescription = data.Description,
//                        Manufacturer = data.Manufacturer,
//                        Category = data.Category,
//                        Package = data.Package ?? "N/A",
//                        DatasheetUrl = data.DatasheetUrl,
//                        ProductUrl = data.ProductUrl,
//                        Stock = data.Stock,
//                        Specs = new Dictionary<string, string>
//                        {
//                            ["Package"] = data.Package ?? "",
//                            ["LCSC Part No"] = data.LcscPartNumber ?? "",
//                            ["MPN"] = data.MpnNumber ?? "",
//                            ["Price"] = data.Price ?? ""
//                        },
//                        MatchVerdict = ""
//                    };
//            }
//            catch { }

//            // 💡 Nexar — uncomment when ready
//            // try
//            // {
//            //     var rawJson = await _nexar.GetPartData(mpn);
//            //     ...
//            //     return new VerifySource { Source = "Nexar", ... };
//            // }
//            // catch { }

//            return new VerifySource
//            {
//                Source = "Not Found",
//                FetchedDescription = "",
//                Specs = new Dictionary<string, string>()
//            };
//        }

//        // ── Calculate match score ─────────────────────────────────
//        private double CalculateMatchScore(string userDesc,
//            string fetchedDesc)
//        {
//            if (string.IsNullOrWhiteSpace(fetchedDesc)) return 0;

//            var userWords = userDesc.ToLower().Split(' ',
//                StringSplitOptions.RemoveEmptyEntries);
//            var fetchedLower = fetchedDesc.ToLower();
//            int matched = userWords.Count(w =>
//                fetchedLower.Contains(w));
//            return Math.Round(
//                (double)matched / userWords.Length * 100, 1);
//        }

//        // ── Build spec comparison table ───────────────────────────
//        private List<SpecComparison> BuildSpecComparison(
//            Dictionary<string, string> origSpecs,
//            Dictionary<string, string> altSpecs)
//        {
//            var comparisons = new List<SpecComparison>();
//            origSpecs ??= new Dictionary<string, string>();
//            altSpecs ??= new Dictionary<string, string>();

//            var prioritySpecs = new[]
//            {
//                "Case/Package", "Number of Pins",
//                "Supply Voltage", "Operating Temperature",
//                "Mounting Style", "Output Current",
//                "Power Rating", "Technology", "Part Status"
//            };

//            var allKeys = origSpecs.Keys.Union(altSpecs.Keys)
//                .OrderBy(k =>
//                    Array.IndexOf(prioritySpecs, k) >= 0
//                    ? Array.IndexOf(prioritySpecs, k) : 999)
//                .ToList();

//            foreach (var key in allKeys)
//            {
//                origSpecs.TryGetValue(key, out var origVal);
//                altSpecs.TryGetValue(key, out var altVal);

//                string status;
//                if (!string.IsNullOrEmpty(origVal) &&
//                    !string.IsNullOrEmpty(altVal))
//                    status = origVal.Equals(altVal,
//                        StringComparison.OrdinalIgnoreCase)
//                        ? "Match" : "Mismatch";
//                else if (!string.IsNullOrEmpty(origVal))
//                    status = "Only Original";
//                else
//                    status = "Only Alternate";

//                comparisons.Add(new SpecComparison
//                {
//                    SpecName = key,
//                    OriginalValue = origVal ?? "—",
//                    AlternateValue = altVal ?? "—",
//                    Status = status
//                });
//            }
//            return comparisons;
//        }

//        // ── Determine final verdict ───────────────────────────────
//        private void DetermineVerdict(CompareResult result)
//        {
//            bool origFound =
//                result.OriginalDetails?.Source != "Not Found";
//            bool altFound =
//                result.AlternateDetails?.Source != "Not Found";

//            if (!origFound && !altFound)
//            {
//                result.Verdict = "❌ Cannot Verify";
//                result.VerdictReason = "Neither part was found.";
//                result.RecommendedPart = "Unknown";
//                return;
//            }

//            int criticalMismatches = result.SpecComparisons
//                .Count(s => s.Status == "Mismatch" &&
//                    (s.SpecName.Contains("Package") ||
//                     s.SpecName.Contains("Pin")));
//            int totalMismatches = result.SpecComparisons
//                .Count(s => s.Status == "Mismatch");
//            int totalMatches = result.SpecComparisons
//                .Count(s => s.Status == "Match");
//            double origScore = result.OriginalMatchScore;
//            double altScore = result.AlternateMatchScore;

//            if (criticalMismatches > 0)
//            {
//                result.Verdict =
//                    "⚠️ Use Original Part Only";
//                result.VerdictReason =
//                    $"Alternate has {criticalMismatches} critical " +
//                    "spec mismatch(es) (Package/Pins). " +
//                    "It may not fit the board.";
//                result.RecommendedPart = "Original";
//                return;
//            }

//            if (totalMatches > totalMismatches && altScore >= 50)
//            {
//                result.Verdict =
//                    "✅ Alternate Part is Okay to Use";
//                result.VerdictReason =
//                    $"Specs match in {totalMatches} of " +
//                    $"{totalMatches + totalMismatches} parameters. " +
//                    $"Description match: {altScore}%.";
//                result.RecommendedPart = "Alternate";
//                return;
//            }

//            if (altScore > origScore && altScore >= 60)
//            {
//                result.Verdict =
//                    "✅ Alternate Part is Okay to Use";
//                result.VerdictReason =
//                    $"Alternate matches better " +
//                    $"({altScore}% vs {origScore}%).";
//                result.RecommendedPart = "Alternate";
//                return;
//            }

//            if (origScore > altScore && origScore >= 60)
//            {
//                result.Verdict = "⚠️ Use Original Part";
//                result.VerdictReason =
//                    $"Original matches better " +
//                    $"({origScore}% vs {altScore}%).";
//                result.RecommendedPart = "Original";
//                return;
//            }

//            if (altScore >= 40 && origScore >= 40 &&
//                totalMismatches <= 2)
//            {
//                result.Verdict = "✅ Either Part Can Be Used";
//                result.VerdictReason =
//                    $"Both match reasonably. " +
//                    $"Original: {origScore}%, " +
//                    $"Alternate: {altScore}%.";
//                result.RecommendedPart = "Either";
//                return;
//            }

//            result.Verdict = "⚠️ Check Manually";
//            result.VerdictReason =
//                $"Insufficient data. " +
//                $"Original: {origScore}%, Alternate: {altScore}%.";
//            result.RecommendedPart = "Unknown";
//        }

//        // ── GET: /Verify/BomVerify ────────────────────────────────
//        [HttpGet]
//        public IActionResult BomVerify() => View();

//        // ── POST: /Verify/BomVerify ───────────────────────────────

//        // ── POST: /Verify/BomVerify ───────────────────────────────────
//        [HttpPost]
//        public async Task<IActionResult> BomVerify(IFormFile bomFile)
//        {
//            if (bomFile == null || bomFile.Length == 0)
//            {
//                ViewBag.Error = "Please select a valid Excel file.";
//                return View();
//            }

//            var extension = Path.GetExtension(bomFile.FileName).ToLower();
//            if (extension != ".xlsx" && extension != ".xls")
//            {
//                ViewBag.Error = "Only .xlsx or .xls files are supported.";
//                return View();
//            }

//            try
//            {
//                using var stream = new MemoryStream();
//                await bomFile.CopyToAsync(stream);
//                stream.Position = 0;

//                ExcelPackage.License
//                    .SetNonCommercialPersonal("Alter_Parts");
//                using var package = new ExcelPackage(stream);
//                var sheet = package.Workbook.Worksheets[0];

//                if (sheet.Dimension == null)
//                {
//                    ViewBag.Error = "The Excel sheet is empty.";
//                    return View();
//                }

//                // Detect columns
//                var headers = new Dictionary<string, int>(
//                    StringComparer.OrdinalIgnoreCase);
//                for (int col = 1;
//                    col <= sheet.Dimension.End.Column; col++)
//                {
//                    var header = sheet.Cells[1, col].Text?.Trim();
//                    if (!string.IsNullOrEmpty(header))
//                        headers[header] = col;
//                }

//                int colPart = FindColumn(headers,
//                    "part number", "part no", "mpn",
//                    "part", "original part", "component");
//                int colDesc = FindColumn(headers,
//                    "description", "desc",
//                    "part description", "component description");

//                if (colPart == -1 || colDesc == -1)
//                {
//                    ViewBag.Error =
//                        $"Could not find required columns. " +
//                        $"Found: {string.Join(", ", headers.Keys)}. " +
//                        "Need: Part Number and Description.";
//                    return View();
//                }

//                var result = new BomVerifyResult
//                {
//                    FileName = bomFile.FileName,
//                    TotalRows = sheet.Dimension.End.Row - 1
//                };

//                for (int row = 2;
//                    row <= sheet.Dimension.End.Row; row++)
//                {
//                    var partNumber =
//                        sheet.Cells[row, colPart].Text?.Trim() ?? "";
//                    var userDesc =
//                        sheet.Cells[row, colDesc].Text?.Trim() ?? "";

//                    if (string.IsNullOrWhiteSpace(partNumber) &&
//                        string.IsNullOrWhiteSpace(userDesc))
//                        continue;

//                    var bomRow = new BomVerifyRow
//                    {
//                        RowNumber = row,
//                        PartNumber = partNumber,
//                        UserDescription = userDesc,
//                        Status = "Pending"
//                    };

//                    if (string.IsNullOrWhiteSpace(partNumber))
//                    {
//                        bomRow.MatchVerdict = "⚠️ No part number";
//                        bomRow.Status = "Skipped";
//                        result.Rows.Add(bomRow);
//                        continue;
//                    }

//                    try
//                    {
//                        // ── Fetch from all 3 simultaneously ───────────
//                        var dkTask = Task.Run(async () =>
//                        {
//                            try
//                            {
//                                return await _digikey
//                                    .GetPartDetails(partNumber);
//                            }
//                            catch { return null; }
//                        });

//                        var mouserTask = Task.Run(async () =>
//                        {
//                            try
//                            {
//                                return await _mouser
//                                    .GetPartDetails(partNumber);
//                            }
//                            catch { return null; }
//                        });

//                        var lcscTask = Task.Run(async () =>
//                        {
//                            try
//                            {
//                                var results = await _lcsc
//                                    .SearchByKeyword(partNumber, limit: 1);
//                                return results.FirstOrDefault();
//                            }
//                            catch { return null; }
//                        });

//                        await Task.WhenAll(dkTask, mouserTask, lcscTask);

//                        var dkData = await dkTask;
//                        var mouserData = await mouserTask;
//                        var lcscData = await lcscTask;

//                        // ── DigiKey result ────────────────────────────
//                        if (dkData != null)
//                        {
//                            bomRow.DigiKeyDescription = dkData.Description;
//                            bomRow.DigiKeyScore = string.IsNullOrWhiteSpace(
//                                userDesc) ? 0 :
//                                CalculateMatchScore(userDesc,
//                                    dkData.Description);
//                            bomRow.DigiKeyVerdict = string.IsNullOrWhiteSpace(
//                                userDesc) ? "⚠️ No description" :
//                                GetMatchVerdict(userDesc, dkData.Description);
//                        }
//                        else
//                        {
//                            bomRow.DigiKeyVerdict = "❌ Not found";
//                        }

//                        // ── Mouser result ─────────────────────────────
//                        if (mouserData != null)
//                        {
//                            bomRow.MouserDescription = mouserData.Description;
//                            bomRow.MouserScore = string.IsNullOrWhiteSpace(
//                                userDesc) ? 0 :
//                                CalculateMatchScore(userDesc,
//                                    mouserData.Description);
//                            bomRow.MouserVerdict = string.IsNullOrWhiteSpace(
//                                userDesc) ? "⚠️ No description" :
//                                GetMatchVerdict(userDesc,
//                                    mouserData.Description);
//                        }
//                        else
//                        {
//                            bomRow.MouserVerdict = "❌ Not found";
//                        }

//                        // ── LCSC result ───────────────────────────────
//                        if (lcscData != null)
//                        {
//                            bomRow.LCSCDescription = lcscData.Description;
//                            bomRow.LCSCScore = string.IsNullOrWhiteSpace(
//                                userDesc) ? 0 :
//                                CalculateMatchScore(userDesc,
//                                    lcscData.Description);
//                            bomRow.LCSCVerdict = string.IsNullOrWhiteSpace(
//                                userDesc) ? "⚠️ No description" :
//                                GetMatchVerdict(userDesc,
//                                    lcscData.Description);
//                        }
//                        else
//                        {
//                            bomRow.LCSCVerdict = "❌ Not found";
//                        }

//                        // ── Pick best source ──────────────────────────
//                        var scores = new[]
//                        {
//                    ("DigiKey", bomRow.DigiKeyScore,
//                        bomRow.DigiKeyDescription),
//                    ("Mouser",  bomRow.MouserScore,
//                        bomRow.MouserDescription),
//                    ("LCSC",    bomRow.LCSCScore,
//                        bomRow.LCSCDescription)
//                }
//                        .Where(x => !string.IsNullOrEmpty(x.Item3))
//                        .OrderByDescending(x => x.Item2)
//                        .FirstOrDefault();

//                        if (scores != default)
//                        {
//                            bomRow.BestSource = scores.Item1;
//                            bomRow.Source = scores.Item1;
//                            bomRow.FetchedDescription = scores.Item3;
//                            bomRow.MatchScore = scores.Item2;
//                            bomRow.MatchVerdict = string.IsNullOrWhiteSpace(
//                                userDesc)
//                                ? "⚠️ No description to verify"
//                                : GetMatchVerdict(userDesc, scores.Item3);

//                            // Set manufacturer/category from best source
//                            if (scores.Item1 == "DigiKey" && dkData != null)
//                            {
//                                bomRow.Manufacturer = dkData.Manufacturer;
//                                bomRow.Category = dkData.Category;
//                            }
//                            else if (scores.Item1 == "Mouser" &&
//                                mouserData != null)
//                            {
//                                bomRow.Manufacturer = mouserData.Manufacturer;
//                                bomRow.Category = mouserData.Category;
//                            }
//                            else if (scores.Item1 == "LCSC" &&
//                                lcscData != null)
//                            {
//                                bomRow.Manufacturer = lcscData.Manufacturer;
//                                bomRow.Category = lcscData.Category;
//                            }
//                        }
//                        else
//                        {
//                            bomRow.BestSource = "Not Found";
//                            bomRow.Source = "Not Found";
//                            bomRow.MatchVerdict = "❌ Part not found";
//                            bomRow.MatchScore = 0;
//                        }

//                        bomRow.Status = "Done";
//                    }
//                    catch (Exception ex)
//                    {
//                        bomRow.MatchVerdict = $"❌ API Error: {ex.Message}";
//                        bomRow.Status = "Error";
//                    }

//                    result.Rows.Add(bomRow);
//                }

//                result.MatchedCount = result.Rows.Count(r =>
//                    r.MatchVerdict?.StartsWith("✅") == true);
//                result.NotMatchedCount = result.Rows.Count(r =>
//                    r.MatchVerdict?.StartsWith("❌") == true);
//                result.ManualCheckCount = result.Rows.Count(r =>
//                    r.MatchVerdict?.StartsWith("⚠️") == true);
//                result.NotFoundCount = result.Rows.Count(r =>
//                    r.MatchVerdict == "❌ Part not found");

//                return View("BomVerifyResult", result);
//            }
//            catch (Exception ex)
//            {
//                ViewBag.Error = "Failed to process file: " + ex.Message;
//                return View();
//            }
//        }

//        // ── POST: /Verify/ExportVerifyResults ─────────────────────

//        [HttpPost]
//        public IActionResult ExportVerifyResults(string resultsJson)
//        {
//            var rows = JsonSerializer
//                .Deserialize<List<BomVerifyRow>>(resultsJson);
//            if (rows == null || rows.Count == 0)
//                return BadRequest("No data to export.");

//            ExcelPackage.License
//                .SetNonCommercialPersonal("Alter_Parts");
//            using var package = new ExcelPackage();
//            var sheet = package.Workbook.Worksheets
//                .Add("BOM Verify Results");

//            // ✅ Updated headers with all 3 sources
//            string[] hdrs = {
//        "Row #", "Part Number", "Your Description",
//        "Best Source", "Best Match %", "Overall Verdict",
//        "DigiKey Description", "DigiKey Match %",
//        "DigiKey Verdict",
//        "Mouser Description",  "Mouser Match %",
//        "Mouser Verdict",
//        "LCSC Description",    "LCSC Match %",
//        "LCSC Verdict",
//        "Manufacturer", "Category", "Status"
//    };

//            for (int i = 0; i < hdrs.Length; i++)
//            {
//                sheet.Cells[1, i + 1].Value = hdrs[i];
//                sheet.Cells[1, i + 1].Style.Font.Bold = true;
//                sheet.Cells[1, i + 1].Style.Fill.PatternType =
//                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
//                sheet.Cells[1, i + 1].Style.Fill.BackgroundColor
//                    .SetColor(System.Drawing.Color.FromArgb(28, 57, 107));
//                sheet.Cells[1, i + 1].Style.Font.Color
//                    .SetColor(System.Drawing.Color.White);
//            }

//            for (int i = 0; i < rows.Count; i++)
//            {
//                var row = rows[i];
//                int excelRow = i + 2;

//                sheet.Cells[excelRow, 1].Value = row.RowNumber;
//                sheet.Cells[excelRow, 2].Value = row.PartNumber;
//                sheet.Cells[excelRow, 3].Value = row.UserDescription;
//                sheet.Cells[excelRow, 4].Value = row.BestSource;
//                sheet.Cells[excelRow, 5].Value = $"{row.MatchScore}%";
//                sheet.Cells[excelRow, 6].Value = row.MatchVerdict;
//                sheet.Cells[excelRow, 7].Value = row.DigiKeyDescription;
//                sheet.Cells[excelRow, 8].Value = $"{row.DigiKeyScore}%";
//                sheet.Cells[excelRow, 9].Value = row.DigiKeyVerdict;
//                sheet.Cells[excelRow, 10].Value = row.MouserDescription;
//                sheet.Cells[excelRow, 11].Value = $"{row.MouserScore}%";
//                sheet.Cells[excelRow, 12].Value = row.MouserVerdict;
//                sheet.Cells[excelRow, 13].Value = row.LCSCDescription;
//                sheet.Cells[excelRow, 14].Value = $"{row.LCSCScore}%";
//                sheet.Cells[excelRow, 15].Value = row.LCSCVerdict;
//                sheet.Cells[excelRow, 16].Value = row.Manufacturer;
//                sheet.Cells[excelRow, 17].Value = row.Category;
//                sheet.Cells[excelRow, 18].Value = row.Status;

//                // Color code overall verdict
//                var color =
//                    row.MatchVerdict?.StartsWith("✅") == true
//                    ? System.Drawing.Color.FromArgb(198, 239, 206)
//                    : row.MatchVerdict?.StartsWith("❌") == true
//                    ? System.Drawing.Color.FromArgb(255, 199, 206)
//                    : System.Drawing.Color.FromArgb(255, 235, 156);

//                sheet.Cells[excelRow, 6].Style.Fill.PatternType =
//                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
//                sheet.Cells[excelRow, 6].Style.Fill.BackgroundColor
//                    .SetColor(color);

//                // Color DigiKey verdict
//                var dkColor =
//                    row.DigiKeyVerdict?.StartsWith("✅") == true
//                    ? System.Drawing.Color.FromArgb(198, 239, 206)
//                    : row.DigiKeyVerdict?.StartsWith("❌") == true
//                    ? System.Drawing.Color.FromArgb(255, 199, 206)
//                    : System.Drawing.Color.FromArgb(255, 235, 156);
//                sheet.Cells[excelRow, 9].Style.Fill.PatternType =
//                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
//                sheet.Cells[excelRow, 9].Style.Fill.BackgroundColor
//                    .SetColor(dkColor);

//                // Color Mouser verdict
//                var mouserColor =
//                    row.MouserVerdict?.StartsWith("✅") == true
//                    ? System.Drawing.Color.FromArgb(198, 239, 206)
//                    : row.MouserVerdict?.StartsWith("❌") == true
//                    ? System.Drawing.Color.FromArgb(255, 199, 206)
//                    : System.Drawing.Color.FromArgb(255, 235, 156);
//                sheet.Cells[excelRow, 12].Style.Fill.PatternType =
//                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
//                sheet.Cells[excelRow, 12].Style.Fill.BackgroundColor
//                    .SetColor(mouserColor);

//                // Color LCSC verdict
//                var lcscColor =
//                    row.LCSCVerdict?.StartsWith("✅") == true
//                    ? System.Drawing.Color.FromArgb(198, 239, 206)
//                    : row.LCSCVerdict?.StartsWith("❌") == true
//                    ? System.Drawing.Color.FromArgb(255, 199, 206)
//                    : System.Drawing.Color.FromArgb(255, 235, 156);
//                sheet.Cells[excelRow, 15].Style.Fill.PatternType =
//                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
//                sheet.Cells[excelRow, 15].Style.Fill.BackgroundColor
//                    .SetColor(lcscColor);
//            }

//            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
//            var fileBytes = package.GetAsByteArray();
//            return File(fileBytes,
//                "application/vnd.openxmlformats-officedocument" +
//                ".spreadsheetml.sheet",
//                $"BOM_Verify_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
//        }

//        // ── Helper: find column ───────────────────────────────────
//        private static int FindColumn(
//            Dictionary<string, int> headers,
//            params string[] names)
//        {
//            foreach (var name in names)
//                if (headers.TryGetValue(name, out int col))
//                    return col;
//            return -1;
//        }
//    }
//}s








using Alter_Parts.Models;
using Alter_Parts.Services;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Alter_Parts.Controllers
{
    public class VerifyController : Controller
    {
        private readonly DigiKeyService _digikey;
        private readonly MouserService _mouser;
        private readonly LCSCService _lcsc;

        // 💡 Nexar — uncomment when ready
        // private readonly NexarService _nexar;

        public VerifyController(DigiKeyService digikey,
                                MouserService mouser,
                                LCSCService lcsc)
        {
            _digikey = digikey;
            _mouser = mouser;
            _lcsc = lcsc;
            // _nexar = nexar;
        }

        // ── GET: /Verify ──────────────────────────────────────────
        [HttpGet]
        public IActionResult Index() => View(new VerifyRequest());

        // ── POST: /Verify ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Index(
            VerifyRequest request)
        {
            if (!ModelState.IsValid) return View(request);

            var result = new VerifyResult
            {
                PartNumber = request.PartNumber.Trim(),
                UserDescription = request.Description.Trim()
            };

            // ── Fetch from DigiKey ────────────────────────────────
            try
            {
                var dkData = await _digikey
                    .GetPartDetails(request.PartNumber);
                if (dkData != null)
                {
                    result.DigiKeyResult = new VerifySource
                    {
                        Source = "DigiKey",
                        FetchedDescription = dkData.Description,
                        Manufacturer = dkData.Manufacturer,
                        Category = dkData.Category,
                        Package = dkData.Specs.TryGetValue(
                            "Package / Case", out var pkg) ? pkg :
                            dkData.Specs.TryGetValue(
                            "Supplier Device Package", out var spkg)
                            ? spkg : "N/A",
                        DatasheetUrl = dkData.DatasheetUrl,
                        ProductUrl = dkData.ProductUrl,
                        Stock = dkData.Stock,
                        Specs = dkData.Specs,
                        MatchVerdict = GetMatchVerdict(
                            request.Description, dkData.Description)
                    };
                }
                else
                {
                    result.DigiKeyResult = new VerifySource
                    {
                        Source = "DigiKey",
                        MatchVerdict =
                            "❌ Part not found on DigiKey",
                        Specs = new Dictionary<string, string>()
                    };
                }
            }
            catch (Exception ex)
            {
                result.DigiKeyResult = new VerifySource
                {
                    Source = "DigiKey",
                    MatchVerdict = $"❌ Error: {ex.Message}",
                    Specs = new Dictionary<string, string>()
                };
            }

            // ── Fetch from Mouser ─────────────────────────────────
            try
            {
                var mouserData = await _mouser
                    .GetPartDetails(request.PartNumber);
                if (mouserData != null)
                {
                    result.MouserResult = new VerifySource
                    {
                        Source = "Mouser",
                        FetchedDescription = mouserData.Description,
                        Manufacturer = mouserData.Manufacturer,
                        Category = mouserData.Category,
                        Package = mouserData.Specs.TryGetValue(
                            "Package / Case", out var pkg) ? pkg :
                            mouserData.Specs.TryGetValue(
                            "Case/Package", out var cpkg)
                            ? cpkg : "N/A",
                        DatasheetUrl = mouserData.DatasheetUrl,
                        ProductUrl = mouserData.ProductUrl,
                        Stock = mouserData.Stock,
                        Specs = mouserData.Specs,
                        MatchVerdict = GetMatchVerdict(
                            request.Description,
                            mouserData.Description)
                    };
                }
                else
                {
                    result.MouserResult = new VerifySource
                    {
                        Source = "Mouser",
                        MatchVerdict =
                            "❌ Part not found on Mouser",
                        Specs = new Dictionary<string, string>()
                    };
                }
            }
            catch (Exception ex)
            {
                result.MouserResult = new VerifySource
                {
                    Source = "Mouser",
                    MatchVerdict = $"❌ Error: {ex.Message}",
                    Specs = new Dictionary<string, string>()
                };
            }

            // ── Fetch from LCSC ───────────────────────────────────
            try
            {
                var lcscResults = await _lcsc
                    .SearchByKeyword(request.PartNumber, limit: 1);
                var lcscData = lcscResults.FirstOrDefault();
                if (lcscData != null)
                {
                    result.LCSCResult = new VerifySource
                    {
                        Source = "LCSC",
                        FetchedDescription = lcscData.Description,
                        Manufacturer = lcscData.Manufacturer,
                        Category = lcscData.Category,
                        Package = lcscData.Package ?? "N/A",
                        DatasheetUrl = lcscData.DatasheetUrl,
                        ProductUrl = lcscData.ProductUrl,
                        Stock = lcscData.Stock,
                        Specs = new Dictionary<string, string>
                        {
                            ["Package"] = lcscData.Package
                                           ?? "",
                            ["LCSC Part No"] = lcscData.LcscPartNumber
                                               ?? "",
                            ["MPN"] = lcscData.MpnNumber
                                       ?? "",
                            ["Price"] = lcscData.Price ?? ""
                        },
                        MatchVerdict = GetMatchVerdict(
                            request.Description,
                            lcscData.Description)
                    };
                }
                else
                {
                    result.LCSCResult = new VerifySource
                    {
                        Source = "LCSC",
                        MatchVerdict = "❌ Part not found on LCSC",
                        Specs = new Dictionary<string, string>()
                    };
                }
            }
            catch (Exception ex)
            {
                result.LCSCResult = new VerifySource
                {
                    Source = "LCSC",
                    MatchVerdict = $"❌ Error: {ex.Message}",
                    Specs = new Dictionary<string, string>()
                };
            }

            result.OverallVerdict = GetOverallVerdict(result);
            return View("Result", result);
        }

        // ── Match Logic ───────────────────────────────────────────
        private string GetMatchVerdict(string userDesc,
            string fetchedDesc)
        {
            if (string.IsNullOrWhiteSpace(fetchedDesc))
                return "⚠️ No description available";

            var user = userDesc.ToLower().Trim();
            var fetched = fetchedDesc.ToLower().Trim();

            if (user == fetched) return "✅ Exact Match";

            var userWords = user.Split(' ',
                StringSplitOptions.RemoveEmptyEntries);
            var matchedWords = userWords.Count(w =>
                fetched.Contains(w));
            var matchPercent =
                (double)matchedWords / userWords.Length * 100;

            if (matchPercent >= 80)
                return $"✅ Strong Match " +
                       $"({matchPercent:0}% keywords matched)";
            if (matchPercent >= 50)
                return $"⚠️ Partial Match " +
                       $"({matchPercent:0}% keywords matched)";
            if (matchPercent >= 20)
                return $"⚠️ Weak Match " +
                       $"({matchPercent:0}% keywords matched)";

            return
                $"❌ No Match ({matchPercent:0}% keywords matched)";
        }

        // ── Overall verdict ───────────────────────────────────────
        private string GetOverallVerdict(VerifyResult result)
        {
            bool dkMatch = result.DigiKeyResult?.MatchVerdict
                                     ?.StartsWith("✅") == true;
            bool mouserMatch = result.MouserResult?.MatchVerdict
                                     ?.StartsWith("✅") == true;
            bool lcscMatch = result.LCSCResult?.MatchVerdict
                                     ?.StartsWith("✅") == true;

            int matchCount = new[] { dkMatch, mouserMatch, lcscMatch }
                .Count(m => m);

            if (matchCount == 3)
                return "✅ Verified — DigiKey, Mouser and LCSC " +
                       "all confirm this description matches.";
            if (matchCount == 2)
                return "✅ Verified — 2 out of 3 sources confirm " +
                       "this description matches.";
            if (matchCount == 1)
                return "⚠️ Partially Verified — Only 1 source " +
                       "confirms. Check the others manually.";

            return "❌ Not Verified — No source matches " +
                   "the description you entered.";
        }

        // ── GET: /Verify/Compare ──────────────────────────────────
        [HttpGet]
        public IActionResult Compare() => View(new CompareRequest());

        // ── POST: /Verify/Compare ─────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Compare(
            CompareRequest request)
        {
            if (!ModelState.IsValid) return View(request);

            var result = new CompareResult
            {
                OriginalPart = request.OriginalPart.Trim(),
                AlternatePart = request.AlternatePart.Trim(),
                UserDescription = request.Description.Trim()
            };

            result.OriginalDetails =
                await FetchBestDetails(request.OriginalPart.Trim());
            result.AlternateDetails =
                await FetchBestDetails(request.AlternatePart.Trim());

            result.OriginalMatchScore = CalculateMatchScore(
                request.Description,
                result.OriginalDetails?.FetchedDescription);
            result.AlternateMatchScore = CalculateMatchScore(
                request.Description,
                result.AlternateDetails?.FetchedDescription);

            result.SpecComparisons = BuildSpecComparison(
                result.OriginalDetails?.Specs,
                result.AlternateDetails?.Specs);

            DetermineVerdict(result);
            return View("CompareResult", result);
        }

        // ── GET: /Verify/DescriptionSearch ────────────────────────
        [HttpGet]
        public IActionResult DescriptionSearch()
            => View(new DescriptionSearchViewModel());

        // ── POST: /Verify/DescriptionSearch ───────────────────────
        [HttpPost]
        public async Task<IActionResult> DescriptionSearch(
            DescriptionSearchRequest request)
        {
            var vm = new DescriptionSearchViewModel
            {
                Description = request.Description
            };

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                vm.Error = "Please enter a description.";
                return View(vm);
            }

            // ── Run all 3 simultaneously ──────────────────────────
            var digikeyTask = Task.Run(async () =>
            {
                try
                {
                    return await _digikey.SearchByDescription(
                        request.Description, request.Limit);
                }
                catch { return new List<PartDetails>(); }
            });

            var mouserTask = Task.Run(async () =>
            {
                try
                {
                    return await _mouser.SearchByDescription(
                        request.Description, request.Limit);
                }
                catch { return new List<PartDetails>(); }
            });

            var lcscTask = Task.Run(async () =>
            {
                try
                {
                    var lcscResults = await _lcsc.SearchByKeyword(
                        request.Description, request.Limit);
                    return lcscResults.Select(r => new PartDetails
                    {
                        Mpn = r.MpnNumber,
                        Description = r.Description,
                        Manufacturer = r.Manufacturer,
                        Category = r.Category,
                        DatasheetUrl = r.DatasheetUrl,
                        ProductUrl = r.ProductUrl,
                        Stock = r.Stock,
                        Source = "LCSC",
                        Specs = new Dictionary<string, string>
                        {
                            ["Package"] = r.Package ?? "",
                            ["LCSC Part No"] = r.LcscPartNumber ?? "",
                            ["Unit Price"] = r.Price ?? "N/A",
                            ["Match Score"] = $"{r.MatchScore}%"
                        }
                    }).ToList();
                }
                catch { return new List<PartDetails>(); }
            });

            await Task.WhenAll(digikeyTask, mouserTask, lcscTask);

            vm.DigiKeyResults = await digikeyTask;
            vm.MouserResults = await mouserTask;
            vm.LCSCResults = await lcscTask;
            vm.DigiKeyTotal = vm.DigiKeyResults.Count;
            vm.MouserTotal = vm.MouserResults.Count;
            vm.LCSCTotal = vm.LCSCResults.Count;

            if (!vm.DigiKeyResults.Any() &&
                !vm.MouserResults.Any() &&
                !vm.LCSCResults.Any())
                vm.Error =
                    "No parts found. Try different keywords.";

            return View(vm);
        }

        // ── FetchBestDetails ──────────────────────────────────────
        // DigiKey → Mouser → LCSC fallback chain
        private async Task<VerifySource> FetchBestDetails(
            string mpn)
        {
            // ── PRIMARY: DigiKey ──────────────────────────────────
            try
            {
                var data = await _digikey.GetPartDetails(mpn);
                if (data != null)
                    return new VerifySource
                    {
                        Source = "DigiKey",
                        FetchedDescription = data.Description,
                        Manufacturer = data.Manufacturer,
                        Category = data.Category,
                        Package = data.Specs.TryGetValue(
                            "Package / Case", out var pkg) ? pkg :
                            data.Specs.TryGetValue(
                            "Supplier Device Package", out var spkg)
                            ? spkg : "N/A",
                        DatasheetUrl = data.DatasheetUrl,
                        ProductUrl = data.ProductUrl,
                        Stock = data.Stock,
                        Specs = data.Specs,
                        MatchVerdict = ""
                    };
            }
            catch { }

            // ── FALLBACK: Mouser ──────────────────────────────────
            try
            {
                var data = await _mouser.GetPartDetails(mpn);
                if (data != null)
                    return new VerifySource
                    {
                        Source = "Mouser",
                        FetchedDescription = data.Description,
                        Manufacturer = data.Manufacturer,
                        Category = data.Category,
                        Package = data.Specs.TryGetValue(
                            "Case/Package", out var pkg)
                            ? pkg : "N/A",
                        DatasheetUrl = data.DatasheetUrl,
                        ProductUrl = data.ProductUrl,
                        Stock = data.Stock,
                        Specs = data.Specs,
                        MatchVerdict = ""
                    };
            }
            catch { }

            // ── FALLBACK: LCSC ────────────────────────────────────
            try
            {
                var results = await _lcsc
                    .SearchByKeyword(mpn, limit: 1);
                var data = results.FirstOrDefault();
                if (data != null)
                    return new VerifySource
                    {
                        Source = "LCSC",
                        FetchedDescription = data.Description,
                        Manufacturer = data.Manufacturer,
                        Category = data.Category,
                        Package = data.Package ?? "N/A",
                        DatasheetUrl = data.DatasheetUrl,
                        ProductUrl = data.ProductUrl,
                        Stock = data.Stock,
                        Specs = new Dictionary<string, string>
                        {
                            ["Package"] = data.Package ?? "",
                            ["LCSC Part No"] = data.LcscPartNumber ?? "",
                            ["MPN"] = data.MpnNumber ?? "",
                            ["Price"] = data.Price ?? ""
                        },
                        MatchVerdict = ""
                    };
            }
            catch { }

            return new VerifySource
            {
                Source = "Not Found",
                FetchedDescription = "",
                Specs = new Dictionary<string, string>()
            };
        }

        // ── Calculate match score ─────────────────────────────────
        private double CalculateMatchScore(string userDesc,
            string fetchedDesc)
        {
            if (string.IsNullOrWhiteSpace(fetchedDesc)) return 0;

            var userWords = userDesc.ToLower().Split(' ',
                StringSplitOptions.RemoveEmptyEntries);
            var fetchedLower = fetchedDesc.ToLower();
            int matched = userWords.Count(w =>
                fetchedLower.Contains(w));
            return Math.Round(
                (double)matched / userWords.Length * 100, 1);
        }

        // ── Build spec comparison table ───────────────────────────
        private List<SpecComparison> BuildSpecComparison(
            Dictionary<string, string> origSpecs,
            Dictionary<string, string> altSpecs)
        {
            var comparisons = new List<SpecComparison>();
            origSpecs ??= new Dictionary<string, string>();
            altSpecs ??= new Dictionary<string, string>();

            var prioritySpecs = new[]
            {
                "Case/Package", "Number of Pins",
                "Supply Voltage", "Operating Temperature",
                "Mounting Style", "Output Current",
                "Power Rating", "Technology", "Part Status"
            };

            var allKeys = origSpecs.Keys.Union(altSpecs.Keys)
                .OrderBy(k =>
                    Array.IndexOf(prioritySpecs, k) >= 0
                    ? Array.IndexOf(prioritySpecs, k) : 999)
                .ToList();

            foreach (var key in allKeys)
            {
                origSpecs.TryGetValue(key, out var origVal);
                altSpecs.TryGetValue(key, out var altVal);

                string status;
                if (!string.IsNullOrEmpty(origVal) &&
                    !string.IsNullOrEmpty(altVal))
                    status = origVal.Equals(altVal,
                        StringComparison.OrdinalIgnoreCase)
                        ? "Match" : "Mismatch";
                else if (!string.IsNullOrEmpty(origVal))
                    status = "Only Original";
                else
                    status = "Only Alternate";

                comparisons.Add(new SpecComparison
                {
                    SpecName = key,
                    OriginalValue = origVal ?? "—",
                    AlternateValue = altVal ?? "—",
                    Status = status
                });
            }
            return comparisons;
        }

        // ── Determine final verdict ───────────────────────────────
        private void DetermineVerdict(CompareResult result)
        {
            bool origFound =
                result.OriginalDetails?.Source != "Not Found";
            bool altFound =
                result.AlternateDetails?.Source != "Not Found";

            if (!origFound && !altFound)
            {
                result.Verdict = "❌ Cannot Verify";
                result.VerdictReason = "Neither part was found.";
                result.RecommendedPart = "Unknown";
                return;
            }

            int criticalMismatches = result.SpecComparisons
                .Count(s => s.Status == "Mismatch" &&
                    (s.SpecName.Contains("Package") ||
                     s.SpecName.Contains("Pin")));
            int totalMismatches = result.SpecComparisons
                .Count(s => s.Status == "Mismatch");
            int totalMatches = result.SpecComparisons
                .Count(s => s.Status == "Match");
            double origScore = result.OriginalMatchScore;
            double altScore = result.AlternateMatchScore;

            if (criticalMismatches > 0)
            {
                result.Verdict =
                    "⚠️ Use Original Part Only";
                result.VerdictReason =
                    $"Alternate has {criticalMismatches} critical " +
                    "spec mismatch(es) (Package/Pins). " +
                    "It may not fit the board.";
                result.RecommendedPart = "Original";
                return;
            }

            if (totalMatches > totalMismatches && altScore >= 50)
            {
                result.Verdict =
                    "✅ Alternate Part is Okay to Use";
                result.VerdictReason =
                    $"Specs match in {totalMatches} of " +
                    $"{totalMatches + totalMismatches} parameters. " +
                    $"Description match: {altScore}%.";
                result.RecommendedPart = "Alternate";
                return;
            }

            if (altScore > origScore && altScore >= 60)
            {
                result.Verdict =
                    "✅ Alternate Part is Okay to Use";
                result.VerdictReason =
                    $"Alternate matches better " +
                    $"({altScore}% vs {origScore}%).";
                result.RecommendedPart = "Alternate";
                return;
            }

            if (origScore > altScore && origScore >= 60)
            {
                result.Verdict = "⚠️ Use Original Part";
                result.VerdictReason =
                    $"Original matches better " +
                    $"({origScore}% vs {altScore}%).";
                result.RecommendedPart = "Original";
                return;
            }

            if (altScore >= 40 && origScore >= 40 &&
                totalMismatches <= 2)
            {
                result.Verdict = "✅ Either Part Can Be Used";
                result.VerdictReason =
                    $"Both match reasonably. " +
                    $"Original: {origScore}%, " +
                    $"Alternate: {altScore}%.";
                result.RecommendedPart = "Either";
                return;
            }

            result.Verdict = "⚠️ Check Manually";
            result.VerdictReason =
                $"Insufficient data. " +
                $"Original: {origScore}%, Alternate: {altScore}%.";
            result.RecommendedPart = "Unknown";
        }

        // ── GET: /Verify/BomVerify ────────────────────────────────
        [HttpGet]
        public IActionResult BomVerify() => View();

        // ── POST: /Verify/BomVerify ───────────────────────────────
        [HttpPost]
        public async Task<IActionResult> BomVerify(IFormFile bomFile)
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

                ExcelPackage.License
                    .SetNonCommercialPersonal("Alter_Parts");
                using var package = new ExcelPackage(stream);
                var sheet = package.Workbook.Worksheets[0];

                if (sheet.Dimension == null)
                {
                    ViewBag.Error = "The Excel sheet is empty.";
                    return View();
                }

                // Detect columns
                var headers = new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase);
                for (int col = 1;
                    col <= sheet.Dimension.End.Column; col++)
                {
                    var header = sheet.Cells[1, col].Text?.Trim();
                    if (!string.IsNullOrEmpty(header))
                        headers[header] = col;
                }

                int colPart = FindColumn(headers,
                    "part number", "part no", "mpn",
                    "part", "original part", "component");
                int colDesc = FindColumn(headers,
                    "description", "desc",
                    "part description", "component description");

                if (colPart == -1 || colDesc == -1)
                {
                    ViewBag.Error =
                        $"Could not find required columns. " +
                        $"Found: {string.Join(", ", headers.Keys)}. " +
                        "Need: Part Number and Description.";
                    return View();
                }

                var result = new BomVerifyResult
                {
                    FileName = bomFile.FileName,
                    TotalRows = sheet.Dimension.End.Row - 1
                };

                for (int row = 2; row <= sheet.Dimension.End.Row; row++)
                {
                    // 🚨 FIX 1: Use .Value?.ToString() instead of .Text to prevent the "######" bug!
                    var partNumber = sheet.Cells[row, colPart].Value?.ToString()?.Trim() ?? "";
                    var userDesc = sheet.Cells[row, colDesc].Value?.ToString()?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(partNumber) && string.IsNullOrWhiteSpace(userDesc))
                        continue;

                    var bomRow = new BomVerifyRow
                    {
                        RowNumber = row,
                        PartNumber = partNumber,
                        UserDescription = userDesc,
                        Status = "Pending"
                    };

                    if (string.IsNullOrWhiteSpace(partNumber))
                    {
                        bomRow.MatchVerdict = "⚠️ No part number";
                        bomRow.Status = "Skipped";
                        result.Rows.Add(bomRow);
                        continue;
                    }

                    try
                    {
                        // 🚨 FIX 2: Run sequentially to prevent thread crashing and network socket exhaustion.

                        // 1. DigiKey
                        PartDetails dkData = null;
                        try { dkData = await _digikey.GetPartDetails(partNumber); }
                        catch { /* Ignore */ }

                        // 2. Mouser
                        PartDetails mouserData = null;
                        try { mouserData = await _mouser.GetPartDetails(partNumber); }
                        catch { /* Ignore */ }

                        // 3. LCSC
                        LCSCPartResult lcscData = null;
                        try
                        {
                            var lcscResults = await _lcsc.SearchByKeyword(partNumber, limit: 1);
                            lcscData = lcscResults.FirstOrDefault();
                        }
                        catch { /* Ignore */ }

                        // ── DigiKey result ────────────────────────────
                        if (dkData != null)
                        {
                            bomRow.DigiKeyDescription = dkData.Description;
                            bomRow.DigiKeyScore = string.IsNullOrWhiteSpace(userDesc) ? 0 : CalculateMatchScore(userDesc, dkData.Description);
                            bomRow.DigiKeyVerdict = string.IsNullOrWhiteSpace(userDesc) ? "⚠️ No description" : GetMatchVerdict(userDesc, dkData.Description);
                        }
                        else
                        {
                            bomRow.DigiKeyVerdict = "❌ Not found";
                        }

                        // ── Mouser result ─────────────────────────────
                        if (mouserData != null)
                        {
                            bomRow.MouserDescription = mouserData.Description;
                            bomRow.MouserScore = string.IsNullOrWhiteSpace(userDesc) ? 0 : CalculateMatchScore(userDesc, mouserData.Description);
                            bomRow.MouserVerdict = string.IsNullOrWhiteSpace(userDesc) ? "⚠️ No description" : GetMatchVerdict(userDesc, mouserData.Description);
                        }
                        else
                        {
                            bomRow.MouserVerdict = "❌ Not found";
                        }

                        // ── LCSC result ───────────────────────────────
                        if (lcscData != null)
                        {
                            bomRow.LCSCDescription = lcscData.Description;
                            bomRow.LCSCScore = string.IsNullOrWhiteSpace(userDesc) ? 0 : CalculateMatchScore(userDesc, lcscData.Description);
                            bomRow.LCSCVerdict = string.IsNullOrWhiteSpace(userDesc) ? "⚠️ No description" : GetMatchVerdict(userDesc, lcscData.Description);
                        }
                        else
                        {
                            bomRow.LCSCVerdict = "❌ Not found";
                        }

                        // ── Pick best source ──────────────────────────
                        var scores = new[]
                        {
                            ("DigiKey", bomRow.DigiKeyScore, bomRow.DigiKeyDescription),
                            ("Mouser",  bomRow.MouserScore, bomRow.MouserDescription),
                            ("LCSC",    bomRow.LCSCScore, bomRow.LCSCDescription)
                        }
                        .Where(x => !string.IsNullOrEmpty(x.Item3))
                        .OrderByDescending(x => x.Item2)
                        .FirstOrDefault();

                        if (scores != default)
                        {
                            bomRow.BestSource = scores.Item1;
                            bomRow.Source = scores.Item1;
                            bomRow.FetchedDescription = scores.Item3;
                            bomRow.MatchScore = scores.Item2;
                            bomRow.MatchVerdict = string.IsNullOrWhiteSpace(userDesc)
                                ? "⚠️ No description to verify"
                                : GetMatchVerdict(userDesc, scores.Item3);

                            if (scores.Item1 == "DigiKey" && dkData != null)
                            {
                                bomRow.Manufacturer = dkData.Manufacturer;
                                bomRow.Category = dkData.Category;
                            }
                            else if (scores.Item1 == "Mouser" && mouserData != null)
                            {
                                bomRow.Manufacturer = mouserData.Manufacturer;
                                bomRow.Category = mouserData.Category;
                            }
                            else if (scores.Item1 == "LCSC" && lcscData != null)
                            {
                                bomRow.Manufacturer = lcscData.Manufacturer;
                                bomRow.Category = lcscData.Category;
                            }
                        }
                        else
                        {
                            bomRow.BestSource = "Not Found";
                            bomRow.Source = "Not Found";
                            bomRow.MatchVerdict = "❌ Part not found";
                            bomRow.MatchScore = 0;
                        }

                        bomRow.Status = "Done";
                    }
                    catch (Exception ex)
                    {
                        bomRow.MatchVerdict = $"❌ System Error";
                        bomRow.Status = "Error";
                    }

                    result.Rows.Add(bomRow);

                    // 🚨 FIX 3: Rate Limiting Delay
                    await Task.Delay(1000);
                }

                result.MatchedCount = result.Rows.Count(r =>
                    r.MatchVerdict?.StartsWith("✅") == true);
                result.NotMatchedCount = result.Rows.Count(r =>
                    r.MatchVerdict?.StartsWith("❌") == true);
                result.ManualCheckCount = result.Rows.Count(r =>
                    r.MatchVerdict?.StartsWith("⚠️") == true);
                result.NotFoundCount = result.Rows.Count(r =>
                    r.MatchVerdict == "❌ Part not found");

                return View("BomVerifyResult", result);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Failed to process file: " + ex.Message;
                return View();
            }
        }

        // ── POST: /Verify/ExportVerifyResults ─────────────────────

        [HttpPost]
        public IActionResult ExportVerifyResults(string resultsJson)
        {
            var rows = JsonSerializer
                .Deserialize<List<BomVerifyRow>>(resultsJson);
            if (rows == null || rows.Count == 0)
                return BadRequest("No data to export.");

            ExcelPackage.License
                .SetNonCommercialPersonal("Alter_Parts");
            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets
                .Add("BOM Verify Results");

            string[] hdrs = {
                "Row #", "Part Number", "Your Description",
                "Best Source", "Best Match %", "Overall Verdict",
                "DigiKey Description", "DigiKey Match %",
                "DigiKey Verdict",
                "Mouser Description",  "Mouser Match %",
                "Mouser Verdict",
                "LCSC Description",    "LCSC Match %",
                "LCSC Verdict",
                "Manufacturer", "Category", "Status"
            };

            for (int i = 0; i < hdrs.Length; i++)
            {
                sheet.Cells[1, i + 1].Value = hdrs[i];
                sheet.Cells[1, i + 1].Style.Font.Bold = true;
                sheet.Cells[1, i + 1].Style.Fill.PatternType =
                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[1, i + 1].Style.Fill.BackgroundColor
                    .SetColor(System.Drawing.Color.FromArgb(28, 57, 107));
                sheet.Cells[1, i + 1].Style.Font.Color
                    .SetColor(System.Drawing.Color.White);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                int excelRow = i + 2;

                sheet.Cells[excelRow, 1].Value = row.RowNumber;
                sheet.Cells[excelRow, 2].Value = row.PartNumber;
                sheet.Cells[excelRow, 3].Value = row.UserDescription;
                sheet.Cells[excelRow, 4].Value = row.BestSource;
                sheet.Cells[excelRow, 5].Value = $"{row.MatchScore}%";
                sheet.Cells[excelRow, 6].Value = row.MatchVerdict;
                sheet.Cells[excelRow, 7].Value = row.DigiKeyDescription;
                sheet.Cells[excelRow, 8].Value = $"{row.DigiKeyScore}%";
                sheet.Cells[excelRow, 9].Value = row.DigiKeyVerdict;
                sheet.Cells[excelRow, 10].Value = row.MouserDescription;
                sheet.Cells[excelRow, 11].Value = $"{row.MouserScore}%";
                sheet.Cells[excelRow, 12].Value = row.MouserVerdict;
                sheet.Cells[excelRow, 13].Value = row.LCSCDescription;
                sheet.Cells[excelRow, 14].Value = $"{row.LCSCScore}%";
                sheet.Cells[excelRow, 15].Value = row.LCSCVerdict;
                sheet.Cells[excelRow, 16].Value = row.Manufacturer;
                sheet.Cells[excelRow, 17].Value = row.Category;
                sheet.Cells[excelRow, 18].Value = row.Status;

                // Color code overall verdict
                var color =
                    row.MatchVerdict?.StartsWith("✅") == true
                    ? System.Drawing.Color.FromArgb(198, 239, 206)
                    : row.MatchVerdict?.StartsWith("❌") == true
                    ? System.Drawing.Color.FromArgb(255, 199, 206)
                    : System.Drawing.Color.FromArgb(255, 235, 156);

                sheet.Cells[excelRow, 6].Style.Fill.PatternType =
                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[excelRow, 6].Style.Fill.BackgroundColor
                    .SetColor(color);

                // Color DigiKey verdict
                var dkColor =
                    row.DigiKeyVerdict?.StartsWith("✅") == true
                    ? System.Drawing.Color.FromArgb(198, 239, 206)
                    : row.DigiKeyVerdict?.StartsWith("❌") == true
                    ? System.Drawing.Color.FromArgb(255, 199, 206)
                    : System.Drawing.Color.FromArgb(255, 235, 156);
                sheet.Cells[excelRow, 9].Style.Fill.PatternType =
                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[excelRow, 9].Style.Fill.BackgroundColor
                    .SetColor(dkColor);

                // Color Mouser verdict
                var mouserColor =
                    row.MouserVerdict?.StartsWith("✅") == true
                    ? System.Drawing.Color.FromArgb(198, 239, 206)
                    : row.MouserVerdict?.StartsWith("❌") == true
                    ? System.Drawing.Color.FromArgb(255, 199, 206)
                    : System.Drawing.Color.FromArgb(255, 235, 156);
                sheet.Cells[excelRow, 12].Style.Fill.PatternType =
                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[excelRow, 12].Style.Fill.BackgroundColor
                    .SetColor(mouserColor);

                // Color LCSC verdict
                var lcscColor =
                    row.LCSCVerdict?.StartsWith("✅") == true
                    ? System.Drawing.Color.FromArgb(198, 239, 206)
                    : row.LCSCVerdict?.StartsWith("❌") == true
                    ? System.Drawing.Color.FromArgb(255, 199, 206)
                    : System.Drawing.Color.FromArgb(255, 235, 156);
                sheet.Cells[excelRow, 15].Style.Fill.PatternType =
                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[excelRow, 15].Style.Fill.BackgroundColor
                    .SetColor(lcscColor);
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
            var fileBytes = package.GetAsByteArray();
            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument" +
                ".spreadsheetml.sheet",
                $"BOM_Verify_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // ── Helper: find column ───────────────────────────────────
        private static int FindColumn(
            Dictionary<string, int> headers,
            params string[] names)
        {
            foreach (var name in names)
                if (headers.TryGetValue(name, out int col))
                    return col;
            return -1;
        }
    }
}











//using Alter_Parts.Models;
//using Alter_Parts.Services;
//using Microsoft.AspNetCore.Mvc;
//using OfficeOpenXml;
//using System.Text.Json;
//using System.Text.RegularExpressions; // ✅ Added for aggressive data sanitization

//namespace Alter_Parts.Controllers
//{
//    public class VerifyController : Controller
//    {
//        private readonly DigiKeyService _digikey;
//        private readonly MouserService _mouser;
//        private readonly LCSCService _lcsc;

//        // 💡 Nexar — uncomment when ready
//        // private readonly NexarService _nexar;

//        public VerifyController(DigiKeyService digikey,
//                                MouserService mouser,
//                                LCSCService lcsc)
//        {
//            _digikey = digikey;
//            _mouser = mouser;
//            _lcsc = lcsc;
//            // _nexar = nexar;
//        }

//        // ── GET: /Verify ──────────────────────────────────────────
//        [HttpGet]
//        public IActionResult Index() => View(new VerifyRequest());

//        // ── POST: /Verify ─────────────────────────────────────────
//        [HttpPost]
//        public async Task<IActionResult> Index(
//            VerifyRequest request)
//        {
//            if (!ModelState.IsValid) return View(request);

//            var result = new VerifyResult
//            {
//                PartNumber = request.PartNumber.Trim(),
//                UserDescription = request.Description.Trim()
//            };

//            // ── Fetch from DigiKey ────────────────────────────────
//            try
//            {
//                var dkData = await _digikey
//                    .GetPartDetails(request.PartNumber);
//                if (dkData != null)
//                {
//                    result.DigiKeyResult = new VerifySource
//                    {
//                        Source = "DigiKey",
//                        FetchedDescription = dkData.Description,
//                        Manufacturer = dkData.Manufacturer,
//                        Category = dkData.Category,
//                        Package = dkData.Specs.TryGetValue(
//                            "Package / Case", out var pkg) ? pkg :
//                            dkData.Specs.TryGetValue(
//                            "Supplier Device Package", out var spkg)
//                            ? spkg : "N/A",
//                        DatasheetUrl = dkData.DatasheetUrl,
//                        ProductUrl = dkData.ProductUrl,
//                        Stock = dkData.Stock,
//                        Specs = dkData.Specs,
//                        MatchVerdict = GetMatchVerdict(
//                            request.Description, dkData.Description)
//                    };
//                }
//                else
//                {
//                    result.DigiKeyResult = new VerifySource
//                    {
//                        Source = "DigiKey",
//                        MatchVerdict =
//                            "❌ Part not found on DigiKey",
//                        Specs = new Dictionary<string, string>()
//                    };
//                }
//            }
//            catch (Exception ex)
//            {
//                result.DigiKeyResult = new VerifySource
//                {
//                    Source = "DigiKey",
//                    MatchVerdict = $"❌ Error: {ex.Message}",
//                    Specs = new Dictionary<string, string>()
//                };
//            }

//            // ── Fetch from Mouser ─────────────────────────────────
//            try
//            {
//                var mouserData = await _mouser
//                    .GetPartDetails(request.PartNumber);
//                if (mouserData != null)
//                {
//                    result.MouserResult = new VerifySource
//                    {
//                        Source = "Mouser",
//                        FetchedDescription = mouserData.Description,
//                        Manufacturer = mouserData.Manufacturer,
//                        Category = mouserData.Category,
//                        Package = mouserData.Specs.TryGetValue(
//                            "Package / Case", out var pkg) ? pkg :
//                            mouserData.Specs.TryGetValue(
//                            "Case/Package", out var cpkg)
//                            ? cpkg : "N/A",
//                        DatasheetUrl = mouserData.DatasheetUrl,
//                        ProductUrl = mouserData.ProductUrl,
//                        Stock = mouserData.Stock,
//                        Specs = mouserData.Specs,
//                        MatchVerdict = GetMatchVerdict(
//                            request.Description,
//                            mouserData.Description)
//                    };
//                }
//                else
//                {
//                    result.MouserResult = new VerifySource
//                    {
//                        Source = "Mouser",
//                        MatchVerdict =
//                            "❌ Part not found on Mouser",
//                        Specs = new Dictionary<string, string>()
//                    };
//                }
//            }
//            catch (Exception ex)
//            {
//                result.MouserResult = new VerifySource
//                {
//                    Source = "Mouser",
//                    MatchVerdict = $"❌ Error: {ex.Message}",
//                    Specs = new Dictionary<string, string>()
//                };
//            }

//            // ── Fetch from LCSC ───────────────────────────────────
//            try
//            {
//                var lcscResults = await _lcsc
//                    .SearchByKeyword(request.PartNumber, limit: 1);
//                var lcscData = lcscResults.FirstOrDefault();
//                if (lcscData != null)
//                {
//                    result.LCSCResult = new VerifySource
//                    {
//                        Source = "LCSC",
//                        FetchedDescription = lcscData.Description,
//                        Manufacturer = lcscData.Manufacturer,
//                        Category = lcscData.Category,
//                        Package = lcscData.Package ?? "N/A",
//                        DatasheetUrl = lcscData.DatasheetUrl,
//                        ProductUrl = lcscData.ProductUrl,
//                        Stock = lcscData.Stock,
//                        Specs = new Dictionary<string, string>
//                        {
//                            ["Package"] = lcscData.Package
//                                           ?? "",
//                            ["LCSC Part No"] = lcscData.LcscPartNumber
//                                               ?? "",
//                            ["MPN"] = lcscData.MpnNumber
//                                       ?? "",
//                            ["Price"] = lcscData.Price ?? ""
//                        },
//                        MatchVerdict = GetMatchVerdict(
//                            request.Description,
//                            lcscData.Description)
//                    };
//                }
//                else
//                {
//                    result.LCSCResult = new VerifySource
//                    {
//                        Source = "LCSC",
//                        MatchVerdict = "❌ Part not found on LCSC",
//                        Specs = new Dictionary<string, string>()
//                    };
//                }
//            }
//            catch (Exception ex)
//            {
//                result.LCSCResult = new VerifySource
//                {
//                    Source = "LCSC",
//                    MatchVerdict = $"❌ Error: {ex.Message}",
//                    Specs = new Dictionary<string, string>()
//                };
//            }

//            result.OverallVerdict = GetOverallVerdict(result);
//            return View("Result", result);
//        }

//        // ── Match Logic ───────────────────────────────────────────
//        private string GetMatchVerdict(string userDesc,
//            string fetchedDesc)
//        {
//            if (string.IsNullOrWhiteSpace(fetchedDesc))
//                return "⚠️ No description available";

//            var user = userDesc.ToLower().Trim();
//            var fetched = fetchedDesc.ToLower().Trim();

//            if (user == fetched) return "✅ Exact Match";

//            var userWords = user.Split(' ',
//                StringSplitOptions.RemoveEmptyEntries);
//            var matchedWords = userWords.Count(w =>
//                fetched.Contains(w));
//            var matchPercent =
//                (double)matchedWords / userWords.Length * 100;

//            if (matchPercent >= 80)
//                return $"✅ Strong Match " +
//                       $"({matchPercent:0}% keywords matched)";
//            if (matchPercent >= 50)
//                return $"⚠️ Partial Match " +
//                       $"({matchPercent:0}% keywords matched)";
//            if (matchPercent >= 20)
//                return $"⚠️ Weak Match " +
//                       $"({matchPercent:0}% keywords matched)";

//            return
//                $"❌ No Match ({matchPercent:0}% keywords matched)";
//        }

//        // ── Overall verdict ───────────────────────────────────────
//        private string GetOverallVerdict(VerifyResult result)
//        {
//            bool dkMatch = result.DigiKeyResult?.MatchVerdict
//                                     ?.StartsWith("✅") == true;
//            bool mouserMatch = result.MouserResult?.MatchVerdict
//                                     ?.StartsWith("✅") == true;
//            bool lcscMatch = result.LCSCResult?.MatchVerdict
//                                     ?.StartsWith("✅") == true;

//            int matchCount = new[] { dkMatch, mouserMatch, lcscMatch }
//                .Count(m => m);

//            if (matchCount == 3)
//                return "✅ Verified — DigiKey, Mouser and LCSC " +
//                       "all confirm this description matches.";
//            if (matchCount == 2)
//                return "✅ Verified — 2 out of 3 sources confirm " +
//                       "this description matches.";
//            if (matchCount == 1)
//                return "⚠️ Partially Verified — Only 1 source " +
//                       "confirms. Check the others manually.";

//            return "❌ Not Verified — No source matches " +
//                   "the description you entered.";
//        }

//        // ── GET: /Verify/Compare ──────────────────────────────────
//        [HttpGet]
//        public IActionResult Compare() => View(new CompareRequest());

//        // ── POST: /Verify/Compare ─────────────────────────────────
//        [HttpPost]
//        public async Task<IActionResult> Compare(
//            CompareRequest request)
//        {
//            if (!ModelState.IsValid) return View(request);

//            var result = new CompareResult
//            {
//                OriginalPart = request.OriginalPart.Trim(),
//                AlternatePart = request.AlternatePart.Trim(),
//                UserDescription = request.Description.Trim()
//            };

//            result.OriginalDetails =
//                await FetchBestDetails(request.OriginalPart.Trim());
//            result.AlternateDetails =
//                await FetchBestDetails(request.AlternatePart.Trim());

//            result.OriginalMatchScore = CalculateMatchScore(
//                request.Description,
//                result.OriginalDetails?.FetchedDescription);
//            result.AlternateMatchScore = CalculateMatchScore(
//                request.Description,
//                result.AlternateDetails?.FetchedDescription);

//            result.SpecComparisons = BuildSpecComparison(
//                result.OriginalDetails?.Specs,
//                result.AlternateDetails?.Specs);

//            DetermineVerdict(result);
//            return View("CompareResult", result);
//        }

//        // ── GET: /Verify/DescriptionSearch ────────────────────────
//        [HttpGet]
//        public IActionResult DescriptionSearch()
//            => View(new DescriptionSearchViewModel());

//        // ── POST: /Verify/DescriptionSearch ───────────────────────
//        [HttpPost]
//        public async Task<IActionResult> DescriptionSearch(
//            DescriptionSearchRequest request)
//        {
//            var vm = new DescriptionSearchViewModel
//            {
//                Description = request.Description
//            };

//            if (string.IsNullOrWhiteSpace(request.Description))
//            {
//                vm.Error = "Please enter a description.";
//                return View(vm);
//            }

//            // ── Run all 3 simultaneously ──────────────────────────
//            var digikeyTask = Task.Run(async () =>
//            {
//                try
//                {
//                    return await _digikey.SearchByDescription(
//                        request.Description, request.Limit);
//                }
//                catch { return new List<PartDetails>(); }
//            });

//            var mouserTask = Task.Run(async () =>
//            {
//                try
//                {
//                    return await _mouser.SearchByDescription(
//                        request.Description, request.Limit);
//                }
//                catch { return new List<PartDetails>(); }
//            });

//            var lcscTask = Task.Run(async () =>
//            {
//                try
//                {
//                    var lcscResults = await _lcsc.SearchByKeyword(
//                        request.Description, request.Limit);
//                    return lcscResults.Select(r => new PartDetails
//                    {
//                        Mpn = r.MpnNumber,
//                        Description = r.Description,
//                        Manufacturer = r.Manufacturer,
//                        Category = r.Category,
//                        DatasheetUrl = r.DatasheetUrl,
//                        ProductUrl = r.ProductUrl,
//                        Stock = r.Stock,
//                        Source = "LCSC",
//                        Specs = new Dictionary<string, string>
//                        {
//                            ["Package"] = r.Package ?? "",
//                            ["LCSC Part No"] = r.LcscPartNumber ?? "",
//                            ["Unit Price"] = r.Price ?? "N/A",
//                            ["Match Score"] = $"{r.MatchScore}%"
//                        }
//                    }).ToList();
//                }
//                catch { return new List<PartDetails>(); }
//            });

//            await Task.WhenAll(digikeyTask, mouserTask, lcscTask);

//            vm.DigiKeyResults = await digikeyTask;
//            vm.MouserResults = await mouserTask;
//            vm.LCSCResults = await lcscTask;
//            vm.DigiKeyTotal = vm.DigiKeyResults.Count;
//            vm.MouserTotal = vm.MouserResults.Count;
//            vm.LCSCTotal = vm.LCSCResults.Count;

//            if (!vm.DigiKeyResults.Any() &&
//                !vm.MouserResults.Any() &&
//                !vm.LCSCResults.Any())
//                vm.Error =
//                    "No parts found. Try different keywords.";

//            return View(vm);
//        }

//        // ── FetchBestDetails ──────────────────────────────────────
//        // DigiKey → Mouser → LCSC fallback chain
//        private async Task<VerifySource> FetchBestDetails(
//            string mpn)
//        {
//            // ── PRIMARY: DigiKey ──────────────────────────────────
//            try
//            {
//                var data = await _digikey.GetPartDetails(mpn);
//                if (data != null)
//                    return new VerifySource
//                    {
//                        Source = "DigiKey",
//                        FetchedDescription = data.Description,
//                        Manufacturer = data.Manufacturer,
//                        Category = data.Category,
//                        Package = data.Specs.TryGetValue(
//                            "Package / Case", out var pkg) ? pkg :
//                            data.Specs.TryGetValue(
//                            "Supplier Device Package", out var spkg)
//                            ? spkg : "N/A",
//                        DatasheetUrl = data.DatasheetUrl,
//                        ProductUrl = data.ProductUrl,
//                        Stock = data.Stock,
//                        Specs = data.Specs,
//                        MatchVerdict = ""
//                    };
//            }
//            catch { }

//            // ── FALLBACK: Mouser ──────────────────────────────────
//            try
//            {
//                var data = await _mouser.GetPartDetails(mpn);
//                if (data != null)
//                    return new VerifySource
//                    {
//                        Source = "Mouser",
//                        FetchedDescription = data.Description,
//                        Manufacturer = data.Manufacturer,
//                        Category = data.Category,
//                        Package = data.Specs.TryGetValue(
//                            "Case/Package", out var pkg)
//                            ? pkg : "N/A",
//                        DatasheetUrl = data.DatasheetUrl,
//                        ProductUrl = data.ProductUrl,
//                        Stock = data.Stock,
//                        Specs = data.Specs,
//                        MatchVerdict = ""
//                    };
//            }
//            catch { }

//            // ── FALLBACK: LCSC ────────────────────────────────────
//            try
//            {
//                var results = await _lcsc
//                    .SearchByKeyword(mpn, limit: 1);
//                var data = results.FirstOrDefault();
//                if (data != null)
//                    return new VerifySource
//                    {
//                        Source = "LCSC",
//                        FetchedDescription = data.Description,
//                        Manufacturer = data.Manufacturer,
//                        Category = data.Category,
//                        Package = data.Package ?? "N/A",
//                        DatasheetUrl = data.DatasheetUrl,
//                        ProductUrl = data.ProductUrl,
//                        Stock = data.Stock,
//                        Specs = new Dictionary<string, string>
//                        {
//                            ["Package"] = data.Package ?? "",
//                            ["LCSC Part No"] = data.LcscPartNumber ?? "",
//                            ["MPN"] = data.MpnNumber ?? "",
//                            ["Price"] = data.Price ?? ""
//                        },
//                        MatchVerdict = ""
//                    };
//            }
//            catch { }

//            return new VerifySource
//            {
//                Source = "Not Found",
//                FetchedDescription = "",
//                Specs = new Dictionary<string, string>()
//            };
//        }

//        // ── Calculate match score ─────────────────────────────────
//        private double CalculateMatchScore(string userDesc,
//            string fetchedDesc)
//        {
//            if (string.IsNullOrWhiteSpace(fetchedDesc)) return 0;

//            var userWords = userDesc.ToLower().Split(' ',
//                StringSplitOptions.RemoveEmptyEntries);
//            var fetchedLower = fetchedDesc.ToLower();
//            int matched = userWords.Count(w =>
//                fetchedLower.Contains(w));
//            return Math.Round(
//                (double)matched / userWords.Length * 100, 1);
//        }

//        // ── Build spec comparison table ───────────────────────────
//        private List<SpecComparison> BuildSpecComparison(
//            Dictionary<string, string> origSpecs,
//            Dictionary<string, string> altSpecs)
//        {
//            var comparisons = new List<SpecComparison>();
//            origSpecs ??= new Dictionary<string, string>();
//            altSpecs ??= new Dictionary<string, string>();

//            var prioritySpecs = new[]
//            {
//                "Case/Package", "Number of Pins",
//                "Supply Voltage", "Operating Temperature",
//                "Mounting Style", "Output Current",
//                "Power Rating", "Technology", "Part Status"
//            };

//            var allKeys = origSpecs.Keys.Union(altSpecs.Keys)
//                .OrderBy(k =>
//                    Array.IndexOf(prioritySpecs, k) >= 0
//                    ? Array.IndexOf(prioritySpecs, k) : 999)
//                .ToList();

//            foreach (var key in allKeys)
//            {
//                origSpecs.TryGetValue(key, out var origVal);
//                altSpecs.TryGetValue(key, out var altVal);

//                string status;
//                if (!string.IsNullOrEmpty(origVal) &&
//                    !string.IsNullOrEmpty(altVal))
//                    status = origVal.Equals(altVal,
//                        StringComparison.OrdinalIgnoreCase)
//                        ? "Match" : "Mismatch";
//                else if (!string.IsNullOrEmpty(origVal))
//                    status = "Only Original";
//                else
//                    status = "Only Alternate";

//                comparisons.Add(new SpecComparison
//                {
//                    SpecName = key,
//                    OriginalValue = origVal ?? "—",
//                    AlternateValue = altVal ?? "—",
//                    Status = status
//                });
//            }
//            return comparisons;
//        }

//        // ── Determine final verdict ───────────────────────────────
//        private void DetermineVerdict(CompareResult result)
//        {
//            bool origFound =
//                result.OriginalDetails?.Source != "Not Found";
//            bool altFound =
//                result.AlternateDetails?.Source != "Not Found";

//            if (!origFound && !altFound)
//            {
//                result.Verdict = "❌ Cannot Verify";
//                result.VerdictReason = "Neither part was found.";
//                result.RecommendedPart = "Unknown";
//                return;
//            }

//            int criticalMismatches = result.SpecComparisons
//                .Count(s => s.Status == "Mismatch" &&
//                    (s.SpecName.Contains("Package") ||
//                     s.SpecName.Contains("Pin")));
//            int totalMismatches = result.SpecComparisons
//                .Count(s => s.Status == "Mismatch");
//            int totalMatches = result.SpecComparisons
//                .Count(s => s.Status == "Match");
//            double origScore = result.OriginalMatchScore;
//            double altScore = result.AlternateMatchScore;

//            if (criticalMismatches > 0)
//            {
//                result.Verdict =
//                    "⚠️ Use Original Part Only";
//                result.VerdictReason =
//                    $"Alternate has {criticalMismatches} critical " +
//                    "spec mismatch(es) (Package/Pins). " +
//                    "It may not fit the board.";
//                result.RecommendedPart = "Original";
//                return;
//            }

//            if (totalMatches > totalMismatches && altScore >= 50)
//            {
//                result.Verdict =
//                    "✅ Alternate Part is Okay to Use";
//                result.VerdictReason =
//                    $"Specs match in {totalMatches} of " +
//                    $"{totalMatches + totalMismatches} parameters. " +
//                    $"Description match: {altScore}%.";
//                result.RecommendedPart = "Alternate";
//                return;
//            }

//            if (altScore > origScore && altScore >= 60)
//            {
//                result.Verdict =
//                    "✅ Alternate Part is Okay to Use";
//                result.VerdictReason =
//                    $"Alternate matches better " +
//                    $"({altScore}% vs {origScore}%).";
//                result.RecommendedPart = "Alternate";
//                return;
//            }

//            if (origScore > altScore && origScore >= 60)
//            {
//                result.Verdict = "⚠️ Use Original Part";
//                result.VerdictReason =
//                    $"Original matches better " +
//                    $"({origScore}% vs {altScore}%).";
//                result.RecommendedPart = "Original";
//                return;
//            }

//            if (altScore >= 40 && origScore >= 40 &&
//                totalMismatches <= 2)
//            {
//                result.Verdict = "✅ Either Part Can Be Used";
//                result.VerdictReason =
//                    $"Both match reasonably. " +
//                    $"Original: {origScore}%, " +
//                    $"Alternate: {altScore}%.";
//                result.RecommendedPart = "Either";
//                return;
//            }

//            result.Verdict = "⚠️ Check Manually";
//            result.VerdictReason =
//                $"Insufficient data. " +
//                $"Original: {origScore}%, Alternate: {altScore}%.";
//            result.RecommendedPart = "Unknown";
//        }

//        // ── GET: /Verify/BomVerify ────────────────────────────────
//        [HttpGet]
//        public IActionResult BomVerify() => View();

//        // ── POST: /Verify/BomVerify ───────────────────────────────
//        [HttpPost]
//        public async Task<IActionResult> BomVerify(IFormFile bomFile)
//        {
//            if (bomFile == null || bomFile.Length == 0)
//            {
//                ViewBag.Error = "Please select a valid Excel file.";
//                return View();
//            }

//            var extension = Path.GetExtension(bomFile.FileName).ToLower();
//            if (extension != ".xlsx" && extension != ".xls")
//            {
//                ViewBag.Error = "Only .xlsx or .xls files are supported.";
//                return View();
//            }

//            try
//            {
//                using var stream = new MemoryStream();
//                await bomFile.CopyToAsync(stream);
//                stream.Position = 0;

//                ExcelPackage.License
//                    .SetNonCommercialPersonal("Alter_Parts");
//                using var package = new ExcelPackage(stream);
//                var sheet = package.Workbook.Worksheets[0];

//                if (sheet.Dimension == null)
//                {
//                    ViewBag.Error = "The Excel sheet is empty.";
//                    return View();
//                }

//                // Detect columns
//                var headers = new Dictionary<string, int>(
//                    StringComparer.OrdinalIgnoreCase);
//                for (int col = 1;
//                    col <= sheet.Dimension.End.Column; col++)
//                {
//                    var header = sheet.Cells[1, col].Text?.Trim();
//                    if (!string.IsNullOrEmpty(header))
//                        headers[header] = col;
//                }

//                int colPart = FindColumn(headers,
//                    "part number", "part no", "mpn",
//                    "part", "original part", "component");
//                int colDesc = FindColumn(headers,
//                    "description", "desc",
//                    "part description", "component description");

//                if (colPart == -1 || colDesc == -1)
//                {
//                    ViewBag.Error =
//                        $"Could not find required columns. " +
//                        $"Found: {string.Join(", ", headers.Keys)}. " +
//                        "Need: Part Number and Description.";
//                    return View();
//                }

//                var result = new BomVerifyResult
//                {
//                    FileName = bomFile.FileName,
//                    TotalRows = sheet.Dimension.End.Row - 1
//                };

//                for (int row = 2; row <= sheet.Dimension.End.Row; row++)
//                {
//                    // ✅ Extract raw text from the cell
//                    var rawPart = sheet.Cells[row, colPart].Value?.ToString();
//                    var rawDesc = sheet.Cells[row, colDesc].Value?.ToString();

//                    // ✅ Force it through the aggressive sanitizer
//                    var partNumber = CleanExcelData(rawPart);
//                    var userDesc = CleanExcelData(rawDesc);

//                    if (string.IsNullOrWhiteSpace(partNumber) && string.IsNullOrWhiteSpace(userDesc))
//                        continue;

//                    var bomRow = new BomVerifyRow
//                    {
//                        RowNumber = row,
//                        PartNumber = partNumber,
//                        UserDescription = userDesc,
//                        Status = "Pending"
//                    };

//                    if (string.IsNullOrWhiteSpace(partNumber))
//                    {
//                        bomRow.MatchVerdict = "⚠️ No part number";
//                        bomRow.Status = "Skipped";
//                        result.Rows.Add(bomRow);
//                        continue;
//                    }

//                    try
//                    {
//                        // 1. DigiKey
//                        PartDetails dkData = null;
//                        try { dkData = await _digikey.GetPartDetails(partNumber); }
//                        catch { /* Ignore */ }

//                        // 2. Mouser
//                        PartDetails mouserData = null;
//                        try { mouserData = await _mouser.GetPartDetails(partNumber); }
//                        catch { /* Ignore */ }

//                        // 3. LCSC
//                        LCSCPartResult lcscData = null;
//                        try
//                        {
//                            var lcscResults = await _lcsc.SearchByKeyword(partNumber, limit: 1);
//                            lcscData = lcscResults.FirstOrDefault();
//                        }
//                        catch { /* Ignore */ }

//                        // ── DigiKey result ────────────────────────────
//                        if (dkData != null)
//                        {
//                            bomRow.DigiKeyDescription = dkData.Description;
//                            bomRow.DigiKeyScore = string.IsNullOrWhiteSpace(userDesc) ? 0 : CalculateMatchScore(userDesc, dkData.Description);
//                            bomRow.DigiKeyVerdict = string.IsNullOrWhiteSpace(userDesc) ? "⚠️ No description" : GetMatchVerdict(userDesc, dkData.Description);
//                        }
//                        else
//                        {
//                            bomRow.DigiKeyVerdict = "❌ Not found";
//                        }

//                        // ── Mouser result ─────────────────────────────
//                        if (mouserData != null)
//                        {
//                            bomRow.MouserDescription = mouserData.Description;
//                            bomRow.MouserScore = string.IsNullOrWhiteSpace(userDesc) ? 0 : CalculateMatchScore(userDesc, mouserData.Description);
//                            bomRow.MouserVerdict = string.IsNullOrWhiteSpace(userDesc) ? "⚠️ No description" : GetMatchVerdict(userDesc, mouserData.Description);
//                        }
//                        else
//                        {
//                            bomRow.MouserVerdict = "❌ Not found";
//                        }

//                        // ── LCSC result ───────────────────────────────
//                        if (lcscData != null)
//                        {
//                            bomRow.LCSCDescription = lcscData.Description;
//                            bomRow.LCSCScore = string.IsNullOrWhiteSpace(userDesc) ? 0 : CalculateMatchScore(userDesc, lcscData.Description);
//                            bomRow.LCSCVerdict = string.IsNullOrWhiteSpace(userDesc) ? "⚠️ No description" : GetMatchVerdict(userDesc, lcscData.Description);
//                        }
//                        else
//                        {
//                            bomRow.LCSCVerdict = "❌ Not found";
//                        }

//                        // ── Pick best source ──────────────────────────
//                        var scores = new[]
//                        {
//                            ("DigiKey", bomRow.DigiKeyScore, bomRow.DigiKeyDescription),
//                            ("Mouser",  bomRow.MouserScore, bomRow.MouserDescription),
//                            ("LCSC",    bomRow.LCSCScore, bomRow.LCSCDescription)
//                        }
//                        .Where(x => !string.IsNullOrEmpty(x.Item3))
//                        .OrderByDescending(x => x.Item2)
//                        .FirstOrDefault();

//                        if (scores != default)
//                        {
//                            bomRow.BestSource = scores.Item1;
//                            bomRow.Source = scores.Item1;
//                            bomRow.FetchedDescription = scores.Item3;
//                            bomRow.MatchScore = scores.Item2;
//                            bomRow.MatchVerdict = string.IsNullOrWhiteSpace(userDesc)
//                                ? "⚠️ No description to verify"
//                                : GetMatchVerdict(userDesc, scores.Item3);

//                            if (scores.Item1 == "DigiKey" && dkData != null)
//                            {
//                                bomRow.Manufacturer = dkData.Manufacturer;
//                                bomRow.Category = dkData.Category;
//                            }
//                            else if (scores.Item1 == "Mouser" && mouserData != null)
//                            {
//                                bomRow.Manufacturer = mouserData.Manufacturer;
//                                bomRow.Category = mouserData.Category;
//                            }
//                            else if (scores.Item1 == "LCSC" && lcscData != null)
//                            {
//                                bomRow.Manufacturer = lcscData.Manufacturer;
//                                bomRow.Category = lcscData.Category;
//                            }
//                        }
//                        else
//                        {
//                            bomRow.BestSource = "Not Found";
//                            bomRow.Source = "Not Found";
//                            bomRow.MatchVerdict = "❌ Part not found";
//                            bomRow.MatchScore = 0;
//                        }

//                        bomRow.Status = "Done";
//                    }
//                    catch (Exception ex)
//                    {
//                        bomRow.MatchVerdict = $"❌ System Error";
//                        bomRow.Status = "Error";
//                    }

//                    result.Rows.Add(bomRow);

//                    // 🚨 Keeps the APIs from blocking us
//                    await Task.Delay(2200);
//                }

//                result.MatchedCount = result.Rows.Count(r =>
//                    r.MatchVerdict?.StartsWith("✅") == true);
//                result.NotMatchedCount = result.Rows.Count(r =>
//                    r.MatchVerdict?.StartsWith("❌") == true);
//                result.ManualCheckCount = result.Rows.Count(r =>
//                    r.MatchVerdict?.StartsWith("⚠️") == true);
//                result.NotFoundCount = result.Rows.Count(r =>
//                    r.MatchVerdict == "❌ Part not found");

//                return View("BomVerifyResult", result);
//            }
//            catch (Exception ex)
//            {
//                ViewBag.Error = "Failed to process file: " + ex.Message;
//                return View();
//            }
//        }

//        // ── POST: /Verify/ExportVerifyResults ─────────────────────

//        [HttpPost]
//        public IActionResult ExportVerifyResults(string resultsJson)
//        {
//            var rows = JsonSerializer
//                .Deserialize<List<BomVerifyRow>>(resultsJson);
//            if (rows == null || rows.Count == 0)
//                return BadRequest("No data to export.");

//            ExcelPackage.License
//                .SetNonCommercialPersonal("Alter_Parts");
//            using var package = new ExcelPackage();
//            var sheet = package.Workbook.Worksheets
//                .Add("BOM Verify Results");

//            string[] hdrs = {
//                "Row #", "Part Number", "Your Description",
//                "Best Source", "Best Match %", "Overall Verdict",
//                "DigiKey Description", "DigiKey Match %",
//                "DigiKey Verdict",
//                "Mouser Description",  "Mouser Match %",
//                "Mouser Verdict",
//                "LCSC Description",    "LCSC Match %",
//                "LCSC Verdict",
//                "Manufacturer", "Category", "Status"
//            };

//            for (int i = 0; i < hdrs.Length; i++)
//            {
//                sheet.Cells[1, i + 1].Value = hdrs[i];
//                sheet.Cells[1, i + 1].Style.Font.Bold = true;
//                sheet.Cells[1, i + 1].Style.Fill.PatternType =
//                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
//                sheet.Cells[1, i + 1].Style.Fill.BackgroundColor
//                    .SetColor(System.Drawing.Color.FromArgb(28, 57, 107));
//                sheet.Cells[1, i + 1].Style.Font.Color
//                    .SetColor(System.Drawing.Color.White);
//            }

//            for (int i = 0; i < rows.Count; i++)
//            {
//                var row = rows[i];
//                int excelRow = i + 2;

//                sheet.Cells[excelRow, 1].Value = row.RowNumber;
//                sheet.Cells[excelRow, 2].Value = row.PartNumber;
//                sheet.Cells[excelRow, 3].Value = row.UserDescription;
//                sheet.Cells[excelRow, 4].Value = row.BestSource;
//                sheet.Cells[excelRow, 5].Value = $"{row.MatchScore}%";
//                sheet.Cells[excelRow, 6].Value = row.MatchVerdict;
//                sheet.Cells[excelRow, 7].Value = row.DigiKeyDescription;
//                sheet.Cells[excelRow, 8].Value = $"{row.DigiKeyScore}%";
//                sheet.Cells[excelRow, 9].Value = row.DigiKeyVerdict;
//                sheet.Cells[excelRow, 10].Value = row.MouserDescription;
//                sheet.Cells[excelRow, 11].Value = $"{row.MouserScore}%";
//                sheet.Cells[excelRow, 12].Value = row.MouserVerdict;
//                sheet.Cells[excelRow, 13].Value = row.LCSCDescription;
//                sheet.Cells[excelRow, 14].Value = $"{row.LCSCScore}%";
//                sheet.Cells[excelRow, 15].Value = row.LCSCVerdict;
//                sheet.Cells[excelRow, 16].Value = row.Manufacturer;
//                sheet.Cells[excelRow, 17].Value = row.Category;
//                sheet.Cells[excelRow, 18].Value = row.Status;

//                // Color code overall verdict
//                var color =
//                    row.MatchVerdict?.StartsWith("✅") == true
//                    ? System.Drawing.Color.FromArgb(198, 239, 206)
//                    : row.MatchVerdict?.StartsWith("❌") == true
//                    ? System.Drawing.Color.FromArgb(255, 199, 206)
//                    : System.Drawing.Color.FromArgb(255, 235, 156);

//                sheet.Cells[excelRow, 6].Style.Fill.PatternType =
//                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
//                sheet.Cells[excelRow, 6].Style.Fill.BackgroundColor
//                    .SetColor(color);

//                // Color DigiKey verdict
//                var dkColor =
//                    row.DigiKeyVerdict?.StartsWith("✅") == true
//                    ? System.Drawing.Color.FromArgb(198, 239, 206)
//                    : row.DigiKeyVerdict?.StartsWith("❌") == true
//                    ? System.Drawing.Color.FromArgb(255, 199, 206)
//                    : System.Drawing.Color.FromArgb(255, 235, 156);
//                sheet.Cells[excelRow, 9].Style.Fill.PatternType =
//                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
//                sheet.Cells[excelRow, 9].Style.Fill.BackgroundColor
//                    .SetColor(dkColor);

//                // Color Mouser verdict
//                var mouserColor =
//                    row.MouserVerdict?.StartsWith("✅") == true
//                    ? System.Drawing.Color.FromArgb(198, 239, 206)
//                    : row.MouserVerdict?.StartsWith("❌") == true
//                    ? System.Drawing.Color.FromArgb(255, 199, 206)
//                    : System.Drawing.Color.FromArgb(255, 235, 156);
//                sheet.Cells[excelRow, 12].Style.Fill.PatternType =
//                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
//                sheet.Cells[excelRow, 12].Style.Fill.BackgroundColor
//                    .SetColor(mouserColor);

//                // Color LCSC verdict
//                var lcscColor =
//                    row.LCSCVerdict?.StartsWith("✅") == true
//                    ? System.Drawing.Color.FromArgb(198, 239, 206)
//                    : row.LCSCVerdict?.StartsWith("❌") == true
//                    ? System.Drawing.Color.FromArgb(255, 199, 206)
//                    : System.Drawing.Color.FromArgb(255, 235, 156);
//                sheet.Cells[excelRow, 15].Style.Fill.PatternType =
//                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
//                sheet.Cells[excelRow, 15].Style.Fill.BackgroundColor
//                    .SetColor(lcscColor);
//            }

//            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
//            var fileBytes = package.GetAsByteArray();
//            return File(fileBytes,
//                "application/vnd.openxmlformats-officedocument" +
//                ".spreadsheetml.sheet",
//                $"BOM_Verify_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
//        }

//        // ── Helper: find column ───────────────────────────────────
//        private static int FindColumn(
//            Dictionary<string, int> headers,
//            params string[] names)
//        {
//            foreach (var name in names)
//                if (headers.TryGetValue(name, out int col))
//                    return col;
//            return -1;
//        }

//        // ── Helper: Aggressive Data Sanitizer ───────────────────────
//        private static string CleanExcelData(string rawInput)
//        {
//            if (string.IsNullOrWhiteSpace(rawInput)) return string.Empty;

//            // 1. Remove standard whitespace from ends
//            var cleaned = rawInput.Trim();

//            // 2. Remove "Non-Breaking Spaces" (Unicode 00A0) that standard Trim() misses
//            cleaned = cleaned.Replace("\u00A0", "");

//            // 3. The "Nuclear Option" Regex:
//            // This allows ONLY standard, printable ASCII characters (hex 20 to hex 7E).
//            // It instantly deletes all invisible Unicode garbage, zero-width spaces, 
//            // carriage returns, and weird formatting characters.
//            cleaned = Regex.Replace(cleaned, @"[^\x20-\x7E]", string.Empty);

//            return cleaned;
//        }
//    }
//}