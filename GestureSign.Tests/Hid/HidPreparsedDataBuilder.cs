using System;
using System.Collections.Generic;

namespace GestureSign.Tests.Hid
{
    /// <summary>Usage page / usage pair of a top-level collection.</summary>
    public struct HidTopLevelCollection
    {
        public HidTopLevelCollection(ushort usagePage, ushort usage)
        {
            UsagePage = usagePage;
            Usage = usage;
        }

        public ushort UsagePage { get; }
        public ushort Usage { get; }

        public override string ToString() => $"{UsagePage:X4}:{Usage:X4}";
    }

    /// <summary>
    /// Compiles a HID report descriptor into the opaque Windows <c>HIDP_PREPARSED_DATA</c> blob
    /// ("HidP KDR" layout) that hid.dll's HidP_* functions consume, so tests can exercise the real
    /// parser without hardware.
    /// </summary>
    /// <remarks>
    /// Layout: hidapi's reverse engineering (windows/hidapi_descriptor_reconstruct.h: hidp_preparsed_data,
    /// hid_pp_caps_info, 104-byte hid_pp_cap, 16-byte hid_pp_link_collection_node).
    /// Semantics (cap ordering, data indices, usage list rules, report byte lengths) follow Wine's
    /// Windows-compatible hidparse.sys (dlls/hidparse.sys/main.c) and are validated field-by-field
    /// against preparsed data dumped from real Windows in hidapi's windows/test/data corpus
    /// (see PreparsedDataBuilderCorpusTests).
    /// Unsupported (throws <see cref="NotSupportedException"/>): Delimiter, long items, reserved item tags.
    /// Designator/string items are accepted but, lacking Windows reference data, unvalidated.
    /// </remarks>
    public static class HidPreparsedDataBuilder
    {
        public const int HeaderSize = 44;
        public const int CapSize = 104;
        public const int LinkCollectionNodeSize = 16;

        private const byte FlagArrayHasMore = 0x01;
        private const byte FlagIsConstant = 0x02;
        private const byte FlagIsButton = 0x04;
        private const byte FlagIsAbsolute = 0x08;
        private const byte FlagIsRange = 0x10;
        private const byte FlagIsStringRange = 0x40;
        private const byte FlagIsDesignatorRange = 0x80;

        private const uint MainConstant = 0x01;
        private const uint MainVariable = 0x02;
        private const uint MainRelative = 0x04;
        private const uint MainNullState = 0x40;

        /// <summary>Lists the top-level collections of a report descriptor in declaration order.</summary>
        public static IReadOnlyList<HidTopLevelCollection> GetTopLevelCollections(byte[] reportDescriptor)
        {
            var parsed = Parse(reportDescriptor);
            var result = new List<HidTopLevelCollection>(parsed.Count);
            foreach (var tlc in parsed)
                result.Add(new HidTopLevelCollection(tlc.Collections[0].UsagePage, tlc.Collections[0].Usage));
            return result;
        }

        /// <summary>
        /// Builds the preparsed data of top-level collection <paramref name="topLevelCollection"/>
        /// (0-based, in descriptor order), exactly as Windows would hand it to user mode.
        /// </summary>
        public static byte[] Build(byte[] reportDescriptor, int topLevelCollection = 0)
        {
            var tlcs = Parse(reportDescriptor);
            if (topLevelCollection < 0 || topLevelCollection >= tlcs.Count)
                throw new ArgumentOutOfRangeException(nameof(topLevelCollection),
                    $"Descriptor has {tlcs.Count} top-level collection(s).");
            return Serialize(tlcs[topLevelCollection]);
        }

        #region Model

        private sealed class Cap
        {
            public ushort UsagePage;
            public byte ReportId;
            public byte BitPosition;
            public ushort BitSize;
            public ushort ReportCount;
            public ushort BytePosition;
            public ushort BitCount;
            public uint BitField;
            public ushort NextBytePosition;
            public ushort LinkCollection;
            public ushort LinkUsagePage;
            public ushort LinkUsage;
            public byte Flags;
            public ushort UsageMin, UsageMax;
            public ushort StringMin, StringMax;
            public ushort DesignatorMin, DesignatorMax;
            public ushort DataIndexMin, DataIndexMax;
            public ushort NullValue;
            public int LogicalMin, LogicalMax, PhysicalMin, PhysicalMax;
            public int Units, UnitsExp;
        }

        private sealed class Collection
        {
            public ushort UsagePage;
            public ushort Usage;
            public ushort Parent;
            public byte CollectionType;
        }

