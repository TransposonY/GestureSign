using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using GestureSign.Common.Localization;
using GestureSign.Common.Log;
using Microsoft.Win32;

namespace GestureSign.CorePlugins.LaunchApp
{
    /// <summary>
    ///     Interaction logic for LaunchAppView.xaml
    /// Modify base https://github.com/luisrigoni/metro-apps-list
    /// </summary>
    public partial class LaunchAppView : UserControl
    {
        public LaunchAppView()
        {
            InitializeComponent();
        }

        [DllImport("shlwapi.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true,
            SetLastError = false, ThrowOnUnmappableChar = true)]
        private static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, int cchOutBuf,
            IntPtr ppvReserved);

        public KeyValuePair<string, string> SelectedAppInfo
        {
            get
            {
                Model model = comboBox.SelectedItem as Model;
                return model?.AppInfo ?? new KeyValuePair<string, string>();
            }
        }

        private void comboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var apps = new List<Model>(10);

            var getAppsTask = Task.Run(() =>
            {
                if (comboBox.ItemsSource != null) return;

                var currentUser = WindowsIdentity.GetCurrent();
                if (currentUser?.User == null) return;
                var sid = currentUser.User.ToString();

                var packageManager = new PackageManager();
                var packages = packageManager.FindPackagesForUserWithPackageTypes(sid, PackageTypes.Main).ToList();

                if (Environment.OSVersion.Version.Major == 10)
                {
                    var appXInfos = GetAppXInfosFromReg();
                    if (appXInfos == null || appXInfos.Count == 0) return;

                    foreach (var package in packages)
                    {
                        try
                        {
                            using (var key = Registry.ClassesRoot.OpenSubKey($@"Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\{package.Id.FamilyName}\SplashScreen\"))
                            {
                                var appUserModelIds =
                                    key?.GetSubKeyNames().Where(k => k.StartsWith(package.Id.FamilyName));
                                List<AppInfo> infoList = ReadAppInfosFromManifest(package.InstalledLocation.Path);
                                if (appUserModelIds == null || infoList == null) continue;

                                foreach (string appUserModelId in appUserModelIds)
                                {
                                    Model model = new Model();
                                    AppInfo info = infoList.Find(i => appUserModelId.EndsWith(i.ID));
                                    string displayName = info.DisplayName;

                                    var color = ColorConverter.ConvertFromString(info.BackgroundColor);
                                    if (color != null)
                                    {
                                        model.BackgroundColor = (Color)color == Colors.Transparent ? SystemParameters.WindowGlassBrush : new SolidColorBrush((Color)color);
                                        model.BackgroundColor.Freeze();
                                    }

                                    if (appXInfos.ContainsKey(appUserModelId))
                                    {
                                        var appXInfo = appXInfos[appUserModelId];

                                        displayName = ExtractDisplayName(package, appXInfo.ApplicationName);
                                        if (string.IsNullOrEmpty(displayName)) continue;
                                        if (infoList.Count > 1)
                                            displayName = displayName + "\n(" + info.ID + ")";
                                        var logoPath = GetDisplayIconPath(package.InstalledLocation.Path, appXInfo.ApplicationIcon);
                                        if (string.IsNullOrEmpty(logoPath))
                                        {
                                            logoPath = ExtractDisplayIcon(package.InstalledLocation.Path, info.Logo);
                                        }
                                        model.Logo = logoPath;
                                        model.AppInfo = new KeyValuePair<string, string>(appUserModelId, displayName);
                                    }
                                    else
                                    {
                                        using (var subKey = key.OpenSubKey(appUserModelId))
                                        {
                                            var appName = subKey?.GetValue("AppName");
                                            if (appName != null)
                                                displayName = ExtractDisplayName(package, appName.ToString());
                                            if (string.IsNullOrEmpty(displayName) || displayName.Contains("ms-resource:")) continue;
                                            if (infoList.Count > 1)
                                                displayName = displayName + "\n(" + info.ID + ")";
                                            model.AppInfo = new KeyValuePair<string, string>(appUserModelId, displayName);

                                            model.Logo = ExtractDisplayIcon(package.InstalledLocation.Path, info.Logo);
                                        }
                                    }
                                    apps.Add(model);
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            Logging.LogMessage(package.Id.FullName);
                            Logging.LogException(exception);
                            continue;
                        }
                    }
                }
                else
                {
                    foreach (var package in packages)
                    {
                        try
                        {
                            List<AppInfo> infoList = ReadAppInfosFromManifest(package.InstalledLocation.Path);
                            if (infoList == null)
                                continue;
                            foreach (var info in infoList)
                            {
                                string displayName = ExtractDisplayName(package.InstalledLocation.Path, package.Id.Name, info.DisplayName);
                                string logo = ExtractDisplayIcon(package.InstalledLocation.Path, info.Logo);

                                var color = ColorConverter.ConvertFromString(info.BackgroundColor);
                                if (color == null) continue;
                                var model = new Model
                                {
                                    AppInfo = new KeyValuePair<string, string>(GetAppUserModelId(package.Id.FullName), displayName),
                                    BackgroundColor = (Color)color == Colors.Transparent ? SystemParameters.WindowGlassBrush : new SolidColorBrush((Color)color),
                                    Logo = logo
                                };
                                model.BackgroundColor.Freeze();
                                apps.Add(model);
                            }
                        }
                        catch (Exception exception)
                        {
                            Logging.LogMessage(package.Id.FullName);
                            Logging.LogException(exception);
                            continue;
                        }
                    }
                }
            });
            getAppsTask.ContinueWith(task =>
            {
                string message = null;
                if (task.Exception != null)
                {
                    foreach (var item in task.Exception.InnerExceptions)
                    {
                        Logging.LogException(item);
                        message = item.Message;
                    }
                }

                comboBox.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (message != null)
                        {
                            TipTextBlock.Text = message;
                            return;
                        }
                        if (apps.Count != 0)
                            comboBox.ItemsSource = apps.OrderBy(app => app.AppInfo.Value).ToList();

                        var launchApp = DataContext as LaunchApp;
                        if (launchApp != null)
                        {
                            comboBox.SelectedItem = (comboBox.ItemsSource as List<Model>)?.Find(m => m.AppInfo.Equals(launchApp.AppInfo));
                        }
                        TipTextBlock.Text = LocalizationProvider.Instance.GetTextValue("CorePlugins.LaunchApp.Tip");
                        comboBox.Visibility = Visibility.Visible;
                    }));
            });

        }

