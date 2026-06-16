using Alter_Parts.Models;

namespace Alter_Parts.Services
{
    public class PartLookupService
    {
        private readonly DigiKeyService _digikey;
        private readonly MouserService _mouser;
        private readonly LCSCService _lcsc;

        public PartLookupService(
            DigiKeyService digikey,
            MouserService mouser,
            LCSCService lcsc)
        {
            _digikey = digikey;
            _mouser = mouser;
            _lcsc = lcsc;
        }

        // ── SINGLE PART LOOKUP ──────────────────────────────────────
        public async Task<PartDetails> GetPartDetails(string mpn)
        {
            Console.WriteLine($"[LOOKUP] Searching for MPN: {mpn}");

            var digiKeyTask = SafeGetDigiKey(mpn);
            var mouserTask = SafeGetMouser(mpn);
            var lcscTask = SafeGetLCSC(mpn);

            await Task.WhenAll(digiKeyTask, mouserTask, lcscTask);

            var digiKeyResult = await digiKeyTask;
            var mouserResult = await mouserTask;
            var lcscResult = await lcscTask;

            if (digiKeyResult != null)
            {
                Console.WriteLine($"[LOOKUP] Found on DigiKey: {digiKeyResult.Manufacturer}");
                return digiKeyResult;
            }

            if (mouserResult != null)
            {
                Console.WriteLine($"[LOOKUP] Found on Mouser: {mouserResult.Manufacturer}");
                return mouserResult;
            }

            if (lcscResult != null)
            {
                Console.WriteLine($"[LOOKUP] Found on LCSC: {lcscResult.Manufacturer}");
                return lcscResult;
            }

            Console.WriteLine($"[LOOKUP] Not found anywhere: {mpn}");
            return new PartDetails
            {
                Mpn = mpn,
                Source = "Not Found",
                Specs = new Dictionary<string, string>()
            };
        }

        // ── MULTI-DISTRIBUTOR LOOKUP ────────────────────────────────
        public async Task<MultiDistributorResult> GetPartFromAllDistributors(string mpn)
        {
            Console.WriteLine($"[LOOKUP] Multi-distributor search for: {mpn}");

            var digiKeyTask = SafeGetDigiKey(mpn);
            var mouserTask = SafeGetMouser(mpn);
            var lcscTask = SafeGetLCSC(mpn);

            await Task.WhenAll(digiKeyTask, mouserTask, lcscTask);

            var result = new MultiDistributorResult
            {
                Mpn = mpn,
                DigiKey = await digiKeyTask,
                Mouser = await mouserTask,
                LCSC = await lcscTask
            };

            result.Manufacturer =
                result.DigiKey?.Manufacturer ??
                result.Mouser?.Manufacturer ??
                result.LCSC?.Manufacturer ??
                "Unknown";

            result.Description =
                result.DigiKey?.Description ??
                result.Mouser?.Description ??
                result.LCSC?.Description ??
                "";

            result.Category =
                result.DigiKey?.Category ??
                result.Mouser?.Category ??
                result.LCSC?.Category ??
                "";

            Console.WriteLine($"[LOOKUP] DigiKey: {(result.DigiKey != null ? "✓" : "✗")} | " +
                              $"Mouser: {(result.Mouser != null ? "✓" : "✗")} | " +
                              $"LCSC: {(result.LCSC != null ? "✓" : "✗")}");

            return result;
        }

        // ── DESCRIPTION SEARCH ──────────────────────────────────────
        public async Task<List<PartDetails>> SearchByDescription(
            string description, int limit = 10)
        {
            Console.WriteLine($"[LOOKUP] Description search: {description}");

            var digiKeyTask = SafeSearchDescriptionDigiKey(description, limit);
            var mouserTask = SafeSearchDescriptionMouser(description, limit);
            var lcscTask = SafeSearchDescriptionLCSC(description, limit);

            await Task.WhenAll(digiKeyTask, mouserTask, lcscTask);

            var combined = new List<PartDetails>();
            combined.AddRange(await digiKeyTask);
            combined.AddRange(await mouserTask);
            combined.AddRange(await lcscTask);

            Console.WriteLine($"[LOOKUP] Description search found {combined.Count} total results");

            return combined
                .OrderByDescending(r =>
                    double.TryParse(
                        r.Specs.GetValueOrDefault("Match Score", "0")
                         .Replace("%", ""),
                        out var score) ? score : 0)
                .Take(limit)
                .ToList();
        }

