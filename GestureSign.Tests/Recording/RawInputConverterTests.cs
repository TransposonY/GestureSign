using GestureSign.Daemon.Native;
using GestureSign.InputRecorder.Recording;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace GestureSign.Tests.Recording
{
    public class RawInputConverterTests
    {
        private static readonly int HeaderSize = Marshal.SizeOf(typeof(RAWINPUTHEADER));

        /// <summary>Lays out RAWINPUT { RAWINPUTHEADER; RAWHID { dwSizHid; dwCount; [gap]; bRawData } }.</summary>
        private static byte[] BuildRawInput(int dwType, int sizHid, int count, byte[] payload, int gap = 0, int? dwSizeOverride = null)
        {
            int dwSize = HeaderSize + 8 + gap + payload.Length;
            var buffer = new byte[dwSize];
            BitConverter.GetBytes(dwType).CopyTo(buffer, 0);
            BitConverter.GetBytes(dwSizeOverride ?? dwSize).CopyTo(buffer, 4);
            // hDevice / wParam: arbitrary non-zero handle.
            BitConverter.GetBytes((long)0x1234).Take(IntPtr.Size).ToArray().CopyTo(buffer, 8);
            BitConverter.GetBytes(sizHid).CopyTo(buffer, HeaderSize);
            BitConverter.GetBytes(count).CopyTo(buffer, HeaderSize + 4);
            for (int i = 0; i < gap; i++)
                buffer[HeaderSize + 8 + i] = 0xEE;
            payload.CopyTo(buffer, HeaderSize + 8 + gap);
            return buffer;
        }

        [Fact]
        public void HidBufferWithSeveralReportsKeepsAllReportBytes()
        {
            byte[] payload = Enumerable.Range(1, 10).Select(i => (byte)i).ToArray();
            byte[] buffer = BuildRawInput(NativeMethods.RIM_TYPEHID, sizHid: 5, count: 2, payload: payload);

            RecordedEvent e = RawInputConverter.ToHidEvent(buffer, deviceId: 3, t: 42, tickCount: 1000, cursorX: 10, cursorY: -20);

            Assert.Equal(RecordedEventTypes.Hid, e.Type);
            Assert.Equal(3, e.Device);
            Assert.Equal(5, e.SizeHid);
            Assert.Equal(2, e.Count);
            Assert.Equal(payload, Convert.FromBase64String(e.Data));
            Assert.Equal(42, e.T);
            Assert.Equal(1000, e.TickCount);
            Assert.Equal(10, e.CursorX);
            Assert.Equal(-20, e.CursorY);
            Assert.Equal(new IntPtr(0x1234), RawInputConverter.GetDeviceHandle(buffer));
        }

        [Fact]
        public void PayloadIsAnchoredAtDwSizeEndLikeTheDaemon()
        {
            // HidDevice.GetRawDataPtr uses header.dwSize - dwSizHid * dwCount, not the offset right after RAWHID.
            byte[] payload = { 0xA1, 0xA2, 0xA3, 0xA4 };
            byte[] buffer = BuildRawInput(NativeMethods.RIM_TYPEHID, sizHid: 4, count: 1, payload: payload, gap: 4);

            RecordedEvent e = RawInputConverter.ToHidEvent(buffer, 1, 0, 0, 0, 0);

            Assert.Equal(payload, Convert.FromBase64String(e.Data));
        }

        [Fact]
        public void NonHidInputIsIgnored()
        {
            const int RimTypeMouse = 0;
            byte[] buffer = BuildRawInput(RimTypeMouse, sizHid: 0, count: 0, payload: new byte[16]);

            Assert.Null(RawInputConverter.ToHidEvent(buffer, 1, 0, 0, 0, 0));
        }

        [Fact]
        public void SizesPointingOutsideTheBufferAreRejected()
        {
            byte[] buffer = BuildRawInput(NativeMethods.RIM_TYPEHID, sizHid: 4, count: 1, payload: new byte[4], dwSizeOverride: HeaderSize + 8 + 64);

            Assert.Throws<InvalidDataException>(() => RawInputConverter.ToHidEvent(buffer, 1, 0, 0, 0, 0));
        }
    }
}
