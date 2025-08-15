using System;
using System.Collections.Generic;
using System.IO;
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
        var lineItems = BuildLineItems(result);
        Directory.CreateDirectory(outputDir);
        return FillAndPrint(cfg.ExcelTemplatePath, outputDir, cfg.PdfPrinter, headers, lineItems);
    }

    static List<(string Desc, int Qty)> BuildLineItems(EstimateResult res)
    {
        var items = new List<(string, int)>();
        foreach (var kvp in res.WallPanels)
            items.Add(($"Wall Panel {kvp.Key}'", kvp.Value));
        foreach (var kvp in res.CeilingPanels)
            items.Add(($"Ceiling Panel {kvp.Key}'", kvp.Value));
        foreach (var part in res.Parts)
            items.Add(($"{part.PartCode}", part.QtyPacks));
        items.Add(($"Plug/Spacer Packs", res.Hardware.PlugSpacerPacks));
        items.Add(($"Expansion Tools", res.Hardware.ExpansionTools));
        items.Add(($"Screw Boxes", res.Hardware.ScrewBoxes));
        return items;
    }

    static string FillAndPrint(string templatePath, string outputDir, string printer, Dictionary<string, string> headers, List<(string Desc, int Qty)> items)
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
            Excel.ListObject? table = null;
            try { table = ws.ListObjects["TableLineItems"]; }
            catch { }
            if (table != null)
            {
                foreach (var item in items)
                {
                    var row = table.ListRows.Add();
                    row.Range.Cells[1, 1].Value2 = item.Desc;
                    row.Range.Cells[1, 2].Value2 = item.Qty;
                    Marshal.FinalReleaseComObject(row);
                }
                Marshal.FinalReleaseComObject(table);
            }
            else
            {
                int startRow = 20;
                for (int i = 0; i < items.Count; i++)
                {
                    ws.Cells[startRow + i, 1].Value2 = items[i].Desc;
                    ws.Cells[startRow + i, 2].Value2 = items[i].Qty;
                }
            }
            var savePath = Path.Combine(outputDir, Path.GetFileName(templatePath));
            wb.SaveAs(savePath);
            var pdfPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(savePath) + ".pdf");
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
