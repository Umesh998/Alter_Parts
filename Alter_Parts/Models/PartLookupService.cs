//using Alter_Parts.Models;
//using Vinrox_Tools.Services;
//using System.Text.Json;

//public class PartLookupService
//{
//    private readonly NexarService _nexar;
//    private readonly DigiKeyService _digikey;
//    private readonly MouserService _mouser;
//    private readonly LCSCService _lcsc;

//    public PartLookupService(NexarService nexar, DigiKeyService digikey, MouserService mouser, LCSCService lcsc)
//    {
//        _nexar = nexar;
//        _digikey = digikey;
//        _mouser = mouser;
//        _lcsc = lcsc;
//    }

//    public async Task<PartDetails> GetPartDetails(string mpn)
//    {


//        // Try DigiKey first
//        try
//        {
//            var result = await _digikey.GetPartDetails(mpn);
//            if (result != null) return result;
//        }
//        catch { /* DigiKey failed, try Mouser */ }

//        try
//        {
//            var result = await _nexar.GetPartDetailsAsync(mpn);
//            if (result != null) return result;
//        }
//        catch { /* DigiKey failed, try Mouser */ }

//        // Fallback to Mouser
//        try
//        {
//            var result = await _mouser.GetPartDetails(mpn);
//            if (result != null) return result;
//        }
//        catch { /* Mouser also failed */ }

//        //Theb search for LCSC
//        try
//        {
//            var result = await _lcsc.GetPartDetails(mpn);
//            if (result != null) return result;
//        }
//        catch { }

//        // Neither found it
//        return new PartDetails
//        {
//            Mpn = mpn,
//            Source = "Not Found"
//        };
//    }

//    public string ComparePartDetails(PartDetails orig, PartDetails alt)
//    {
//        if (orig.Source == "Not Found" || alt.Source == "Not Found")
//            return $"⚠️ Part not found in DigiKey or Mouser";

//        var mismatches = new List<string>();
//        var warnings = new List<string>();
//        var matches = new List<string>();
//        string manNote = orig.Manufacturer != alt.Manufacturer
//            ? $" [Diff Mfr: {alt.Manufacturer}]" : "";

//        // Category check
//        if (!string.IsNullOrEmpty(orig.Category) && !string.IsNullOrEmpty(alt.Category)
//            && orig.Category != alt.Category)
//            mismatches.Add($"Category: {orig.Category} vs {alt.Category}");

//        // Compare specs that exist in both parts
//        var commonSpecs = new[]
//        {
//            "Package / Case", "Supplier Device Package",
//            "Number of Pins", "Pin Count",
//            "Voltage - Supply", "Supply Voltage",
//            "Operating Temperature", "Current - Output",
//            "Technology", "Part Status"
//        };

//        foreach (var specName in commonSpecs)
//        {
//            // Try exact match first, then partial
//            var key1 = orig.Specs.Keys.FirstOrDefault(k =>
//                k.Equals(specName, StringComparison.OrdinalIgnoreCase) ||
//                k.Contains(specName.Split(' ')[0], StringComparison.OrdinalIgnoreCase));

//            var key2 = alt.Specs.Keys.FirstOrDefault(k =>
//                k.Equals(specName, StringComparison.OrdinalIgnoreCase) ||
//                k.Contains(specName.Split(' ')[0], StringComparison.OrdinalIgnoreCase));

//            if (key1 == null || key2 == null) continue;

//            var val1 = orig.Specs[key1];
//            var val2 = alt.Specs[key2];

//            if (string.IsNullOrEmpty(val1) || string.IsNullOrEmpty(val2)) continue;

//            // Package/Pins = hard fail if different
//            bool isCritical = specName.Contains("Package") || specName.Contains("Pin");

//            if (val1.Equals(val2, StringComparison.OrdinalIgnoreCase))
//                matches.Add($"{specName.Split('/')[0].Trim()}: {val1}");
//            else if (isCritical)
//                mismatches.Add($"{specName.Split('/')[0].Trim()}: {val1} vs {val2}");
//            else
//                warnings.Add($"{specName.Split('/')[0].Trim()}: {val1} vs {val2}");
//        }

//        string src = $" [{orig.Source}→{alt.Source}]";

//        if (mismatches.Count > 0)
//            return $"❌ Not Compatible - {string.Join(", ", mismatches)}{manNote}{src}";
//        if (warnings.Count > 0 && matches.Count == 0)
//            return $"⚠️ Check Manually - {string.Join(", ", warnings)}{manNote}{src}";
//        if (warnings.Count > 0)
//            return $"⚠️ Likely Compatible (verify: {string.Join(", ", warnings)}){manNote}{src}";
//        if (matches.Count > 0)
//            return $"✅ Compatible - {string.Join(", ", matches)}{manNote}{src}";

