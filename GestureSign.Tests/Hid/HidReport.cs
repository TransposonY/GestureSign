using System;
using System.Runtime.InteropServices;

namespace GestureSign.Tests.Hid
{
    /// <summary>
    /// Builds HID input reports with the real hid.dll (HidP_InitializeReportForID / HidP_Set*) from a
    /// preparsed data blob, e.g. one produced by <see cref="HidPreparsedDataBuilder"/>.
    /// Every call throws <see cref="HidPException"/> when hid.dll returns anything but HIDP_STATUS_SUCCESS.
    /// </summary>
    public sealed class HidReport
    {
        private readonly byte[] _preparsedData;
        private readonly byte[] _report;

        private HidReport(byte[] preparsedData, byte[] report)
        {
            _preparsedData = preparsedData;
            _report = report;
        }

        /// <summary>Starts an input report for <paramref name="reportId"/> (0 when the device uses no report IDs).</summary>
        public static HidReport Create(byte[] preparsedData, byte reportId)
        {
            if (preparsedData == null) throw new ArgumentNullException(nameof(preparsedData));
            var report = new byte[ReportLength(preparsedData)];
            HidPNative.Check(HidPNative.HidP_InitializeReportForID(HidPNative.HidP_Input, reportId, preparsedData, report, (uint)report.Length),
                "HidP_InitializeReportForID");
            return new HidReport(preparsedData, report);
        }

        /// <summary>HIDP_CAPS.InputReportByteLength (includes the report ID byte).</summary>
        public static int ReportLength(byte[] preparsedData) => GetCaps(preparsedData).InputReportByteLength;

        public static HidPNative.HIDP_CAPS GetCaps(byte[] preparsedData)
        {
            var caps = new HidPNative.HIDP_CAPS();
            HidPNative.Check(HidPNative.HidP_GetCaps(preparsedData, ref caps), "HidP_GetCaps");
            return caps;
        }

        /// <summary>Sets a raw (logical) value with HidP_SetUsageValue. Link collection 0 matches any collection.</summary>
        public HidReport SetValue(ushort usagePage, short linkCollection, ushort usage, int value)
        {
            HidPNative.Check(HidPNative.HidP_SetUsageValue(HidPNative.HidP_Input, usagePage, (ushort)linkCollection, usage, (uint)value,
                _preparsedData, _report, (uint)_report.Length), $"HidP_SetUsageValue({usagePage:X2}:{usage:X2}, link {linkCollection})");
            return this;
        }

        /// <summary>Sets a physical value with HidP_SetScaledUsageValue (hid.dll converts it to the logical range).</summary>
        public HidReport SetScaledValue(ushort usagePage, short linkCollection, ushort usage, int value)
        {
            HidPNative.Check(HidPNative.HidP_SetScaledUsageValue(HidPNative.HidP_Input, usagePage, (ushort)linkCollection, usage, value,
                _preparsedData, _report, (uint)_report.Length), $"HidP_SetScaledUsageValue({usagePage:X2}:{usage:X2}, link {linkCollection})");
            return this;
        }

        /// <summary>Turns on the given buttons (binary usages) with HidP_SetUsages.</summary>
        public HidReport SetButtons(ushort usagePage, short linkCollection, params ushort[] usages)
        {
            if (usages == null || usages.Length == 0) return this;
            var list = (ushort[])usages.Clone();
            uint length = (uint)list.Length;
            HidPNative.Check(HidPNative.HidP_SetUsages(HidPNative.HidP_Input, usagePage, (ushort)linkCollection, list, ref length,
                _preparsedData, _report, (uint)_report.Length), $"HidP_SetUsages({usagePage:X2}, link {linkCollection})");
            return this;
        }

        /// <summary>Reads back a raw value with HidP_GetUsageValue (self-check).</summary>
        public int GetValue(ushort usagePage, short linkCollection, ushort usage)
        {
            uint value = 0;
            HidPNative.Check(HidPNative.HidP_GetUsageValue(HidPNative.HidP_Input, usagePage, (ushort)linkCollection, usage, out value,
                _preparsedData, _report, (uint)_report.Length), $"HidP_GetUsageValue({usagePage:X2}:{usage:X2}, link {linkCollection})");
            return (int)value;
        }

