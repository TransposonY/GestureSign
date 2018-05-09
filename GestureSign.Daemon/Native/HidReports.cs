/*
   HidLibrary.Net   http://sourceforge.net/projects/hidlibrary/
 
   Copyright (C)    Roman Reichel 2006
					Bauhaus University of Weimar

   This library is free software; you can redistribute it and/or
   modify it under the terms of the GNU Lesser General Public
   License as published by the Free Software Foundation; either
   version 2.1 of the License, or (at your option) any later version.

   This library is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
   Lesser General Public License for more details.

   You should have received a copy of the GNU Lesser General Public
   License along with this library; if not, see http://www.gnu.org/licenses .

*/

using System;

namespace GestureSign.Daemon.Native
{
    /// <summary>
    /// Type of a Hid Report
    /// </summary>
    public enum HidReportType
    {
        /// <summary>
        /// Input Report
        /// </summary>
        Input = 0,
        /// <summary>
        /// Output Report
        /// </summary>
        Output = 1,
        /// <summary>
        /// Feature Report
        /// </summary>
        Feature = 2
    }

    public abstract class HidReport
    {
        private byte[] reportBuffer;

        internal HidReport(byte[] buffer)
        {
            reportBuffer = new byte[buffer.Length];
            for (int i = 0; i < buffer.Length; i++) reportBuffer[i] = buffer[i];
        }

        public int Length
        {
            get { return reportBuffer.Length; }
        }

        public HidReportType Type
        {
            get { throw new Exception("Not implemented in base class."); }
        }

        public byte[] Bytes
        {
            get
            {
                return reportBuffer;
            }

            set
            {
                reportBuffer = new byte[value.Length];
                for (int i = 0; i < value.Length; i++) reportBuffer[i] = value[i];
            }
        }

        public byte this[int i]
        {
            get
            {
                return reportBuffer[i];
            }
            set
            {
                reportBuffer[i] = value;
            }
        }

    }

    internal class HidInReport : HidReport
    {
        public HidInReport(byte[] buffer)
            : base(buffer)
        { }

        public new HidReportType Type
        {
            get { return HidReportType.Input; }
        }
    }

    internal class HidOutReport : HidReport
    {
        public HidOutReport(byte[] buffer)
            : base(buffer)
        { }

        public new HidReportType Type
        {
            get { return HidReportType.Output; }
        }
    }

    internal class HidFeatureReport : HidReport
    {
        public HidFeatureReport(byte[] buffer)
            : base(buffer)
        { }

        public new HidReportType Type
        {
            get { return HidReportType.Feature; }
        }
    }

    public class HidDataReceivedEventArgs : EventArgs
    {
        private HidInReport report;

        public int Length
        {
            get { return report.Length; }
        }

        public HidReportType Type
        {
            get { return HidReportType.Input; }
        }

        public byte this[int i]
        {
            get
            {
                return report[i];
            }
        }

        internal HidDataReceivedEventArgs(byte[] data)
        {
            report = new HidInReport(data);
        }

    }


}