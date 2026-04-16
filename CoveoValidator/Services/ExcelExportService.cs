using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CoveoValidator.Models.Coveo;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace CoveoValidator.Services
{
    /// <summary>
    /// Builds an Excel workbook from Coveo validation results.
    /// Sheet 1 (Summary)      — one row per item with overall status and field counts.
    /// Sheet 2 (Field Detail) — one row per field with status and value.
    /// </summary>
    public static class ExcelExportService
    {
        // Bootstrap-flavoured status colours (light variants).
        private static readonly Color ColourMissing  = Color.FromArgb(0xF8, 0xD7, 0xDA); // danger-subtle
        private static readonly Color ColourEmpty    = Color.FromArgb(0xFF, 0xF3, 0xCD); // warning-subtle
        private static readonly Color ColourValid    = Color.FromArgb(0xD1, 0xE7, 0xDD); // success-subtle
        private static readonly Color ColourNotFound = Color.FromArgb(0xE2, 0xE3, 0xE5); // secondary-subtle
        private static readonly Color ColourHeader   = Color.FromArgb(0x21, 0x25, 0x29); // dark

        public static byte[] Export(List<ComparisonResultModel> results, string language)
        {
            using (var package = new ExcelPackage())
            {
                BuildSummarySheet(package, results, language);
                BuildDetailSheet(package, results);
                return package.GetAsByteArray();
            }
        }

        // ── Sheet 1: Summary ────────────────────────────────────────────────────

        private static void BuildSummarySheet(
            ExcelPackage package,
            List<ComparisonResultModel> results,
            string language)
        {
            var ws = package.Workbook.Worksheets.Add("Summary");

            // Headers
            var headers = new[] { "SriggleId", "Title", "ItemId", "ContentType", "Language",
                                   "Missing", "Empty", "Valid", "Overall Status",
                                   "Missing Field Names", "Empty Field Names" };
            for (int c = 0; c < headers.Length; c++)
                ws.Cells[1, c + 1].Value = headers[c];

            StyleHeader(ws.Cells[1, 1, 1, headers.Length]);

            int row = 2;
            foreach (var item in results)
            {
                ws.Cells[row, 1].Value = item.SriggleId;
                ws.Cells[row, 2].Value = item.Title;
                ws.Cells[row, 3].Value = item.ItemId;
                ws.Cells[row, 4].Value = item.ContentType;
                ws.Cells[row, 5].Value = language;
                ws.Cells[row, 6].Value = item.MissingCount;
                ws.Cells[row, 7].Value = item.EmptyCount;
                ws.Cells[row, 8].Value = item.ValidCount;

                string status;
                Color colour;

                if (item.IsNotFound)
                {
                    status = "Not Found";
                    colour = ColourNotFound;
                }
                else if (item.HasError)
                {
                    status = "Error";
                    colour = ColourMissing;
                }
                else if (item.HasMissing)
                {
                    status = "Missing Fields";
                    colour = ColourMissing;
                }
                else if (item.HasEmpty)
                {
                    status = "Empty Fields";
                    colour = ColourEmpty;
                }
                else
                {
                    status = "Valid";
                    colour = ColourValid;
                }

                var statusCell = ws.Cells[row, 9];
                statusCell.Value = status;
                statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                statusCell.Style.Fill.BackgroundColor.SetColor(colour);

                var v = item.SafeValidation;

                var missingNames = string.Join(", ", v.MissingFields.Select(f => f.FieldName));
                var emptyNames   = string.Join(", ", v.EmptyFields.Select(f => f.FieldName));

                var missingCell = ws.Cells[row, 10];
                missingCell.Value = missingNames;
                if (missingNames.Length > 0)
                {
                    missingCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    missingCell.Style.Fill.BackgroundColor.SetColor(ColourMissing);
                }

                var emptyCell = ws.Cells[row, 11];
                emptyCell.Value = emptyNames;
                if (emptyNames.Length > 0)
                {
                    emptyCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    emptyCell.Style.Fill.BackgroundColor.SetColor(ColourEmpty);
                }

                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            // Cap the field-name list columns so they don't blow out the sheet.
            ws.Column(10).Width = System.Math.Min(ws.Column(10).Width, 60);
            ws.Column(11).Width = System.Math.Min(ws.Column(11).Width, 60);
        }

        // ── Sheet 2: Field Detail ────────────────────────────────────────────────

        private static void BuildDetailSheet(
            ExcelPackage package,
            List<ComparisonResultModel> results)
        {
            var ws = package.Workbook.Worksheets.Add("Field Detail");

            var headers = new[] { "SriggleId", "Title", "ContentType", "FieldName", "Status", "Value" };
            for (int c = 0; c < headers.Length; c++)
                ws.Cells[1, c + 1].Value = headers[c];

            StyleHeader(ws.Cells[1, 1, 1, headers.Length]);

            int row = 2;
            foreach (var item in results)
            {
                if (item.HasError || item.IsNotFound)
                    continue;

                var v = item.SafeValidation;

                foreach (var f in v.MissingFields)
                    WriteDetailRow(ws, row++, item, f.FieldName, "Missing", null, ColourMissing);

                foreach (var f in v.EmptyFields)
                    WriteDetailRow(ws, row++, item, f.FieldName, "Empty", f.Value, ColourEmpty);

                foreach (var f in v.ValidFields)
                    WriteDetailRow(ws, row++, item, f.FieldName, "Valid", f.Value, ColourValid);
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            // Cap the Value column so long JSON doesn't blow out the sheet.
            if (ws.Dimension != null)
                ws.Column(6).Width = System.Math.Min(ws.Column(6).Width, 80);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static void WriteDetailRow(
            ExcelWorksheet ws, int row, ComparisonResultModel item,
            string fieldName, string status, string value, Color statusColour)
        {
            ws.Cells[row, 1].Value = item.SriggleId;
            ws.Cells[row, 2].Value = item.Title;
            ws.Cells[row, 3].Value = item.ContentType;
            ws.Cells[row, 4].Value = fieldName;
            ws.Cells[row, 5].Value = status;
            ws.Cells[row, 6].Value = Truncate(value, 500);

            var statusCell = ws.Cells[row, 5];
            statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            statusCell.Style.Fill.BackgroundColor.SetColor(statusColour);
        }

        private static void StyleHeader(ExcelRange range)
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(ColourHeader);
            range.Style.Font.Color.SetColor(Color.White);
        }

        private static string Truncate(string value, int max)
        {
            if (value == null) return null;
            return value.Length > max ? value.Substring(0, max) + "…" : value;
        }
    }
}