//        return $"⚠️ Check Manually (no matching specs found){manNote}{src}";
//    }
//}






//using Alter_Parts.Models;
//using Vinrox_Tools.Services;
//using System.Text.Json;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using System;

//public class PartLookupService
//{
//    private readonly NexarService _nexar;
//    private readonly DigiKeyService _digikey;
//    private readonly MouserService _mouser;
//    private readonly LCSCService _lcsc;

//    public PartLookupService(NexarService nexar, DigiKeyService digikey, MouserService mouser, LCSCService lcsc)
//    {
//        _nexar = nexar;
//        _digikey = digikey;
//        _mouser = mouser;
//        _lcsc = lcsc;
//    }

//    public async Task<PartDetails> GetPartDetails(string mpn)
//    {
//        // 1. Try DigiKey first
//        try
//        {
//            var result = await _digikey.GetPartDetails(mpn);
//            if (result != null) return result;
//        }
//        catch { /* DigiKey failed, try Nexar */ }

//        // 2. Try Nexar
//        try
//        {
//            string rawJson = await _nexar.GetPartDetailsAsync(mpn);

//            using var doc = JsonDocument.Parse(rawJson);
//            var root = doc.RootElement;
//            var resultsArray = root.GetProperty("data").GetProperty("supSearchMpn").GetProperty("results");

//            if (resultsArray.GetArrayLength() > 0)
//            {
//                var partNode = resultsArray[0].GetProperty("part");

//                var nexarPart = new PartDetails
//                {
//                    Mpn = partNode.GetProperty("mpn").GetString(),
//                    Source = "Nexar",
//                    Specs = new Dictionary<string, string>() // Initialize dictionary to prevent crashes
//                };

//                // Safely grab Manufacturer
//                if (partNode.TryGetProperty("manufacturer", out var mfgNode) && mfgNode.ValueKind != JsonValueKind.Null)
//                    nexarPart.Manufacturer = mfgNode.GetProperty("name").GetString();

//                // Safely grab Category
//                if (partNode.TryGetProperty("category", out var catNode) && catNode.ValueKind != JsonValueKind.Null)
//                    nexarPart.Category = catNode.GetProperty("name").GetString();

//                // Loop through Nexar's Specs array and add them to your Dictionary
//                if (partNode.TryGetProperty("specs", out var specsArray) && specsArray.ValueKind == JsonValueKind.Array)
//                {
//                    foreach (var spec in specsArray.EnumerateArray())
//                    {
//                        var specName = spec.GetProperty("attribute").GetProperty("name").GetString();
//                        var specValue = spec.GetProperty("displayValue").GetString();

//                        if (!string.IsNullOrEmpty(specName) && !string.IsNullOrEmpty(specValue))
//                        {
//                            nexarPart.Specs[specName] = specValue;
//                        }
//                    }
//                }

//                return nexarPart;
//            }
//        }
//        catch { /* Nexar failed, try Mouser */ }

//        // 3. Fallback to Mouser
//        try
//        {
//            var result = await _mouser.GetPartDetails(mpn);
//            if (result != null) return result;
//        }
//        catch { /* Mouser also failed, try LCSC */ }

//        // 4. Then search for LCSC
//        try
//        {
//            var result = await _lcsc.GetPartDetails(mpn);
//            if (result != null) return result;
//        }
//        catch { /* LCSC also failed */ }

//        // Neither found it
//        return new PartDetails
//        {
//            Mpn = mpn,
//            Source = "Not Found",
//            Specs = new Dictionary<string, string>()
//        };
//    }

//    public string ComparePartDetails(PartDetails orig, PartDetails alt)
//    {
//        if (orig.Source == "Not Found" || alt.Source == "Not Found")
//            return $"⚠️ Part not found in any database";

//        var mismatches = new List<string>();
//        var warnings = new List<string>();
//        var matches = new List<string>();
//        string manNote = orig.Manufacturer != alt.Manufacturer
//            ? $" [Diff Mfr: {alt.Manufacturer}]" : "";

//        // Category check
//        if (!string.IsNullOrEmpty(orig.Category) && !string.IsNullOrEmpty(alt.Category)
//            && orig.Category != alt.Category)
//            mismatches.Add($"Category: {orig.Category} vs {alt.Category}");

//        // Compare specs that exist in both parts
//        var commonSpecs = new[]
//        {
//            "Package / Case", "Supplier Device Package",
//            "Number of Pins", "Pin Count",
//            "Voltage - Supply", "Supply Voltage",
//            "Operating Temperature", "Current - Output",
//            "Technology", "Part Status"
//        };

