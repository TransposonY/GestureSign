using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;

namespace GestureSign.Tests.Hid
{
    /// <summary>
    /// Compares <see cref="HidPreparsedDataBuilder"/> output field by field with preparsed data dumped
    /// from real Windows (hidapi windows/test/data corpus in Fixtures/hidapi).
    /// </summary>
    public class PreparsedDataBuilderCorpusTests
    {
        /// <summary>
        /// Fields that cannot be reproduced. 046D_C534_0080_0001: the header Reserved words read
        /// 0x0003/0x8000 on Windows; this is the only corpus sample with a non-zero value (it is also the
        /// only array with several individual usages) and no rule can be inferred from one sample.
        /// hid.dll does not need it (the blob works without it, see the round-trip tests).
        /// </summary>
        private static readonly Dictionary<string, string[]> Excluded = new Dictionary<string, string[]>
        {
            ["046D_C534_0080_0001"] = new[] { "pp_data->Reserved" },
        };

        public static IEnumerable<object[]> CorpusNames => HidapiCorpus.Names.Select(n => new object[] { n });

        [Fact]
        public void CorpusIsPresent()
        {
            Assert.True(HidapiCorpus.Names.Count() >= 25, "Fixtures/hidapi corpus missing from the output directory.");
        }

        [Theory]
        [MemberData(nameof(CorpusNames))]
        public void BuildMatchesWindowsPreparsedData(string name)
        {
            var descriptor = HidapiCorpus.ReadDescriptor(name);
            var expected = PreparsedDataDump.ParseText(HidapiCorpus.ReadPpData(name));
            var usage = expected.Single(f => f.Key == "pp_data->Usage").Value;
            var usagePage = expected.Single(f => f.Key == "pp_data->UsagePage").Value;

            var tlcs = HidPreparsedDataBuilder.GetTopLevelCollections(descriptor);
            int tlc = Enumerable.Range(0, tlcs.Count).First(i =>
                HexEquals("0x" + tlcs[i].Usage.ToString("X4"), usage) && HexEquals("0x" + tlcs[i].UsagePage.ToString("X4"), usagePage));

            AssertSameFields(name, expected, HidPreparsedDataBuilder.Build(descriptor, tlc));
        }

        [Theory]
        [MemberData(nameof(CorpusNames))]
        public void HidDllAcceptsBuiltPreparsedData(string name)
        {
            var descriptor = HidapiCorpus.ReadDescriptor(name);
            var expected = PreparsedDataDump.ParseText(HidapiCorpus.ReadPpData(name)).ToDictionary(f => f.Key, f => f.Value);
            var pp = HidPreparsedDataBuilder.Build(descriptor, 0);

            var caps = HidReport.GetCaps(pp);

            Assert.Equal(Convert.ToUInt16(expected["pp_data->Usage"], 16), caps.Usage);
            Assert.Equal(Convert.ToUInt16(expected["pp_data->UsagePage"], 16), caps.UsagePage);
            Assert.Equal(ushort.Parse(expected["pp_data->caps_info[0]->ReportByteLength"], CultureInfo.InvariantCulture), caps.InputReportByteLength);
            Assert.Equal(ushort.Parse(expected["pp_data->caps_info[1]->ReportByteLength"], CultureInfo.InvariantCulture), caps.OutputReportByteLength);
            Assert.Equal(ushort.Parse(expected["pp_data->caps_info[2]->ReportByteLength"], CultureInfo.InvariantCulture), caps.FeatureReportByteLength);
            Assert.Equal(ushort.Parse(expected["pp_data->NumberLinkCollectionNodes"], CultureInfo.InvariantCulture), caps.NumberLinkCollectionNodes);
        }

        /// <summary>
        /// The mac-hid-dump in 046D_B010_*_real.rpt_desc holds the complete 246-byte device descriptor
        /// (six TLCs); each TLC must match the Windows dump of that collection, so TLC selection and
        /// global state carried across TLCs are exercised. TLC 5 (a second Consumer Control collection) has
        /// no own dump: its usage pair collides with TLC 1, which is the one hidapi dumped.
        /// </summary>
        [Theory]
        [InlineData(0, "046D_B010_0002_0001")]
        [InlineData(1, "046D_B010_0001_000C")]
        [InlineData(2, "046D_B010_0001_FF00")]
        [InlineData(3, "046D_B010_0002_FF00")]
        [InlineData(4, "046D_B010_0006_0001")]
        public void FullMultiCollectionDescriptorSelectsTopLevelCollection(int tlc, string name)
        {
            var dump = File.ReadAllLines(Path.Combine(HidapiCorpus.Directory, "046D_B010_0002_0001_real.rpt_desc"));
            var descriptor = dump
                .Select(l => l.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                .Where(t => t.Length > 0 && t.All(x => x.Length == 2 && Uri.IsHexDigit(x[0]) && Uri.IsHexDigit(x[1])))
                .SelectMany(t => t)
                .Select(x => Convert.ToByte(x, 16))
                .ToArray();
            Assert.Equal(246, descriptor.Length);
            Assert.Equal(6, HidPreparsedDataBuilder.GetTopLevelCollections(descriptor).Count);

            AssertSameFields(name, PreparsedDataDump.ParseText(HidapiCorpus.ReadPpData(name)),
                HidPreparsedDataBuilder.Build(descriptor, tlc));
        }

        [Fact]
        public void DelimiterIsRejected()
        {
            var descriptor = new byte[] { 0x05, 0x01, 0x09, 0x02, 0xA1, 0x01, 0xA9, 0x01, 0x09, 0x30, 0xA9, 0x00, 0xC0 };
            Assert.Throws<NotSupportedException>(() => HidPreparsedDataBuilder.Build(descriptor));
        }

        [Fact]
        public void MissingTopLevelCollectionIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HidPreparsedDataBuilder.Build(HidDescriptors.Pen, 1));
        }

        private static void AssertSameFields(string name, List<KeyValuePair<string, string>> expected, byte[] pp)
        {
            string[] excluded;
            var skip = new HashSet<string>(Excluded.TryGetValue(name, out excluded) ? excluded : new string[0]);
            var actual = PreparsedDataDump.Format(pp);
            var actualByKey = actual.ToDictionary(f => f.Key, f => f.Value);
            var expectedKeys = new HashSet<string>(expected.Select(f => f.Key));

            var diffs = expected
                .Where(f => !skip.Contains(f.Key))
                .Where(f => !actualByKey.ContainsKey(f.Key) || !HexEquals(actualByKey[f.Key], f.Value))
                .Select(f => $"{f.Key}: Windows {f.Value}, built {(actualByKey.ContainsKey(f.Key) ? actualByKey[f.Key] : "<missing>")}")
                .Concat(actual.Where(f => !expectedKeys.Contains(f.Key)).Select(f => $"{f.Key}: not in Windows dump (built {f.Value})"))
                .ToList();

            Assert.True(diffs.Count == 0, $"{name}: {diffs.Count} field(s) differ:\n" + string.Join("\n", diffs.Take(40)));
        }

        /// <summary>Hex fields compare numerically (the corpus prints some 3-byte fields as "0x000").</summary>
        private static bool HexEquals(string a, string b)
        {
            if (a == b) return true;
            return a.StartsWith("0x", StringComparison.Ordinal) && b.StartsWith("0x", StringComparison.Ordinal)
                && Convert.ToUInt64(a.Substring(2), 16) == Convert.ToUInt64(b.Substring(2), 16);
        }
    }
}
