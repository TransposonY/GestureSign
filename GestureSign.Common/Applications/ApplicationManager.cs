using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.Input;
using GestureSign.Common.InterProcessCommunication;
using ManagedWinapi.Windows;

namespace GestureSign.Common.Applications
{
    public class ApplicationManager : IApplicationManager
    {
        #region Private Variables

        // Create variable to hold the only allowed instance of this class
        private static ApplicationManager _instance;
        private List<IApplication> _Applications;
        IApplication _CurrentApplication = null;
        IEnumerable<IApplication> RecognizedApplication;
        private Timer timer;
        public static event EventHandler OnLoadApplicationsCompleted;
        #endregion

        #region Public Instance Properties

        public SystemWindow CaptureWindow { get; private set; }
        public IApplication CurrentApplication
        {
            get { return _CurrentApplication; }
            set
            {
                _CurrentApplication = value;
            }
        }

        public List<IApplication> Applications
        {
            get
            {
                return _Applications ?? new List<IApplication>();
            }
        }

        public static ApplicationManager Instance
        {
            get { return _instance ?? (_instance = new ApplicationManager()); }
        }

        public static bool FinishedLoading { get; set; }

        #endregion

        #region Constructors

        protected ApplicationManager()
        {
            Action<bool> loadCompleted =
                result =>
                {
                    if (!result)
                        if (!LoadBackup())
                            if (!LoadLegacy())
                                if (!LoadDefaults())
                                    _Applications = new List<IApplication>();
                    if (OnLoadApplicationsCompleted != null) OnLoadApplicationsCompleted(this, EventArgs.Empty);
                    FinishedLoading = true;
                };
            // Load applications from disk, if file couldn't be loaded, create an empty applications list
            LoadApplications().ContinueWith(antecendent => loadCompleted(antecendent.Result));
        }



        #endregion

        #region Events

        protected void PointCapture_CaptureStarted(object sender, PointsCapturedEventArgs e)
        {
            var pointCapture = (IPointCapture)sender;
            if (pointCapture.Mode == CaptureMode.Training) return;

            if (Environment.OSVersion.Version.Major == 6)
            {
                IntPtr hwndCharmBar = FindWindow("NativeHWNDHost", "Charm Bar");
                var window = SystemWindow.FromPointEx(SystemWindow.DesktopWindow.Rectangle.Right - 1, 1, true, true);

                if (window != null && window.HWnd.Equals(hwndCharmBar))
                {
                    e.Cancel = false;
                    e.BlockTouchInputThreshold = 0;
                    return;
                }
            }

            CaptureWindow = GetWindowFromPoint(e.FirstCapturedPoints.FirstOrDefault());
            RecognizedApplication = GetApplicationFromWindow(CaptureWindow);

            int maxThreshold = 0;
            bool? limitNumberFlag = null;

            foreach (IApplication app in RecognizedApplication)
            {
                var userApplication = app as UserApp;
                if (userApplication != null)
                {
                    maxThreshold = userApplication.BlockTouchInputThreshold > maxThreshold ? userApplication.BlockTouchInputThreshold : maxThreshold;

                    //Got UserApplication
                    if (limitNumberFlag == null)
                        limitNumberFlag = e.Points.Count < userApplication.LimitNumberOfFingers;
                    else
                        limitNumberFlag |= e.Points.Count < userApplication.LimitNumberOfFingers;
                }
                else
                {
                    var ignoredApplication = app as IgnoredApp;
                    if (ignoredApplication != null && ignoredApplication.IsEnabled)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }
            e.Cancel = pointCapture.SourceDevice == Device.Touch && (limitNumberFlag ?? e.Points.Count == 1);
            e.BlockTouchInputThreshold = maxThreshold;
        }

        protected void PointCapture_BeforePointsCaptured(object sender, PointsCapturedEventArgs e)
        {
            // Derive capture window from capture point
            CaptureWindow = GetWindowFromPoint(e.FirstCapturedPoints.FirstOrDefault());
            RecognizedApplication = GetApplicationFromWindow(CaptureWindow);
        }

        #endregion

        #region Custom Events

        public static event ApplicationChangedEventHandler ApplicationChanged;

        #endregion

        #region Public Methods

        public void Load(IPointCapture pointCapture)
        {
            // Shortcut method to control singleton instantiation
            // Consume Point Capture events
            if (pointCapture != null)
            {
                pointCapture.CaptureStarted += new PointsCapturedEventHandler(PointCapture_CaptureStarted);
                pointCapture.BeforePointsCaptured += new PointsCapturedEventHandler(PointCapture_BeforePointsCaptured);
            }
        }

        public void AddApplication(IApplication application)
        {
            _Applications.Add(application);
            ApplicationChanged?.Invoke(this, new ApplicationChangedEventArgs(application));
        }

        public void AddApplicationRange(List<IApplication> applications)
        {
            _Applications.AddRange(applications);
            ApplicationChanged?.Invoke(this, new ApplicationChangedEventArgs(applications.FirstOrDefault()));
        }

        public void RemoveApplication(IApplication application)
        {
            _Applications.Remove(application);
            ApplicationChanged?.Invoke(this, new ApplicationChangedEventArgs(application));
        }

        public void ReplaceApplication(IApplication oldApplication, IApplication newApplication)
        {
            _Applications.Remove(oldApplication);
            _Applications.Add(newApplication);
            ApplicationChanged?.Invoke(this, new ApplicationChangedEventArgs(newApplication));
        }

        public void RemoveIgnoredApplications(string applicationName)
        {
            _Applications.RemoveAll(app => app is IgnoredApp && app.Name == applicationName);
        }

        public void RenameGesture(string newName, string oldName)
        {
            _Applications.ForEach(app => app.Actions?.ForEach(action =>
              {
                  if (action.GestureName == oldName)
                      action.GestureName = newName;
              }));

            SaveApplications();
        }

        public void TrimGesture()
        {
            _Applications.ForEach(app => app?.Actions?.ForEach(a =>
            {
                if (!GestureManager.Instance.GestureExists(a.GestureName))
                    a.GestureName = string.Empty;
            }));
            SaveApplications();
        }

        public bool SaveApplications()
        {
            if (timer == null)
            {
                timer = new Timer(new TimerCallback(SaveFile), true, 200, Timeout.Infinite);
            }
            else timer.Change(200, Timeout.Infinite);
            return true;
        }

        private void SaveFile(object state)
        {
            bool notice = (bool)state;
            // Save application list
            bool flag = FileManager.SaveObject(
                 _Applications, Path.Combine(AppConfig.ApplicationDataPath, "Actions.gsa"), true);
            if (flag && notice) { NamedPipe.SendMessageAsync("LoadApplications", "GestureSignDaemon"); }

        }

        public Task<bool> LoadApplications()
        {
            return Task.Run(() =>
            {
                // Load application list from file
                _Applications =
                    FileManager.LoadObject<List<IApplication>>(
                        Path.Combine(AppConfig.ApplicationDataPath, "Actions.gsa"), true, true);
                return _Applications != null;
            });
        }

        private bool LoadDefaults()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Defaults\Actions.gsa");

            _Applications = FileManager.LoadObject<List<IApplication>>(path, true, true);
            // Ensure we got an object back
            if (_Applications == null)
                return false; // No object, failed

            return true; // Success
        }