//        // Ensure Specs dictionaries are not null before checking
//        if (orig.Specs != null && alt.Specs != null)
//        {
//            foreach (var specName in commonSpecs)
//            {
//                // Try exact match first, then partial
//                var key1 = orig.Specs.Keys.FirstOrDefault(k =>
//                    k.Equals(specName, StringComparison.OrdinalIgnoreCase) ||
//                    k.Contains(specName.Split(' ')[0], StringComparison.OrdinalIgnoreCase));

//                var key2 = alt.Specs.Keys.FirstOrDefault(k =>
//                    k.Equals(specName, StringComparison.OrdinalIgnoreCase) ||
//                    k.Contains(specName.Split(' ')[0], StringComparison.OrdinalIgnoreCase));

//                if (key1 == null || key2 == null) continue;

//                var val1 = orig.Specs[key1];
//                var val2 = alt.Specs[key2];

//                if (string.IsNullOrEmpty(val1) || string.IsNullOrEmpty(val2)) continue;

//                // Package/Pins = hard fail if different
//                bool isCritical = specName.Contains("Package") || specName.Contains("Pin");

//                if (val1.Equals(val2, StringComparison.OrdinalIgnoreCase))
//                    matches.Add($"{specName.Split('/')[0].Trim()}: {val1}");
//                else if (isCritical)
//                    mismatches.Add($"{specName.Split('/')[0].Trim()}: {val1} vs {val2}");
//                else
//                    warnings.Add($"{specName.Split('/')[0].Trim()}: {val1} vs {val2}");
//            }
//        }

//        string src = $" [{orig.Source}→{alt.Source}]";

//        if (mismatches.Count > 0)
//            return $"❌ Not Compatible - {string.Join(", ", mismatches)}{manNote}{src}";
//        if (warnings.Count > 0 && matches.Count == 0)
//            return $"⚠️ Check Manually - {string.Join(", ", warnings)}{manNote}{src}";
//        if (warnings.Count > 0)
//            return $"⚠️ Likely Compatible (verify: {string.Join(", ", warnings)}){manNote}{src}";
//        if (matches.Count > 0)
//            return $"✅ Compatible - {string.Join(", ", matches)}{manNote}{src}";

//        return $"⚠️ Check Manually (no matching specs found){manNote}{src}";
//    }
//}






