using System;
using System.Linq;
using System.Runtime.InteropServices;
using GestureSign.Daemon.Native;
using Xunit;

namespace GestureSign.Tests.Hid
{
    /// <summary>
    /// Builds preparsed data for the sample descriptors, writes reports with <see cref="HidReport"/> and
    /// reads them back through the Daemon's own <see cref="HidNativeApi"/> declarations (real hid.dll),
    /// i.e. the calls the daemon makes for every WM_INPUT.
    /// </summary>
    public class HidDescriptorRoundTripTests
    {
        private const ushort Digitizer = 0x0D;
        private const ushort GenericDesktop = 0x01;
        private const ushort ButtonPage = 0x09;
        private const ushort Finger = 0x22;
        private const ushort Tip = 0x42, InRange = 0x32, Barrel = 0x44, Invert = 0x3C, Eraser = 0x45, Confidence = 0x47;
        private const ushort ContactId = 0x51, ContactCount = 0x54, ContactCountMaximum = 0x55, ScanTime = 0x56;
        private const ushort X = 0x30, Y = 0x31, TipPressure = 0x30, XTilt = 0x3D;

        [Fact]
        public void TouchpadTopLevelCollectionsAreSelectable()
        {
            var tlcs = HidPreparsedDataBuilder.GetTopLevelCollections(HidDescriptors.PrecisionTouchpad);

            Assert.Equal(new[] { "000D:0005", "000D:000E", "0001:0002" }, tlcs.Select(t => t.ToString()).ToArray());
            using (var config = new Pinned(HidPreparsedDataBuilder.Build(HidDescriptors.PrecisionTouchpad, 1)))
            {
                var caps = GetCaps(config);
                Assert.Equal(0x0E, caps.Usage);
                Assert.Equal(0, caps.InputReportByteLength);
                Assert.Equal(2, caps.FeatureReportByteLength);
            }
        }

        [Fact]
        public void TouchpadFingerCollectionsAreLinkNodesOneToFive()
        {
            using (var pp = new Pinned(HidPreparsedDataBuilder.Build(HidDescriptors.PrecisionTouchpad)))
            {
                var caps = GetCaps(pp);
                Assert.Equal(0x05, caps.Usage);
                Assert.Equal(0x0D, caps.UsagePage);
                Assert.Equal(30, caps.InputReportByteLength);
                Assert.Equal(30, HidReport.ReportLength(pp.Bytes));

                var nodes = GetNodes(pp, caps.NumberLinkCollectionNodes);
                Assert.Equal(1 + HidDescriptors.TouchpadFingers, nodes.Length);
                Assert.Equal(HidDescriptors.TouchpadFingers, nodes[0].NumberOfChildren);
                // Windows links children newest-first: FirstChild is the last finger, NextSibling walks back to 1.
                Assert.Equal(HidDescriptors.TouchpadFingers, nodes[0].FirstChild);
                for (int i = 1; i <= HidDescriptors.TouchpadFingers; i++)
                {
                    Assert.Equal(Finger, (ushort)nodes[i].LinkUsage);
                    Assert.Equal(Digitizer, (ushort)nodes[i].LinkUsagePage);
                    Assert.Equal(0, nodes[i].Parent);
                    Assert.Equal(2, nodes[i].CollectionType);
                    Assert.Equal(i - 1, nodes[i].NextSibling);
                }
            }
        }