        private bool LoadBackup()
        {
            var actionfiles = Directory.GetFiles(AppConfig.ApplicationDataPath, "Actions*.gsa");
            foreach (var file in actionfiles)
            {
                _Applications = FileManager.LoadObject<List<IApplication>>(file, true, true);
                if (_Applications != null) return true;
            }
            return false;
        }

        public SystemWindow GetWindowFromPoint(Point point)
        {
            return SystemWindow.FromPointEx(point.X, point.Y, true, true);
        }

        public IApplication[] GetApplicationFromWindow(SystemWindow Window, bool userApplicationOnly = false)
        {
            if (Applications == null)
            {
                return new[] { GetGlobalApplication() };
            }
            IApplication[] definedApplications = userApplicationOnly
                ? FindMatchApplications(Applications.Where(a => a is UserApp), Window)
                : FindMatchApplications(Applications.Where(a => !(a is GlobalApp)), Window);
            // Try to find any user or ignored applications that match the given system window
            // If not user or ignored application could be found, return the global application
            return definedApplications.Length != 0
                ? definedApplications
                : userApplicationOnly ? null : new IApplication[] { GetGlobalApplication() };
        }

        public IEnumerable<IApplication> GetApplicationFromPoint(Point testPoint)
        {
            var systemWindow = GetWindowFromPoint(testPoint);
            return GetApplicationFromWindow(systemWindow);
        }

        public IEnumerable<IAction> GetRecognizedDefinedAction(string GestureName)
        {
            return GetDefinedAction(GestureName, RecognizedApplication, true);
        }

        public IAction GetAnyDefinedAction(string actionName, string applicationName)
        {
            IApplication app = GetGlobalApplication().Name == applicationName ? GetGlobalApplication() : GetExistingUserApplication(applicationName);
            if (app != null && app.Actions.Exists(a => a.Name == actionName))
                return app.Actions.Find(a => a.Name == actionName);

            return null;
        }

