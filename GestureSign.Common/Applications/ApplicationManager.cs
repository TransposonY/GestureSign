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
using Action = GestureSign.Applications.Action;
using RECT = ManagedWinapi.Windows.RECT;

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
                OnApplicationChanged(new ApplicationChangedEventArgs(value));
            }
        }

        public List<IApplication> Applications { get { return _Applications; } }

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
                        if (!LoadDefaults())
                            _Applications = new List<IApplication>();
                    if (OnLoadApplicationsCompleted != null) OnLoadApplicationsCompleted(this, EventArgs.Empty);
                    FinishedLoading = true;
                };
            GestureManager.GestureEdited += GestureManager_GestureEdited;
            // Load applications from disk, if file couldn't be loaded, create an empty applications list
            LoadApplications().ContinueWith(antecendent => loadCompleted(antecendent.Result));
        }



        #endregion

        #region Events

        protected void TouchCapture_CaptureStarted(object sender, PointsCapturedEventArgs e)
        {
            var touchCapture = (ITouchCapture)sender;
            if (touchCapture.Mode == CaptureMode.Training) return;

            if (Environment.OSVersion.Version.Major == 6)
            {
                IntPtr hwndCharmBar = FindWindow("NativeHWNDHost", "Charm Bar");
                var window = SystemWindow.FromPointEx(SystemWindow.DesktopWindow.Rectangle.Right - 1, 1, true, true);

                if (window != null && window.HWnd.Equals(hwndCharmBar))
                {
                    e.Cancel = e.InterceptTouchInput = false;
                    return;
                }
            }

            CaptureWindow = GetWindowFromPoint(e.LastCapturedPoints.FirstOrDefault());
            IApplication[] applicationFromWindow = GetApplicationFromWindow(CaptureWindow);
            foreach (IApplication app in applicationFromWindow)
            {
                e.InterceptTouchInput |= (app is UserApplication && (app as UserApplication).InterceptTouchInput);
                if ((app is IgnoredApplication) && (app as IgnoredApplication).IsEnabled)
                {
                    e.Cancel = true;
                    return;
                }
                else if (e.Points.Count == 1)
                {
                    e.Cancel = true;
                    UserApplication userApplication = app as UserApplication;
                    if (userApplication != null && userApplication.AllowSingleStroke)
                    {
                        e.Cancel = false;
                        return;
                    }
                }
            }


        }

        protected void TouchCapture_BeforePointsCaptured(object sender, PointsCapturedEventArgs e)
        {
            // Derive capture window from capture point
            CaptureWindow = GetWindowFromPoint(e.LastCapturedPoints.FirstOrDefault());
            RecognizedApplication = GetApplicationFromWindow(CaptureWindow);
        }

        protected void GestureManager_GestureEdited(object sender, GestureEventArgs e)
        {
            GetGlobalApplication().Actions.FindAll(a => a.GestureName == e.GestureName).ForEach(a => a.GestureName = e.NewGestureName);

            foreach (UserApplication uApp in Applications.OfType<UserApplication>())
                uApp.Actions.FindAll(a => a.GestureName == e.GestureName).ForEach(a => a.GestureName = e.NewGestureName);
            SaveApplications();
        }
        #endregion

        #region Custom Events

        public event ApplicationChangedEventHandler ApplicationChanged;

        protected virtual void OnApplicationChanged(ApplicationChangedEventArgs e)
        {
            if (ApplicationChanged != null) ApplicationChanged(this, e);
        }

        #endregion

        #region Public Methods

        public void Load(ITouchCapture touchCapture)
        {
            // Shortcut method to control singleton instantiation
            // Consume Touch Capture events
            if (touchCapture != null)
            {
                touchCapture.CaptureStarted += new PointsCapturedEventHandler(TouchCapture_CaptureStarted);
                touchCapture.BeforePointsCaptured += new PointsCapturedEventHandler(TouchCapture_BeforePointsCaptured);
            }
        }

        public void AddApplication(IApplication Application)
        {
            _Applications.Add(Application);
        }

        public void RemoveApplication(IApplication Application)
        {
            _Applications.Remove(Application);
        }

        public void RemoveIgnoredApplications(string applicationName)
        {
            _Applications.RemoveAll(app => app is IgnoredApplication && app.Name.Equals(applicationName));
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
                 _Applications, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestureSign", "Actions.act"), true);
            if (flag && notice) { NamedPipe.SendMessageAsync("LoadApplications", "GestureSignDaemon"); }

        }

        public Task<bool> LoadApplications()
        {
            return Task.Run(() =>
            {
                // Load application list from file
                _Applications =
                    FileManager.LoadObject<List<IApplication>>(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestureSign",
                            "Actions.act"),
                        true, true);
                if (_Applications == null)
                {
                    _Applications = FileManager.LoadObject<List<IApplication>>(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestureSign", "Applications.json"),
                        new[]
                        {
                            typeof (GlobalApplication), typeof (UserApplication), typeof (IgnoredApplication),
                            typeof (Action)
                        }, true);
                    if (_Applications != null)
                    {
                        _Applications.ForEach(a => { if (a.Group == null) a.Group = String.Empty; });
                        SaveFile(false);
                    }
                    else return false;// No object, failed
                }
                return true; // Success
            });
        }

        private bool LoadDefaults()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Defaults\Actions.act");

            _Applications = FileManager.LoadObject<List<IApplication>>(path, true, true);
            // Ensure we got an object back
            if (_Applications == null)
                return false; // No object, failed

            return true; // Success
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
                ? FindMatchApplications(Applications.Where(a => a is UserApplication), Window)
                : FindMatchApplications(Applications.Where(a => !(a is GlobalApplication)), Window);
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
            return GetEnabledDefinedAction(GestureName, RecognizedApplication, true);
        }

        public IAction GetAnyDefinedAction(string actionName, string applicationName)
        {
            IApplication app = GetGlobalApplication().Name == applicationName ? GetGlobalApplication() : GetExistingUserApplication(applicationName);
            if (app != null && app.Actions.Exists(a => a.Name.Equals(actionName)))
                return app.Actions.Find(a => a.Name.Equals(actionName));

            return null;
        }

        public IEnumerable<IAction> GetEnabledDefinedAction(string gestureName, IEnumerable<IApplication> application, bool useGlobal)
        {
            if (application == null)
            {
                return null;
            }
            // Attempt to retrieve an action on the application passed in
            IEnumerable<IAction> finalAction =
                application.Where(app => !(app is IgnoredApplication)).SelectMany(app => app.Actions.Where(a => a.IsEnabled && a.GestureName.Equals(gestureName, StringComparison.Ordinal)));
            // If there is was no action found on given application, try to get an action for global application
            if (!finalAction.Any() && useGlobal)
                finalAction = GetGlobalApplication().Actions.Where(a => a.GestureName == gestureName);

            // Return whatever the result was
            return finalAction;
        }

        public IApplication GetExistingUserApplication(string ApplicationName)
        {
            return Applications.FirstOrDefault(a => a is UserApplication && a.Name == ApplicationName.Trim());
        }

        public bool IsGlobalAction(string ActionName)
        {
            return _Applications.Exists(a => a is GlobalApplication && a.Actions.Any(ac => ac.Name == ActionName.Trim()));
        }

        public bool ApplicationExists(string ApplicationName)
        {
            return _Applications.Exists(a => a.Name == ApplicationName.Trim());
        }

        public IApplication[] GetAvailableUserApplications()
        {
            return Applications.Where(a => a is UserApplication).OrderBy(a => a.Name).Cast<UserApplication>().ToArray();
        }

        public IEnumerable<IgnoredApplication> GetIgnoredApplications()
        {
            return Applications.Where(a => a is IgnoredApplication).OrderBy(a => a.Name).Cast<IgnoredApplication>();
        }

        public IApplication GetGlobalApplication()
        {
            if (_Applications == null)
            {
                _Applications = new List<IApplication> { new GlobalApplication { Group = String.Empty } };
            }
            else if (!_Applications.Exists(a => a is GlobalApplication))
                _Applications.Add(new GlobalApplication() { Group = String.Empty });

            return _Applications.FirstOrDefault(a => a is GlobalApplication);
        }

        public IEnumerable<IApplication> GetAllGlobalApplication()
        {
            if (!_Applications.Exists(a => a is GlobalApplication))
                _Applications.Add(new GlobalApplication() { Group = String.Empty });
            return _Applications.Where(a => a is GlobalApplication);
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
                    windowMatchString = window.Process.MainModule.ModuleName;
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

        #endregion

        #region P/Invoke
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        #endregion
    }
}