using Alter_Parts.Models;
using Vinrox_Tools.Services;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Alter_Parts.Services
{
    public class PartLookupService
    {
        // ── ACTIVE SERVICES ──
        private readonly DigiKeyService _digikey;
        private readonly MouserService _mouser;

        // 💡 FUTURE SERVICES (Uncomment when you have API keys)
        // private readonly NexarService _nexar;
        private readonly LCSCService _lcsc;

        public PartLookupService(
            DigiKeyService digikey,
            MouserService mouser,
        // NexarService nexar, 
         LCSCService lcsc

        )
        {
            _digikey = digikey;
            _mouser = mouser;

            // _nexar = nexar;
            _lcsc = lcsc;
        }

        public async Task<PartDetails> GetPartDetails(string mpn)
        {
            // ── 1. PRIMARY: Nexar (Commented for future use) ──────
            /*
            try
            {
                var rawJson = await _nexar.GetPartDetailsAsync(mpn);
                var result = ParseNexarResponse(rawJson, mpn);
                if (result.Source != "Not Found") return result;
            }
            catch { } // If Nexar fails, move to DigiKey
            */


            // ── 2. ACTIVE: DigiKey ──────────────────────────────────
            try
            {
                var result = await _digikey.GetPartDetails(mpn);
                if (result != null) return result;
            }
            catch { /* If DigiKey fails, swallow it and move to Mouser */ }


            // ── 3. ACTIVE: Mouser ───────────────────────────────────
            try
            {
                var result = await _mouser.GetPartDetails(mpn);
                if (result != null) return result;
            }
            catch { /* If Mouser fails, move to LCSC */ }


            // ── 4. FALLBACK: LCSC (Commented for future use) ────────

            try
            {
                var lcscData = await _lcsc.GetPartByMpn(mpn);
                if (lcscData != null)
                {
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
                        Specs = new Dictionary<string, string>
                        {
                            { "Package / Case", lcscData.Package }
                        }
                    };
                }
            }
            catch { }


            // ── NOT FOUND IN ANY SERVICE ────────────────────────────
            return new PartDetails
            {
                Mpn = mpn,
                Source = "Not Found",
                Specs = new Dictionary<string, string>() // Prevent null reference crashes
            };
        }

        // ── UNIFIED COMPARISON ENGINE ───────────────────────────────
        public string ComparePartDetails(PartDetails orig, PartDetails alt)
        {
            if (orig.Source == "Not Found" || alt.Source == "Not Found")
                return "⚠️ Part not found in database";

            var mismatches = new List<string>();
            var warnings = new List<string>();
            var matches = new List<string>();

            string manNote = orig.Manufacturer != alt.Manufacturer
                ? $" [Diff Mfr: {alt.Manufacturer}]" : "";

            // Category check
            if (!string.IsNullOrEmpty(orig.Category) &&
                !string.IsNullOrEmpty(alt.Category) &&
                orig.Category != alt.Category)
            {
                mismatches.Add($"Category: {orig.Category} vs {alt.Category}");
            }

            // Ensure Specs dictionaries exist
            orig.Specs ??= new Dictionary<string, string>();
            alt.Specs ??= new Dictionary<string, string>();

            // Critical specs — mismatch = not compatible
            var criticalSpecs = new[]
            {
                "Case/Package", "Package / Case", "Supplier Device Package",
                "Number of Pins", "Pin Count",
                "Mounting Style", "Mounting Type"
            };

            // Warning specs — mismatch = check manually
            var warningSpecs = new[]
            {
                "Supply Voltage", "Voltage - Supply",
                "Operating Temperature",
                "Output Current", "Current - Output",
                "Power Rating", "Part Status"
            };

            // Check Critical Specs
            foreach (var specName in criticalSpecs)
            {
                var key1 = orig.Specs.Keys.FirstOrDefault(k => k.Contains(specName, StringComparison.OrdinalIgnoreCase));
                var key2 = alt.Specs.Keys.FirstOrDefault(k => k.Contains(specName, StringComparison.OrdinalIgnoreCase));

                if (key1 == null || key2 == null) continue;

                if (orig.Specs[key1].Equals(alt.Specs[key2], StringComparison.OrdinalIgnoreCase))
                    matches.Add($"{specName.Split('/')[0].Trim()}: {orig.Specs[key1]}");
                else
                    mismatches.Add($"{specName.Split('/')[0].Trim()}: {orig.Specs[key1]} vs {alt.Specs[key2]}");
            }

            // Check Warning Specs
            foreach (var specName in warningSpecs)
            {
                var key1 = orig.Specs.Keys.FirstOrDefault(k => k.Contains(specName, StringComparison.OrdinalIgnoreCase));
                var key2 = alt.Specs.Keys.FirstOrDefault(k => k.Contains(specName, StringComparison.OrdinalIgnoreCase));

                if (key1 == null || key2 == null) continue;

                if (orig.Specs[key1].Equals(alt.Specs[key2], StringComparison.OrdinalIgnoreCase))
                    matches.Add($"{specName.Split('-')[0].Trim()}: {orig.Specs[key1]}");
                else
                    warnings.Add($"{specName.Split('-')[0].Trim()}: {orig.Specs[key1]} vs {alt.Specs[key2]}");
            }

            // Dynamically show where the data came from
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

        // ── NEXAR PARSER (Commented for future use) ─────────────────
        /*
        private PartDetails ParseNexarResponse(string json, string mpn)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("supSearchMpn", out var search) ||
                search.ValueKind == JsonValueKind.Null ||
                !search.TryGetProperty("results", out var results) ||
                results.GetArrayLength() == 0)
                return new PartDetails { Mpn = mpn, Source = "Not Found" };

            var part = results[0].GetProperty("part");

            // Extract specs
            var specs = new Dictionary<string, string>();
            if (part.TryGetProperty("specs", out var specList) && specList.ValueKind != JsonValueKind.Null)
            {
                foreach (var spec in specList.EnumerateArray())
                {
                    if (!spec.TryGetProperty("attribute", out var attr)) continue;
                    var name = attr.TryGetProperty("name", out var n) ? n.GetString() : null;
                    var val = spec.TryGetProperty("displayValue", out var v) ? v.GetString() : null;
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(val))
                        specs[name] = val;
                }
            }

            return new PartDetails
            {
                Mpn = part.TryGetProperty("mpn", out var m) ? m.GetString() : mpn,
                Description = part.TryGetProperty("shortDescription", out var d) ? d.GetString() : "",
                Manufacturer = part.TryGetProperty("manufacturer", out var mfr) && mfr.ValueKind != JsonValueKind.Null && mfr.TryGetProperty("name", out var mn) ? mn.GetString() : "",
                Category = part.TryGetProperty("category", out var cat) && cat.ValueKind != JsonValueKind.Null && cat.TryGetProperty("name", out var cn) ? cn.GetString() : "",
                DatasheetUrl = part.TryGetProperty("bestDatasheet", out var ds) && ds.ValueKind != JsonValueKind.Null && ds.TryGetProperty("url", out var du) ? du.GetString() : "",
                Specs = specs,
                Source = "Nexar"
            };
        }
        */
    }
}