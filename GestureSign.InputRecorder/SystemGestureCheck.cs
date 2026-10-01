using Microsoft.Win32;
using System.Collections.Generic;
using System.Diagnostics;

namespace GestureSign.InputRecorder
{
    /// <summary>
    /// Detects software that reacts to the gestures being recorded. Raw input is recorded either way, but a
    /// 3-finger swipe that opens Task View changes what the user sees mid-take.
    /// </summary>
    internal static class SystemGestureCheck
    {
        private const string PrecisionTouchPadKey = @"Software\Microsoft\Windows\CurrentVersion\PrecisionTouchPad";

        /// <summary>Warning text, or null when nothing interferes.</summary>
        public static string Describe(bool touchpadPresent)
        {
            var warnings = new List<string>();
            if (touchpadPresent)
            {
                var enabled = new List<string>();
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(PrecisionTouchPadKey))
                {
                    AddIfEnabled(enabled, key, "ThreeFingerSlideEnabled", "3-finger swipes");
                    AddIfEnabled(enabled, key, "ThreeFingerTapEnabled", "3-finger tap");
                    AddIfEnabled(enabled, key, "FourFingerSlideEnabled", "4-finger swipes");
                    AddIfEnabled(enabled, key, "FourFingerTapEnabled", "4-finger tap");
                }
                if (enabled.Count > 0)
                {
                    warnings.Add("Windows also acts on touchpad " + string.Join(", ", enabled) +
                                 " (Task View, search, desktops...). The take is still recorded, but set them to \"Nothing\" to record undisturbed.");
                }
            }
            if (IsGestureSignRunning())
                warnings.Add("GestureSign is running and will also execute actions for recognized gestures.");
            return warnings.Count == 0 ? null : string.Join(" ", warnings);
        }

        /// <summary>A missing value means the Windows default, which is enabled.</summary>
        private static void AddIfEnabled(List<string> enabled, RegistryKey key, string valueName, string label)
        {
            object value = key?.GetValue(valueName);
            if (!(value is int) || (int)value != 0)
                enabled.Add(label);
        }

        private static bool IsGestureSignRunning()
        {
            Process[] processes = Process.GetProcessesByName("GestureSign");
            foreach (Process process in processes)
                process.Dispose();
            return processes.Length > 0;
        }
    }
}