        private string ExtractDisplayName(Package package, string displayName)
        {
            var priPath = Path.Combine(package.InstalledLocation.Path, "resources.pri");

            var manifestString = displayName?.Replace(package.Id.FullName, priPath);
            var outBuff = new StringBuilder(128);
            SHLoadIndirectString(manifestString, outBuff, outBuff.Capacity, IntPtr.Zero);
            return outBuff.ToString();
        }

        private string ExtractDisplayName(string path, string packageName, string displayName)
        {
            var priPath = Path.Combine(path, "resources.pri");
            Uri uri;
            if (!Uri.TryCreate(displayName, UriKind.Absolute, out uri))
                return displayName;

            var resource = $"ms-resource://{packageName}/resources/{uri.Segments.Last()}";
            var name = ExtractStringFromPriFile(priPath, resource);
            if (!string.IsNullOrWhiteSpace(name))
                return name;

            var res = string.Concat(uri.Segments.Skip(1));
            resource = $"ms-resource://{packageName}/{res}";
            return ExtractStringFromPriFile(priPath, resource);
        }

        private string ExtractStringFromPriFile(string pathToPri, string resourceKey)
        {
            string sWin8ManifestString = $"@{{{pathToPri}? {resourceKey}}}";
            var outBuff = new StringBuilder(128);
            SHLoadIndirectString(sWin8ManifestString, outBuff, outBuff.Capacity, IntPtr.Zero);
            return outBuff.ToString();
        }

        private string ExtractDisplayIcon(string dir, string logo)
        {
            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(logo))
                return null;
            var logoPath = Path.Combine(dir, logo.TrimStart('\\'));
            if (File.Exists(logoPath))
                return logoPath;

            var scale100LogoPath = Path.Combine(dir, Path.ChangeExtension(logoPath, "scale-100.png"));
            if (File.Exists(scale100LogoPath))
                return scale100LogoPath;

            var directory = Path.GetDirectoryName(logoPath);
            var pattern = Path.GetFileNameWithoutExtension(logoPath) + ".*" + Path.GetExtension(logoPath);
            if (Directory.Exists(directory))
            {
                try
                {
                    var logoPaths = Directory.GetFiles(directory, pattern);
                    if (logoPaths.Length != 0)
                    {
                        return logoPaths[0];
                    }
                }
                catch { }
            }

            var localized = Path.Combine(dir, "en-us", logo);
            if (localized == null) return null;
            localized = Path.Combine(dir, Path.ChangeExtension(localized, "scale-100.png"));
            if (File.Exists(localized))
                return localized;

