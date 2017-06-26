using System.Diagnostics;
using GestureSign.Common.Plugins;
using Microsoft.Win32;
using GestureSign.Common.Localization;

namespace GestureSign.CorePlugins
{
    public class DefaultBrowser : IPlugin
    {
        #region Private Variables

        IHostControl _HostControl = null;

        #endregion

        #region IAction Properties

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.DefaultBrowser.Name"); }
        }

        public string Description
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.DefaultBrowser.Description"); }
        }

        public object GUI
        {
            get { return null; }
        }

        public bool ActivateWindowDefault
        {
            get { return false; }
        }

        public string Category
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.DefaultBrowser.Category"); }
        }

        public bool IsAction
        {
            get { return true; }
        }

        public object Icon => IconSource.Browser;

        #endregion

        #region IAction Methods

        public void Initialize()
        {

        }

        public bool Gestured(PointInfo ActionPoint)
        {
            // Extract default browser path from registery
            var defaultBrowserInfo = GetDefaultBrowserPath();

            // If path is incorrect or empty and exception will be thrown, catch it and return false
            try { Process.Start(defaultBrowserInfo); }
            catch { return false; }
            return true;
        }

        public bool Deserialize(string SerializedData)
        {
            return true;
            // Nothing to deserialize
        }

        public string Serialize()
        {
            // Nothing to serialize, send empty string
            return "";
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Reads path of default browser from registry
        /// source:
        /// http://www.seirer.net/blog/2014/6/10/solved-how-to-open-a-url-in-the-default-browser-in-csharp
        /// </summary>
        /// <returns>Rooted path to the browser</returns>
        private static ProcessStartInfo GetDefaultBrowserPath()
        {
            string urlAssociation = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http";
            string browserPathKey = @"$BROWSER$\shell\open\command";

            try
            {
                //Read default browser path from userChoiceLKey
                var userChoiceKey = Registry.CurrentUser.OpenSubKey(urlAssociation + @"\UserChoice", false);

                //If user choice was not found, try machine default
                if (userChoiceKey == null)
                {
                    //Read default browser path from Win XP registry key
                    var browserKey = Registry.ClassesRoot.OpenSubKey(@"HTTP\shell\open\command", false) ?? Registry.CurrentUser.OpenSubKey(urlAssociation, false);

                    //If browser path wasn’t found, try Win Vista (and newer) registry key
                    var path = CleanifyBrowserPath(browserKey.GetValue(null) as string);
                    browserKey.Close();
                    return new ProcessStartInfo(path);
                }
                else
                {
                    // user defined browser choice was found
                    string progId = (userChoiceKey.GetValue("ProgId").ToString());
                    userChoiceKey.Close();

                    if (progId.StartsWith("AppX"))
                    {
                        string appUserModelID;
                        using (var applicationInfo = Registry.ClassesRoot.OpenSubKey(progId + @"\Application"))
                        {
                            appUserModelID = applicationInfo.GetValue("AppUserModelID") as string;
                        }
                        return new ProcessStartInfo("explorer.exe", @"shell:AppsFolder\" + appUserModelID);
                    }
                    else
                    {
                        // now look up the path of the executable
                        string concreteBrowserKey = browserPathKey.Replace("$BROWSER$", progId);
                        var kp = Registry.ClassesRoot.OpenSubKey(concreteBrowserKey, false);
                        var browserPath = CleanifyBrowserPath(kp.GetValue(null) as string);
                        kp.Close();
                        return new ProcessStartInfo(browserPath);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private static string CleanifyBrowserPath(string p)
        {
            string[] url = p.Split('"');
            string clean = url[1];
            return clean;
        }
        #endregion

        #region Host Control

        public IHostControl HostControl
        {
            get { return _HostControl; }
            set { _HostControl = value; }
        }

        #endregion
    }
}