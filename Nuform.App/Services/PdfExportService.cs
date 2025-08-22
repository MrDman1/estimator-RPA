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
            var ctx  = CreatePageContext(page, margin);

            ScaleToFit(col, ctx.ContentWidth);

            double y = ctx.Top;

            DrawTitle(gfx, fontTitle, title, ctx.Left, ref y);
            DrawHeader(gfx, fontHeader, ctx, col, ref y);

            foreach (var r in rows)
            {
                var cells = new[]
                {
                    r.PartNumber ?? "",
                    r.Name ?? "",
                    r.SuggestedQty.ToString("N2"),
                    string.IsNullOrWhiteSpace(r.Change) ? "0" : r.Change,
                    r.FinalQty.ToString("N2"),
                    r.Unit ?? "",
                    r.Category ?? ""
                };

                double lineH  = MeasureLineHeight(gfx, fontCell);
                int    lines  = MaxWrapLines(gfx, fontCell, cells, col);
                double rowH   = lines * lineH + 6; // padding

                // page break?
                if (y + rowH > ctx.Bottom)
                {
                    gfx.Dispose();
                    page = NewPage(doc);
                    gfx  = XGraphics.FromPdfPage(page);
                    ctx  = CreatePageContext(page, margin);
                    y    = ctx.Top;

                    DrawTitle(gfx, fontTitle, title, ctx.Left, ref y);
                    DrawHeader(gfx, fontHeader, ctx, col, ref y);
                }

                DrawRow(gfx, fontCell, ctx, col, cells, ref y);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            gfx.Dispose();
            doc.Save(path);
        }

        // ===== helpers =====

        private struct PageContext
        {
            public double Left, Top, Right, Bottom, ContentWidth;
        }

        private static double MeasureLineHeight(XGraphics gfx, XFont font)
            => gfx.MeasureString("Ag", font).Height + 2; // small padding

        private static int MaxWrapLines(XGraphics gfx, XFont font, string[] cells, double[] widths)
        {
            int max = 1;
            for (int i = 0; i < cells.Length; i++)
                max = Math.Max(max, WrapCount(gfx, font, cells[i] ?? "", widths[i]));
            return max;
        }

        private static PageContext CreatePageContext(PdfPage page, double margin)
        {
            var left = margin;
            var right = page.Width - margin;
            var top = margin;
            var bottom = page.Height - margin;
            return new PageContext
            {
                Left = left,
                Right = right,
                Top = top,
                Bottom = bottom,
                ContentWidth = right - left
            };
        }

        private static PdfPage NewPage(PdfDocument doc)
        {
            var p = doc.AddPage();
            p.Orientation = PdfSharpCore.PageOrientation.Landscape;
            return p;
        }

        private static void DrawTitle(XGraphics gfx, XFont font, string title, double x, ref double y)
        {
            gfx.DrawString(title, font, XBrushes.Black, new XPoint(x, y));
            y += 24;
        }

        private static void DrawHeader(XGraphics gfx, XFont font, PageContext ctx, double[] col, ref double y)
        {
            string[] headers = { "Part #", "Name", "Suggested", "Change", "Final", "Unit", "Category" };
            double rowH = MeasureLineHeight(gfx, font) + 8;

            gfx.DrawRectangle(XBrushes.LightGray, ctx.Left, y - 2, ctx.ContentWidth, rowH);

            double x = ctx.Left;
            for (int i = 0; i < col.Length; i++)
            {
                gfx.DrawString(headers[i], font, XBrushes.Black, new XPoint(x + 2, y + 2));
                x += col[i];
            }

            gfx.DrawLine(XPens.LightGray, ctx.Left, y + rowH, ctx.Right, y + rowH);
            y += rowH;
        }

        private static void DrawRow(XGraphics gfx, XFont font, PageContext ctx, double[] col, string[] cells, ref double y)
        {
            double lineH = MeasureLineHeight(gfx, font);
            int    lines = MaxWrapLines(gfx, font, cells, col);
            double rowH  = lines * lineH + 6;

            double x = ctx.Left;
            for (int i = 0; i < col.Length; i++)
            {
                DrawWrapped(gfx, font, cells[i] ?? "", x + 2, y + 3, col[i] - 4, lineH);
                x += col[i];
            }
            gfx.DrawLine(XPens.LightGray, ctx.Left, y + rowH, ctx.Right, y + rowH);
            y += rowH;
        }

        private static void ScaleToFit(double[] widths, double available)
        {
            double total = 0; foreach (var w in widths) total += w;
            if (total <= available) return;
            double scale = available / total;
            for (int i = 0; i < widths.Length; i++) widths[i] *= scale;
        }

        private static int WrapCount(XGraphics gfx, XFont font, string text, double maxWidth)
        {
            if (string.IsNullOrWhiteSpace(text)) return 1;
            var words = text.Split(' ');
            int lines = 1; string current = "";
            foreach (var w in words)
            {
                var candidate = current.Length == 0 ? w : current + " " + w;
                var sz = gfx.MeasureString(candidate, font);
                if (sz.Width <= maxWidth) current = candidate;
                else { lines++; current = w; }
            }
            return Math.Max(1, lines);
        }

        private static void DrawWrapped(XGraphics gfx, XFont font, string text, double x, double y, double maxWidth, double lineH)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var words = text.Split(' ');
            string line = ""; double yy = y;
            foreach (var w in words)
            {
                var candidate = line.Length == 0 ? w : line + " " + w;
                var sz = gfx.MeasureString(candidate, font);
                if (sz.Width <= maxWidth) line = candidate;
                else { gfx.DrawString(line, font, XBrushes.Black, new XPoint(x, yy)); yy += lineH; line = w; }
            }
            if (line.Length > 0) gfx.DrawString(line, font, XBrushes.Black, new XPoint(x, yy));
        }
    }
}