        private sealed class ReportTypeState
        {
            public readonly List<Cap> Caps = new List<Cap>();
            public readonly Dictionary<byte, int> BitsPerReportId = new Dictionary<byte, int>();
            public int ByteLength;
            public int EmptyCaps;
            public int DataCount;
        }

        private sealed class TopLevel
        {
            public readonly List<Collection> Collections = new List<Collection>();
            public readonly ReportTypeState[] Types = { new ReportTypeState(), new ReportTypeState(), new ReportTypeState() };
        }

        private struct Globals
        {
            public ushort UsagePage;
            public int LogicalMin, LogicalMax, PhysicalMin, PhysicalMax;
            public int UnitsExp, Units;
            public ushort ReportSize;
            public byte ReportId;
            public ushort ReportCount;
        }

        private sealed class Locals
        {
            public readonly List<ushort> UsagePages = new List<ushort>();
            public readonly List<ushort> UsageMins = new List<ushort>();
            public readonly List<ushort> UsageMaxs = new List<ushort>();
            public bool IsRange;
            public ushort StringMin, StringMax, DesignatorMin, DesignatorMax;
            public bool IsStringRange, IsDesignatorRange;

            public int Count => UsagePages.Count;

            public void Clear()
            {
                UsagePages.Clear();
                UsageMins.Clear();
                UsageMaxs.Clear();
                IsRange = false;
                StringMin = StringMax = DesignatorMin = DesignatorMax = 0;
                IsStringRange = IsDesignatorRange = false;
            }

            // Windows keeps either a list of single usages or exactly one Min/Max range: a Usage after a
            // range discards the range, Usage Minimum/Maximum replaces the list with slot 0.
            public void AddUsage(ushort page, ushort usage)
            {
                if (IsRange) ClearUsages();
                IsRange = false;
                if (UsagePages.Count == 256) throw new NotSupportedException("More than 256 usages before a main item.");
                UsagePages.Add(page);
                UsageMins.Add(usage);
                UsageMaxs.Add(usage);
            }

            public void SetRangeBound(ushort page, ushort usage, bool isMax)
            {
                ushort min = 0, max = 0;
                if (IsRange && Count > 0)
                {
                    min = UsageMins[0];
                    max = UsageMaxs[0];
                }
                ClearUsages();
                IsRange = true;
                UsagePages.Add(page);
                UsageMins.Add(isMax ? min : usage);
                UsageMaxs.Add(isMax ? usage : max);
            }

            private void ClearUsages()
            {
                UsagePages.Clear();
                UsageMins.Clear();
                UsageMaxs.Clear();
            }
        }

        #endregion

        #region Parsing

        private static List<TopLevel> Parse(byte[] descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));

            var tlcs = new List<TopLevel>();
            TopLevel current = null;
            var globals = new Globals();
            var globalStack = new Stack<Globals>();
            var locals = new Locals();
            // Collection indexes (within current TLC) of the open collections.
            var open = new Stack<ushort>();

