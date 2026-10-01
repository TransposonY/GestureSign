using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GestureSign.Tests.Hid
{
    /// <summary>
    /// Renders a preparsed data blob as the key/value fields printed by hidapi's
    /// windows/pp_data_dump/pp_data_dump.c (same keys, same number formatting), and parses such
    /// <c>.pp_data</c> dumps, so blobs can be compared field by field with Windows output.
    /// </summary>
    public static class PreparsedDataDump
    {
        private static readonly Regex FieldLine = new Regex(@"^(pp_data->\S+)\s*=\s*(.*?)\s*$", RegexOptions.Compiled);

        /// <summary>Parses the <c>pp_data-&gt;... = value</c> lines of a hidapi .pp_data file (in file order).</summary>
        public static List<KeyValuePair<string, string>> ParseText(string text)
        {
            var result = new List<KeyValuePair<string, string>>();
            foreach (var raw in text.Split('\n'))
            {
                var m = FieldLine.Match(raw.TrimEnd('\r'));
                if (m.Success) result.Add(new KeyValuePair<string, string>(m.Groups[1].Value, m.Groups[2].Value));
            }
            return result;
        }

        /// <summary>Formats <paramref name="pp"/> exactly like pp_data_dump.c (only the pp_data fields).</summary>
        public static List<KeyValuePair<string, string>> Format(byte[] pp)
        {
            var f = new List<KeyValuePair<string, string>>();
            Action<string, string> add = (k, v) => f.Add(new KeyValuePair<string, string>("pp_data->" + k, v));

            var magic = new System.Text.StringBuilder("0x");
            for (int i = 0; i < 8; i++) magic.Append(pp[i].ToString("X2"));
            add("MagicKey", magic.ToString());
            add("Usage", Hex4(U16(pp, 8)));
            add("UsagePage", Hex4(U16(pp, 10)));
            add("Reserved", "0x" + U16(pp, 12).ToString("X4") + U16(pp, 14).ToString("X4"));
            for (int t = 0; t < 3; t++)
            {
                int o = 16 + t * 8;
                add($"caps_info[{t}]->FirstCap", U16(pp, o).ToString(CultureInfo.InvariantCulture));
                add($"caps_info[{t}]->LastCap", U16(pp, o + 4).ToString(CultureInfo.InvariantCulture));
                add($"caps_info[{t}]->NumberOfCaps", U16(pp, o + 2).ToString(CultureInfo.InvariantCulture));
                add($"caps_info[{t}]->ReportByteLength", U16(pp, o + 6).ToString(CultureInfo.InvariantCulture));
            }
            int linkOffset = U16(pp, 40);
            int nodes = U16(pp, 42);
            add("FirstByteOfLinkCollectionArray", Hex4(linkOffset));
            add("NumberLinkCollectionNodes", nodes.ToString(CultureInfo.InvariantCulture));

            for (int t = 0; t < 3; t++)
            {
                int o = 16 + t * 8;
                for (int idx = U16(pp, o); idx < U16(pp, o + 4); idx++)
                    FormatCap(pp, HidPreparsedDataBuilder.HeaderSize + idx * HidPreparsedDataBuilder.CapSize, idx, add);
            }

            for (int i = 0; i < nodes; i++)
            {
                int o = HidPreparsedDataBuilder.HeaderSize + linkOffset + i * HidPreparsedDataBuilder.LinkCollectionNodeSize;
                string p = $"LinkCollectionArray[{i}]->";
                uint bits = U32(pp, o + 12);
                add(p + "LinkUsage", Hex4(U16(pp, o)));
                add(p + "LinkUsagePage", Hex4(U16(pp, o + 2)));
                add(p + "Parent", U16(pp, o + 4).ToString(CultureInfo.InvariantCulture));
                add(p + "NumberOfChildren", U16(pp, o + 6).ToString(CultureInfo.InvariantCulture));
                add(p + "NextSibling", U16(pp, o + 8).ToString(CultureInfo.InvariantCulture));
                add(p + "FirstChild", U16(pp, o + 10).ToString(CultureInfo.InvariantCulture));
                add(p + "CollectionType", (bits & 0xFF).ToString(CultureInfo.InvariantCulture));
                add(p + "IsAlias", ((bits >> 8) & 1).ToString(CultureInfo.InvariantCulture));
                add(p + "Reserved", "0x" + (bits >> 9).ToString("X8"));
            }
            return f;
        }

        private static void FormatCap(byte[] pp, int o, int idx, Action<string, string> add)
        {
            string p = $"cap[{idx}]->";
            byte flags = pp[o + 24];
            add(p + "UsagePage", Hex4(U16(pp, o)));
            add(p + "ReportID", "0x" + pp[o + 2].ToString("X2"));
            add(p + "BitPosition", pp[o + 3].ToString(CultureInfo.InvariantCulture));
            add(p + "BitSize", U16(pp, o + 4).ToString(CultureInfo.InvariantCulture));
            add(p + "ReportCount", U16(pp, o + 6).ToString(CultureInfo.InvariantCulture));
            add(p + "BytePosition", Hex4(U16(pp, o + 8)));
            add(p + "BitCount", U16(pp, o + 10).ToString(CultureInfo.InvariantCulture));
            add(p + "BitField", "0x" + U32(pp, o + 12).ToString("X2"));
            add(p + "NextBytePosition", Hex4(U16(pp, o + 16)));
            add(p + "LinkCollection", Hex4(U16(pp, o + 18)));
            add(p + "LinkUsagePage", Hex4(U16(pp, o + 20)));
            add(p + "LinkUsage", Hex4(U16(pp, o + 22)));
            add(p + "IsMultipleItemsForArray", Bit(flags, 0));
            add(p + "IsButtonCap", Bit(flags, 2));
            add(p + "IsPadding", Bit(flags, 1));
            add(p + "IsAbsolute", Bit(flags, 3));
            add(p + "IsRange", Bit(flags, 4));
            add(p + "IsAlias", Bit(flags, 5));
            add(p + "IsStringRange", Bit(flags, 6));
            add(p + "IsDesignatorRange", Bit(flags, 7));
            add(p + "Reserved1", "0x" + pp[o + 25].ToString("X2") + pp[o + 26].ToString("X2") + pp[o + 27].ToString("X2"));
            for (int t = 0; t < 4; t++)
            {
                int to = o + 28 + t * 8;
                add(p + $"pp_cap->UnknownTokens[{t}].Token", "0x" + pp[to].ToString("X2"));
                add(p + $"pp_cap->UnknownTokens[{t}].Reserved", "0x" + pp[to + 1].ToString("X2") + pp[to + 2].ToString("X2") + pp[to + 3].ToString("X2"));
                add(p + $"pp_cap->UnknownTokens[{t}].BitField", "0x" + U32(pp, to + 4).ToString("X8"));
            }
            if ((flags & 0x10) != 0)
            {
                add(p + "Range.UsageMin", Hex4(U16(pp, o + 60)));
                add(p + "Range.UsageMax", Hex4(U16(pp, o + 62)));
                add(p + "Range.StringMin", Dec16(pp, o + 64));
                add(p + "Range.StringMax", Dec16(pp, o + 66));
                add(p + "Range.DesignatorMin", Dec16(pp, o + 68));
                add(p + "Range.DesignatorMax", Dec16(pp, o + 70));
                add(p + "Range.DataIndexMin", Dec16(pp, o + 72));
                add(p + "Range.DataIndexMax", Dec16(pp, o + 74));
            }
            else
            {
                add(p + "NotRange.Usage", Hex4(U16(pp, o + 60)));
                add(p + "NotRange.Reserved1", Hex4(U16(pp, o + 62)));
                add(p + "NotRange.StringIndex", Dec16(pp, o + 64));
                add(p + "NotRange.Reserved2", Dec16(pp, o + 66));
                add(p + "NotRange.DesignatorIndex", Dec16(pp, o + 68));
                add(p + "NotRange.Reserved3", Dec16(pp, o + 70));
                add(p + "NotRange.DataIndex", Dec16(pp, o + 72));
                add(p + "NotRange.Reserved4", Dec16(pp, o + 74));
            }
            if ((flags & 0x04) != 0)
            {
                add(p + "Button.LogicalMin", ((int)U32(pp, o + 76)).ToString(CultureInfo.InvariantCulture));
                add(p + "Button.LogicalMax", ((int)U32(pp, o + 80)).ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                add(p + "NotButton.HasNull", pp[o + 76].ToString(CultureInfo.InvariantCulture));
                add(p + "NotButton.Reserved4", "0x" + pp[o + 77].ToString("X2") + pp[o + 78].ToString("X2") + pp[o + 79].ToString("X2"));
                add(p + "NotButton.LogicalMin", ((int)U32(pp, o + 80)).ToString(CultureInfo.InvariantCulture));
                add(p + "NotButton.LogicalMax", ((int)U32(pp, o + 84)).ToString(CultureInfo.InvariantCulture));
                add(p + "NotButton.PhysicalMin", ((int)U32(pp, o + 88)).ToString(CultureInfo.InvariantCulture));
                add(p + "NotButton.PhysicalMax", ((int)U32(pp, o + 92)).ToString(CultureInfo.InvariantCulture));
            }
            add(p + "Units", U32(pp, o + 96).ToString(CultureInfo.InvariantCulture));
            add(p + "UnitsExp", U32(pp, o + 100).ToString(CultureInfo.InvariantCulture));
        }

        private static string Bit(byte flags, int bit) => ((flags >> bit) & 1).ToString(CultureInfo.InvariantCulture);
        private static string Hex4(int v) => "0x" + v.ToString("X4");
        private static string Dec16(byte[] b, int o) => U16(b, o).ToString(CultureInfo.InvariantCulture);
        private static int U16(byte[] b, int o) => b[o] | (b[o + 1] << 8);
        private static uint U32(byte[] b, int o) => (uint)(b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24));
    }
}
