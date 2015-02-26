using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace ManagedWinapi
{
    /// <summary>
    /// Collection of miscellaneous actions that cannot be performed as 
    /// a non-administrative user, like shutdown or setting the system time.
    /// </summary>
    public static class PrivilegedActions
    {
        /// <summary>
        /// Shutdown the system.
        /// </summary>
        public static void ShutDown(ShutdownAction action)
        {
            ShutDown(action, ShutdownForceMode.NoForce);
        }

        /// <summary>
        /// Shutdown the system.
        /// </summary>
        public static void ShutDown(ShutdownAction action, ShutdownForceMode forceMode)
        {
            ApiHelper.FailIfZero(ExitWindowsEx((uint)action | (uint)forceMode, SHTDN_REASON_FLAG_PLANNED));
        }

        /// <summary>
        /// Get or set the system time in the local timezone.
        /// </summary>
        public static DateTime LocalTime
        {
            get
            {
                SYSTEMTIME st = new SYSTEMTIME();
                ApiHelper.FailIfZero(GetLocalTime(ref st));
                return st.ToDateTime();
            }

            set
            {
                SYSTEMTIME st = new SYSTEMTIME(value);
                // Set it twice due to possible daylight savings change
                ApiHelper.FailIfZero(SetLocalTime(ref st));
                ApiHelper.FailIfZero(SetLocalTime(ref st));
            }
        }

        /// <summary>
        /// Get or set the system time, in UTC.
        /// </summary>
        public static DateTime SystemTime
        {
            get
            {
                SYSTEMTIME st = new SYSTEMTIME();
                ApiHelper.FailIfZero(GetSystemTime(ref st));
                return st.ToDateTime();
            }

            set
            {
                SYSTEMTIME st = new SYSTEMTIME(value);
                ApiHelper.FailIfZero(SetLocalTime(ref st));
            }
        }

        /// <summary>
        /// Actions that can be performed at shutdown.
        /// </summary>
        public enum ShutdownAction : uint
        {
            /// <summary>
            /// Log off the currently logged-on user.
            /// </summary>
            LogOff = 0x00,

            /// <summary>
            /// Shut down the system.
            /// </summary>
            ShutDown = 0x01,

            /// <summary>
            /// Reboot the system.
            /// </summary>
            Reboot = 0x02,

            /// <summary>
            /// Shut down the system and power it off.
            /// </summary>
            PowerOff = 0x08,

            /// <summary>
            /// Reboot the system and restart applications that are running
            /// now and support this feature.
            /// </summary>
            RestartApps = 0x40,
        }

        /// <summary>
        /// Whether shutdown should be forced if an application cancels it
        /// or is hung.
        /// </summary>
        public enum ShutdownForceMode : uint
        {
            /// <summary>
            /// Do not force shutdown, applications can cancel it.
            /// </summary>
            NoForce = 0x00,

            /// <summary>
            /// Force shutdown, even if application cancels it or is hung.
            /// </summary>
            Force = 0x04,

            /// <summary>
            /// Force shutdown if application is hung, but not if it cancels it.
            /// </summary>
            ForceIfHung = 0x10
        }

        #region PInvoke Declarations

        [DllImport("user32.dll", SetLastError = true)]
        static extern int ExitWindowsEx(uint uFlags, uint dwReason);

        const uint SHTDN_REASON_FLAG_PLANNED = 0x80000000;

        struct SYSTEMTIME
        {
            internal ushort wYear, wMonth, wDayOfWeek, wDay,
               wHour, wMinute, wSecond, wMilliseconds;

            internal SYSTEMTIME(DateTime time)
            {
                wYear = (ushort)time.Year;
                wMonth = (ushort)time.Month;
                wDayOfWeek = (ushort)time.DayOfWeek;
                wDay = (ushort)time.Day;
                wHour = (ushort)time.Hour;
                wMinute = (ushort)time.Minute;
                wSecond = (ushort)time.Second;
                wMilliseconds = (ushort)time.Millisecond;
            }

            internal DateTime ToDateTime()
            {
                return new DateTime(wYear, wMonth, wDay, wHour, wMinute, wSecond, wMilliseconds);
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int GetSystemTime(ref SYSTEMTIME lpSystemTime);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int SetSystemTime(ref SYSTEMTIME lpSystemTime);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int GetLocalTime(ref SYSTEMTIME lpSystemTime);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int SetLocalTime(ref SYSTEMTIME lpSystemTime);

        #endregion
    }
}
