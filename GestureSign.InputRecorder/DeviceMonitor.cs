using GestureSign.Daemon.Native;
using GestureSign.InputRecorder.Recording;
using System;
using System.Drawing;

namespace GestureSign.InputRecorder
{
    /// <summary>Live state of one digitizer collection: decoder, contact trackers and report counters.</summary>
    internal sealed class DeviceMonitor : IDisposable
    {
        private int _rateReports;
        private int _rateTick = Environment.TickCount;

        public DeviceMonitor(IntPtr handle, RecordedDevice info)
        {
            Handle = handle;
            Info = info;
            Decoder = ContactDecoder.TryCreate(DeviceCatalog.GetPreparsedData(handle));
            bool pen = Decoder?.IsPen ?? info.Usage == NativeMethods.PenUsage;
            Live = new ContactTracker(pen);
            Take = new ContactTracker(pen);
        }

        public IntPtr Handle { get; }

        public RecordedDevice Info { get; }

        /// <summary>Null when the preparsed data has no decodable X/Y contacts (reports are still recorded).</summary>
        public ContactDecoder Decoder { get; }

        /// <summary>All contacts since the last idle period; shown when no take is displayed.</summary>
        public ContactTracker Live { get; }

        /// <summary>Contacts of the current take only.</summary>
        public ContactTracker Take { get; }

        public int Reports { get; set; }

        /// <summary>Environment.TickCount of the last report that left a contact down.</summary>
        public int LastActiveTick { get; set; }

        public int ReportsPerSecond { get; private set; }

        public string Kind => DeviceCatalog.UsageName(Info.UsagePage, Info.Usage);

        public string VidPid => string.Format("{0:X4}:{1:X4}", Info.VendorId, Info.ProductId);

        public string SurfaceText
        {
            get
            {
                SizeF? size = Decoder?.PhysicalSizeMm;
                return size.HasValue ? string.Format("{0:0} x {1:0} mm", size.Value.Width, size.Value.Height) : "";
            }
        }

        public void UpdateRate(int now)
        {
            int elapsed = unchecked(now - _rateTick);
            if (elapsed < 1000)
                return;
            ReportsPerSecond = (Reports - _rateReports) * 1000 / elapsed;
            _rateReports = Reports;
            _rateTick = now;
        }

        public void Dispose()
        {
            Decoder?.Dispose();
        }
    }
}
