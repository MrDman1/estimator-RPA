using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nuform.Core;

public enum CeilingOrientation { Widthwise, Lengthwise }

public class Room
{
    public double LengthFt { get; set; }
    public double WidthFt { get; set; }
    public double HeightFt { get; set; }
    public double WallPanelLengthFt { get; set; }
    public double PanelWidthInches { get; set; } = 12;
    public bool HasCeiling { get; set; }
    public double CeilingPanelLengthFt { get; set; }
    public CeilingOrientation CeilingOrientation { get; set; } = CeilingOrientation.Lengthwise;
}

public class Opening
{
    public double WidthFt { get; set; }
    public double HeightFt { get; set; }
    public int Count { get; set; }
    public double HeaderHeight { get; set; }
    public double SillHeight { get; set; }
}

public class EstimateOptions
{
    public double Contingency { get; set; } = 0.05;
}

public class EstimateInput
{
    public List<Room> Rooms { get; set; } = new();
    public List<Opening> Openings { get; set; } = new();
    public EstimateOptions Options { get; set; } = new();
}

public class TrimResult
{
    public double JTrimLF { get; set; }
    public int JTrimPacks { get; set; }
    public double CornerTrimLF { get; set; }
    public int CornerPacks { get; set; }
    public double TopTrackLF { get; set; }
}

public class HardwareResult
{
    public int PlugSpacerPacks { get; set; }
    public int ExpansionTools { get; set; }
    public int ScrewBoxes { get; set; }
}

public class EstimateResult
{
    public Dictionary<double, int> WallPanels { get; set; } = new();
    public Dictionary<double, int> CeilingPanels { get; set; } = new();
    public TrimResult Trims { get; set; } = new();
    public HardwareResult Hardware { get; set; } = new();
}

public class Estimator
{
    static double PanelWidthFt(double inches) => inches == 18 ? 1.5 : 1.0;

    static int RoundPanels(double panels)
    {
        if (panels <= 150)
            return (int)Math.Ceiling(panels / 2.0) * 2;
        return (int)Math.Ceiling(panels / 5.0) * 5;
    }

    public EstimateResult Estimate(EstimateInput input)
    {
        var result = new EstimateResult();
        double netLF = 0;
        foreach (var room in input.Rooms)
        {
            netLF += 2 * (room.LengthFt + room.WidthFt);
        }
        if (input.Openings.Any())
        {
            var firstRoom = input.Rooms.FirstOrDefault();
            double panelWidthFt = PanelWidthFt(firstRoom?.PanelWidthInches ?? 12);
            double panelLenFt = firstRoom?.WallPanelLengthFt ?? 0;
            foreach (var op in input.Openings)
            {
                var piecesPerFull = panelLenFt / (op.HeaderHeight + op.SillHeight);
                var headerPanelsAdded = op.WidthFt / piecesPerFull;
                var headerLFAdded = headerPanelsAdded * panelWidthFt;
                netLF += (-op.WidthFt + headerLFAdded) * op.Count;
            }
        }
        if (input.Rooms.Any())
        {
            double panelWidthFt = PanelWidthFt(input.Rooms.First().PanelWidthInches);
            double panels = netLF / panelWidthFt;
            panels *= 1 + input.Options.Contingency;
            int rounded = RoundPanels(panels);
            double panelLen = input.Rooms.First().WallPanelLengthFt;
            result.WallPanels[panelLen] = rounded;

            // Trims J-trim around wall perimeter
            double jtrimLF = netLF * (1 + input.Options.Contingency);
            result.Trims.JTrimLF = jtrimLF;
            double perPack = 10 * 12; // assume 12' pieces
            result.Trims.JTrimPacks = (int)Math.Ceiling(jtrimLF / perPack);

            // Corner trim: 4 corners per room
            double cornerLF = input.Rooms.Sum(r => r.HeightFt * 4);
            result.Trims.CornerTrimLF = cornerLF;
            double perCornerPack = 5 * 12; // assume 12' pieces
            int basePacks = (int)Math.Ceiling(cornerLF / perCornerPack);
            int contPacks = (int)Math.Ceiling(cornerLF * (1 + input.Options.Contingency) / perCornerPack);
            result.Trims.CornerPacks = contPacks > basePacks ? basePacks : contPacks;
        }

        // Ceilings
        foreach (var room in input.Rooms.Where(r => r.HasCeiling))
        {
            double panelWidthFt = PanelWidthFt(room.PanelWidthInches);
            int qty; double panelLen;
            if (room.CeilingOrientation == CeilingOrientation.Widthwise)
            {
                qty = (int)Math.Ceiling(room.LengthFt / panelWidthFt * (1 + input.Options.Contingency));
                panelLen = room.WidthFt;
            }
            else
            {
                var perRow = Math.Ceiling(room.WidthFt / panelWidthFt);
                var rows = Math.Ceiling(room.LengthFt / room.CeilingPanelLengthFt);
                qty = (int)Math.Ceiling(perRow * rows * (1 + input.Options.Contingency));
                panelLen = room.CeilingPanelLengthFt;
            }
            if (result.CeilingPanels.ContainsKey(panelLen))
                result.CeilingPanels[panelLen] += qty;
            else
                result.CeilingPanels[panelLen] = qty;

            // Top track
            result.Trims.TopTrackLF += 2 * (room.LengthFt + room.WidthFt);
        }
        if (result.Trims.TopTrackLF > 0)
            result.Trims.TopTrackLF *= 1.05; // add 5%

        // Hardware calculations
        int totalPanels = result.WallPanels.Values.Sum() + result.CeilingPanels.Values.Sum();
        result.Hardware.PlugSpacerPacks = totalPanels <= 0 ? 0 : Math.Max(1, (int)Math.Ceiling((totalPanels - 100) / 50.0));
        result.Hardware.ExpansionTools = totalPanels <= 250 ? 1 : 2;

        double wallPanelLenTotal = result.WallPanels.Sum(kvp => kvp.Key * kvp.Value);
        double ceilingPanelLenTotal = result.CeilingPanels.Sum(kvp => kvp.Key * kvp.Value);
        double wallTrimLF = result.Trims.JTrimLF + result.Trims.CornerTrimLF;
        double ceilingTrimLF = result.Trims.TopTrackLF;
        double wallScrews = (wallPanelLenTotal + wallTrimLF) / 2.0;
        double ceilingScrews = (ceilingPanelLenTotal + ceilingTrimLF) / 1.5;
        result.Hardware.ScrewBoxes = (int)Math.Ceiling((wallScrews + ceilingScrews) / 500.0);

        return result;
    }
}

