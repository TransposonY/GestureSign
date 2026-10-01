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

        public static void CreateArchive(IEnumerable<IApplication> applications, IEnumerable<IGesture> gestures, IEnumerable<ContinuousGesture> continuousGestures, string destinationArchiveFileName, string configPath = null)
        {
            string tempArchivePath = GetTempDirectory();
            FileManager.SaveObject(applications, Path.Combine(tempArchivePath, Constants.ActionFileName), true, true);
            FileManager.SaveObject(gestures, Path.Combine(tempArchivePath, Constants.GesturesFileName), false, true);
            FileManager.SaveObject(continuousGestures, Path.Combine(tempArchivePath, Constants.ContinuousGesturesFileName), false, true);

            if (File.Exists(configPath))
                File.Copy(configPath, Path.Combine(tempArchivePath, Path.GetFileName(configPath)));

            if (File.Exists(destinationArchiveFileName))
                File.Delete(destinationArchiveFileName);
            ZipFile.CreateFromDirectory(tempArchivePath, destinationArchiveFileName);

            Directory.Delete(tempArchivePath, true);
        }

        /// <param name="continuousGestures">Catalog entries of the archive; legacy archives are migrated, so the applications reference them by name.</param>
        public static void LoadFromArchive(string sourceArchiveFileName, out IEnumerable<IApplication> applications, out IEnumerable<IGesture> gestures, out List<ContinuousGesture> continuousGestures)
        {
            string tempArchivePath = ExtractToTempDirectory(sourceArchiveFileName);

            var applicationList = FileManager.LoadObject<List<IApplication>>(Path.Combine(tempArchivePath, Constants.ActionFileName), false, true, true);
            gestures = GestureManager.LoadGesturesFromFile(Path.Combine(tempArchivePath, Constants.GesturesFileName), true);
            continuousGestures = LoadContinuousGestures(tempArchivePath, applicationList);
            applications = applicationList;

            Directory.Delete(tempArchivePath, true);
        }

        /// <summary>
        /// Reads <see cref="Constants.ContinuousGesturesFileName"/> from an extracted archive (empty when absent) and moves
        /// continuous gestures still embedded in <paramref name="applications"/> (pre-catalog archives) into it.
        /// </summary>
        public static List<ContinuousGesture> LoadContinuousGestures(string extractedArchivePath, IEnumerable<IApplication> applications)
        {
            string path = Path.Combine(extractedArchivePath, Constants.ContinuousGesturesFileName);
            var continuousGestures = File.Exists(path) ? ContinuousGestureManager.LoadFromFile(path, true) ?? new List<ContinuousGesture>() : new List<ContinuousGesture>();
            if (applications != null)
                ContinuousGestureCatalog.MigrateLegacy(applications, continuousGestures);
            return continuousGestures;
        }
    }
}
