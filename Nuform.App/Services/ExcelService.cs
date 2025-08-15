// Nuform.App/Services/ExcelService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Excel = Microsoft.Office.Interop.Excel;

namespace Nuform.App.Services
{
    public sealed class ExcelWriteOp
    {
        public string Sheet { get; set; } = "Customer Info";
        public string A1 { get; set; } = "C7";
        public object? Value { get; set; }
    }

    public class ExcelService
    {
        /// <summary>
        /// Writes (sheet, A1, value) ops into an existing workbook.
        /// Handles merged cells by writing to the top-left cell.
        /// </summary>
        public void WriteCells(string workbookPath, IEnumerable<ExcelWriteOp> ops)
        {
            if (string.IsNullOrWhiteSpace(workbookPath) || !File.Exists(workbookPath))
                throw new FileNotFoundException("Excel workbook not found", workbookPath);

            Excel.Application? app = null;
            Excel.Workbooks? books = null;
            Excel.Workbook? book = null;

            try
            {
                app = new Excel.Application { Visible = false, DisplayAlerts = false };
                books = app.Workbooks;
                book = books.Open(workbookPath);

                foreach (var op in ops)
                {
                    Excel.Worksheet ws = (Excel.Worksheet)book.Worksheets[op.Sheet];
                    Excel.Range cell = (Excel.Range)ws.Range[op.A1];

                    try
                    {
                        bool isMerged = (cell.MergeCells is bool b) && b;
                        if (isMerged)
                        {
                            Excel.Range tl = (Excel.Range)cell.MergeArea.Cells[1, 1];
                            tl.Value2 = op.Value;
                            ReleaseCom(tl);
                        }
                        else
                        {
                            cell.Value2 = op.Value;
                        }
                    }
                    finally
                    {
                        ReleaseCom(cell);
                        ReleaseCom(ws);
                    }
                }

                book.Save();
            }
            finally
            {
                if (book != null) { book.Close(true); ReleaseCom(book); }
                if (books != null) ReleaseCom(books);
                if (app != null) { app.Quit(); ReleaseCom(app); }
            }
        }

        private static void ReleaseCom(object? o)
        {
            try { if (o != null && Marshal.IsComObject(o)) Marshal.ReleaseComObject(o); }
            catch { /* ignore */ }
        }
    }
}