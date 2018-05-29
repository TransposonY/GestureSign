using GestureSign.Common;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace GestureSign.ControlPanel.Common
{
    class Archive
    {
        private static string GetTempDirectory()
        {
            string tempArchivePath = Path.Combine(AppConfig.LocalApplicationDataPath, "Archive");
            if (Directory.Exists(tempArchivePath))
                Directory.Delete(tempArchivePath, true);
            Directory.CreateDirectory(tempArchivePath);
            return tempArchivePath;
        }

        public static string ExtractToTempDirectory(string sourceArchiveFileName)
        {
            string tempArchivePath = GetTempDirectory();
            ZipFile.ExtractToDirectory(sourceArchiveFileName, tempArchivePath);
            return tempArchivePath;
        }

        public static void CreateArchive(IEnumerable<IApplication> applications, IEnumerable<IGesture> gestures, string destinationArchiveFileName, string configPath = null)
        {
            string tempArchivePath = GetTempDirectory();
            FileManager.SaveObject(applications, Path.Combine(tempArchivePath, Constants.ActionFileName), true, true);
            FileManager.SaveObject(gestures, Path.Combine(tempArchivePath, Constants.GesturesFileName), false, true);

            if (File.Exists(configPath))
                File.Copy(configPath, Path.Combine(tempArchivePath, Path.GetFileName(configPath)));

            if (File.Exists(destinationArchiveFileName))
                File.Delete(destinationArchiveFileName);
            ZipFile.CreateFromDirectory(tempArchivePath, destinationArchiveFileName);

            Directory.Delete(tempArchivePath, true);
        }

        public static void LoadFromArchive(string sourceArchiveFileName, out IEnumerable<IApplication> applications, out IEnumerable<IGesture> gestures)
        {
            string tempArchivePath = ExtractToTempDirectory(sourceArchiveFileName);

            applications = FileManager.LoadObject<List<IApplication>>(Path.Combine(tempArchivePath, Constants.ActionFileName), false, true, true);
            gestures = FileManager.LoadObject<List<Gesture>>(Path.Combine(tempArchivePath, Constants.GesturesFileName), false, false, true);

            Directory.Delete(tempArchivePath, true);
        }
    }
}
