using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

// One-off tool to extract RELINE parts from the reference PDF
// Usage: dotnet run --project utils/RelinePdfToCsv.cs <pdf> <outputCsv>
class Program
{
    static void Main(string[] args)
    {
        var input = args.Length > 0 ? args[0] : "Nuform.Core/Data/reference/RELINE Part List.pdf";
        var output = args.Length > 1 ? args[1] : "Nuform.Core/Data/parts.generated.csv";
        using var pdf = PdfDocument.Open(input);
        var text = string.Join("\n", pdf.GetPages().Select(p => p.Text));
        var lines = text.Split('\n');
        using var sw = new StreamWriter(output);
        sw.WriteLine("PartNumber,Description,Units,PackPieces,LengthFt,Color,Category");
        foreach (var line in lines)
        {
            // Rough regex that captures code, description, length, color and pack pieces
            var m = Regex.Match(line, @"^(?<code>\S+)\s+(?<desc>.+?)\s+(?<len>\d+)'\s+(?<color>[A-Z\s]+)\s+(?<pack>\d+)\s*PCS");
            if (!m.Success) continue;
            sw.WriteLine($"{m.Groups["code"].Value},{m.Groups["desc"].Value},pkg,{m.Groups["pack"].Value},{m.Groups["len"].Value},{m.Groups["color"].Value},Other");
        }
    }
}
