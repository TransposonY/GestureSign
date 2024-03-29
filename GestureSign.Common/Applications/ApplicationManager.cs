﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using ManagedWinapi.Windows;

namespace GestureSign.Common.Applications
{
    public class ApplicationManager : IApplicationManager, INotifyCollectionChanged
    {
        #region Private Variables

        // Create variable to hold the only allowed instance of this class
        private static ApplicationManager _instance;
        private List<IApplication> _applications;
        IEnumerable<IApplication> _recognizedApplication;
        private Timer _timer;
        #endregion

        #region Public Instance Properties

        public SystemWindow CaptureWindow { get; private set; }
        public IEnumerable<IApplication> RecognizedApplication { get { return _recognizedApplication; } }

        public List<IApplication> Applications
        {
            get
            {
                if (LoadingTask.IsCompleted)
                    return _applications != null ? _applications : _applications = new List<IApplication>();
                else
                    return new List<IApplication>();
            }
        }

        public static ApplicationManager Instance
        {
            get { return _instance ?? (_instance = new ApplicationManager()); }
        }

        public Task LoadingTask { get; }

        #endregion

        #region Constructors

        protected ApplicationManager()
        {
            // Load applications from disk, if file couldn't be loaded, create an empty applications list
            LoadingTask = LoadApplications();
        }

        #endregion

        #region Events

        protected void PointCapture_CaptureStarted(object sender, PointsCapturedEventArgs e)
        {
            var pointCapture = (IPointCapture)sender;
            if (pointCapture.Mode == CaptureMode.Training) return;

            if (VersionHelper.IsWindows8OrGreater() && !VersionHelper.IsWindows10OrGreater())
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
            _recognizedApplication = GetApplicationFromWindow(CaptureWindow);

            int maxThreshold = 0, maxLimitNumber = 1;

            foreach (IApplication app in _recognizedApplication)
            {
                switch (app)
                {
                    case GlobalApp a:
                        maxLimitNumber = a.LimitNumberOfFingers > maxLimitNumber ? a.LimitNumberOfFingers : maxLimitNumber;
                        if (AppConfig.IgnoreFullScreen && IsFullScreenWindow(e.FirstCapturedPoints.FirstOrDefault()))
                        {
                            e.Cancel = true;
                            return;
                        }
                        break;
                    case UserApp a:
                        maxThreshold = a.BlockTouchInputThreshold > maxThreshold ? a.BlockTouchInputThreshold : maxThreshold;
                        maxLimitNumber = a.LimitNumberOfFingers > maxLimitNumber ? a.LimitNumberOfFingers : maxLimitNumber;
                        break;
                    case IgnoredApp a:
                        if (a.IsEnabled)
                        {
                            e.Cancel = true;
                            return;
                        }
                        break;
                    default:
                        return;
                }
            }
            e.Cancel = (pointCapture.SourceDevice & Devices.TouchDevice) != 0 && (e.Points.Count < maxLimitNumber);
            e.BlockTouchInputThreshold = maxThreshold;
        }

        protected void PointCapture_BeforePointsCaptured(object sender, PointsCapturedEventArgs e)
        {
            var appsToMatch = Applications.Where(a => a is UserApp && a.MatchActivated);
            if (appsToMatch.Any())
            {
                CaptureWindow = SystemWindow.ForegroundWindow;
                string className, title, fileName;
                GetWindowInfo(CaptureWindow, out className, out title, out fileName);
                var matchedForegroundApps = FindMatchApplications(appsToMatch, className, title, fileName);

                if (matchedForegroundApps.Length != 0)
                {
                    _recognizedApplication = matchedForegroundApps;
                    return;
                }
            }

            // Derive capture window from capture point
            CaptureWindow = GetWindowFromPoint(e.FirstCapturedPoints.FirstOrDefault());
            _recognizedApplication = GetApplicationFromWindow(CaptureWindow);
        }

        #endregion