        public IEnumerable<IAction> GetDefinedAction(string gestureName, IEnumerable<IApplication> application, bool useGlobal)
        {
            if (application == null)
            {
                return null;
            }
            // Attempt to retrieve an action on the application passed in
            IEnumerable<IAction> finalAction =
                application.Where(app => !(app is IgnoredApp)).SelectMany(app => app.Actions.Where(a => a.GestureName == gestureName));
            // If there is was no action found on given application, try to get an action for global application
            if (!finalAction.Any() && useGlobal)
                finalAction = GetGlobalApplication().Actions.Where(a => a.GestureName == gestureName);

            // Return whatever the result was
            return finalAction;
        }

        public IApplication GetExistingUserApplication(string ApplicationName)
        {
            return Applications.FirstOrDefault(a => a is UserApp && a.Name == ApplicationName.Trim());
        }

        public bool IsGlobalAction(string ActionName)
        {
            return _Applications.Exists(a => a is GlobalApp && a.Actions.Any(ac => ac.Name == ActionName.Trim()));
        }

        public bool ApplicationExists(string ApplicationName)
        {
            return _Applications.Exists(a => a.Name == ApplicationName.Trim());
        }

        public IApplication[] GetAvailableUserApplications()
        {
            return Applications.Where(a => a is UserApp).OrderBy(a => a.Name).ToArray();
        }

        public IEnumerable<IgnoredApp> GetIgnoredApplications()
        {
            return Applications.Where(a => a is IgnoredApp).OrderBy(a => a.Name).Cast<IgnoredApp>();
        }

        public IApplication GetGlobalApplication()
        {
            if (_Applications == null)
            {
                _Applications = new List<IApplication> { new GlobalApp { Group = String.Empty } };
            }
            else if (!_Applications.Exists(a => a is GlobalApp))
                _Applications.Add(new GlobalApp() { Group = String.Empty });

            return _Applications.FirstOrDefault(a => a is GlobalApp);
        }

        public IEnumerable<IApplication> GetAllGlobalApplication()
        {
            if (!_Applications.Exists(a => a is GlobalApp))
                _Applications.Add(new GlobalApp() { Group = String.Empty });
            return _Applications.Where(a => a is GlobalApp);
        }
        public void RemoveGlobalAction(string ActionName)
        {
            RemoveAction(ActionName, true);
        }

        public void RemoveNonGlobalAction(string ActionName)
        {
            RemoveAction(ActionName, false);
        }

        public IApplication[] FindMatchApplications<TApplication>(MatchUsing matchUsing, string matchString, string excludedApplication = null) where TApplication : IApplication
        {
            return _Applications.FindAll(
                    a => a is TApplication &&
                        matchString.Equals(a.MatchString, StringComparison.CurrentCultureIgnoreCase) &&
                        matchUsing == a.MatchUsing &&
                        excludedApplication != a.Name).ToArray();
        }

        public SystemWindow GetForegroundApplications()
        {
            CaptureWindow = SystemWindow.ForegroundWindow;
            RecognizedApplication = GetApplicationFromWindow(CaptureWindow);
            return CaptureWindow;
        }

        public static string GetNextCommandName(string name, IAction action, int number = 1)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var newName = number == 1 ? name : $"{name}({number})";
            if (action.Commands.Exists(a => a.Name == newName))
                return GetNextCommandName(name, action, ++number);
            return newName;
        }

        #endregion

        #region Private Methods

        private void RemoveAction(string ActionName, bool Global)
        {
            if (Global)
                // Attempt to remove action from global actions
                GetGlobalApplication().RemoveAllActions(a => a.Name.Trim() == ActionName.Trim());
            else
                // Select applications where this action may exist and delete them
                foreach (IApplication app in GetAvailableUserApplications().Where(a => a.Actions.Any(ac => ac.Name == ActionName)))
                    app.RemoveAllActions(a => a.Name.Trim() == ActionName.Trim());
        }

