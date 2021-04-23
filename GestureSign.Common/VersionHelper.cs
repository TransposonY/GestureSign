using System;

namespace GestureSign.Common
{
    public class VersionHelper
    {
        private static Version _osVersion;

        public static Version OsVersion
        {
            get
            {
                if (_osVersion == null)
                {
                    _osVersion = Environment.OSVersion.Version;
                }
                return _osVersion;
            }
        }

        public static bool IsWindowsVistaOrGreater()
        {
            return OsVersion.Major >= 6;
        }

        public static bool IsWindows8OrGreater()
        {
            return OsVersion >= new Version(6, 2);
        }

        public static bool IsWindows8Point1OrGreater()
        {
            return OsVersion >= new Version(6, 3);
        }

        public static bool IsWindows10OrGreater()
        {
            return OsVersion.Major >= 10;
        }
    }
}