        public byte[] ToArray() => (byte[])_report.Clone();
    }

    public sealed class HidPException : Exception
    {
        public HidPException(string operation, int status)
            : base($"{operation} failed: {HidPNative.StatusName(status)} (0x{status:X8})")
        {
            Status = status;
        }

        public int Status { get; }
    }

    /// <summary>hid.dll HidP_* declarations taking the preparsed data as a byte[] (pinned for the call).</summary>
    public static class HidPNative
    {
        public const int HidP_Input = 0;
        public const int HIDP_STATUS_SUCCESS = 0x00110000;

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDP_CAPS
        {
            public ushort Usage;
            public ushort UsagePage;
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public ushort[] Reserved;
            public ushort NumberLinkCollectionNodes;
            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;
            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;
            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;
        }

        [DllImport("hid.dll")]
        public static extern int HidP_GetCaps(byte[] preparsedData, ref HIDP_CAPS capabilities);

        [DllImport("hid.dll")]
        public static extern int HidP_InitializeReportForID(int reportType, byte reportId, byte[] preparsedData,
            [In, Out] byte[] report, uint reportLength);

        [DllImport("hid.dll")]
        public static extern int HidP_SetUsageValue(int reportType, ushort usagePage, ushort linkCollection, ushort usage,
            uint usageValue, byte[] preparsedData, [In, Out] byte[] report, uint reportLength);

        [DllImport("hid.dll")]
        public static extern int HidP_SetScaledUsageValue(int reportType, ushort usagePage, ushort linkCollection, ushort usage,
            int usageValue, byte[] preparsedData, [In, Out] byte[] report, uint reportLength);

        [DllImport("hid.dll")]
        public static extern int HidP_SetUsages(int reportType, ushort usagePage, ushort linkCollection, [In, Out] ushort[] usageList,
            ref uint usageLength, byte[] preparsedData, [In, Out] byte[] report, uint reportLength);

        [DllImport("hid.dll")]
        public static extern int HidP_GetUsageValue(int reportType, ushort usagePage, ushort linkCollection, ushort usage,
            out uint usageValue, byte[] preparsedData, byte[] report, uint reportLength);

        public static void Check(int status, string operation)
        {
            if (status != HIDP_STATUS_SUCCESS) throw new HidPException(operation, status);
        }

        public static string StatusName(int status)
        {
            switch ((uint)status)
            {
                case 0x00110000: return "HIDP_STATUS_SUCCESS";
                case 0x80110001: return "HIDP_STATUS_NULL";
                case 0xC0110001: return "HIDP_STATUS_INVALID_PREPARSED_DATA";
                case 0xC0110002: return "HIDP_STATUS_INVALID_REPORT_TYPE";
                case 0xC0110003: return "HIDP_STATUS_INVALID_REPORT_LENGTH";
                case 0xC0110004: return "HIDP_STATUS_USAGE_NOT_FOUND";
                case 0xC0110005: return "HIDP_STATUS_VALUE_OUT_OF_RANGE";
                case 0xC0110006: return "HIDP_STATUS_BAD_LOG_PHY_VALUES";
                case 0xC0110007: return "HIDP_STATUS_BUFFER_TOO_SMALL";
                case 0xC0110008: return "HIDP_STATUS_INTERNAL_ERROR";
                case 0xC011000A: return "HIDP_STATUS_INCOMPATIBLE_REPORT_ID";
                case 0xC011000B: return "HIDP_STATUS_NOT_VALUE_ARRAY";
                case 0xC011000C: return "HIDP_STATUS_IS_VALUE_ARRAY";
                case 0xC011000D: return "HIDP_STATUS_DATA_INDEX_NOT_FOUND";
                case 0xC011000E: return "HIDP_STATUS_DATA_INDEX_OUT_OF_RANGE";
                case 0xC011000F: return "HIDP_STATUS_BUTTON_NOT_PRESSED";
                case 0xC0110010: return "HIDP_STATUS_REPORT_DOES_NOT_EXIST";
                case 0xC0110020: return "HIDP_STATUS_NOT_IMPLEMENTED";
                default: return "NTSTATUS";
            }
        }
    }
}
