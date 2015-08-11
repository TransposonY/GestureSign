using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
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

        private bool IsWindows10()
        {
            try
            {
                var rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                return rk != null && rk.GetValue("ProductName").ToString().Contains("10");
            }
            catch
            {
                return false;
            }
        }

        private void comboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var apps = new List<Model>(10);

            var currentUser = WindowsIdentity.GetCurrent();
            var sid = currentUser.User.ToString();

            var packageManager = new PackageManager();
            var packages = packageManager.FindPackagesForUser(sid);


            var isWin10 = IsWindows10();
            if (isWin10)
            {
                var appXInfos = GetAppXInfosFromReg();
                if (appXInfos == null || appXInfos.Count == 0) return;

                foreach (var package in packages)
                {
                    if (package.IsFramework) continue;

                    using (
                        var key =
                            Registry.ClassesRoot.OpenSubKey(
                                $@"Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\{
                                    package.Id.FamilyName}\SplashScreen\"))
                    {
                        var appUserModelIds =
                            key?.GetSubKeyNames().Where(k => k.StartsWith(package.Id.FamilyName));
                        if (appUserModelIds == null) continue;

                        foreach (string appUserModelId in appUserModelIds)
                        {
                            Model model = new Model();
                            if (appXInfos.ContainsKey(appUserModelId))
                            {
                                var appXInfo = appXInfos[appUserModelId];

                                var displayName = ExtractDisplayName(package, appXInfo.ApplicationName);
                                var logo = ExtractDisplayIconFromReg(package.InstalledLocation.Path, appXInfo.ApplicationIcon);

                                if (string.IsNullOrEmpty(displayName)) continue;

                                model.Logo = logo;
                                model.AppInfo = new KeyValuePair<string, string>(appUserModelId, displayName);

                                string backgroundColor;
                                if (!ReadAppxInfoFromXml(package.InstalledLocation.Path, out displayName, out logo, out backgroundColor))
                                    continue;

                                var convertFromString = ColorConverter.ConvertFromString(backgroundColor);
                                if (convertFromString != null)
                                    model.BackgroundColor = new SolidColorBrush((Color)convertFromString);

                            }
                            else
                            {
                                using (var subKey = key.OpenSubKey(appUserModelId))
                                {
                                    if (subKey == null) continue;

                                    string appName = subKey.GetValue("AppName").ToString();
                                    var displayName = ExtractDisplayName(package, appName);
                                    if (string.IsNullOrEmpty(displayName)) continue;
                                    model.AppInfo = new KeyValuePair<string, string>(appUserModelId, displayName);

                                    string logo, backgroundColor;
                                    if (!ReadAppxInfoFromXml(package.InstalledLocation.Path, out displayName, out logo, out backgroundColor))
                                        continue;

                                    logo = ExtractDisplayIcon(package.InstalledLocation.Path, logo);
                                    model.Logo = logo;

                                    var convertFromString = ColorConverter.ConvertFromString(backgroundColor);
                                    if (convertFromString != null)
                                        model.BackgroundColor = new SolidColorBrush((Color)convertFromString);
                                }
                            }
                            apps.Add(model);
                        }
                    }
                }
            }
            else
            {
                foreach (var package in packages)
                {
                    if (package.IsFramework) continue;

                    string displayName, logo, backgroundColor;
                    if (
                        !ReadAppxInfoFromXml(package.InstalledLocation.Path, out displayName, out logo,
                            out backgroundColor))
                        continue;

                    displayName = ExtractDisplayName(package.InstalledLocation.Path, package.Id.Name, displayName);
                    logo = ExtractDisplayIcon(package.InstalledLocation.Path, logo);

                    var convertFromString = ColorConverter.ConvertFromString(backgroundColor);
                    if (convertFromString == null) continue;
                    var model = new Model
                    {
                        AppInfo = new KeyValuePair<string, string>(GetAppUserModelId(package.Id.FullName), displayName),
                        BackgroundColor = new SolidColorBrush((Color)convertFromString),
                        Logo = logo
                    };
                    apps.Add(model);
                }
            }

            comboBox.ItemsSource = apps.OrderBy(app => app.AppInfo.Value).ToList();

            var launchApp = DataContext as LaunchApp;
            if (launchApp != null)
            {
                comboBox.SelectedItem = apps.Find(m => m.AppInfo.Equals(launchApp.AppInfo));
            }
        }

        private string ExtractDisplayName(Package package, string displayName)
        {
            var priPath = Path.Combine(package.InstalledLocation.Path, "resources.pri");

            var manifestString = displayName.Replace(package.Id.FullName, priPath);
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
            var logoPath = Path.Combine(dir, logo.TrimStart('\\'));
            if (File.Exists(logoPath))
                return logoPath;

            logoPath = Path.Combine(dir, Path.ChangeExtension(logoPath, "scale-100.png"));
            if (File.Exists(logoPath))
                return logoPath;

            var localized = Path.Combine(dir, "en-us", logo); //TODO: How determine if culture parameter is necessary?
            localized = Path.Combine(dir, Path.ChangeExtension(localized, "scale-100.png"));
            return localized;
        }

        private string ExtractDisplayIconFromReg(string dir, string logo)
        {
            logo = logo.TrimEnd('}').Split('?')[1];

            Uri uri;
            if (!Uri.TryCreate(logo, UriKind.Absolute, out uri))
                return string.Empty;
            //remove "/Files"
            logo = uri.AbsolutePath.Substring(6).Replace('/', '\\');

            return ExtractDisplayIcon(dir, logo);
        }

        private bool ReadAppxInfoFromXml(string path, out string displayName, out string logo,
            out string backgroundColor)
        {
            var file = Path.Combine(path, "AppxManifest.xml");
            backgroundColor = displayName = logo = null;
            if (!File.Exists(file)) return false;
            using (var xtr = new XmlTextReader(file) { WhitespaceHandling = WhitespaceHandling.None })
            {
                while (xtr.Read())
                {
                    if (xtr.NodeType != XmlNodeType.Element) continue;

                    if ("DisplayName".Equals(xtr.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        xtr.Read();
                        displayName = xtr.Value;
                    }
                    else if ("Logo".Equals(xtr.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        xtr.Read();
                        logo = xtr.Value;
                    }
                    else if ("VisualElements".Equals(xtr.LocalName, StringComparison.OrdinalIgnoreCase))
                    {
                        backgroundColor = xtr.GetAttribute("BackgroundColor");
                        return true;
                    }
                }
            }
            return false;
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
    }
}