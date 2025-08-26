using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Nuform.Core
{
    public sealed class SofPart
    {
        public string PartCode { get; init; } = "";
        public int Quantity { get; init; }
        public string Units { get; init; } = "pcs";
        public string Description { get; init; } = "";
    }

    public sealed class SofCompanyInfo
    {
        public string Venture { get; init; } = "";
        public string ModelName { get; init; } = "";
        public string ModelSubName { get; init; } = "";
        public string Location { get; init; } = "";
        public int Quantity { get; init; } = 1;
        public double Width { get; init; } = 0;
        public double Height { get; init; } = 0;
        public int Floors { get; init; } = 1;
        public string SoldTo { get; init; } = "";
        public string SoldToAddress1 { get; init; } = "";
        public string SoldToAddress2 { get; init; } = "";
        public string SoldToAddress3 { get; init; } = "";
        public string SoldToContact { get; init; } = "";
        public string SoldToTelephone { get; init; } = "";
        public string SoldToFax { get; init; } = "";
        public string SoldToEmail { get; init; } = "";
        public string ShipTo { get; init; } = "SoldTo";
        public string FreightBy { get; init; } = "Nuform";
        public DateTime Date { get; init; } = DateTime.Today;
    }

    public static class SofV2Writer
    {
        public static void Write(string path, SofCompanyInfo info, IEnumerable<SofPart> parts)
        {
            var nl = "\r\n";
            var sb = new StringBuilder();
            sb.Append("RBT Component Order v2.0").Append(nl).Append(nl);

            sb.Append("[Company Info]").Append(nl);
            sb.Append("Venture=").Append(info.Venture).Append(nl);
            sb.Append("ModelName=").Append(info.ModelName).Append(nl);
            sb.Append("ModelSubName=").Append(info.ModelSubName).Append(nl);
            sb.Append("Location=").Append(info.Location).Append(nl);
            sb.Append("Quantity=").Append(info.Quantity.ToString(CultureInfo.InvariantCulture)).Append(nl);
            sb.Append("Width=").Append(info.Width.ToString("F6", CultureInfo.InvariantCulture)).Append(nl);
            sb.Append("Height=").Append(info.Height.ToString("F6", CultureInfo.InvariantCulture)).Append(nl);
            sb.Append("Floors=").Append(info.Floors.ToString(CultureInfo.InvariantCulture)).Append(nl);
            sb.Append("SoldTo=").Append(info.SoldTo).Append(nl);
            sb.Append("SoldToAddress1=").Append(info.SoldToAddress1).Append(nl);
            sb.Append("SoldToAddress2=").Append(info.SoldToAddress2).Append(nl);
            sb.Append("SoldToAddress3=").Append(info.SoldToAddress3).Append(nl);
            sb.Append("SoldToContact=").Append(info.SoldToContact).Append(nl);
            sb.Append("SoldToTelephone=").Append(info.SoldToTelephone).Append(nl);
            sb.Append("SoldToFax=").Append(info.SoldToFax).Append(nl);
            sb.Append("SoldToEmail=").Append(info.SoldToEmail).Append(nl);
            sb.Append("ShipTo=").Append(info.ShipTo).Append(nl);
            sb.Append("FreightBy=").Append(info.FreightBy).Append(nl);
            sb.Append("Date=").Append(info.Date.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture)).Append(nl).Append(nl);

            sb.Append("[Label Info]").Append(nl).Append(nl);
            sb.Append("[Part List]").Append(nl);

            foreach (var p in parts)
            {
                sb.Append(p.PartCode).Append('|')
                  .Append("1|1|1|")
                  .Append(p.Quantity.ToString(CultureInfo.InvariantCulture)).Append('|')
                  .Append("|||||")
                  .Append(p.Description).Append('|')
                  .Append(p.Units).Append('|')
                  .Append(nl);
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }
    }
}