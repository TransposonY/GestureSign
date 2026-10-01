using GestureSign.Daemon.Native;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace GestureSign.InputRecorder.Recording
{
    /// <summary>One contact slot of a digitizer input report.</summary>
    public struct DecodedContact
    {
        /// <summary>Contact Identifier (0x0D:0x51); the slot index when the device reports none.</summary>
        public int Id;
        public bool Tip;
        public bool InRange;
        public bool Confidence;

        /// <summary>X normalized to the logical range: 0 = left, 1 = right.</summary>
        public double X;

        /// <summary>Y normalized to the logical range: 0 = top, 1 = bottom.</summary>
        public double Y;
    }

    public sealed class DecodedReport
    {
        /// <summary>Contact Count (0x0D:0x54); null when the device has no such field (pen).</summary>
        public int? ContactCount { get; set; }

        /// <summary>
        /// Every contact slot in report order, including unused ones. Which slots are valid depends on the
        /// contact count of the current frame; <see cref="ContactTracker"/> decides.
        /// </summary>
        public DecodedContact[] Slots { get; set; }

        /// <summary>Any Button page usage is on (touchpad click).</summary>
        public bool ButtonDown { get; set; }
    }

    /// <summary>
    /// Decodes digitizer input reports (touch pad, touch screen, pen) into contacts with hid.dll, driven by the
    /// device's preparsed data. Contacts are read from the Finger link collections in index order, like the
    /// daemon's TouchPadDevice/TouchScreenDevice; a pen reports one contact from the top-level collection.
    /// </summary>
    public sealed class ContactDecoder : IDisposable
    {
        private const ushort DigitizerPage = 0x0D, GenericDesktopPage = 0x01, ButtonPage = 0x09;
        private const ushort PenUsage = 0x02, FingerUsage = 0x22;
        private const ushort TipSwitch = 0x42, InRange = 0x32, Confidence = 0x47, ContactId = 0x51, ContactCount = 0x54;
        private const ushort XUsage = 0x30, YUsage = 0x31;

        private readonly SafeUnmanagedMemoryHandle _preparsedData;
        private readonly Slot[] _slots;
        private readonly bool _hasContactCount;
        private readonly ushort[] _usages = new ushort[32];

        private ContactDecoder(SafeUnmanagedMemoryHandle preparsedData, int usage, Slot[] slots, bool hasContactCount, SizeF? physicalSize)
        {
            _preparsedData = preparsedData;
            Usage = usage;
            _slots = slots;
            _hasContactCount = hasContactCount;
            PhysicalSizeMm = physicalSize;
        }

        /// <summary>Top-level collection usage on the digitizer page (0x02 pen, 0x04 touch screen, 0x05 touch pad).</summary>
        public int Usage { get; }

        public bool IsPen => Usage == PenUsage;

        /// <summary>Contacts one report can carry (5 for a parallel-mode precision touchpad).</summary>
        public int ContactSlots => _slots.Length;

        /// <summary>Surface size from the X/Y physical ranges; null when the descriptor has no length unit.</summary>
        public SizeF? PhysicalSizeMm { get; }

        /// <summary>Width / height of the surface: physical when known, otherwise from the logical ranges.</summary>
        public float AspectRatio
        {
            get
            {
                if (PhysicalSizeMm.HasValue && PhysicalSizeMm.Value.Height > 0)
                    return PhysicalSizeMm.Value.Width / PhysicalSizeMm.Value.Height;
                Slot slot = _slots[0];
                return slot.Y.Span > 0 ? (float)slot.X.Span / slot.Y.Span : 1f;
            }
        }

        /// <summary>Returns null when the preparsed data is not a digitizer collection with X/Y contacts.</summary>
        public static ContactDecoder TryCreate(byte[] preparsedData)
        {
            if (preparsedData == null || preparsedData.Length == 0)
                return null;

            IntPtr native = Marshal.AllocHGlobal(preparsedData.Length);
            Marshal.Copy(preparsedData, 0, native, preparsedData.Length);
            var handle = new SafeUnmanagedMemoryHandle(native);
            ContactDecoder decoder = null;
            try
            {
                decoder = Create(handle);
                return decoder;
            }
            finally
            {
                if (decoder == null)
                    handle.Dispose();
            }
        }

        private static ContactDecoder Create(SafeUnmanagedMemoryHandle handle)
        {
            IntPtr pp = handle.DangerousGetHandle();
            var caps = new HidNativeApi.HIDP_CAPS();
            if (HidNativeApi.HidP_GetCaps(pp, ref caps) != HidNativeApi.HIDP_STATUS_SUCCESS || (ushort)caps.UsagePage != DigitizerPage)
                return null;

            int nodeCount = Math.Max((int)caps.NumberLinkCollectionNodes, 1);
            var nodes = new HidNativeApi.HIDP_LINK_COLLECTION_NODE[nodeCount];
            if (HidNativeApi.HidP_GetLinkCollectionNodes(nodes, ref nodeCount, pp) != HidNativeApi.HIDP_STATUS_SUCCESS)
                return null;

            var slots = new List<Slot>();
            for (short i = 1; i < nodeCount; i++)
            {
                if ((ushort)nodes[i].LinkUsagePage != DigitizerPage || (ushort)nodes[i].LinkUsage != FingerUsage)
                    continue;
                Slot slot;
                if (TryCreateSlot(pp, i, out slot))
                    slots.Add(slot);
            }
            if (slots.Count == 0)
            {
                // Pen and single-contact devices: X/Y live in the top-level collection.
                Slot slot;
                if (!TryCreateSlot(pp, 0, out slot))
                    return null;
                slots.Add(slot);
            }

            var countCaps = new HidNativeApi.HidP_Value_Caps[1];
            short countLength = 1;
            bool hasContactCount = HidNativeApi.HidP_GetSpecificValueCaps(HidReportType.Input, DigitizerPage, 0, ContactCount,
                countCaps, ref countLength, pp) == HidNativeApi.HIDP_STATUS_SUCCESS;

            SizeF? size = null;
            float width = slots[0].X.PhysicalMm, height = slots[0].Y.PhysicalMm;
            if (width > 0 && height > 0)
                size = new SizeF(width, height);

            return new ContactDecoder(handle, (ushort)caps.Usage, slots.ToArray(), hasContactCount, size);
        }

        private static bool TryCreateSlot(IntPtr pp, short linkCollection, out Slot slot)
        {
            slot = new Slot { LinkCollection = linkCollection };
            return TryGetAxis(pp, linkCollection, XUsage, out slot.X) && TryGetAxis(pp, linkCollection, YUsage, out slot.Y);
        }

        private static bool TryGetAxis(IntPtr pp, short linkCollection, ushort usage, out Axis axis)
        {
            axis = default(Axis);
            var caps = new HidNativeApi.HidP_Value_Caps[1];
            short length = 1;
            if (HidNativeApi.HidP_GetSpecificValueCaps(HidReportType.Input, GenericDesktopPage, (ushort)linkCollection, usage, caps, ref length, pp) != HidNativeApi.HIDP_STATUS_SUCCESS || length < 1)
                return false;
            HidNativeApi.HidP_Value_Caps c = caps[0];
            axis = new Axis
            {
                LogicalMin = c.LogicalMin,
                LogicalMax = c.LogicalMax,
                BitSize = c.BitSize,
                PhysicalMm = PhysicalLengthMm(c),
            };
            return axis.LogicalMax > axis.LogicalMin;
        }

        /// <summary>Physical extent in millimetres for centimetre (SI linear) or inch (English linear) units, else 0.</summary>
        private static float PhysicalLengthMm(HidNativeApi.HidP_Value_Caps caps)
        {
            int system = caps.Units & 0xF;
            int length = (caps.Units >> 4) & 0xF;
            int range = caps.PhysicalMax - caps.PhysicalMin;
            if (length != 1 || range <= 0)
                return 0;
            double mmPerUnit;
            switch (system)
            {
                case 1: mmPerUnit = 10; break;    // SI linear: centimetre
                case 3: mmPerUnit = 25.4; break;  // English linear: inch
                default: return 0;
            }
            int exponent = caps.UnitsExp & 0xF;
            if (exponent > 7) exponent -= 16;
            return (float)(range * Math.Pow(10, exponent) * mmPerUnit);
        }

        /// <summary>Decodes one input report of <paramref name="length"/> bytes at <paramref name="offset"/>.</summary>
        /// <returns>null when the report does not carry this collection's contacts (other report ID).</returns>
        public DecodedReport Decode(byte[] buffer, int offset, int length)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || length <= 0 || offset + length > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(length));

            GCHandle pin = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                return Decode(IntPtr.Add(pin.AddrOfPinnedObject(), offset), length);
            }
            finally
            {
                pin.Free();
            }
        }

        private DecodedReport Decode(IntPtr report, int length)
        {
            IntPtr pp = _preparsedData.DangerousGetHandle();
            var slots = new DecodedContact[_slots.Length];
            for (int i = 0; i < _slots.Length; i++)
            {
                Slot slot = _slots[i];
                int rawX = 0, rawY = 0;
                if (HidNativeApi.HidP_GetUsageValue(HidReportType.Input, GenericDesktopPage, slot.LinkCollection, XUsage, ref rawX, pp, report, length) != HidNativeApi.HIDP_STATUS_SUCCESS ||
                    HidNativeApi.HidP_GetUsageValue(HidReportType.Input, GenericDesktopPage, slot.LinkCollection, YUsage, ref rawY, pp, report, length) != HidNativeApi.HIDP_STATUS_SUCCESS)
                {
                    return null;
                }

                int id = 0;
                if (HidNativeApi.HidP_GetUsageValue(HidReportType.Input, DigitizerPage, slot.LinkCollection, ContactId, ref id, pp, report, length) != HidNativeApi.HIDP_STATUS_SUCCESS)
                    id = i;

                var contact = new DecodedContact { Id = id, X = slot.X.Normalize(rawX), Y = slot.Y.Normalize(rawY) };
                int usageCount = _usages.Length;
                if (HidNativeApi.HidP_GetUsages(HidReportType.Input, DigitizerPage, slot.LinkCollection, _usages, ref usageCount, pp, report, length) == HidNativeApi.HIDP_STATUS_SUCCESS)
                {
                    for (int u = 0; u < usageCount; u++)
                    {
                        switch (_usages[u])
                        {
                            case TipSwitch: contact.Tip = true; break;
                            case InRange: contact.InRange = true; break;
                            case Confidence: contact.Confidence = true; break;
                        }
                    }
                }
                slots[i] = contact;
            }

            var decoded = new DecodedReport { Slots = slots };
            if (_hasContactCount)
            {
                int count = 0;
                if (HidNativeApi.HidP_GetUsageValue(HidReportType.Input, DigitizerPage, 0, ContactCount, ref count, pp, report, length) == HidNativeApi.HIDP_STATUS_SUCCESS)
                    decoded.ContactCount = count;
            }
            int buttons = _usages.Length;
            decoded.ButtonDown = HidNativeApi.HidP_GetUsages(HidReportType.Input, ButtonPage, 0, _usages, ref buttons, pp, report, length) == HidNativeApi.HIDP_STATUS_SUCCESS && buttons > 0;
            return decoded;
        }

        public void Dispose()
        {
            _preparsedData.Dispose();
        }

        private struct Slot
        {
            public short LinkCollection;
            public Axis X;
            public Axis Y;
        }

        private struct Axis
        {
            public int LogicalMin;
            public int LogicalMax;
            public int BitSize;
            public float PhysicalMm;

            public long Span => (long)LogicalMax - LogicalMin;

            public double Normalize(int raw)
            {
                long value = (uint)raw;
                // hid.dll returns the raw bits; sign-extend fields with a negative logical minimum.
                if (LogicalMin < 0 && BitSize > 0 && BitSize < 32 && (value & (1L << (BitSize - 1))) != 0)
                    value -= 1L << BitSize;
                double n = (double)(value - LogicalMin) / Span;
                return n < 0 ? 0 : n > 1 ? 1 : n;
            }
        }
    }
}
