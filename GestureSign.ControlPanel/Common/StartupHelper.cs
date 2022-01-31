using GestureSign.Common.Configuration;
using GestureSign.Common.Localization;
using IWshRuntimeLibrary;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using File = System.IO.File;

namespace GestureSign.ControlPanel.Common
{
    static class StartupHelper
    {
        private static string DaemonPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GestureSign.Common.Constants.DaemonFileName);

        private static string StartupLnkPath => Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\" + GestureSign.Common.Constants.ProductName + ".lnk";

        public static bool IsRunAsAdmin => AppConfig.RunAsAdmin;

        /// <summary>
        /// https://gist.github.com/Winand/997ed38269e899eb561991a0c663fa49
        /// https://stackoverflow.com/questions/64126236/reading-the-target-of-a-lnk-file-in-c-sharp-net-core
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        private static string GetLnkTargetPath(string filepath)
        {
            using (var br = new BinaryReader(File.OpenRead(filepath)))
            {
                // skip the first 20 bytes (HeaderSize and LinkCLSID)
                br.ReadBytes(0x14);
                // read the LinkFlags structure (4 bytes)
                uint lflags = br.ReadUInt32();
                // if the HasLinkTargetIDList bit is set then skip the stored IDList 
                // structure and header
                if ((lflags & 0x01) == 1)
                {
                    br.ReadBytes(0x34);
                    var skip = br.ReadUInt16(); // this counts of how far we need to skip ahead
                    br.ReadBytes(skip);
                }
                // get the number of bytes the path contains
                var length = br.ReadUInt32();
                // skip 12 bytes (LinkInfoHeaderSize, LinkInfoFlgas, and VolumeIDOffset)
                br.ReadBytes(0x0C);
                // Find the location of the LocalBasePath position
                var lbpos = br.ReadUInt32();
                // Skip to the path position 
                // (subtract the length of the read (4 bytes), the length of the skip (12 bytes), and
                // the length of the lbpos read (4 bytes) from the lbpos)
                br.ReadBytes((int)lbpos - 0x14);
                var size = length - lbpos - 0x02;
                var bytePath = br.ReadBytes((int)size);
                int index = Array.IndexOf(bytePath, (byte)0x00);
                var path = index < 0 ? System.Text.Encoding.Default.GetString(bytePath, 0, bytePath.Length) :
                    System.Text.Encoding.Unicode.GetString(bytePath, index + 1, bytePath.Length - index - 1);
                return path;
            }
        }

        private static void CreateLnk(string lnkPath, string targetPath)
        {
            WshShell shell = new WshShell();
            IWshShortcut shortCut = (IWshShortcut)shell.CreateShortcut(lnkPath);
            shortCut.TargetPath = targetPath;
            //Application.ResourceAssembly.Location;// System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            shortCut.WindowStyle = 7;
            shortCut.Arguments = "";
            shortCut.Description = Application.ResourceAssembly.GetName().Version.ToString();
            // Application.ProductName + Application.ProductVersion;
            //shortCut.IconLocation = Application.ResourceAssembly.Location;// Application.ExecutablePath;
            //shortCut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;// Application.ResourceAssembly.;
            shortCut.Save();
        }

        private static bool AddStartupTask(string filePath)
        {
            try
            {
                string taskXml = Properties.Resources.StartGestureSignTask.Replace("GestureSignFilePath", filePath);
                string xmlFilePath = Path.Combine(AppConfig.LocalApplicationDataPath, "StartGestureSignTask.xml");
                File.WriteAllText(xmlFilePath, taskXml, System.Text.Encoding.Unicode);

                using (Process schtasks = new Process())
                {
                    string arguments = string.Format(" /create /tn StartGestureSign /f /xml \"{0}\"", xmlFilePath);
                    schtasks.StartInfo = new ProcessStartInfo("schtasks.exe", arguments)
                    {
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = true,
                        Verb = "runas",
                    };
                    schtasks.Start();
                    schtasks.WaitForExit();
                }
                if (File.Exists(xmlFilePath))
                    File.Delete(xmlFilePath);
            }
            catch (Exception exception)
            {
                GestureSign.Common.Log.Logging.LogAndNotice(exception);
                return false;
            }

            return true;
        }

        private static bool DelStartupTask()
        {
            try
            {
                using (Process schtasks = new Process())
                {
                    schtasks.StartInfo = new ProcessStartInfo("schtasks.exe", " /delete /tn StartGestureSign /f")
                    {
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = true,
                        Verb = "runas",
                    };
                    schtasks.Start();
                    schtasks.WaitForExit();
                }
            }
            catch (Exception exception)
            {
                GestureSign.Common.Log.Logging.LogAndNotice(exception);
                return false;
            }

            return true;
        }

        public static async Task<bool> CheckStoreAppStartupStatus()
        {
            var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("GestureSignTask");
            switch (startupTask.State)
            {
                case Windows.ApplicationModel.StartupTaskState.Disabled:
                    return false;
                case Windows.ApplicationModel.StartupTaskState.DisabledByUser:
                    return false;
                case Windows.ApplicationModel.StartupTaskState.Enabled:
                    return true;
                default:
                    return false;
            }
        }

        public static async Task<bool> EnableStoreAppStartup()
        {
            var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("GestureSignTask");
            if (startupTask.State != Windows.ApplicationModel.StartupTaskState.Enabled)
            {
                var state = await startupTask.RequestEnableAsync();
                if (state == Windows.ApplicationModel.StartupTaskState.DisabledByUser)
                {
                    MessageBox.Show(LocalizationProvider.Instance.GetTextValue("Options.Messages.TaskUserDisabled"), LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            return true;
        }

        public static async Task<bool> DisableStoreAppStartup()
        {
            var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("GestureSignTask");
            if (startupTask.State == Windows.ApplicationModel.StartupTaskState.Enabled)
            {
                startupTask.Disable();
            }
            return true;
        }

        public static bool GetStartupStatus()
        {
            try
            {
                string startupLnkPath = StartupLnkPath;
                if (File.Exists(startupLnkPath))
                {
                    var targetPath = GetLnkTargetPath(startupLnkPath);
                    var daemonPath = DaemonPath;
                    if (daemonPath != targetPath)
                    {
                        CreateLnk(startupLnkPath, daemonPath);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static bool EnableNormalStartup()
        {
            CreateLnk(StartupLnkPath, DaemonPath);
            return true;
        }

        public static bool DisableNormalStartup()
        {
            if (File.Exists(StartupLnkPath))
            {
                try
                {
                    File.Delete(StartupLnkPath);
                }
                catch (Exception exception)
                {
                    GestureSign.Common.Log.Logging.LogAndNotice(exception);
                    return false;
                }
            }
            return true;
        }

        public static bool EnableHighPrivilegeStartup()
        {
            return AddStartupTask(DaemonPath);
        }

        public static bool DisableHighPrivilegeStartup()
        {
            return DelStartupTask();
        }
    }
}