            int pos = 0;
            while (pos < descriptor.Length)
            {
                byte prefix = descriptor[pos];
                if (prefix == 0xFE) throw new NotSupportedException($"Long item at offset {pos} is not supported.");
                int size = prefix & 0x03;
                if (size == 3) size = 4;
                if (pos + 1 + size > descriptor.Length)
                    throw new FormatException($"Item at offset {pos} needs {size} data bytes past the end of the descriptor.");

                uint value = 0;
                for (int i = 0; i < size; i++) value |= (uint)descriptor[pos + 1 + i] << (8 * i);
                int signedValue = size == 1 ? (sbyte)value : size == 2 ? (short)value : (int)value;
                int type = (prefix >> 2) & 0x03;
                int tag = prefix >> 4;

                switch (type)
                {
                    case 0: // Main
                        switch (tag)
                        {
                            case 0x8:
                            case 0x9:
                            case 0xB:
                                if (current == null)
                                    throw new FormatException($"Main item at offset {pos} outside any collection.");
                                AddMainItem(current, tag == 0x8 ? 0 : tag == 0x9 ? 1 : 2, value, ref globals, locals, open.Peek());
                                break;
                            case 0xA:
                                if (open.Count == 0)
                                {
                                    current = new TopLevel();
                                    tlcs.Add(current);
                                }
                                var collection = new Collection
                                {
                                    UsagePage = globals.UsagePage,
                                    Usage = locals.Count > 0 ? locals.UsageMins[0] : (ushort)0,
                                    Parent = open.Count > 0 ? open.Peek() : (ushort)0,
                                    CollectionType = (byte)value,
                                };
                                current.Collections.Add(collection);
                                open.Push((ushort)(current.Collections.Count - 1));
                                break;
                            case 0xC:
                                if (open.Count == 0)
                                    throw new FormatException($"End Collection at offset {pos} without open collection.");
                                open.Pop();
                                break;
                            default:
                                throw new NotSupportedException($"Reserved main item 0x{prefix:X2} at offset {pos}.");
                        }
                        locals.Clear();
                        break;

                    case 1: // Global
                        switch (tag)
                        {
                            case 0x0: globals.UsagePage = (ushort)value; break;
                            case 0x1: globals.LogicalMin = signedValue; break;
                            case 0x2: globals.LogicalMax = signedValue; break;
                            case 0x3: globals.PhysicalMin = signedValue; break;
                            case 0x4: globals.PhysicalMax = signedValue; break;
                            case 0x5: globals.UnitsExp = signedValue; break;
                            case 0x6: globals.Units = signedValue; break;
                            case 0x7: globals.ReportSize = (ushort)value; break;
                            case 0x8: globals.ReportId = (byte)value; break;
                            case 0x9: globals.ReportCount = (ushort)value; break;
                            case 0xA: globalStack.Push(globals); break;
                            case 0xB:
                                if (globalStack.Count == 0) throw new FormatException($"Pop at offset {pos} with empty global stack.");
                                globals = globalStack.Pop();
                                break;
                            default:
                                throw new NotSupportedException($"Reserved global item 0x{prefix:X2} at offset {pos}.");
                        }
                        break;

                    case 2: // Local
                        ushort page = size == 4 && (value >> 16) != 0 ? (ushort)(value >> 16) : globals.UsagePage;
                        ushort usage = (ushort)value;
                        switch (tag)
                        {
                            case 0x0: locals.AddUsage(page, usage); break;
                            case 0x1: locals.SetRangeBound(page, usage, false); break;
                            case 0x2: locals.SetRangeBound(page, usage, true); break;
                            case 0x3:
                                locals.DesignatorMin = locals.DesignatorMax = (ushort)value;
                                locals.IsDesignatorRange = false;
                                break;
                            case 0x4: locals.DesignatorMin = (ushort)value; locals.IsDesignatorRange = true; break;
                            case 0x5: locals.DesignatorMax = (ushort)value; locals.IsDesignatorRange = true; break;
                            case 0x7:
                                locals.StringMin = locals.StringMax = (ushort)value;
                                locals.IsStringRange = false;
                                break;
                            case 0x8: locals.StringMin = (ushort)value; locals.IsStringRange = true; break;
                            case 0x9: locals.StringMax = (ushort)value; locals.IsStringRange = true; break;
                            case 0xA:
                                throw new NotSupportedException($"Delimiter at offset {pos} is not supported.");
                            default:
                                throw new NotSupportedException($"Reserved local item 0x{prefix:X2} at offset {pos}.");
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Reserved item type 0x{prefix:X2} at offset {pos}.");
                }

                pos += 1 + size;
            }

            if (open.Count != 0) throw new FormatException("Descriptor ends with unclosed collection(s).");
            if (tlcs.Count == 0) throw new FormatException("Descriptor has no top-level collection.");
            return tlcs;
        }