            return null;
        }

        private string GetDisplayIconPath(string dir, string logo)
        {
            var ss = logo.TrimEnd('}').Split('?');
            if (ss.Length < 2) return null;
            logo = ss[1];

            Uri uri;
            if (!Uri.TryCreate(logo, UriKind.Absolute, out uri))
                return string.Empty;
            //remove "/Files"
            logo = (uri.AbsolutePath.StartsWith("/Files", StringComparison.OrdinalIgnoreCase) ? uri.AbsolutePath.Substring(6) : uri.AbsolutePath).Replace('/', '\\');

            return ExtractDisplayIcon(dir, logo);
        }

        private List<AppInfo> ReadAppInfosFromManifest(string path)
        {
            var file = Path.Combine(path, "AppxManifest.xml");
            if (!File.Exists(file)) return null;
            List<AppInfo> list = new List<AppInfo>();
            using (var xtr = new XmlTextReader(file) { WhitespaceHandling = WhitespaceHandling.None })
            {
                if (xtr.ReadToFollowing("Applications"))
                    if (xtr.ReadToDescendant("Application"))
                    {
                        int depth = xtr.Depth;
                        do
                        {
                            if (xtr.NodeType != XmlNodeType.Element || xtr.Depth > depth) continue;
                            var id = xtr.GetAttribute("Id");
                            if (string.IsNullOrWhiteSpace(id)) continue;

                            AppInfo info = new AppInfo
                            {
                                ID = id
                            };
                            while (xtr.Read())
                            {
                                if (xtr.NodeType != XmlNodeType.Element) continue;
                                if ("VisualElements".Equals(xtr.LocalName, StringComparison.OrdinalIgnoreCase))
                                {
                                    info.BackgroundColor = xtr.GetAttribute("BackgroundColor");
                                    info.DisplayName = xtr.GetAttribute("DisplayName");
                                    info.Logo = xtr.GetAttribute("Square44x44Logo");

                                    list.Add(info);
                                    break;
                                }
                            }
                        }
                        while (xtr.Read() && xtr.Depth >= depth);

                        if (list.Count > 0)
                            return list;
                    }
            }
            return null;
        }

        /// <summary>
        ///     Windows 10 only
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, AppXInfo> GetAppXInfosFromReg()
        {
            var dic = new Dictionary<string, AppXInfo>(10);

            using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\"))
            {
                if (key == null) return null;
                var appKeys = from k in key.GetSubKeyNames()
                              where k.StartsWith("AppX")
                              select k;
                foreach (var appKey in appKeys)
                {
                    using (var appRegKey = key.OpenSubKey(appKey))
                    {
                        if (appRegKey == null) continue;
                        using (var applicationKey = appRegKey.OpenSubKey("Application"))
                        {
                            if (applicationKey == null) continue;
                            var appUserModelId = applicationKey.GetValue("AppUserModelId").ToString();
                            var applicationIcon = applicationKey.GetValue("ApplicationIcon").ToString();
                            var applicationName = applicationKey.GetValue("ApplicationName").ToString();

                            if (!dic.ContainsKey(appUserModelId))
                                dic.Add(appUserModelId, new AppXInfo
                                {
                                    ApplicationIcon = applicationIcon,
                                    ApplicationName = applicationName
                                });
                            applicationKey.Close();
                        }
                    }
                }
            }
            return dic;
        }

        private string GetAppUserModelId(string packageFullName)
        {
            var str = string.Empty;
            using (var key = Registry.CurrentUser.CreateSubKey(
                $@"SOFTWARE\Classes\ActivatableClasses\Package\{packageFullName}\Server\"))
            {
                if (key == null) return str;

                var appKeys = from k in key.GetSubKeyNames()
                              where k.StartsWith("Appex")
                              select k;

                foreach (var appKey in appKeys)
                {
                    using (var serverKey = key.OpenSubKey(appKey))
                    {
                        if (serverKey?.GetValue("AppUserModelId") == null) continue;
                        str = serverKey.GetValue("AppUserModelId").ToString();
                        serverKey.Close();
                        break;
                    }
                }
            }

            return str;
        }

        private class AppXInfo
        {
            public string ApplicationIcon { get; set; }
            public string ApplicationName { get; set; }
        }

        private class Model
        {
            public string Logo { get; set; }
            public KeyValuePair<string, string> AppInfo { get; set; }
            public Brush BackgroundColor { get; set; }
        }

        private struct AppInfo
        {
            public string ID { get; set; }
            public string BackgroundColor { get; set; }
            public string DisplayName { get; set; }
            public string Logo { get; set; }
        }
    }
}