using System.IO;
using System.Collections.Generic;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using Nuform.App.Models;

namespace Nuform.App.Services;

public static class PdfExportService
{
    public static void ExportBom(string path, IEnumerable<BomRow> rows, string title)
    {
        var doc = new Document();
        doc.Info.Title = title;
        var section = doc.AddSection();
        section.PageSetup.Orientation = Orientation.Landscape;
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.2);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.2);
        section.PageSetup.TopMargin = Unit.FromCentimeter(1.0);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.0);

        var h = section.AddParagraph(title);
        h.Format.Font.Size = 14;
        h.Format.Font.Bold = true;
        h.Format.SpaceAfter = Unit.FromPoint(12);

        var table = section.AddTable();
        table.Borders.Width = 0.5;

        table.AddColumn(Unit.FromCentimeter(4.0));
        table.AddColumn(Unit.FromCentimeter(10.0));
        table.AddColumn(Unit.FromCentimeter(3.0));
        table.AddColumn(Unit.FromCentimeter(3.0));
        table.AddColumn(Unit.FromCentimeter(3.0));
        table.AddColumn(Unit.FromCentimeter(2.0));
        table.AddColumn(Unit.FromCentimeter(2.0));

        var header = table.AddRow();
        header.HeadingFormat = true;
        header.Format.Alignment = ParagraphAlignment.Left;
        header.Shading.Color = Colors.LightGray;
        header.Cells[0].AddParagraph("Part #");
        header.Cells[1].AddParagraph("Name");
        header.Cells[2].AddParagraph("Suggested");
        header.Cells[3].AddParagraph("Change");
        header.Cells[4].AddParagraph("Final");
        header.Cells[5].AddParagraph("Unit");
        header.Cells[6].AddParagraph("Category");

        foreach (var r in rows)
        {
            var row = table.AddRow();
            row.Cells[0].AddParagraph(r.PartNumber);
            var pName = row.Cells[1].AddParagraph(r.Name);
            pName.Format.Font.Size = 9;
            row.Cells[2].AddParagraph(r.SuggestedQty.ToString("N2"));
            row.Cells[3].AddParagraph(string.IsNullOrWhiteSpace(r.Change) ? "0" : r.Change);
            row.Cells[4].AddParagraph(r.FinalQty.ToString("N2"));
            row.Cells[5].AddParagraph(r.Unit);
            row.Cells[6].AddParagraph(r.Category);
        }

        table.SetEdge(0, 0, 7, table.Rows.Count, Edge.Box, BorderStyle.Single, 0.5, Colors.Gray);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var renderer = new PdfDocumentRenderer(unicode: true) { Document = doc };
        renderer.RenderDocument();
        renderer.PdfDocument.Save(path);
    }
}
