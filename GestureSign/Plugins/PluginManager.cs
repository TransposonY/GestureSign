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

namespace GestureSign.Plugins
{
    public class PluginManager : ILoadable, IPluginManager
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
            // Bind to mouse capture class to execute plugin
            Gestures.GestureManager.Instance.GestureRecognized += new RecognitionEventHandler(GestureManager_GestureRecognized);

            // Reload plugins if options were saved
        }

        #endregion

        #region Events

        protected void GestureManager_GestureRecognized(object sender, RecognitionEventArgs e)
        {
            // Exit if we're teaching
            if (GestureSign.Configuration.AppConfig.Teaching)
                return;

            // Get action to be executed
            IEnumerable<IAction> executableActions = Applications.ApplicationManager.Instance.GetRecognizedDefinedAction(e.GestureName);
            foreach (IAction executableAction in executableActions)
            {
                // Exit if there is no action configured
                if (executableAction == null || !executableAction.IsEnabled)
                    continue;

                // Locate the plugin associated with this action
                IPluginInfo pluginInfo = FindPluginByClassAndFilename(executableAction.PluginClass, executableAction.PluginFilename);

                // Exit if there is no plugin available for action
                if (pluginInfo == null)
                    continue;

                // Load action settings into plugin
                pluginInfo.Plugin.Deserialize(executableAction.ActionSettings);
                // Execute plugin process
                pluginInfo.Plugin.Gestured(new PointInfo(e.CapturePoints[0]));
            }
        }

        #endregion

        #region Public Methods

        public bool LoadPlugins()
        {
            // Default return value to failure
            bool bFailed = true;

            // Clear any existing plugins
            _Plugins = new List<IPluginInfo>();
            //_Plugins.Clear();
            foreach (string sFilePath in Directory.GetFiles(Directory.GetCurrentDirectory(), "*Plugins.dll"))
            {
                try
                {
                    _Plugins.AddRange(LoadPluginsFromAssembly(Path.GetFullPath(sFilePath)));
                    bFailed = false;
                }
                catch
                { }
            }


            return bFailed;
        }

        public IPluginInfo FindPluginByClassAndFilename(string PluginClass, string PluginFilename)
        {
            // Get reference to plugin using PluginClass and PluginFilename
            return _Plugins.Where(p => p.Class == PluginClass && p.Filename == PluginFilename).FirstOrDefault();
        }

        public bool PluginExists(string PluginClass, string PluginFilename)
        {
            return _Plugins.Exists(p => p.Class == PluginClass && p.Filename == PluginFilename);
        }

        #endregion

        #region Private Methods

        private List<IPluginInfo> LoadPluginsFromAssembly(string AssemblyLocation)
        {
            List<IPluginInfo> retPlugins = new List<IPluginInfo>();

            // Create host control class and pass to plugins
            HostControl hostControl = new HostControl()
            {
                _ApplicationManager = global::GestureSign.Applications.ApplicationManager.Instance,
                _FormManager = global::GestureSign.UI.FormManager.Instance,
                _GestureManager = global::GestureSign.Gestures.GestureManager.Instance,
                _TouchCapture = global::GestureSign.Input.TouchCapture.Instance,
                _PluginManager = global::GestureSign.Plugins.PluginManager.Instance,
                _TrayManager = global::GestureSign.UI.TrayManager.Instance
            };

            // Catch an unexpected errors during load
            try
            {
                Assembly aPlugin = Assembly.LoadFile(AssemblyLocation);

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
                            retPlugins.Add(new PluginInfo(plugin, tPluginType.FullName, Path.GetFileName(AssemblyLocation)));
                        }
                    }
            }
            catch { }

            return retPlugins;
        }

        #endregion

        #region ILoadable Methods

        public void Load()
        {
            // Create empty list of plugins, then load as many as possible from plugin directory
            LoadPlugins();
        }

        #endregion
    }
}