        [Fact]
        public void TouchpadReportRoundTripsThroughDaemonDeclarations()
        {
            var ppBytes = HidPreparsedDataBuilder.Build(HidDescriptors.PrecisionTouchpad);
            var report = HidReport.Create(ppBytes, HidDescriptors.TouchpadReportId)
                .SetValue(Digitizer, 0, ContactCount, 4)
                .SetValue(Digitizer, 0, ScanTime, 12345)
                .SetButtons(ButtonPage, 0, 1);
            for (short finger = 1; finger <= 4; finger++)
            {
                report.SetValue(Digitizer, finger, ContactId, finger - 1)
                    .SetValue(GenericDesktop, finger, X, 1000 * finger)
                    .SetValue(GenericDesktop, finger, Y, 4095 - 1000 * finger);
                if (finger == 4) report.SetButtons(Digitizer, finger, Confidence); // lifted finger
                else report.SetButtons(Digitizer, finger, Tip, Confidence);
            }
            var bytes = report.ToArray();

            using (var pp = new Pinned(ppBytes))
            using (var r = new Pinned(bytes))
            {
                Assert.Equal(HidDescriptors.TouchpadReportId, bytes[0]);
                Assert.Equal(4, GetValue(pp, r, Digitizer, 0, ContactCount));
                Assert.Equal(12345, GetValue(pp, r, Digitizer, 0, ScanTime));
                Assert.Equal(new ushort[] { 1 }, GetUsages(pp, r, ButtonPage, 0));

                for (short finger = 1; finger <= 4; finger++)
                {
                    Assert.Equal(finger - 1, GetValue(pp, r, Digitizer, finger, ContactId));
                    Assert.Equal(1000 * finger, GetValue(pp, r, GenericDesktop, finger, X));
                    Assert.Equal(4095 - 1000 * finger, GetValue(pp, r, GenericDesktop, finger, Y));
                    // Scaled values are in physical units (0..400 x 0..275 hundredths of an inch).
                    Assert.InRange(GetScaledValue(pp, r, GenericDesktop, finger, X), 1000 * finger * 400 / 4095 - 1, 1000 * finger * 400 / 4095 + 1);

                    var usages = GetUsages(pp, r, Digitizer, finger);
                    if (finger == 4) Assert.Equal(new[] { Confidence }, usages);
                    else
                    {
                        // The daemon checks usageList[0] == Tip: hid.dll lists Tip (declared after Confidence) first.
                        Assert.Equal(new[] { Tip, Confidence }, usages);
                    }
                }
                Assert.Equal(0, GetValue(pp, r, GenericDesktop, 5, X));
                Assert.Empty(GetUsages(pp, r, Digitizer, 5));

                var xCaps = GetValueCaps(pp, GenericDesktop, 0, X);
                Assert.Equal(HidDescriptors.TouchpadFingers, xCaps.Length);
                Assert.All(xCaps, c => Assert.Equal(HidDescriptors.TouchpadPhysicalMaxX, c.PhysicalMax));
                Assert.All(xCaps, c => Assert.Equal(HidDescriptors.TouchpadLogicalMaxX, c.LogicalMax));
                Assert.Equal(new[] { 1, 2, 3, 4, 5 }, xCaps.Select(c => (int)c.LinkCollection).OrderBy(l => l).ToArray());
                var yCaps = GetValueCaps(pp, GenericDesktop, 1, Y);
                Assert.Equal(HidDescriptors.TouchpadPhysicalMaxY, Assert.Single(yCaps).PhysicalMax);
            }
        }

