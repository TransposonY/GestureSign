using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using GestureSign.Common;
using GestureSign.Common.Plugins;
using GestureSign.Common.Gestures;
using GestureSign.Common.Applications;

namespace GestureSign.Common.Plugins
{
    public class PluginManager : IPluginManager
    {
        #region Private Variables

        // Create variable to hold the only allowed instance of this class
        static readonly PluginManager _Instance = new PluginManager();
        List<IPluginInfo> _Plugins = new List<IPluginInfo>();

        #endregion

        #region Public Properties

        public IPluginInfo[] Plugins { get { return _Plugins.ToArray(); } }

        public static PluginManager Instance
        {
            get { return _Instance; }
        }

        #endregion

        #region Constructors

        protected PluginManager()
        {

        }

        #endregion

        #region Events

        protected void TouchCapture_GestureRecognized(object sender, Input.RecognitionEventArgs e)
        {
            // Exit if we're teaching
            if (e.Mode == Input.CaptureMode.Training)
                return;

            // Get action to be executed
            IEnumerable<IAction> executableActions = ApplicationManager.Instance.GetRecognizedDefinedAction(e.GestureName);
            foreach (IAction executableAction in executableActions)
            {
                // Exit if there is no action configured
                if (executableAction == null || !executableAction.IsEnabled ||
                    (e.Mode == Input.CaptureMode.UserDisabled &&
                    !"GestureSign.CorePlugins.ToggleDisableGestures".Equals(executableAction.PluginClass)))
                    continue;

                // Locate the plugin associated with this action
                IPluginInfo pluginInfo = FindPluginByClassAndFilename(executableAction.PluginClass, executableAction.PluginFilename);

                // Exit if there is no plugin available for action
                if (pluginInfo == null)
                    continue;

                // Load action settings into plugin
                pluginInfo.Plugin.Deserialize(executableAction.ActionSettings);
                // Execute plugin process
                pluginInfo.Plugin.Gestured(new PointInfo(e.LastCapturedPoints, e.Points));
            }
        }

        #endregion

        #region Public Methods

        public bool LoadPlugins(IHostControl host)
        {
            // Default return value to failure
            bool bFailed = true;

            // Clear any existing plugins
            _Plugins = new List<IPluginInfo>();
            //_Plugins.Clear();
            string directoryPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            if (directoryPath == null) return true;

            // Load core plugins.
            string corePluginsPath = Path.Combine(directoryPath, "GestureSign.CorePlugins.dll");
            if (File.Exists(corePluginsPath))
            {
                _Plugins.AddRange(LoadPluginsFromAssembly(corePluginsPath, host));
                bFailed = false;
            }

            // Load extra plugins.
            string extraPlugins = Path.Combine(directoryPath, "Plugins");
            if (Directory.Exists(extraPlugins))
                foreach (string sFilePath in Directory.GetFiles(extraPlugins, "*.dll"))
                {

                    _Plugins.AddRange(LoadPluginsFromAssembly(sFilePath, host));
                    bFailed = false;

                }


            return bFailed;
        }

        public IPluginInfo FindPluginByClassAndFilename(string PluginClass, string PluginFilename)
        {
            // Get reference to plugin using PluginClass and PluginFilename
            return _Plugins.FirstOrDefault(p => p.Class == PluginClass && p.Filename == PluginFilename);
        }

        public bool PluginExists(string PluginClass, string PluginFilename)
        {
            return _Plugins.Exists(p => p.Class == PluginClass && p.Filename == PluginFilename);
        }

        #endregion

        #region Private Methods

        private List<IPluginInfo> LoadPluginsFromAssembly(string assemblyLocation, IHostControl hostControl)
        {
            List<IPluginInfo> retPlugins = new List<IPluginInfo>();

            //To avoid exception System.NotSupportedException
            byte[] file = File.ReadAllBytes(assemblyLocation);
            Assembly aPlugin = Assembly.Load(file);

            Type[] tPluginTypes = aPlugin.GetTypes();

            foreach (Type tPluginType in tPluginTypes)
                if (tPluginType.GetInterface("IPlugin") != null)
                {
                    IPlugin plugin = Activator.CreateInstance(tPluginType) as IPlugin;

                    // If we have a new instance of a plugin, initialize it and add it to return list
                    if (plugin != null)
                    {
                        plugin.HostControl = hostControl;
                        plugin.Initialize();
                        retPlugins.Add(new PluginInfo(plugin, tPluginType.FullName, Path.GetFileName(assemblyLocation)));
                    }
                }

            return retPlugins;
        }

        #endregion

        #region ILoadable Methods

        public void Load(IHostControl host)
        {
            // Create empty list of plugins, then load as many as possible from plugin directory
            LoadPlugins(host);

            if (host == null) return;
            host.TouchCapture.GestureRecognized += TouchCapture_GestureRecognized;
        }

        #endregion
    }
}
