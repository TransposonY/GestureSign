using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GestureSign.Tests.Hid
{
    /// <summary>
    /// Access to the hidapi windows/test/data corpus copied into Fixtures/hidapi: pairs of
    /// <c>&lt;name&gt;_real.rpt_desc</c> (report descriptor as captured by various tools) and
    /// <c>&lt;name&gt;.pp_data</c> (preparsed data dumped on real Windows).
    /// </summary>
    public static class HidapiCorpus
    {
        public static string Directory =>
            Path.Combine(Path.GetDirectoryName(typeof(HidapiCorpus).Assembly.Location), "Fixtures", "hidapi");

        public static IEnumerable<string> Names =>
            System.IO.Directory.GetFiles(Directory, "*.pp_data")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(n => n, StringComparer.Ordinal);

        public static string ReadPpData(string name) => File.ReadAllText(Path.Combine(Directory, name + ".pp_data"));

        public static byte[] ReadDescriptor(string name) =>
            ParseDescriptorText(File.ReadAllText(Path.Combine(Directory, name + "_real.rpt_desc")));

        private static readonly Regex CArrayLine = new Regex(@"^\s*#?\s*0x[0-9A-Fa-f]{2}\s*,", RegexOptions.Compiled);
        private static readonly Regex CArrayByte = new Regex(@"0x([0-9A-Fa-f]{2})", RegexOptions.Compiled);
        private static readonly Regex HexPair = new Regex(@"^[0-9A-Fa-f]{2}$", RegexOptions.Compiled);
        private static readonly Regex NamedItemLine = new Regex(@"^\s*[A-Za-z][^:""]*?((?:\s+[0-9A-Fa-f]{2})+)\s*$", RegexOptions.Compiled);

        /// <summary>
        /// The corpus descriptors come from different tools. Supported layouts, tried in order:
        /// C array lines ("0x05, 0x01, // Usage Page", optionally '#'-prefixed as printed by hid-decode;
        /// when a file also contains a raw hex dump only the C array is used), item listings with the
        /// bytes trailing the item name ("Usage Page (Generic Desktop) 05 01"), and bare hex dumps.
        /// </summary>
        public static byte[] ParseDescriptorText(string text)
        {
            var lines = text.Replace("\r", "").Split('\n');

            var cArray = lines.Where(l => CArrayLine.IsMatch(l)).ToList();
            if (cArray.Count > 0)
            {
                return cArray.SelectMany(l =>
                {
                    int comment = l.IndexOf("//", StringComparison.Ordinal);
                    var code = comment >= 0 ? l.Substring(0, comment) : l;
                    return CArrayByte.Matches(code).Cast<Match>().Select(m => ParseByte(m.Groups[1].Value));
                }).ToArray();
            }

            var bytes = new List<byte>();
            foreach (var line in lines)
            {
                var tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0) continue;
                if (tokens.All(t => HexPair.IsMatch(t)))
                {
                    bytes.AddRange(tokens.Select(ParseByte));
                    continue;
                }
                var named = NamedItemLine.Match(line);
                if (named.Success)
                    bytes.AddRange(named.Groups[1].Value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Select(ParseByte));
            }
            if (bytes.Count == 0) throw new FormatException("No descriptor bytes recognised.");
            return bytes.ToArray();
        }

        private static byte ParseByte(string hex) => byte.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }
}