        [Fact]
        public void TouchScreenHybridReportsRoundTrip()
        {
            var ppBytes = HidPreparsedDataBuilder.Build(HidDescriptors.MultiTouchScreen);
            // Hybrid mode: 3 contacts over two reports; only the first carries the contact count.
            var first = HidReport.Create(ppBytes, HidDescriptors.TouchScreenReportId)
                .SetValue(Digitizer, 0, ContactCount, 3)
                .SetButtons(Digitizer, 1, Tip).SetValue(Digitizer, 1, ContactId, 7)
                .SetValue(GenericDesktop, 1, X, 100).SetValue(GenericDesktop, 1, Y, 200)
                .SetButtons(Digitizer, 2, Tip).SetValue(Digitizer, 2, ContactId, 8)
                .SetValue(GenericDesktop, 2, X, 4095).SetValue(GenericDesktop, 2, Y, 0)
                .ToArray();
            var second = HidReport.Create(ppBytes, HidDescriptors.TouchScreenReportId)
                .SetValue(Digitizer, 0, ContactCount, 0)
                .SetValue(Digitizer, 1, ContactId, 9)
                .SetScaledValue(GenericDesktop, 1, X, 3000).SetScaledValue(GenericDesktop, 1, Y, 1500)
                .ToArray();

            using (var pp = new Pinned(ppBytes))
            using (var r1 = new Pinned(first))
            using (var r2 = new Pinned(second))
            {
                var caps = GetCaps(pp);
                Assert.Equal(0x04, caps.Usage);
                Assert.Equal(28, caps.InputReportByteLength);
                var nodes = GetNodes(pp, caps.NumberLinkCollectionNodes);
                Assert.Equal(HidDescriptors.TouchScreenContactsPerReport, nodes[0].NumberOfChildren);
                Assert.All(nodes.Skip(1), n => Assert.Equal(Finger, (ushort)n.LinkUsage));

                Assert.Equal(3, GetValue(pp, r1, Digitizer, 0, ContactCount));
                Assert.Equal(7, GetValue(pp, r1, Digitizer, 1, ContactId));
                Assert.Equal(8, GetValue(pp, r1, Digitizer, 2, ContactId));
                Assert.Equal(100, GetScaledValue(pp, r1, GenericDesktop, 1, X));
                Assert.Equal(200, GetScaledValue(pp, r1, GenericDesktop, 1, Y));
                Assert.Equal(4095, GetScaledValue(pp, r1, GenericDesktop, 2, X));
                Assert.Equal(new[] { Tip }, GetUsages(pp, r1, Digitizer, 2));

                Assert.Equal(0, GetValue(pp, r2, Digitizer, 0, ContactCount));
                Assert.Equal(9, GetValue(pp, r2, Digitizer, 1, ContactId));
                Assert.Equal(3000, GetValue(pp, r2, GenericDesktop, 1, X)); // PhysicalMax == LogicalMax
                Assert.Equal(1500, GetScaledValue(pp, r2, GenericDesktop, 1, Y));
                Assert.Empty(GetUsages(pp, r2, Digitizer, 1));

                var xCap = GetValueCaps(pp, GenericDesktop, 1, X).Single();
                Assert.Equal(HidDescriptors.TouchScreenLogicalMaxX, xCap.LogicalMax);
                Assert.Equal(HidDescriptors.TouchScreenLogicalMaxX, xCap.PhysicalMax);
                Assert.Equal(1, xCap.ReportCount);

                var ccm = new HidNativeApi.HidP_Value_Caps[1];
                short count = 1;
                Assert.Equal(HidNativeApi.HIDP_STATUS_SUCCESS, HidNativeApi.HidP_GetSpecificValueCaps(HidReportType.Feature, Digitizer, 0, ContactCountMaximum, ccm, ref count, pp.Pointer));
                Assert.Equal(HidDescriptors.TouchScreenMaxCountReportId, ccm[0].ReportID);
                Assert.Equal(HidDescriptors.TouchScreenContactCountMaximumLimit, ccm[0].LogicalMax);
            }
        }

        [Fact]
        public void PenReportRoundTrips()
        {
            var ppBytes = HidPreparsedDataBuilder.Build(HidDescriptors.Pen);
            var bytes = HidReport.Create(ppBytes, HidDescriptors.PenReportId)
                .SetButtons(Digitizer, 0, InRange, Tip, Barrel)
                .SetValue(GenericDesktop, 0, X, HidDescriptors.PenLogicalMaxX)
                .SetValue(GenericDesktop, 0, Y, 1234)
                .SetValue(Digitizer, 0, TipPressure, 200)
                .SetScaledValue(Digitizer, 0, XTilt, -50)
                .ToArray();

            using (var pp = new Pinned(ppBytes))
            using (var r = new Pinned(bytes))
            {
                var caps = GetCaps(pp);
                Assert.Equal(0x02, caps.Usage);
                Assert.Equal(10, caps.InputReportByteLength);

                Assert.Equal(Sorted(Tip, Barrel, InRange), Sorted(GetUsages(pp, r, Digitizer, 0)));
                Assert.Equal(HidDescriptors.PenLogicalMaxX, GetScaledValue(pp, r, GenericDesktop, 0, X));
                Assert.Equal(1234, GetScaledValue(pp, r, GenericDesktop, 0, Y));
                Assert.Equal(200, GetValue(pp, r, Digitizer, 0, TipPressure));
                Assert.Equal(-50, GetScaledValue(pp, r, Digitizer, 0, XTilt));

                Assert.Equal(HidDescriptors.PenLogicalMaxX, GetValueCaps(pp, GenericDesktop, 0, X).Single().PhysicalMax);
                Assert.Equal(HidDescriptors.PenLogicalMaxY, GetValueCaps(pp, GenericDesktop, 0, Y).Single().LogicalMax);
                // POP restored the pushed globals: Tip Pressure is 16 bits without a physical range.
                var pressure = GetValueCaps(pp, Digitizer, 0, TipPressure).Single();
                Assert.Equal(16, pressure.BitSize);
                Assert.Equal(0, pressure.PhysicalMax);
                Assert.Equal(HidDescriptors.PenLogicalMaxPressure, pressure.LogicalMax);
            }

            var eraser = HidReport.Create(ppBytes, HidDescriptors.PenReportId).SetButtons(Digitizer, 0, InRange, Invert, Eraser).ToArray();
            using (var pp = new Pinned(ppBytes))
            using (var r = new Pinned(eraser))
                Assert.Equal(Sorted(Invert, Eraser, InRange), Sorted(GetUsages(pp, r, Digitizer, 0)));
        }