        private static void AddMainItem(TopLevel tlc, int reportType, uint bitField, ref Globals globals, Locals locals, ushort linkCollection)
        {
            var state = tlc.Types[reportType];
            int bits;
            if (!state.BitsPerReportId.TryGetValue(globals.ReportId, out bits)) bits = 8; // report ID byte
            int itemBits = globals.ReportSize * globals.ReportCount;
            int startBit = bits;
            bits += itemBits;
            state.BitsPerReportId[globals.ReportId] = bits;
            state.ByteLength = Math.Max(state.ByteLength, (bits + 7) / 8);

            int usageCount = Math.Max(1, locals.Count);
            if (globals.ReportCount == 0)
            {
                state.EmptyCaps += usageCount;
                return;
            }

            bool isArray = (bitField & MainVariable) == 0;

            // Constant fields always occupy report bits. Without usages they get nothing else; constant
            // arrays and constant variables whose usages are all 0 only reserve (empty) cap slots that
            // NumberOfCaps counts; constant variables with real usages become padding caps.
            if ((bitField & MainConstant) != 0)
            {
                if (locals.Count == 0) return;
                if (isArray || locals.UsageMins.TrueForAll(u => u == 0) && locals.UsageMaxs.TrueForAll(u => u == 0))
                {
                    state.EmptyCaps += locals.Count;
                    return;
                }
            }

            var collection = tlc.Collections[linkCollection];

            byte flags = 0;
            if ((bitField & MainRelative) == 0) flags |= FlagIsAbsolute;
            if ((bitField & MainConstant) != 0) flags |= FlagIsConstant;
            if (globals.ReportSize == 1 || isArray) flags |= FlagIsButton;
            if (locals.IsRange) flags |= FlagIsRange;
            if (locals.IsStringRange) flags |= FlagIsStringRange;
            if (locals.IsDesignatorRange) flags |= FlagIsDesignatorRange;

            ushort nullValue;
            if (isArray) nullValue = (ushort)globals.LogicalMin;
            else nullValue = (ushort)((bitField & MainNullState) != 0 ? 1 : 0);

            // Variable items: the first N-1 usages get one field each, the last usage gets the rest;
            // usages beyond the report count only reserve empty cap slots.
            // Caps are emitted last usage first; data indices follow emission order.
            // Array items: every usage describes the whole array field; caps are stored in reverse
            // emission order so the first usage ends up last (and is the only one without ArrayHasMore).
            if (!isArray && usageCount > globals.ReportCount)
            {
                state.EmptyCaps += usageCount - globals.ReportCount;
                usageCount = globals.ReportCount;
            }
            var caps = new Cap[usageCount];
            int reportCount = isArray ? globals.ReportCount : globals.ReportCount - (usageCount - 1);
            int fieldStart = isArray ? startBit : bits;
            int dataIndexMax = state.DataCount - 1;

            for (int i = 0; i < usageCount; i++)
            {
                int capReportCount;
                if (isArray)
                {
                    capReportCount = globals.ReportCount;
                }
                else
                {
                    capReportCount = i == 0 ? reportCount : 1;
                    fieldStart -= capReportCount * globals.ReportSize;
                }

                int slot = isArray ? usageCount - 1 - i : i;
                int usageIndex = usageCount - 1 - slot;
                ushort usagePage = locals.Count > 0 ? locals.UsagePages[usageIndex] : globals.UsagePage;
                ushort usageMin = locals.Count > 0 ? locals.UsageMins[usageIndex] : (ushort)0;
                ushort usageMax = locals.Count > 0 ? locals.UsageMaxs[usageIndex] : (ushort)0;
                int totalBits = capReportCount * globals.ReportSize;

                int count = usageMax - usageMin;
                int dataIndexMin = dataIndexMax + 1;
                dataIndexMax = dataIndexMin + count;

                var cap = new Cap
                {
                    UsagePage = usagePage,
                    ReportId = globals.ReportId,
                    BitPosition = (byte)(fieldStart % 8),
                    BitSize = globals.ReportSize,
                    ReportCount = (ushort)capReportCount,
                    BytePosition = (ushort)(fieldStart / 8),
                    BitCount = (ushort)totalBits,
                    BitField = bitField,
                    NextBytePosition = (ushort)((fieldStart + totalBits + 7) / 8),
                    LinkCollection = linkCollection,
                    LinkUsagePage = collection.UsagePage,
                    LinkUsage = collection.Usage,
                    Flags = (byte)(flags | (isArray && i > 0 ? FlagArrayHasMore : 0)),
                    UsageMin = usageMin,
                    UsageMax = usageMax,
                    StringMin = locals.StringMin,
                    StringMax = locals.StringMax,
                    DesignatorMin = locals.DesignatorMin,
                    DesignatorMax = locals.DesignatorMax,
                    DataIndexMin = (ushort)dataIndexMin,
                    DataIndexMax = (ushort)dataIndexMax,
                    NullValue = nullValue,
                    LogicalMin = globals.LogicalMin,
                    LogicalMax = globals.LogicalMax,
                    PhysicalMin = globals.PhysicalMin,
                    PhysicalMax = globals.PhysicalMax,
                    Units = globals.Units,
                    UnitsExp = globals.UnitsExp,
                };

                if ((cap.Flags & FlagIsButton) != 0)
                {
                    cap.LogicalMin = isArray ? globals.LogicalMax : 0;
                    cap.LogicalMax = 0;
                    cap.PhysicalMin = 0;
                    cap.PhysicalMax = 0;
                }
                else if (cap.LogicalMin == 0 && cap.LogicalMax == 0)
                {
                    // Observed on Windows (hidapi fixture 047F_C056_0003_FFA0: values declared before any
                    // Logical Minimum/Maximum item report LogicalMax 1).
                    cap.LogicalMax = 1;
                }

                caps[slot] = cap;
            }

            state.Caps.AddRange(caps);
            state.DataCount = dataIndexMax + 1;
        }

        #endregion

        #region Serialization

