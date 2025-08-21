using System;

namespace Nuform.Core.Domain;

public class PartSpec
{
    public string PartNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Units { get; set; } = string.Empty;
    public int PackPieces { get; set; }
    public double LengthFt { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