        [Fact]
        public void HidReportRejectsUnknownUsage()
        {
            var pp = HidPreparsedDataBuilder.Build(HidDescriptors.Pen);
            var ex = Assert.Throws<HidPException>(() => HidReport.Create(pp, HidDescriptors.PenReportId).SetValue(GenericDesktop, 0, 0x32, 1));
            Assert.Equal(unchecked((int)0xC0110004), ex.Status); // HIDP_STATUS_USAGE_NOT_FOUND
        }

        private static ushort[] Sorted(params ushort[] usages) => usages.OrderBy(u => u).ToArray();

        #region Daemon HidNativeApi helpers

        private static HidNativeApi.HIDP_CAPS GetCaps(Pinned pp)
        {
            var caps = new HidNativeApi.HIDP_CAPS();
            Assert.Equal(HidNativeApi.HIDP_STATUS_SUCCESS, HidNativeApi.HidP_GetCaps(pp.Pointer, ref caps));
            return caps;
        }

        private static HidNativeApi.HIDP_LINK_COLLECTION_NODE[] GetNodes(Pinned pp, int count)
        {
            var nodes = new HidNativeApi.HIDP_LINK_COLLECTION_NODE[count];
            int length = count;
            Assert.Equal(HidNativeApi.HIDP_STATUS_SUCCESS, HidNativeApi.HidP_GetLinkCollectionNodes(nodes, ref length, pp.Pointer));
            Assert.Equal(count, length);
            return nodes;
        }

        private static int GetValue(Pinned pp, Pinned report, ushort page, short link, ushort usage)
        {
            int value = 0;
            Assert.Equal(HidNativeApi.HIDP_STATUS_SUCCESS,
                HidNativeApi.HidP_GetUsageValue(HidReportType.Input, page, link, usage, ref value, pp.Pointer, report.Pointer, report.Length));
            return value;
        }

        private static int GetScaledValue(Pinned pp, Pinned report, ushort page, short link, ushort usage)
        {
            int value = 0;
            Assert.Equal(HidNativeApi.HIDP_STATUS_SUCCESS,
                HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, page, link, usage, ref value, pp.Pointer, report.Pointer, report.Length));
            return value;
        }

        private static ushort[] GetUsages(Pinned pp, Pinned report, ushort page, short link)
        {
            var list = new ushort[32];
            int length = list.Length;
            Assert.Equal(HidNativeApi.HIDP_STATUS_SUCCESS,
                HidNativeApi.HidP_GetUsages(HidReportType.Input, page, link, list, ref length, pp.Pointer, report.Pointer, report.Length));
            return list.Take(length).ToArray();
        }

        private static HidNativeApi.HidP_Value_Caps[] GetValueCaps(Pinned pp, ushort page, ushort link, ushort usage)
        {
            var caps = new HidNativeApi.HidP_Value_Caps[16];
            short length = (short)caps.Length;
            Assert.Equal(HidNativeApi.HIDP_STATUS_SUCCESS,
                HidNativeApi.HidP_GetSpecificValueCaps(HidReportType.Input, page, link, usage, caps, ref length, pp.Pointer));
            return caps.Take(length).ToArray();
        }

        /// <summary>Pins a byte[] so it can be passed to the IntPtr-based Daemon declarations.</summary>
        private sealed class Pinned : IDisposable
        {
            private GCHandle _handle;

            public Pinned(byte[] bytes)
            {
                Bytes = bytes;
                _handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            }

            public byte[] Bytes { get; }
            public IntPtr Pointer => _handle.AddrOfPinnedObject();
            public int Length => Bytes.Length;

            public void Dispose() => _handle.Free();
        }

        #endregion
    }
}
