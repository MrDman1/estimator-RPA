// Nuform.App/Services/PdfExportService.cs
using System;
using System.Collections.Generic;
using System.IO;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Nuform.App.Models;

namespace Nuform.App.Services
{
    public static class PdfExportService
    {
        // Landscape, auto-fit columns, wraps long names, paginates with repeating header.
        public static void ExportBom(string path, IEnumerable<BomRow> rows, string title)
        {
            var doc = new PdfDocument();

            // layout constants
            const double margin = 36; // 0.5"
            var fontTitle  = new XFont("Segoe UI", 14, XFontStyle.Bold);
            var fontHeader = new XFont("Segoe UI", 9,  XFontStyle.Bold);
            var fontCell   = new XFont("Segoe UI", 9,  XFontStyle.Regular);

            // desired column widths; they’ll be scaled to fit
            double[] col = { 120, 360, 70, 60, 70, 45, 80 }; // Part#, Name, Suggested, Change, Final, Unit, Category

            // page state
            var page = NewPage(doc);
            var gfx  = XGraphics.FromPdfPage(page);
            var area = ContentArea(page, margin);
            double x = area.Left, y = area.Top;

            // title
            gfx.DrawString(title ?? "Bill of Materials", fontTitle, XBrushes.Black, new XPoint(x, y));
            y += 24;

            // compute column scale to fit width
            double totalCol = 0; foreach (var w in col) totalCol += w;
            double scale = (area.ContentWidth) / totalCol;
            for (int i = 0; i < col.Length; i++) col[i] *= scale;

            // header drawing function
            void DrawHeader()
            {
                double hx = x;
                string[] headers = { "Part #", "Name", "Suggested", "Change", "Final", "Unit", "Category" };
                double rowH = 18;
                // background
                gfx.DrawRectangle(XPens.Black, XBrushes.LightGray, area.Left, y, area.ContentWidth, rowH);
                for (int i = 0; i < headers.Length; i++)
                {
                    gfx.DrawString(headers[i], fontHeader, XBrushes.Black, new XPoint(hx + 4, y + 13));
                    hx += col[i];
                    // vertical grid line
                    gfx.DrawLine(XPens.Black, area.Left + (hx - x), y, area.Left + (hx - x), y + rowH);
                }
                // bottom line
                gfx.DrawLine(XPens.Black, area.Left, y + rowH, area.Left + area.ContentWidth, y + rowH);
                y += rowH;
            }

            DrawHeader();

            foreach (var row in rows)
            {
                // Compute row height based on wrapped text in columns 0..6
                double[] heights = new double[col.Length];
                string[] cells = {
                    row.PartNumber ?? "",
                    row.Name ?? "",
                    row.SuggestedQty.ToString("N2"),
                    row.Change ?? "0",
                    row.FinalQty.ToString("N2"),
                    row.Unit ?? "",
                    row.Category ?? ""
                };


                for (int i = 0; i < col.Length; i++)
                {
                    heights[i] = MeasureWrappedHeight(gfx, fontCell, cells[i], col[i] - 8); // padding 4 left/right
                }
                double rowH = Math.Max(16, Max(heights));

                // page break if needed (leave space for next row and header)
                if (y + rowH > area.Bottom - 24)
                {
                    page = NewPage(doc);
                    gfx.Dispose();
                    gfx = XGraphics.FromPdfPage(page);
                    area = ContentArea(page, margin);
                    x = area.Left; y = area.Top;
                    gfx.DrawString(title ?? "Bill of Materials", fontTitle, XBrushes.Black, new XPoint(x, y));
                    y += 24;
                    DrawHeader();
                }

                // draw cell borders first (so text is on top, not through lines)
                double cx = x;
                for (int i = 0; i < col.Length; i++)
                {
                    gfx.DrawRectangle(XPens.Black, cx, y, col[i], rowH);
                    cx += col[i];
                }

                // draw text inside cells with padding
                cx = x;
                for (int i = 0; i < col.Length; i++)
                {
                    DrawWrapped(gfx, fontCell, cells[i], cx + 4, y + 12, col[i] - 8);
                    cx += col[i];
                }

                y += rowH;
            }

            using var fs = File.Create(path);
            doc.Save(fs);
        }

        private static (double Left, double Top, double Right, double Bottom, double ContentWidth) ContentArea(PdfPage page, double margin)
        {
            double left = margin, top = margin, right = page.Width - margin, bottom = page.Height - margin;
            return (left, top, right, bottom, right - left);
        }

        private static PdfPage NewPage(PdfDocument doc)
        {
            var p = doc.AddPage();
            p.Orientation = PdfSharpCore.PageOrientation.Landscape;
            return p;
        }

        private static double Max(double[] arr)
        {
            double m = 0; foreach (var v in arr) if (v > m) m = v; return m;
        }

        private static double MeasureWrappedHeight(XGraphics gfx, XFont font, string text, double maxWidth)
        {
            if (string.IsNullOrWhiteSpace(text)) return 16;
            var words = text.Split(' ');
            string line = "";
            double lineH = gfx.MeasureString("Ay", font).Height + 2;
            int lines = 1;
            foreach (var w in words)
            {
                var candidate = string.IsNullOrEmpty(line) ? w : line + " " + w;
                if (gfx.MeasureString(candidate, font).Width <= maxWidth)
                {
                    line = candidate;
                }
                else
                {
                    lines++;
                    line = w;
                }
            }
            return Math.Max(16, lines * lineH + 4);
        }

        private static void DrawWrapped(XGraphics gfx, XFont font, string text, double x, double y, double maxWidth)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var words = text.Split(' ');
            string line = "";
            double lineH = gfx.MeasureString("Ay", font).Height + 2;
            double yy = y;
            foreach (var w in words)
            {
                var candidate = string.IsNullOrEmpty(line) ? w : line + " " + w;
                if (gfx.MeasureString(candidate, font).Width <= maxWidth)
                {
                    line = candidate;
                }
                else
                {
                    gfx.DrawString(line, font, XBrushes.Black, new XPoint(x, yy));
                    yy += lineH;
                    line = w;
                }
            }
            if (!string.IsNullOrEmpty(line))
            {
                gfx.DrawString(line, font, XBrushes.Black, new XPoint(x, yy));
            }
        }
    }
}
