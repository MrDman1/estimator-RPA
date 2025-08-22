namespace Nuform.Core.LegacyCompat;

/// <summary>
/// Compatibility application configuration used by legacy helpers.
/// Internal note listing referenced members:
/// - <see cref="WipEstimatingRoot"/> : string
/// - <see cref="WipDesignRoot"/> : string
/// - <see cref="PdfPrinter"/> : string
/// - <see cref="ExcelTemplatePath"/> : string
/// </summary>
public sealed class AppConfig
{
    public string WipEstimatingRoot { get; set; } = "I:/CF QUOTES/WIP Estimating";
    public string WipDesignRoot { get; set; } = "I:/CF QUOTES/WIP Design";
    public string PdfPrinter { get; set; } = "Microsoft Print to PDF";
    public string ExcelTemplatePath { get; set; } = @"C:\Templates\Estimating Template.xlsm";
}