        private IApplication[] FindMatchApplications(IEnumerable<IApplication> applications, SystemWindow window)
        {
            var byFileName = new List<IApplication>();
            var byTitle = new List<IApplication>();
            var byClass = new List<IApplication>();
            foreach (var app in applications)
            {
                switch (app.MatchUsing)
                {
                    case MatchUsing.WindowClass:
                        byClass.Add(app);
                        break;
                    case MatchUsing.WindowTitle:
                        byTitle.Add(app);
                        break;
                    case MatchUsing.ExecutableFilename:
                        byFileName.Add(app);
                        break;
                    case MatchUsing.All:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            List<IApplication> result = new List<IApplication>();
            string windowMatchString;
            if (byClass.Count != 0)
            {
                try
                {
                    windowMatchString = window.ClassName;
                    result.AddRange(byClass.Where(a => a.MatchString != null && CompareString(a.MatchString, windowMatchString, a.IsRegEx)));
                }
                catch
                {
                    // ignored
                }
            }
            if (byTitle.Count != 0)
            {
                try
                {
                    windowMatchString = window.Title;
                    result.AddRange(byTitle.Where(a => a.MatchString != null && CompareString(a.MatchString, windowMatchString, a.IsRegEx)));
                }
                catch
                {
                    // ignored
                }
            }
            if (byFileName.Count != 0)
            {
                try
                {
                    windowMatchString = ((Func<string>)delegate
                    {
                        if (Environment.OSVersion.Version.Major >= 10 && "ApplicationFrameWindow".Equals(window.ClassName))
                        {
                            var realWindow = window.AllChildWindows.FirstOrDefault(w => "Windows.UI.Core.CoreWindow".Equals(w.ClassName));
                            if (realWindow != null)
                                return realWindow.Process.MainModule.ModuleName;
                        }
                        return window.Process.MainModule.ModuleName;
                    }).Invoke();

                    result.AddRange(byFileName.Where(a => a.MatchString != null && CompareString(a.MatchString, windowMatchString, a.IsRegEx)));
                }
                catch
                {
                    // ignored
                }
            }
            return result.ToArray();
        }

        private static bool CompareString(string compareMatchString, string windowMatchString, bool useRegEx)
        {
            if (string.IsNullOrEmpty(windowMatchString)) return false;
            return useRegEx
                ? Regex.IsMatch(windowMatchString, compareMatchString, RegexOptions.Singleline | RegexOptions.IgnoreCase)
                : string.Equals(windowMatchString.Trim(), compareMatchString.Trim(), StringComparison.CurrentCultureIgnoreCase);
        }

        private bool LoadLegacy()
        {
            var legacyApps = FileManager.LoadObject<List<LegacyApplicationBase>>(Path.Combine(AppConfig.ApplicationDataPath, "Actions.act"), true, true);
            if (legacyApps == null) return false;
            _Applications = new List<IApplication>();
            foreach (var app in legacyApps)
            {
                var legacyUserApp = app as UserApplication;
                if (legacyUserApp != null)
                {
                    var newApp = new UserApp()
                    {
                        Actions = ConvertLegacyActions(legacyUserApp.Actions),
                        BlockTouchInputThreshold = legacyUserApp.BlockTouchInputThreshold,
                        LimitNumberOfFingers = legacyUserApp.LimitNumberOfFingers,
                        Group = legacyUserApp.Group,
                        IsRegEx = legacyUserApp.IsRegEx,
                        MatchString = legacyUserApp.MatchString,
                        MatchUsing = legacyUserApp.MatchUsing,
                        Name = legacyUserApp.Name
                    };
                    _Applications.Add(newApp);
                    continue;
                }

                var legacyIgnoredApp = app as IgnoredApplication;
                if (legacyIgnoredApp != null)
                {
                    var newApp = new IgnoredApp(legacyIgnoredApp.Name, legacyIgnoredApp.MatchUsing, legacyIgnoredApp.MatchString, legacyIgnoredApp.IsRegEx, legacyIgnoredApp.IsEnabled);
                    _Applications.Add(newApp);
                    continue;
                }

                var legacyGlobalApp = app as GlobalApplication;
                if (legacyGlobalApp != null)
                {
                    IApplication newApp = new GlobalApp()
                    {
                        Actions = ConvertLegacyActions(legacyGlobalApp.Actions)
                    };
                    _Applications.Add(newApp);
                    continue;
                }
            }

            return true;
        }

        private List<IAction> ConvertLegacyActions(List<GestureSign.Applications.Action> legacyActions)
        {
            if (legacyActions == null) return null;
            List<IAction> newActions = new List<IAction>();
            foreach (var grouping in legacyActions.GroupBy(a => a.GestureName))
            {
                IAction newAction = new Action()
                {
                    ActivateWindow = grouping.First().ActivateWindow,
                    Condition = grouping.First().Condition,
                    GestureName = grouping.Key,
                    Name = grouping.First().Name,
                    Commands = new List<ICommand>()
                };
                foreach (var legacyAction in grouping)
                {
                    newAction.Commands.Add(new Command
                    {
                        CommandSettings = legacyAction.ActionSettings,
                        IsEnabled = legacyAction.IsEnabled,
                        Name = legacyAction.Name,
                        PluginClass = legacyAction.PluginClass,
                        PluginFilename = legacyAction.PluginFilename
                    });
                }
                newActions.Add(newAction);
            }
            return newActions;
        }

        #endregion

        #region P/Invoke
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        #endregion
    }
}
