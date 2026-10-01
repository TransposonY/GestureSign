using System;
using System.IO;

namespace GestureSign.InputRecorder.Recording
{
    /// <summary>
    /// Converts a RAWINPUT buffer (bytes as filled by GetRawInputData(RID_INPUT)) into a <see cref="RecordedEvent"/>.
    /// Pure: no Win32 calls, so it can be tested with fabricated buffers.
    /// </summary>
    public static class RawInputConverter
    {
        public const int RimTypeHid = 2;

        /// <summary>sizeof(RAWINPUTHEADER) for the current process: dwType, dwSize, hDevice, wParam.</summary>
        public static int HeaderSize => 8 + 2 * IntPtr.Size;

        /// <summary>sizeof(RAWHID) without bRawData: dwSizHid, dwCount.</summary>
        public const int RawHidHeaderSize = 8;

        public static int GetInputType(byte[] rawInputBuffer)
        {
            CheckLength(rawInputBuffer, HeaderSize);
            return BitConverter.ToInt32(rawInputBuffer, 0);
        }

        public static IntPtr GetDeviceHandle(byte[] rawInputBuffer)
        {
            CheckLength(rawInputBuffer, HeaderSize);
            return IntPtr.Size == 8
                ? new IntPtr(BitConverter.ToInt64(rawInputBuffer, 8))
                : new IntPtr(BitConverter.ToInt32(rawInputBuffer, 8));
        }

        /// <summary>
        /// Locates the RAWHID payload (dwCount reports of dwSizHid bytes). Returns false when the buffer is not
        /// RIM_TYPEHID. The payload offset is computed exactly like the daemon's HidDevice.GetRawDataPtr:
        /// header.dwSize - dwSizHid * dwCount.
        /// </summary>
        /// <exception cref="InvalidDataException">The buffer is truncated or its sizes are inconsistent.</exception>
        public static bool TryGetHidPayload(byte[] rawInputBuffer, out int offset, out int sizeHid, out int count)
        {
            offset = sizeHid = count = 0;
            if (GetInputType(rawInputBuffer) != RimTypeHid)
                return false;

            CheckLength(rawInputBuffer, HeaderSize + RawHidHeaderSize);
            int dwSize = BitConverter.ToInt32(rawInputBuffer, 4);
            sizeHid = BitConverter.ToInt32(rawInputBuffer, HeaderSize);
            count = BitConverter.ToInt32(rawInputBuffer, HeaderSize + 4);

            long payloadOffset = dwSize - (long)sizeHid * count;
            if (sizeHid < 0 || count < 0 || dwSize > rawInputBuffer.Length || payloadOffset < HeaderSize + RawHidHeaderSize)
            {
                throw new InvalidDataException(string.Format(
                    "Inconsistent RAWHID: buffer {0} bytes, dwSize {1}, dwSizHid {2}, dwCount {3}.",
                    rawInputBuffer.Length, dwSize, sizeHid, count));
            }
            offset = (int)payloadOffset;
            return true;
        }

        /// <summary>
        /// Builds a <see cref="RecordedEventTypes.Hid"/> event. Returns null when the buffer is not RIM_TYPEHID.
        /// </summary>
        /// <exception cref="InvalidDataException">The buffer is truncated or its sizes are inconsistent.</exception>
        public static RecordedEvent ToHidEvent(byte[] rawInputBuffer, int deviceId, long t, int tickCount, int cursorX, int cursorY)
        {
            int offset, sizeHid, count;
            if (!TryGetHidPayload(rawInputBuffer, out offset, out sizeHid, out count))
                return null;

            return new RecordedEvent
            {
                T = t,
                TickCount = tickCount,
                Type = RecordedEventTypes.Hid,
                CursorX = cursorX,
                CursorY = cursorY,
                Device = deviceId,
                SizeHid = sizeHid,
                Count = count,
                Data = Convert.ToBase64String(rawInputBuffer, offset, sizeHid * count),
            };
        }

        private static void CheckLength(byte[] buffer, int minimum)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length < minimum)
                throw new InvalidDataException(string.Format("RAWINPUT buffer too short: {0} bytes, need {1}.", buffer.Length, minimum));
        }
    }
}