        private static byte[] Serialize(TopLevel tlc)
        {
            int allCaps = 0;
            foreach (var t in tlc.Types) allCaps += t.Caps.Count + t.EmptyCaps;

            int capsBytes = allCaps * CapSize;
            var blob = new byte[HeaderSize + capsBytes + tlc.Collections.Count * LinkCollectionNodeSize];

            var magic = System.Text.Encoding.ASCII.GetBytes("HidP KDR");
            Buffer.BlockCopy(magic, 0, blob, 0, 8);
            PutU16(blob, 8, tlc.Collections[0].Usage);
            PutU16(blob, 10, tlc.Collections[0].UsagePage);

            int first = 0;
            for (int type = 0; type < 3; type++)
            {
                var t = tlc.Types[type];
                int info = 16 + type * 8;
                PutU16(blob, info + 0, first);                            // FirstCap
                PutU16(blob, info + 2, t.Caps.Count + t.EmptyCaps);       // NumberOfCaps
                PutU16(blob, info + 4, first + t.Caps.Count);             // LastCap
                PutU16(blob, info + 6, t.Caps.Count + t.EmptyCaps > 0 ? t.ByteLength : 0); // ReportByteLength
                for (int i = 0; i < t.Caps.Count; i++)
                    WriteCap(blob, HeaderSize + (first + i) * CapSize, t.Caps[i]);
                first += t.Caps.Count + t.EmptyCaps;
            }

            PutU16(blob, 40, capsBytes);
            PutU16(blob, 42, tlc.Collections.Count);

            int nodesOffset = HeaderSize + capsBytes;
            var firstChild = new ushort[tlc.Collections.Count];
            var nextSibling = new ushort[tlc.Collections.Count];
            var children = new ushort[tlc.Collections.Count];
            for (int i = 1; i < tlc.Collections.Count; i++)
            {
                int parent = tlc.Collections[i].Parent;
                nextSibling[i] = firstChild[parent];
                firstChild[parent] = (ushort)i;
                children[parent]++;
            }

            for (int i = 0; i < tlc.Collections.Count; i++)
            {
                var c = tlc.Collections[i];
                int o = nodesOffset + i * LinkCollectionNodeSize;
                PutU16(blob, o + 0, c.Usage);
                PutU16(blob, o + 2, c.UsagePage);
                PutU16(blob, o + 4, c.Parent);
                PutU16(blob, o + 6, children[i]);
                PutU16(blob, o + 8, nextSibling[i]);
                PutU16(blob, o + 10, firstChild[i]);
                PutU32(blob, o + 12, c.CollectionType);
            }

            return blob;
        }

        private static void WriteCap(byte[] b, int o, Cap c)
        {
            PutU16(b, o + 0, c.UsagePage);
            b[o + 2] = c.ReportId;
            b[o + 3] = c.BitPosition;
            PutU16(b, o + 4, c.BitSize);
            PutU16(b, o + 6, c.ReportCount);
            PutU16(b, o + 8, c.BytePosition);
            PutU16(b, o + 10, c.BitCount);
            PutU32(b, o + 12, c.BitField);
            PutU16(b, o + 16, c.NextBytePosition);
            PutU16(b, o + 18, c.LinkCollection);
            PutU16(b, o + 20, c.LinkUsagePage);
            PutU16(b, o + 22, c.LinkUsage);
            b[o + 24] = c.Flags;
            // 25..27 Reserved1, 28..59 UnknownTokens: zero.
            PutU16(b, o + 60, c.UsageMin);
            PutU16(b, o + 62, c.UsageMax);
            PutU16(b, o + 64, c.StringMin);
            PutU16(b, o + 66, c.StringMax);
            PutU16(b, o + 68, c.DesignatorMin);
            PutU16(b, o + 70, c.DesignatorMax);
            PutU16(b, o + 72, c.DataIndexMin);
            PutU16(b, o + 74, c.DataIndexMax);
            PutU16(b, o + 76, c.NullValue);
            PutU32(b, o + 80, (uint)c.LogicalMin);
            PutU32(b, o + 84, (uint)c.LogicalMax);
            PutU32(b, o + 88, (uint)c.PhysicalMin);
            PutU32(b, o + 92, (uint)c.PhysicalMax);
            PutU32(b, o + 96, (uint)c.Units);
            PutU32(b, o + 100, (uint)c.UnitsExp);
        }

        private static void PutU16(byte[] b, int o, int v)
        {
            b[o] = (byte)v;
            b[o + 1] = (byte)(v >> 8);
        }

        private static void PutU32(byte[] b, int o, uint v)
        {
            b[o] = (byte)v;
            b[o + 1] = (byte)(v >> 8);
            b[o + 2] = (byte)(v >> 16);
            b[o + 3] = (byte)(v >> 24);
        }

        #endregion
    }
}
