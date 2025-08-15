using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Nuform.Core
{
    // Minimal header for [Company Info]; expand as needed
    public class SofHeader
    {
        public string? Venture { get; set; }
        public string? ModelName { get; set; }
        public string? ModelSubName { get; set; }
        public string? Location { get; set; }
        public int Quantity { get; set; } = 1;
        public double? Width { get; set; } = 0;
        public double? Height { get; set; } = 0;
        public int Floors { get; set; } = 1;
        public string? SoldTo { get; set; }
        public string? SoldToAddress1 { get; set; }
        public string? SoldToAddress2 { get; set; }
        public string? SoldToAddress3 { get; set; }
        public string? SoldToContact { get; set; }
        public string? SoldToTelephone { get; set; }
        public string? SoldToFax { get; set; }
        public string? SoldToEmail { get; set; }
        public string? ShipTo { get; set; } = "SoldTo";
        public string? FreightBy { get; set; } = "Nuform";
        public DateTime? Date { get; set; } = DateTime.Today;
    }

    public static class SofWriter
    {
        // Build one [Part List] line in exact column layout.
        // Qty = pieces for panels; Qty = PACKS for trims/accessories.
        private static string BuildPartLine(string partNumber, int qty, string description, string units, int widthInchesOr1)
        {
            // Cols: 0 PN | 1 Qty | 2 "1" | 3 "1" | 4 widthInchesOr1 | 5 "" | 6 "" | 7 "" | 8 "" | 9 Desc | 10 Units | 11 ""
            return string.Join("|", new[]
            {
                partNumber,
                qty.ToString(CultureInfo.InvariantCulture),
                "1",
                "1",
                widthInchesOr1.ToString(CultureInfo.InvariantCulture),
                "",
                "",
                "",
                "",
                description,
                units,
                ""
            });
        }

        /// <summary>
        /// Write a valid .SOF file that opens in Component Order.
        /// - Uses CRLF newlines and Windows-1252 (ANSI) encoding.
        /// - Panels: units=pcs, column4=panelWidthInches (18/12), qty=pieces.
        /// - Trims/Accessories: units=pkg, column4=1, qty=packs (already full-pack rounded).
        /// - Crown/Base: two lines (Base + Cap) with equal quantities.
        /// </summary>
        public static void Write(
            string targetFilePath,
            EstimateResult res,
            CatalogService catalog,
            SofHeader header,
            string panelColor = "BRIGHT WHITE",
            int panelWidthInches = 18 // 18 for RELINE PRO, 12 for 12" family
        )
        {
            // Guard: do NOT create unknown server folders (avoid duplicates)
            var dir = Path.GetDirectoryName(targetFilePath);
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                throw new DirectoryNotFoundException($"SOF target folder does not exist: {dir}");

            var sb = new StringBuilder();

            // First line + blank
            sb.Append("RBT Component Order v2.0").Append("\r\n").Append("\r\n");

            // [Company Info]
            sb.Append("[Company Info]").Append("\r\n");
            sb.Append("Venture=").Append(header.Venture ?? "").Append("\r\n");
            sb.Append("ModelName=").Append(header.ModelName ?? "").Append("\r\n");
            sb.Append("ModelSubName=").Append(header.ModelSubName ?? "").Append("\r\n");
            sb.Append("Location=").Append(header.Location ?? "").Append("\r\n");
            sb.Append("Quantity=").Append((header.Quantity > 0 ? header.Quantity : 1).ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("Width=").Append((header.Width ?? 0).ToString("0.000000", CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("Height=").Append((header.Height ?? 0).ToString("0.000000", CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("Floors=").Append((header.Floors > 0 ? header.Floors : 1).ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("SoldTo=").Append(header.SoldTo ?? "").Append("\r\n");
            sb.Append("SoldToAddress1=").Append(header.SoldToAddress1 ?? "").Append("\r\n");
            sb.Append("SoldToAddress2=").Append(header.SoldToAddress2 ?? "").Append("\r\n");
            sb.Append("SoldToAddress3=").Append(header.SoldToAddress3 ?? "").Append("\r\n");
            sb.Append("SoldToContact=").Append(header.SoldToContact ?? "").Append("\r\n");
            sb.Append("SoldToTelephone=").Append(header.SoldToTelephone ?? "").Append("\r\n");
            sb.Append("SoldToFax=").Append(header.SoldToFax ?? "").Append("\r\n");
            sb.Append("SoldToEmail=").Append(header.SoldToEmail ?? "").Append("\r\n");
            sb.Append("ShipTo=").Append(string.IsNullOrWhiteSpace(header.ShipTo) ? "SoldTo" : header.ShipTo).Append("\r\n");
            sb.Append("FreightBy=").Append(string.IsNullOrWhiteSpace(header.FreightBy) ? "Nuform" : header.FreightBy).Append("\r\n");
            sb.Append("Date=").Append((header.Date ?? DateTime.Today).ToString("MMMM d, yyyy", CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("\r\n");

            // [Label Info] (empty per sample)
            sb.Append("[Label Info]").Append("\r\n");
            sb.Append("\r\n");

            // [Part List]
            sb.Append("[Part List]").Append("\r\n");

            // PANELS (pcs), width column = panelWidthInches
            foreach (var kv in res.WallPanels.OrderBy(k => k.Key))
            {
                int lenFt = (int)Math.Round(kv.Key);
                int qtyPieces = kv.Value;
                var part = catalog.FindPanel(PanelFamily.RelinePro18, lenFt, panelColor);
                if (part != null)
                    sb.Append(BuildPartLine(part.PartNumber, qtyPieces, part.Description, "pcs", panelWidthInches)).Append("\r\n");
            }
            foreach (var kv in res.CeilingPanels.OrderBy(k => k.Key))
            {
                int lenFt = (int)Math.Round(kv.Key);
                int qtyPieces = kv.Value;
                var part = catalog.FindPanel(PanelFamily.RelinePro18, lenFt, panelColor);
                if (part != null)
                    sb.Append(BuildPartLine(part.PartNumber, qtyPieces, part.Description, "pcs", panelWidthInches)).Append("\r\n");
            }

            // J‑TRIM (pkg), qty = packs, width column = 1
            if (res.Trims.JTrimPacks > 0)
            {
                var j = catalog.FindTrim(TrimKind.J, res.Trims.JTrimPackLenFt, panelColor);
                if (j != null)
                    sb.Append(BuildPartLine(j.PartNumber, res.Trims.JTrimPacks, j.Description, "pkg", 1)).Append("\r\n");
            }

            // CORNER 90° (pkg), qty = packs, width column = 1
            if (res.Trims.CornerPacks > 0)
            {
                var c = catalog.FindTrim(TrimKind.Corner90, res.Trims.CornerPackLenFt, panelColor);
                if (c != null)
                    sb.Append(BuildPartLine(c.PartNumber, res.Trims.CornerPacks, c.Description, "pkg", 1)).Append("\r\n");
            }

            // CROWN/BASE (pairs: Base + Cap, equal qty) (pkg), width column = 1
            if (res.Trims.CrownBasePairs > 0)
            {
                var tlen = res.Trims.TopTrackPackLenFt;
                var baseTrim = catalog.FindTrim(TrimKind.CrownBaseBase, tlen, panelColor);
                var capTrim  = catalog.FindTrim(TrimKind.CrownBaseCap,  tlen, panelColor);
                if (baseTrim != null && capTrim != null)
                {
                    sb.Append(BuildPartLine(baseTrim.PartNumber, res.Trims.CrownBasePairs, baseTrim.Description, "pkg", 1)).Append("\r\n");
                    sb.Append(BuildPartLine(capTrim.PartNumber,  res.Trims.CrownBasePairs, capTrim.Description,  "pkg", 1)).Append("\r\n");
                }
            }

            // ACCESSORIES (pkg), qty = packs/boxes, width column = 1
            if (res.Hardware.PlugSpacerPacks > 0)
            {
                var plugs   = catalog.FindAccessory("Plugs", panelColor);
                var spacers = catalog.FindAccessory("Spacers", panelColor);
                if (plugs   != null) sb.Append(BuildPartLine(plugs.PartNumber,   res.Hardware.PlugSpacerPacks, plugs.Description,   "pkg", 1)).Append("\r\n");
                if (spacers != null) sb.Append(BuildPartLine(spacers.PartNumber, res.Hardware.PlugSpacerPacks, spacers.Description, "pkg", 1)).Append("\r\n");
            }
            {
                var tools = res.Hardware.ExpansionTools;
                var tool = catalog.FindAccessory("Expansion Tool", "ANY");
                if (tool != null && tools > 0)
                    sb.Append(BuildPartLine(tool.PartNumber, tools, tool.Description, "pkg", 1)).Append("\r\n");
            }
            {
                var concrete  = catalog.FindAccessory("Concrete Screws",  "ANY");
                var stainless = catalog.FindAccessory("Stainless Screws", "ANY");
                if (concrete  != null && res.Hardware.WallScrewBoxes    > 0) sb.Append(BuildPartLine(concrete.PartNumber,  res.Hardware.WallScrewBoxes,  concrete.Description,  "pkg", 1)).Append("\r\n");
                if (stainless != null && res.Hardware.CeilingScrewBoxes > 0) sb.Append(BuildPartLine(stainless.PartNumber, res.Hardware.CeilingScrewBoxes, stainless.Description, "pkg", 1)).Append("\r\n");
            }

            // Write file with CRLF + Windows-1252 (ANSI).
            var ansi = Encoding.GetEncoding(1252);
            File.WriteAllText(targetFilePath, sb.ToString(), ansi);

            // If your environment prefers UTF-8 (no BOM):
            // var utf8NoBom = new UTF8Encoding(false);
            // File.WriteAllText(targetFilePath, sb.ToString(), utf8NoBom);
        }
    }
}