public class AppConfig
{
    public string WipEstimatingRoot { get; set; } = "I:/CF QUOTES/WIP Estimating";
    public string WipDesignRoot { get; set; } = "I:/CF QUOTES/WIP Design";
    public string PdfPrinter { get; set; } = "Microsoft Print to PDF";
    public string ExcelTemplatePath { get; set; } = @"C:\Users\dbeland\OneDrive - Nuform Building Technologies Inc\Desktop\Estimating Template-v.2025.5.23 (64bit).xlsm";
}

public static class ConfigService
{
    public static AppConfig Load()
    {
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Nuform", "Estimator", "config.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                if (cfg != null) return cfg;
            }
        }
        catch { }
        return new AppConfig();
    }
}

public static class PathDiscovery
{
    static bool TryParseRange(string name, out int start, out int end)
    {
        start = end = 0;
        var m = Regex.Match(name, @"(?:(\d{4})-)?(\d+)\s*to\s*(\d+)");
        if (!m.Success) return false;
        start = int.Parse(m.Groups[2].Value);
        end = int.Parse(m.Groups[3].Value);
        return true;
    }

    public static string? FindEstimateFolder(string root, string estimateNumber)
    {
        if (!Directory.Exists(root)) return null;
        int est = int.Parse(estimateNumber);
        string? matchRange = null;
        foreach (var dir in Directory.GetDirectories(root))
        {
            var name = Path.GetFileName(dir);
            if (TryParseRange(name, out int start, out int end) && est >= start && est <= end)
            {
                matchRange = dir;
                var child = Path.Combine(dir, estimateNumber);
                if (Directory.Exists(child)) return child;
                break;
            }
        }
        if (matchRange != null)
        {
            foreach (var dir in Directory.GetDirectories(root))
            {
                var child = Path.Combine(dir, estimateNumber);
                if (Directory.Exists(child)) return child;
            }
        }
        return null;
    }

    public static string? FindBomFolder(string root, string bomNumber)
    {
        if (!Directory.Exists(root)) return null;
        int bom = int.Parse(bomNumber);
        string? matchRange = null;
        foreach (var dir in Directory.GetDirectories(root))
        {
            var name = Path.GetFileName(dir);
            if (TryParseRange(name, out int start, out int end) && bom >= start && bom <= end)
            {
                matchRange = dir;
                var child = Path.Combine(dir, bomNumber, "1-CURRENT");
                if (Directory.Exists(child)) return child;
                break;
            }
        }
        if (matchRange != null)
        {
            foreach (var dir in Directory.GetDirectories(root))
            {
                var child = Path.Combine(dir, bomNumber, "1-CURRENT");
                if (Directory.Exists(child)) return child;
            }
        }
        return null;
    }
}

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
        var res = new FileNamingResult
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
        return res;
    }
}
