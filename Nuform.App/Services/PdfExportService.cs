using System.Collections.Generic;
using System.IO;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Nuform.App.Models;

namespace Nuform.App.Services;

public static class PdfExportService
{
    public static void ExportBom(string path, IEnumerable<BomRow> rows, string title)
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var fontH = new XFont("Segoe UI", 16, XFontStyle.Bold);
        var font = new XFont("Segoe UI", 10, XFontStyle.Regular);

        double y = 40;
        gfx.DrawString(title, fontH, XBrushes.Black, new XPoint(40, y)); y += 20;

        // header
        y += 10;
        DrawRow(gfx, ref y, font, true, "Part #", "Name", "Suggested", "Change", "Final", "Unit", "Category");

        foreach (var r in rows)
        {
            DrawRow(gfx, ref y, font, false, r.PartNumber, r.Name,
                    r.SuggestedQty.ToString("N2"), r.Change, r.FinalQty.ToString("N2"), r.Unit, r.Category);

            if (y > page.Height - 40)
            {
                page = doc.AddPage(); gfx = XGraphics.FromPdfPage(page); y = 40;
                DrawRow(gfx, ref y, font, true, "Part #", "Name", "Suggested", "Change", "Final", "Unit", "Category");
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        doc.Save(path);
    }

    private static void DrawRow(XGraphics gfx, ref double y, XFont font, bool header,
        string part, string name, string sugg, string change, string final, string unit, string cat)
    {
        var brush = header ? XBrushes.DarkSlateGray : XBrushes.Black;
        double x = 40;
        gfx.DrawString(part, font, brush, new XPoint(x, y)); x += 120;
        gfx.DrawString(name, font, brush, new XPoint(x, y)); x += 250;
        gfx.DrawString(sugg, font, brush, new XPoint(x, y)); x += 70;
        gfx.DrawString(change, font, brush, new XPoint(x, y)); x += 60;
        gfx.DrawString(final, font, brush, new XPoint(x, y)); x += 70;
        gfx.DrawString(unit, font, brush, new XPoint(x, y)); x += 40;
        gfx.DrawString(cat, font, brush, new XPoint(x, y));
        y += 18;
    }
}
