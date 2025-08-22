using System;
using System.Collections.Generic;
using System.IO;

namespace Nuform.Core.LegacyCompat;

public class FileNamingResult
{
    public string? ServerDrawingsPath { get; set; }
    public string? ServerEstimatePath { get; set; }
    public string? ServerInvoicingPath { get; set; }
    public string? ServerCurrentPath { get; set; }
    public string? DesktopDrawingsPath { get; set; }
    public string? DesktopEstimatePath { get; set; }
    public string? DesktopInvoicingPath { get; set; }
    public string? DesktopCurrentPath { get; set; }
    public string EstimatePdfName { get; set; } = string.Empty;
    public string DrawingPdfName { get; set; } = string.Empty;
    public string EmailPdfName { get; set; } = string.Empty;
    public string? SofName { get; set; }
}

public static class FileNaming
{
    public static FileNamingResult Build(string estimateNumber, IEnumerable<string>? monikers,
        string? bomNumber, string? estimateRangeFolder, string? bomRangeFolder)
    {
        var monik = monikers != null ? string.Concat(monikers) : string.Empty;
        var baseName = estimateNumber + monik;
        var desktopBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), baseName);
        return new FileNamingResult
        {
            EstimatePdfName = baseName + ".pdf",
            DrawingPdfName = baseName + " - Drawing.pdf",
            EmailPdfName = baseName + " - Email 1.pdf",
            SofName = bomNumber != null ? bomNumber + ".sof" : null,
            ServerDrawingsPath = estimateRangeFolder != null ? Path.Combine(estimateRangeFolder, estimateNumber, "DRAWINGS") : null,
            ServerEstimatePath = estimateRangeFolder != null ? Path.Combine(estimateRangeFolder, estimateNumber, "ESTIMATE") : null,
            ServerInvoicingPath = estimateRangeFolder != null ? Path.Combine(estimateRangeFolder, estimateNumber, "INVOICING") : null,
            ServerCurrentPath = bomRangeFolder != null && bomNumber != null ? Path.Combine(bomRangeFolder, bomNumber, "1-CURRENT") : null,
            DesktopDrawingsPath = Path.Combine(desktopBase, "DRAWINGS"),
            DesktopEstimatePath = Path.Combine(desktopBase, "ESTIMATE"),
            DesktopInvoicingPath = Path.Combine(desktopBase, "INVOICING"),
            DesktopCurrentPath = bomNumber != null ? Path.Combine(desktopBase, bomNumber, "1-CURRENT") : null
        };
    }
}
