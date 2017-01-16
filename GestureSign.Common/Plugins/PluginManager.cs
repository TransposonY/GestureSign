using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using GestureSign.Common.Applications;
using GestureSign.Common.Input;

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

        protected void PointCapture_GestureRecognized(object sender, RecognitionEventArgs e)
        {
            var pointCapture = (IPointCapture)sender;
            ExecuteAction(pointCapture.Mode, e.GestureName, e.ContactIdentifiers, e.FirstCapturedPoints, e.Points);
        }

        #endregion

        #region Public Methods

        public void ExecuteAction(CaptureMode mode, string gestureName, List<int> contactIdentifiers, List<Point> firstCapturedPoints, List<List<Point>> points)
        {
            // Exit if we're teaching
            if (mode == CaptureMode.Training)
                return;
            // Get action to be executed
            IEnumerable<IAction> executableActions = ApplicationManager.Instance.GetRecognizedDefinedAction(gestureName);
            foreach (IAction executableAction in executableActions)
            {
                // Exit if there is no action configured
                if (executableAction == null || !executableAction.IsEnabled ||
                    (mode == CaptureMode.UserDisabled &&
                    !"GestureSign.CorePlugins.ToggleDisableGestures".Equals(executableAction.PluginClass)) ||
                   !Compute(executableAction.Condition, points, contactIdentifiers))
                    continue;

                // Locate the plugin associated with this action
                IPluginInfo pluginInfo = FindPluginByClassAndFilename(executableAction.PluginClass, executableAction.PluginFilename);

                // Exit if there is no plugin available for action
                if (pluginInfo == null)
                    continue;

                // Load action settings into plugin
                pluginInfo.Plugin.Deserialize(executableAction.ActionSettings);
                // Execute plugin process
                pluginInfo.Plugin.Gestured(new PointInfo(firstCapturedPoints, points));
            }

        }

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

        private bool Compute(string condition, List<List<Point>> pointList, List<int> contactIdentifiers)
        {
            if (string.IsNullOrWhiteSpace(condition)) return true;

            string expression = GetExpression(condition, pointList, contactIdentifiers);

            DataTable dataTable = new DataTable();
            var result = dataTable.Compute(expression, null);

            return result is DBNull || Convert.ToBoolean(result);
        }

        private string GetExpression(string condition, List<List<Point>> pointList, List<int> contactIdentifiers)
        {
            StringBuilder sb = new StringBuilder(condition);

            for (int i = 1; i <= pointList.Count; i++)
            {
                var format = "finger_{0}_start_X";
                string variable = string.Format(format, i);
                sb.Replace(variable, pointList[i - 1].FirstOrDefault().X.ToString());

                format = "finger_{0}_start_Y";
                variable = string.Format(format, i);
                sb.Replace(variable, pointList[i - 1].FirstOrDefault().Y.ToString());

                format = "finger_{0}_end_X";
                variable = string.Format(format, i);
                sb.Replace(variable, pointList[i - 1].LastOrDefault().X.ToString());

                format = "finger_{0}_end_Y";
                variable = string.Format(format, i);
                sb.Replace(variable, pointList[i - 1].LastOrDefault().Y.ToString());

                format = "finger_{0}_ID";
                variable = string.Format(format, i);
                sb.Replace(variable, contactIdentifiers[i - 1].ToString());
            }
            return sb.ToString();
        }

        #endregion

        #region ILoadable Methods

        public void Load(IHostControl host)
        {
            // Create empty list of plugins, then load as many as possible from plugin directory
            LoadPlugins(host);

            if (host == null) return;
            host.PointCapture.GestureRecognized += PointCapture_GestureRecognized;
        }

        #endregion
    }
}
