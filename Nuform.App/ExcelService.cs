using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Nuform.Core;
using Excel = Microsoft.Office.Interop.Excel;

namespace Nuform.App;

public static class ExcelService
{
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
        if (cat.Contains("Specialty", StringComparison.OrdinalIgnoreCase) || cat.Contains("Accessory", StringComparison.OrdinalIgnoreCase))
            return "Specialty/Accessories";
        return "Other";
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
        foreach (var kvp in res.WallPanels)
            items.Add(new ExcelLineItem { Category = "RELINE", Description = $"Wall Panel {kvp.Key}'", Qty = kvp.Value });
        foreach (var kvp in res.CeilingPanels)
            items.Add(new ExcelLineItem { Category = "RELINE", Description = $"Ceiling Panel {kvp.Key}'", Qty = kvp.Value });
        return items;
    }

    static string FillAndPrint(string templatePath, string outputDir, string printer, Dictionary<string, string> headers, List<ExcelLineItem> items, string estimateNumber)
    {
        Excel.Application? app = null;
        Excel.Workbook? wb = null;
        Excel.Worksheet? ws = null;
        try
        {
            app = new Excel.Application { Visible = false };
            wb = app.Workbooks.Open(templatePath);
            ws = (Excel.Worksheet)wb.ActiveSheet;
            foreach (var kvp in headers)
            {
                Excel.Range? rng = null;
                try { rng = ws.Range[kvp.Key]; }
                catch { }
                if (rng == null && HeaderCells.TryGetValue(kvp.Key, out var addr))
                    rng = ws.Range[addr];
                if (rng != null)
                {
                    rng.Value2 = kvp.Value;
                    Marshal.FinalReleaseComObject(rng);
                }
            }

            foreach (var grp in items.GroupBy(i => i.Category))
            {
                Excel.Worksheet? catSheet = null;
                try { catSheet = wb.Worksheets[grp.Key]; }
                catch { }
                if (catSheet == null) continue;
                int row = 5;
                foreach (var item in grp)
                {
                    catSheet.Cells[row, 1].Value2 = item.Description;
                    catSheet.Cells[row, 2].Value2 = item.Qty;
                    catSheet.Cells[row, 3].Value2 = (double)item.UnitPrice;
                    catSheet.Cells[row, 4].Value2 = (double)item.Extended;
                    row++;
                }
                Marshal.FinalReleaseComObject(catSheet);
            }

            var naming = FileNaming.Build(estimateNumber, null, null, null, null);
            var savePath = Path.Combine(outputDir, estimateNumber + Path.GetExtension(templatePath));
            wb.SaveAs(savePath);
            var pdfPath = Path.Combine(outputDir, naming.EstimatePdfName);
            app.ActivePrinter = printer;
            wb.ExportAsFixedFormat(Excel.XlFixedFormatType.xlTypePDF, pdfPath);
            return pdfPath;
        }
        finally
        {
            if (wb != null)
            {
                wb.Close(false);
                Marshal.FinalReleaseComObject(wb);
            }
            if (ws != null) Marshal.FinalReleaseComObject(ws);
            if (app != null)
            {
                app.Quit();
                Marshal.FinalReleaseComObject(app);
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
