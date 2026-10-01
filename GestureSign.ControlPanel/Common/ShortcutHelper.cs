using System;
using System.Runtime.InteropServices;

namespace GestureSign.ControlPanel.Common
{
    // WScript.Shell is installed with Windows; late-bound COM avoids MSBuild's Framework-only ResolveComReference.
    internal static class ShortcutHelper
    {
        public static string GetTargetPath(string path)
        {
            object shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell", throwOnError: true));
            object shortcut = null;
            try
            {
                shortcut = ((dynamic)shell).CreateShortcut(path);
                return ((dynamic)shortcut).TargetPath;
            }
            finally
            {
                if (shortcut != null) Marshal.FinalReleaseComObject(shortcut);
                Marshal.FinalReleaseComObject(shell);
            }
        }

        public static void Create(string path, string targetPath, string description)
        {
            object shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell", throwOnError: true));
            object shortcut = null;
            try
            {
                shortcut = ((dynamic)shell).CreateShortcut(path);
                dynamic link = shortcut;
                link.TargetPath = targetPath;
                link.WindowStyle = 7;
                link.Arguments = "";
                link.Description = description;
                link.Save();
            }
            finally
            {
                if (shortcut != null) Marshal.FinalReleaseComObject(shortcut);
                Marshal.FinalReleaseComObject(shell);
            }
        }
    }
}
