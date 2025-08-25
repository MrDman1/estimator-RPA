namespace Nuform.Core.Domain
{
    public enum NuformColor { BrightWhite, NuformWhite, Black, Gray, Tan }

    public static class PanelCodeResolver
    {
        // Length → letter in Nuform catalog
        public static string LengthLetter(int lengthFt) => lengthFt switch
        {
            10 => "B", 12 => "C", 14 => "D", 16 => "E", 18 => "F", 20 => "G",
            _  => throw new ArgumentOutOfRangeException(nameof(lengthFt))
        };

        public static string ColorSuffix(NuformColor color) => color switch
        {
            NuformColor.BrightWhite => "BW",
            NuformColor.NuformWhite => "WH",
            NuformColor.Black       => "BK",
            NuformColor.Gray        => "GA",
            NuformColor.Tan         => "TN",
            _ => "WH"
        };

        public static (string code, string name) PanelSku(int widthInches, int lengthFt, NuformColor color)
        {
            var L = LengthLetter(lengthFt);
            var C = ColorSuffix(color);
            if (widthInches == 18)
            {
                // RELINE PRO 18"
                return ($"GELPRO{L}A{C}", $"RELINE PRO 18\" Panel ({color}) {lengthFt}′");
            }
            // R3 12"
            return ($"GEL2PL{L}A{C}", $"RELINE R3 12\" Panel ({color}) {lengthFt}′");
        }

        public static NuformColor ParseColor(string color) => color.ToUpperInvariant() switch
        {
            "BRIGHT WHITE" => NuformColor.BrightWhite,
            "NUFORM WHITE" => NuformColor.NuformWhite,
            "BLACK" => NuformColor.Black,
            "GRAY" => NuformColor.Gray,
            "TAN" => NuformColor.Tan,
            _ => NuformColor.NuformWhite
        };

        public static string ColorName(NuformColor color) => color switch
        {
            NuformColor.BrightWhite => "BRIGHT WHITE",
            NuformColor.NuformWhite => "NUFORM WHITE",
            NuformColor.Black       => "BLACK",
            NuformColor.Gray        => "GRAY",
            NuformColor.Tan         => "TAN",
            _ => "NUFORM WHITE"
        };
    }
}
