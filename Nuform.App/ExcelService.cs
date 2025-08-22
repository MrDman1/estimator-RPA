using Excel = Microsoft.Office.Interop.Excel;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Nuform.Core;
using Nuform.Core.LegacyCompat;

namespace Nuform.App;

public static class ExcelService
{
    // Map logical header keys -> actual A1 addresses in the template
    static readonly Dictionary<string, string> HeaderCells = new()
    {
        ["EstimateNumber"] = "B2",
        ["Customer"] = "B3",
        ["Project"] = "B4",
        ["Date"] = "B5"
    };

    public static string FillAndPrint(AppConfig cfg, string estimateNumber, EstimateResult result, string outputDir)
    {
        var headers = new Dictionary<string, string>
        {
            ["EstimateNumber"] = estimateNumber,
            ["Customer"] = "Customer",
            ["Project"] = "Project",
            ["Date"] = DateTime.Now.ToShortDateString()
        };

        var catalog = CatalogService.Load("RELINE Part List 2025-1-0.pdf");
        var lineItems = BuildLineItems(result, catalog);

        Directory.CreateDirectory(outputDir);
        return FillAndPrint(cfg.ExcelTemplatePath, outputDir, cfg.PdfPrinter, headers, lineItems, estimateNumber);
    }

    class ExcelLineItem
    {
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Extended => Qty * UnitPrice;
    }

    static string MapCategory(string cat)
    {
        if (cat.Contains("Shipping", StringComparison.OrdinalIgnoreCase)) return "Shipping";
        if (cat.Contains("RELINEPRO", StringComparison.OrdinalIgnoreCase)) return "RELINEPRO";
        if (cat.Contains("RELINE", StringComparison.OrdinalIgnoreCase)) return "RELINE";
        if (cat.Contains("Specialty", StringComparison.OrdinalIgnoreCase) ||
            cat.Contains("Accessory", StringComparison.OrdinalIgnoreCase)) return "Specialty/Accessories";
        return "Other";
    }

    class TrimSummary
    {
        public decimal JTrimLf;
        public int OutsideCorners;
        public int InsideCorners;
        public int EndCaps;
    }

    static TrimSummary CalculateTrims(IEnumerable<Room> rooms, bool useCeilingPanels)
    {
        decimal perimeter = 0;
        foreach (var r in rooms)
            perimeter += (decimal)(2 * (r.LengthFt + r.WidthFt));
        return new TrimSummary
        {
            JTrimLf = Math.Ceiling(perimeter),
            OutsideCorners = useCeilingPanels ? 4 : 0,
            InsideCorners = 0,
            EndCaps = 0
        };
    }

    static List<ExcelLineItem> BuildLineItems(EstimateResult res, IReadOnlyList<CatalogItem> catalog)
    {
        var lookup = catalog.ToDictionary(c => c.PartCode, StringComparer.OrdinalIgnoreCase);
        var items = new List<ExcelLineItem>();

        foreach (var part in res.Parts)
        {
            if (!lookup.TryGetValue(part.PartCode, out var catItem)) continue;
            items.Add(new ExcelLineItem
            {
                Category = MapCategory(catItem.Category),
                Description = catItem.Description,
                Qty = part.QtyPacks,
                UnitPrice = catItem.PriceUSD
            });
        }

        if (res.Hardware.PlugSpacerPacks > 0)
            items.Add(new ExcelLineItem { Category = "Specialty/Accessories", Description = "Plug/Spacer Packs", Qty = res.Hardware.PlugSpacerPacks });
        if (res.Hardware.ExpansionTools > 0)
            items.Add(new ExcelLineItem { Category = "Specialty/Accessories", Description = "Expansion Tools", Qty = res.Hardware.ExpansionTools });
        if (res.Hardware.ScrewBoxes > 0)
            items.Add(new ExcelLineItem { Category = "Specialty/Accessories", Description = "Screw Boxes", Qty = res.Hardware.ScrewBoxes });

        var trims = CalculateTrims(res.Rooms, res.CeilingPanels.Any());
        void AddTrim(string desc, int qty)
        {
            if (qty > 0)
                items.Add(new ExcelLineItem { Category = "Specialty/Accessories", Description = desc, Qty = qty, UnitPrice = 0m });
        }
        AddTrim("J-Trim (LF)", (int)trims.JTrimLf);
        AddTrim("Outside Corners", trims.OutsideCorners);
        AddTrim("Inside Corners", trims.InsideCorners);
        AddTrim("End Caps", trims.EndCaps);

        foreach (var kvp in res.WallPanels)
            items.Add(new ExcelLineItem { Category = "RELINE", Description = $"Wall Panel {kvp.Key}'", Qty = kvp.Value });
        foreach (var kvp in res.CeilingPanels)
            items.Add(new ExcelLineItem { Category = "RELINE", Description = $"Ceiling Panel {kvp.Key}'", Qty = kvp.Value });

        return items;
    }