        // ── COMPARISON ENGINE ───────────────────────────────────────
        // STEP 2: Compare Original Part vs Alternate Part specs
        public string ComparePartDetails(PartDetails orig, PartDetails alt)
        {
            if (orig.Source == "Not Found" || alt.Source == "Not Found")
                return "⚠️ Part not found in database";

            var mismatches = new List<string>();
            var warnings = new List<string>();
            var matches = new List<string>();

            string manNote = orig.Manufacturer != alt.Manufacturer
                ? $" [Diff Mfr: {alt.Manufacturer}]" : "";

            if (!string.IsNullOrEmpty(orig.Category) &&
                !string.IsNullOrEmpty(alt.Category) &&
                orig.Category != alt.Category)
            {
                mismatches.Add($"Category: {orig.Category} vs {alt.Category}");
            }

            orig.Specs ??= new Dictionary<string, string>();
            alt.Specs ??= new Dictionary<string, string>();

            var criticalSpecs = new[]
            {
                "Case/Package", "Package / Case", "Supplier Device Package",
                "Number of Pins", "Pin Count",
                "Mounting Style", "Mounting Type"
            };

            var warningSpecs = new[]
            {
                "Supply Voltage", "Voltage - Supply",
                "Operating Temperature",
                "Output Current", "Current - Output",
                "Power Rating", "Part Status"
            };

            foreach (var specName in criticalSpecs)
            {
                var key1 = orig.Specs.Keys.FirstOrDefault(
                    k => k.Contains(specName, StringComparison.OrdinalIgnoreCase));
                var key2 = alt.Specs.Keys.FirstOrDefault(
                    k => k.Contains(specName, StringComparison.OrdinalIgnoreCase));

                if (key1 == null || key2 == null) continue;

                if (orig.Specs[key1].Equals(alt.Specs[key2], StringComparison.OrdinalIgnoreCase))
                    matches.Add($"{specName.Split('/')[0].Trim()}: {orig.Specs[key1]}");
                else
                    mismatches.Add($"{specName.Split('/')[0].Trim()}: {orig.Specs[key1]} vs {alt.Specs[key2]}");
            }

            foreach (var specName in warningSpecs)
            {
                var key1 = orig.Specs.Keys.FirstOrDefault(
                    k => k.Contains(specName, StringComparison.OrdinalIgnoreCase));
                var key2 = alt.Specs.Keys.FirstOrDefault(
                    k => k.Contains(specName, StringComparison.OrdinalIgnoreCase));

                if (key1 == null || key2 == null) continue;

                if (orig.Specs[key1].Equals(alt.Specs[key2], StringComparison.OrdinalIgnoreCase))
                    matches.Add($"{specName.Split('-')[0].Trim()}: {orig.Specs[key1]}");
                else
                    warnings.Add($"{specName.Split('-')[0].Trim()}: {orig.Specs[key1]} vs {alt.Specs[key2]}");
            }

            string src = $" [{orig.Source}→{alt.Source}]";

            if (mismatches.Count > 0)
                return $"❌ Not Compatible - {string.Join(", ", mismatches)}{manNote}{src}";

            if (warnings.Count > 0 && matches.Count == 0)
                return $"⚠️ Check Manually - {string.Join(", ", warnings)}{manNote}{src}";

            if (warnings.Count > 0)
                return $"⚠️ Likely Compatible (verify: {string.Join(", ", warnings)}){manNote}{src}";

            if (matches.Count > 0)
                return $"✅ Compatible - {string.Join(", ", matches)}{manNote}{src}";

            return $"⚠️ Check Manually (no matching specs found){manNote}{src}";
        }