        #region Custom Events

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public static event EventHandler ApplicationSaved;
        public static event EventHandler OnLoadApplicationsCompleted;

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
            Applications.Add(application);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, application));
        }

        public void AddApplicationRange(List<IApplication> applications)
        {
            Applications.AddRange(applications);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, applications));
        }

        public void RemoveApplication(IApplication application)
        {
            Applications.Remove(application);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, application));
        }

        public void ReplaceApplication(IApplication oldApplication, IApplication newApplication)
        {
            Applications.Remove(oldApplication);
            Applications.Add(newApplication);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newApplication, oldApplication));
        }

        public void RemoveAllApplication()
        {
            Applications.Clear();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void RemoveIgnoredApplications(string applicationName)
        {
            Applications.RemoveAll(app => app is IgnoredApp && app.Name == applicationName);
        }

        public bool SaveApplications()
        {
            TrimActions(Applications);

            if (_timer == null)
            {
                _timer = new Timer(new TimerCallback(SaveFile), true, 200, Timeout.Infinite);
            }
            else _timer.Change(200, Timeout.Infinite);
            return true;
        }

        private void SaveFile(object state)
        {
            // Save application list
            bool flag = FileManager.SaveObject(
                 Applications, Path.Combine(AppConfig.ApplicationDataPath, Constants.ActionFileName), true);
            if (flag) { ApplicationSaved.Invoke(this, EventArgs.Empty); }

        }

        public Task LoadApplications()
        {
            Action<bool> loadCompleted =
                result =>
                {
                    if (!result)
                        if (!LoadBackup())
                            if (!LoadLegacy())
                                if (!LoadDefaults())
                                    _applications = new List<IApplication>();
                    OnLoadApplicationsCompleted?.Invoke(this, EventArgs.Empty);
                };

            return Task.Run(() =>
            {
                // Load application list from file
                _applications =
                    FileManager.LoadObject<List<IApplication>>(
                        Path.Combine(AppConfig.ApplicationDataPath, Constants.ActionFileName), true, true);
                return _applications != null;
            }).ContinueWith(antecendent => loadCompleted(antecendent.Result));
        }

        private bool LoadDefaults()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Defaults", Constants.ActionFileName);

            _applications = FileManager.LoadObject<List<IApplication>>(path, false, true);
            // Ensure we got an object back
            if (_applications == null)
                return false; // No object, failed

            return true; // Success
        }

        private bool LoadBackup()
        {
            var directory = new DirectoryInfo(AppConfig.BackupPath);
            if (directory.Exists)
            {
                var actionfiles = directory.EnumerateFiles("*" + Constants.ActionExtension).OrderByDescending(f => f.LastWriteTime);
                foreach (var file in actionfiles)
                {
                    _applications = FileManager.LoadObject<List<IApplication>>(file.FullName, false, true);
                    if (_applications != null) return true;
                }
            }
            return false;
        }

        public SystemWindow GetWindowFromPoint(Point point)
        {
            return SystemWindow.FromPointEx(point.X, point.Y, true, true);
        }

        public IApplication[] GetApplicationFromWindow(SystemWindow window, bool userApplicationOnly = false)
        {
            if (Applications == null || window == null)
            {
                return new[] { GetGlobalApplication() };
            }

            string className, title, fileName;
            GetWindowInfo(window, out className, out title, out fileName);

            IApplication[] definedApplications = userApplicationOnly
                ? FindMatchApplications(Applications.Where(a => a is UserApp), className, title, fileName)
                : FindMatchApplications(Applications.Where(a => !(a is GlobalApp)), className, title, fileName);
            // Try to find any user or ignored applications that match the given system window
            // If not user or ignored application could be found, return the global application
            return definedApplications.Length != 0
                ? definedApplications
                : userApplicationOnly ? new IApplication[0] : new IApplication[] { GetGlobalApplication() };
        }

        public IEnumerable<IApplication> GetApplicationFromPoint(Point testPoint)
        {
            var systemWindow = GetWindowFromPoint(testPoint);
            return GetApplicationFromWindow(systemWindow);
        }

        public IEnumerable<IAction> GetRecognizedDefinedAction(string GestureName)
        {
            return GetDefinedAction(GestureName, _recognizedApplication, true);
        }

        public List<IAction> GetRecognizedDefinedAction(Func<IAction, bool> predicate)
        {
            if (_recognizedApplication == null)
            {
                return new List<IAction>();
            }
            var recognizedActions = _recognizedApplication.Where(app => !(app is IgnoredApp) && app.Actions != null).SelectMany(app => app.Actions).Where(a => predicate(a)).ToList();
            // If there is was no action found on given application, try to get an action for global application
            if (recognizedActions.Count == 0)
                recognizedActions = GetGlobalApplication().Actions.Where(a => predicate(a)).ToList();

            return recognizedActions;
        }

        public IEnumerable<IAction> GetDefinedAction(string gestureName, IEnumerable<IApplication> application, bool useGlobal)
        {
            if (application == null)
            {
                return Enumerable.Empty<IAction>();
            }
            // Attempt to retrieve an action on the application passed in
            IEnumerable<IAction> finalAction =
                application.Where(app => !(app is IgnoredApp) && app.Actions != null).SelectMany(app => app.Actions.Where(a => a.GestureName == gestureName && a.Commands != null && a.Commands.Any(com => com != null && com.IsEnabled)));
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
            return Applications.Exists(a => a is GlobalApp && a.Actions.Any(ac => ac.Name == ActionName.Trim()));
        }

        public bool ApplicationExists(string ApplicationName)
        {
            return Applications.Exists(a => a.Name == ApplicationName.Trim());
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
            var apps = Applications;
            GlobalApp globalApp = apps.FirstOrDefault(a => a is GlobalApp) as GlobalApp;
            if (globalApp == null)
            {
                globalApp = new GlobalApp() { Group = String.Empty };
                apps.Add(globalApp);
                return globalApp;
            }
            else return globalApp;
        }

        public IApplication[] FindMatchApplications<TApplication>(MatchUsing matchUsing, string matchString, string excludedApplication = null) where TApplication : IApplication
        {
            return Applications.FindAll(
                    a => a is TApplication &&
                        matchString.Equals(a.MatchString, StringComparison.CurrentCultureIgnoreCase) &&
                        matchUsing == a.MatchUsing &&
                        excludedApplication != a.Name).ToArray();
        }

        public SystemWindow GetForegroundApplications()
        {
            CaptureWindow = SystemWindow.ForegroundWindow;
            _recognizedApplication = GetApplicationFromWindow(CaptureWindow);
            return CaptureWindow;
        }

        public static string GetNextCommandName(string name, IAction action, int number = 1)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var newName = number == 1 ? name : $"{name}({number})";
            if (action.Commands.Any(a => a.Name == newName))
                return GetNextCommandName(name, action, ++number);
            return newName;
        }

        public IApplication AddApplication<TApp>(TApp app, string executablefilePath) where TApp : IApplication
        {
            var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(executablefilePath);
            app.Name = string.IsNullOrWhiteSpace(versionInfo.ProductName) ? Path.GetFileNameWithoutExtension(executablefilePath) : versionInfo.ProductName;
            app.MatchUsing = MatchUsing.ExecutableFilename;
            app.MatchString = Path.GetFileName(executablefilePath);

            var matchApplications = FindMatchApplications<TApp>(app.MatchUsing, app.MatchString);
            if (matchApplications.Length != 0)
            {
                return matchApplications[0];
            }
            var existingApp = Applications.Find(a => a.Name == app.Name && a is TApp);
            if (existingApp != null)
            {
                return existingApp;
            }
            AddApplication(app);
            SaveApplications();
            return app;
        }

        public static SystemWindow GetRealWindow(SystemWindow window)
        {
            try
            {
                if (VersionHelper.IsWindows10OrGreater() && "ApplicationFrameWindow".Equals(window.ClassName))
                {
                    var realWindow = window.AllChildWindows.FirstOrDefault(w => "Windows.UI.Core.CoreWindow".Equals(w.ClassName));
                    if (realWindow != null)
                        return realWindow;
                }
                return window;
            }
            catch (Exception)
            {
                return window;
            }
        }

        public static SystemWindow GetWindowInfo(SystemWindow window, out string className, out string title, out string fileName)
        {
            var realWindow = GetRealWindow(window);
            className = fileName = null;

            if (VersionHelper.OsVersion >= new Version(10, 0, 17134))
            {
                title = window.Title;
            }
            else
                title = realWindow.Title;

            try
            {
                className = realWindow.ClassName;
            }
            catch { }

            try
            {
                fileName = Path.GetFileName(realWindow.GetProcessFilePath());
            }
            catch { }
            return realWindow;
        }

        #endregion

        #region Private Methods

        private IApplication[] FindMatchApplications(IEnumerable<IApplication> applications, string className, string title, string fileName)
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
            if (byClass.Count != 0)
            {
                try
                {
                    result.AddRange(byClass.Where(a => a.MatchString != null && CompareString(a.MatchString, className, a.IsRegEx)));
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
                    result.AddRange(byTitle.Where(a => a.MatchString != null && CompareString(a.MatchString, title, a.IsRegEx)));
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
                    result.AddRange(byFileName.Where(a => a.MatchString != null && CompareString(a.MatchString, fileName, a.IsRegEx)));
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

#pragma warning disable CS0618
        private bool LoadLegacy()
        {
            var legacyApps = FileManager.LoadObject<List<LegacyApplicationBase>>(Path.Combine(AppConfig.ApplicationDataPath, "Actions.act"), true, true);
            if (legacyApps == null) return false;
            _applications = new List<IApplication>();
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
                    _applications.Add(newApp);
                    continue;
                }

                var legacyIgnoredApp = app as IgnoredApplication;
                if (legacyIgnoredApp != null)
                {
                    var temp = legacyIgnoredApp.Name.Split(new[] { '$' }, StringSplitOptions.RemoveEmptyEntries);
                    var newName = temp.Length > 1 ? temp[1] : legacyIgnoredApp.Name;
                    var newApp = new IgnoredApp(newName, legacyIgnoredApp.MatchUsing, legacyIgnoredApp.MatchString, legacyIgnoredApp.IsRegEx, legacyIgnoredApp.IsEnabled);
                    _applications.Add(newApp);
                    continue;
                }

                var legacyGlobalApp = app as GlobalApplication;
                if (legacyGlobalApp != null)
                {
                    IApplication newApp = new GlobalApp()
                    {
                        Actions = ConvertLegacyActions(legacyGlobalApp.Actions)
                    };
                    _applications.Add(newApp);
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
                };
                foreach (var legacyAction in grouping)
                {
                    newAction.AddCommand(new Command
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
#pragma warning restore CS0618  

        private bool IsFullScreenWindow(Point targetPoint)
        {
            SystemWindow deskWindow = SystemWindow.DesktopWindow;

            SystemWindow sw = SystemWindow.FromPoint(targetPoint.X, targetPoint.Y);
            if (sw == null) return false;

            // get the window with the largest area
            RECT rect = sw.Rectangle;
            int area = rect.Height * rect.Width;
            while (sw.ParentSymmetric != null)
            {
                sw = sw.ParentSymmetric;
                RECT parentRect = sw.Rectangle;
                int parentArea = parentRect.Height * parentRect.Width;
                if (parentArea > area)
                {
                    area = parentArea;
                    rect = parentRect;
                }
            }

            if (sw.HWnd == IntPtr.Zero || sw == deskWindow || sw == SystemWindow.ShellWindow)
                return false;

            var desktopRect = deskWindow.Rectangle;

            if (rect.Left == desktopRect.Left && rect.Top == desktopRect.Top && rect.Right == desktopRect.Right && rect.Bottom == desktopRect.Bottom)
            {
                switch (sw.ClassName)
                {
                    case "WorkerW":
                    case "Progman":
                    case "CanvasWindow":
                    case "ImmersiveLauncher":
                        return false;
                    default:
                        return true;
                }
            }

            return false;
        }

        private void TrimActions(IEnumerable<IApplication> applications)
        {
            foreach (var app in applications)
            {
                if (app.Actions == null) continue;
                var emptyActions = app.Actions.Where(a => a.Commands == null || !a.Commands.Any()).ToList();
                emptyActions.ForEach(a => app.RemoveAction(a));
            }
        }

        #endregion

        #region P/Invoke
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        #endregion
    }
}