    static string FillAndPrint(
        string templatePath,
        string outputDir,
        string printer,
        Dictionary<string, string> headers,
        List<ExcelLineItem> items,
        string estimateNumber)
    {
        Excel.Application? app = null;
        Excel.Workbook? wb = null;
        Excel.Worksheet? ws = null;

        try
        {
            app = new Excel.Application { Visible = false, DisplayAlerts = false };
            wb = app.Workbooks.Open(templatePath);
            ws = (Excel.Worksheet)wb.ActiveSheet;

            // --- Write headers (use our HeaderCells map for real A1 addresses)
            foreach (var kvp in headers)
            {
                if (!HeaderCells.TryGetValue(kvp.Key, out var addr))
                    continue; // unknown header key, skip

                var rng = (Excel.Range)ws.Range[addr];
                try
                {
                    // Write to top-left if merged
                    bool merged = rng.MergeCells is bool b && b;
                    if (merged)
                    {
                        var topLeft = (Excel.Range)rng.MergeArea.Cells[1, 1];
                        topLeft.Value2 = kvp.Value;
                        Marshal.FinalReleaseComObject(topLeft);
                    }
                    else
                    {
                        rng.Value2 = kvp.Value;
                    }
                }
                finally
                {
                    Marshal.FinalReleaseComObject(rng);
                }
            }

            // --- Write line items by category (sheet name = category)
            foreach (var grp in items.GroupBy(i => i.Category))
            {
                Excel.Worksheet? catSheet = null;
                try
                {
                    catSheet = (Excel.Worksheet)wb.Worksheets[grp.Key];
                }
                catch
                {
                    catSheet = null; // sheet not found → skip this category
                }

                if (catSheet is null) continue;

                int row = 5;
                foreach (var item in grp)
                {
                    ((Excel.Range)catSheet.Cells[row, 1]).Value2 = item.Description;
                    ((Excel.Range)catSheet.Cells[row, 2]).Value2 = item.Qty;
                    ((Excel.Range)catSheet.Cells[row, 3]).Value2 = (double)item.UnitPrice;
                    ((Excel.Range)catSheet.Cells[row, 4]).Value2 = (double)item.Extended;
                    row++;
                }

                Marshal.FinalReleaseComObject(catSheet);
            }

            // --- Save copy & export PDF
            var naming = FileNaming.Build(estimateNumber, null, null, null, null);
            var saveXls = Path.Combine(outputDir, estimateNumber + Path.GetExtension(templatePath));
            wb.SaveAs(saveXls);

            var pdfPath = Path.Combine(outputDir, naming.EstimatePdfName);
            app.ActivePrinter = printer;
            wb.ExportAsFixedFormat(Excel.XlFixedFormatType.xlTypePDF, pdfPath);

            return pdfPath;
        }
        finally
        {
            // Close & release COM
            if (wb is not null)
            {
                wb.Close(false);
                Marshal.FinalReleaseComObject(wb);
            }
            if (ws is not null) Marshal.FinalReleaseComObject(ws);
            if (app is not null)
            {
                app.Quit();
                Marshal.FinalReleaseComObject(app);
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    public static string FillAndPrint(
        AppConfig cfg, string estimateNumber, EstimateResult result, string outputDir,
        int? insideCornerOverride, int? outsideCornerOverride, decimal? jTrimLfOverride, decimal? ceilingTrimLfOverride)
    {
        var headers = new Dictionary<string, string>
        {
            ["EstimateNumber"] = estimateNumber,
            ["Customer"] = "Customer",
            ["Project"] = "Project",
            ["Date"] = DateTime.Now.ToShortDateString()
        };

        var catalog = CatalogService.Load("RELINE Part List 2025-1-0.pdf");
        var items = BuildLineItems(result, catalog);

        var calced = CalculateTrims(result.Rooms, result.CeilingPanels.Any());
        int inside = insideCornerOverride ?? 0;
        int outside = outsideCornerOverride ?? 0;
        int jtrim = (int)(jTrimLfOverride ?? calced.JTrimLf);
        int ceilLf = (int)(ceilingTrimLfOverride ?? 0);

        void AddTrim(string desc, int qty)
        {
            if (qty > 0)
                items.Add(new ExcelLineItem { Category = "Specialty/Accessories", Description = desc, Qty = qty, UnitPrice = 0m });
        }

        AddTrim("J-Trim (LF)", jtrim);
        AddTrim("Ceiling Trim (LF)", ceilLf);
        AddTrim("Inside Corners", inside);
        AddTrim("Outside Corners", outside);

        Directory.CreateDirectory(outputDir);
        return FillAndPrint(cfg.ExcelTemplatePath, outputDir, cfg.PdfPrinter, headers, items, estimateNumber);
    }
}
