using Alter_Parts.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace Alter_Parts.Services
{
    public class ExcelExportService
    {
        // ── Colours ──────────────────────────────────────────────────────────────
        private static readonly Color HeaderBg = Color.FromArgb(0x00, 0x33, 0x66); // Dark blue
        private static readonly Color HeaderFg = Color.White;
        private static readonly Color LinkColor = Color.FromArgb(0x00, 0x70, 0xC0); // Excel blue
        private static readonly Color AltRowColor = Color.FromArgb(0xF2, 0xF7, 0xFF); // Light blue tint
        private static readonly Color MouserColor = Color.FromArgb(0xFF, 0xF3, 0xE0); // Warm amber tint
        private static readonly Color DigiKeyColor = Color.FromArgb(0xE8, 0xF5, 0xE9); // Soft green tint
        private static readonly Color LcscColor = Color.FromArgb(0xE8, 0xEA, 0xFF); // Soft purple tint

        private static readonly string[] Headers =
        {
            "Requested Description", // A - merged per group
            "Distributor",           // B
            "MPN",                   // C
            "Manufacturer",          // D
            "Price ($)",             // E
            "Stock",                 // F
            "Product Page Link"      // G
        };

        private static readonly double[] ColWidths = { 42, 14, 24, 30, 12, 12, 28 };

        public byte[] GenerateReport(IReadOnlyList<BulkResultGroup> groups)
        {
            

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Sourcing Results");

            WriteHeader(sheet);
            WriteData(sheet, groups);
            ApplySheetSettings(sheet);

            return package.GetAsByteArray();
        }

        // ── Header Row ───────────────────────────────────────────────────────────

        private static void WriteHeader(ExcelWorksheet ws)
        {
            for (int col = 1; col <= Headers.Length; col++)
            {
                var cell = ws.Cells[1, col];
                cell.Value = Headers[col - 1];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Name = "Arial";
                cell.Style.Font.Size = 10;
                cell.Style.Font.Color.SetColor(HeaderFg);
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(HeaderBg);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                cell.Style.WrapText = true;
            }
            ws.Row(1).Height = 28;
        }

        // ── Data Rows ────────────────────────────────────────────────────────────

        private static void WriteData(ExcelWorksheet ws, IReadOnlyList<BulkResultGroup> groups)
        {
            int currentRow = 2;

            foreach (var group in groups)
            {
                int rowCount = Math.Max(group.Results.Count, 1);
                int firstRow = currentRow;
                int lastRow = currentRow + rowCount - 1;

                // Write the description into the first row of the group
                var descCell = ws.Cells[firstRow, 1];
                descCell.Value = group.RequestedDescription;
                descCell.Style.Font.Name = "Arial";
                descCell.Style.Font.Size = 10;
                descCell.Style.Font.Bold = true;
                descCell.Style.WrapText = true;

                if (group.Results.Count == 0)
                {
                    // No results — write a placeholder row
                    SetCell(ws, currentRow, 2, "No results found");
                    for (int c = 3; c <= 7; c++) SetCell(ws, currentRow, c, "—");
                    currentRow++;
                }
                else
                {
                    foreach (var part in group.Results)
                    {
                        // Column B–F: plain text values from PartDetails
                        SetCell(ws, currentRow, 2, part.Source);
                        SetCell(ws, currentRow, 3, part.Mpn);         // ← uses Mpn (not MpnNumber)
                        SetCell(ws, currentRow, 4, part.Manufacturer);

                        // Price: prefer part.Price, fallback to Specs["Unit Price"]
                        var price = !string.IsNullOrWhiteSpace(part.Price)
                            ? part.Price
                            : part.Specs.GetValueOrDefault("Unit Price", "N/A");
                        SetCell(ws, currentRow, 5, price);

                        SetCell(ws, currentRow, 6, part.Stock);

                        // Column G: clickable hyperlink
                        WriteHyperlink(ws, currentRow, 7, part.ProductUrl, $"View on {part.Source}");

                        // Tint each row by distributor for quick visual scanning
                        ApplyDistributorTint(ws, currentRow, part.Source);

                        currentRow++;
                    }
                }

                // Merge Column A across all rows in this group
                if (rowCount > 1)
                {
                    var merge = ws.Cells[firstRow, 1, lastRow, 1];
                    merge.Merge = true;
                    merge.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    merge.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    merge.Style.WrapText = true;
                }
                else
                {
                    descCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    descCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                }

                // Thin separator line after each group
                ws.Cells[lastRow, 1, lastRow, Headers.Length]
                  .Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }
        }

        // ── Cell Helpers ─────────────────────────────────────────────────────────

        private static void SetCell(ExcelWorksheet ws, int row, int col, string? value)
        {
            var cell = ws.Cells[row, col];
            cell.Value = value ?? string.Empty;
            cell.Style.Font.Name = "Arial";
            cell.Style.Font.Size = 10;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            cell.Style.HorizontalAlignment = col == 2
                ? ExcelHorizontalAlignment.Center  // Distributor column centred
                : ExcelHorizontalAlignment.Left;
        }

        private static void WriteHyperlink(
            ExcelWorksheet ws, int row, int col,
            string? url, string displayText)
        {
            var cell = ws.Cells[row, col];

            if (!string.IsNullOrWhiteSpace(url) &&
                Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                cell.Hyperlink = uri;
                cell.Style.Font.UnderLine = true;
                cell.Style.Font.Color.SetColor(LinkColor);
            }

            cell.Value = displayText;
            cell.Style.Font.Name = "Arial";
            cell.Style.Font.Size = 10;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        private static void ApplyDistributorTint(
            ExcelWorksheet ws, int row, string? source)
        {
            var color = source switch
            {
                "Mouser" => MouserColor,
                "DigiKey" => DigiKeyColor,
                "LCSC" => LcscColor,
                _ => Color.White
            };

            // Only tint columns B–G; Column A is handled by group merge
            var range = ws.Cells[row, 2, row, Headers.Length];
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(color);
        }

        // ── Sheet-Level Settings ─────────────────────────────────────────────────

        private static void ApplySheetSettings(ExcelWorksheet ws)
        {
            // Column widths
            for (int col = 1; col <= ColWidths.Length; col++)
                ws.Column(col).Width = ColWidths[col - 1];

            // AutoFilter on header row
            if (ws.Dimension != null)
                ws.Cells[1, 1, 1, Headers.Length].AutoFilter = true;

            // Freeze top row
            ws.View.FreezePanes(2, 1);

            // Comfortable zoom
            ws.View.ZoomScale = 90;

            // Tab colour matches header
            ws.TabColor = HeaderBg;
        }
    }
}