        // ── STEP 1: Match Excel description vs fetched online description ──
        // Returns verdict string + numeric score for use in BomController
        public (string verdict, double score) GetMatchVerdict(
            string userDesc, string fetchedDesc)
        {
            if (string.IsNullOrWhiteSpace(fetchedDesc))
                return ("⚠️ No description available", 0);

            if (string.IsNullOrWhiteSpace(userDesc))
                return ("⚠️ No description provided in Excel", 0);

            var user = userDesc.ToLower().Trim();
            var fetched = fetchedDesc.ToLower().Trim();

            if (user == fetched)
                return ("✅ Exact Match", 100);

            var userWords = user
                .Split(new[] { ' ', ',', '-', '/', '(', ')' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToArray();

            if (userWords.Length == 0)
                return ("⚠️ Description too short to match", 0);

            var matchedWords = userWords.Count(w => fetched.Contains(w));
            double matchPercent = (double)matchedWords / userWords.Length * 100;

            if (matchPercent >= 80)
                return ($"✅ Strong Match ({matchPercent:0}% keywords matched)", matchPercent);
            if (matchPercent >= 50)
                return ($"⚠️ Partial Match ({matchPercent:0}% keywords matched)", matchPercent);
            if (matchPercent >= 20)
                return ($"⚠️ Weak Match ({matchPercent:0}% keywords matched)", matchPercent);

            return ($"❌ No Match ({matchPercent:0}% keywords matched)", matchPercent);
        }

        // ── SAFE WRAPPERS ───────────────────────────────────────────

        private async Task<PartDetails?> SafeGetDigiKey(string mpn)
        {
            try
            {
                var result = await _digikey.GetPartDetails(mpn);
                Console.WriteLine($"[DIGIKEY] {mpn} => " +
                    $"{(result != null ? result.Manufacturer : "NULL")}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DIGIKEY] Exception for {mpn}: {ex.Message}");
                return null;
            }
        }

        private async Task<PartDetails?> SafeGetMouser(string mpn)
        {
            try
            {
                var result = await _mouser.GetPartDetails(mpn);
                Console.WriteLine($"[MOUSER] {mpn} => " +
                    $"{(result != null ? result.Manufacturer : "NULL")}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MOUSER] Exception for {mpn}: {ex.Message}");
                return null;
            }
        }

        private async Task<PartDetails?> SafeGetLCSC(string mpn)
        {
            try
            {
                Console.WriteLine($"[LCSC] Calling GetPartByMpn for: {mpn}");
                var lcscData = await _lcsc.GetPartByMpn(mpn);

                if (lcscData == null)
                {
                    Console.WriteLine($"[LCSC] {mpn} => NULL");
                    return null;
                }

                Console.WriteLine($"[LCSC] {mpn} => {lcscData.Manufacturer}");

                return new PartDetails
                {
                    Mpn = lcscData.MpnNumber,
                    Source = "LCSC",
                    Description = lcscData.Description,
                    Manufacturer = lcscData.Manufacturer,
                    Category = lcscData.Category,
                    DatasheetUrl = lcscData.DatasheetUrl,
                    ProductUrl = lcscData.ProductUrl,
                    Stock = lcscData.Stock,
                    Price = lcscData.Price,
                    Specs = new Dictionary<string, string>
                    {
                        { "Package / Case", lcscData.Package ?? "" },
                        { "LCSC Part #",    lcscData.LcscPartNumber ?? "" },
                        { "Match Score",    $"{lcscData.MatchScore}%" }
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LCSC] Exception for {mpn}: {ex.Message}");
                return null;
            }
        }

        private async Task<List<PartDetails>> SafeSearchDescriptionDigiKey(
            string description, int limit)
        {
            try { return await _digikey.SearchByDescription(description, limit); }
            catch (Exception ex)
            {
                Console.WriteLine($"[DIGIKEY] Description search error: {ex.Message}");
                return new List<PartDetails>();
            }
        }

        private async Task<List<PartDetails>> SafeSearchDescriptionMouser(
            string description, int limit)
        {
            try { return await _mouser.SearchByDescription(description, limit); }
            catch (Exception ex)
            {
                Console.WriteLine($"[MOUSER] Description search error: {ex.Message}");
                return new List<PartDetails>();
            }
        }

        private async Task<List<PartDetails>> SafeSearchDescriptionLCSC(
            string description, int limit)
        {
            try
            {
                var lcscResults = await _lcsc.SearchByKeyword(description, limit);
                return lcscResults.Select(r => new PartDetails
                {
                    Mpn = r.MpnNumber,
                    Source = "LCSC",
                    Description = r.Description,
                    Manufacturer = r.Manufacturer,
                    Category = r.Category,
                    DatasheetUrl = r.DatasheetUrl,
                    ProductUrl = r.ProductUrl,
                    Stock = r.Stock,
                    Price = r.Price,
                    Specs = new Dictionary<string, string>
                    {
                        { "Package / Case", r.Package ?? "" },
                        { "LCSC Part #",    r.LcscPartNumber ?? "" },
                        { "Match Score",    $"{r.MatchScore}%" }
                    }
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LCSC] Description search error: {ex.Message}");
                return new List<PartDetails>();
            }
        }
    }

    // ── MULTI-DISTRIBUTOR RESULT MODEL ──────────────────────────────
    public class MultiDistributorResult
    {
        public string Mpn { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public PartDetails? DigiKey { get; set; }
        public PartDetails? Mouser { get; set; }
        public PartDetails? LCSC { get; set; }

        public bool FoundAnywhere =>
            DigiKey != null || Mouser != null || LCSC != null;

        public PartDetails? BestResult =>
            DigiKey ?? Mouser ?? LCSC;
    }
}
