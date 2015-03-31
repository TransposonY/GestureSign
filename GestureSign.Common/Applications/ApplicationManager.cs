using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Drawing;
using ManagedWinapi.Windows;
using GestureSign.Common;
using GestureSign.Common.Input;
using GestureSign.Common.Gestures;
using System.Text.RegularExpressions;

namespace GestureSign.Common.Applications
{
    public class ApplicationManager : IApplicationManager
    {
        #region Private Variables

        // Create variable to hold the only allowed instance of this class
        static readonly ApplicationManager _Instance = new ApplicationManager();
        List<IApplication> _Applications = new List<IApplication>();
        IApplication _CurrentApplication = null;
        IEnumerable<IApplication> RecognizedApplication;
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
            get { return _Instance; }
        }

        #endregion

        #region Constructors

        protected ApplicationManager()
        {

            Gestures.GestureManager.Instance.GestureEdited += GestureManager_GestureEdited;
            // Load applications from disk, if file couldn't be loaded, create an empty applications list
            if (!LoadApplications())
                _Applications = new List<IApplication>();

        }



        #endregion

        #region Events

        protected void TouchCapture_CaptureStarted(object sender, PointsCapturedEventArgs e)
        {
            CaptureWindow = GetWindowFromPoint(e.CapturePoint.FirstOrDefault());
            IEnumerable<IApplication> ApplicationFromWindow = GetApplicationFromWindow(CaptureWindow);
            foreach (IApplication app in ApplicationFromWindow)
            {
                if ((app is IgnoredApplication) && (app as IgnoredApplication).IsEnabled)
                    e.Cancel = true;
                e.InterceptTouchInput = (app is CustomApplication && (app as CustomApplication).InterceptTouchInput);

            }
        }

        protected void TouchCapture_BeforePointsCaptured(object sender, PointsCapturedEventArgs e)
        {
            // Derive capture window from capture point
            CaptureWindow = GetWindowFromPoint(e.CapturePoint.FirstOrDefault());
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

        public void RemoveApplications(string applicationName)
        {
            _Applications.RemoveAll(app => app.Name.Equals(applicationName));
        }
        public void RemoveIgnoredApplications(string applicationName)
        {
            _Applications.RemoveAll(app => app is IgnoredApplication && app.Name.Equals(applicationName));
        }

        public bool SaveApplications()
        {
            // Save application list
            return Common.Configuration.FileManager.SaveObject<List<IApplication>>(_Applications, Path.Combine("Data", "Applications.json"), new Type[] { typeof(GlobalApplication), typeof(UserApplication), typeof(CustomApplication), typeof(IgnoredApplication), typeof(GestureSign.Applications.Action) });
        }

        public bool LoadApplications()
        {
            // Load application list from file
            _Applications = Common.Configuration.FileManager.LoadObject<List<IApplication>>(Path.Combine("Data", "Applications.json"), new Type[] { typeof(GlobalApplication), typeof(UserApplication), typeof(CustomApplication), typeof(IgnoredApplication), typeof(GestureSign.Applications.Action) }, true);
            if (_Applications != null)
                _Applications = _Applications.ConvertAll<IApplication>(new Converter<IApplication, IApplication>(app =>
                      {
                          if (app is UserApplication && !(app is CustomApplication))
                              return new CustomApplication()
                              {
                                  Actions = app.Actions,
                                  IsRegEx = app.IsRegEx,
                                  InterceptTouchInput = true,
                                  MatchString = app.MatchString,
                                  MatchUsing = app.MatchUsing,
                                  Name = app.Name
                              };
                          else return app;
                      }));
            // Ensure we got an object back
            if (_Applications == null)
                return false;	// No object, failed

            return true;	// Success
        }

        public SystemWindow GetWindowFromPoint(PointF Point)
        {
            return SystemWindow.FromPointEx((int)Math.Floor(Point.X), (int)Math.Floor(Point.Y), true, true);
        }

        public IEnumerable<IApplication> GetApplicationFromWindow(SystemWindow Window)
        {
            IEnumerable<IApplication> definedApplications = Applications.Where(a => !(a is GlobalApplication) && a.IsSystemWindowMatch(Window));
            // Try to find any user or ignored applications that match the given system window
            if (definedApplications.Count() != 0)
                return definedApplications;

            // If not user or ignored application could be found, return the global application
            return new IApplication[] { GetGlobalApplication() };
        }

        public IEnumerable<IApplication> GetApplicationFromPoint(PointF TestPoint)
        {
            return GetApplicationFromWindow(GetWindowFromPoint(TestPoint));
        }

        public IEnumerable<IAction> GetDefinedAction(string GestureName)
        {
            return GetDefinedAction(GestureName, new IApplication[] { this.CurrentApplication }, false);
        }

        public IEnumerable<IAction> GetRecognizedDefinedAction(string GestureName)
        {
            return GetDefinedAction(GestureName, RecognizedApplication, true);
        }

        public IAction GetAnyDefinedAction(string actionName, string applicationName)
        {
            IApplication app = GetGlobalApplication().Name == applicationName ? GetGlobalApplication() : GetExistingUserApplication(applicationName);
            if (app != null && app.Actions.Exists(a => a.Name.Equals(actionName)))
                return app.Actions.Find(a => a.Name.Equals(actionName));

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
                application.Where(app => !(app is IgnoredApplication)).SelectMany(app => app.Actions.Where(a => a.GestureName == gestureName));
            // If there is was no action found on given application, try to get an action for global application
            if (finalAction.Count() == 0 && useGlobal)
                finalAction = GetGlobalApplication().Actions.Where(a => a.GestureName == gestureName);

            // Return whatever the result was
            return finalAction;
        }

        public IApplication GetExistingUserApplication(string ApplicationName)
        {
            return Applications.FirstOrDefault(a => a is UserApplication && a.Name.ToLower() == ApplicationName.Trim().ToLower()) as UserApplication;
        }

        public bool IsGlobalGesture(string GestureName)
        {
            return _Applications.Exists(a => a is GlobalApplication && a.Actions.FirstOrDefault(ac => ac.GestureName.ToLower() == GestureName.Trim().ToLower()) != null);
        }

        public bool IsUserGesture(string GestureName)
        {
            return _Applications.Exists(a => a is UserApplication && a.Actions.FirstOrDefault(ac => ac.GestureName.ToLower() == GestureName.Trim().ToLower()) != null);
        }

        public bool IsGlobalAction(string ActionName)
        {
            return _Applications.Exists(a => a is GlobalApplication && a.Actions.Any(ac => ac.Name.ToLower() == ActionName.Trim().ToLower()));
        }

        public bool IsUserAction(string ActionName)
        {
            return _Applications.Exists(a => a is UserApplication && a.Actions.Any(ac => ac.Name.ToLower() == ActionName.Trim().ToLower()));
        }

        public bool ApplicationExists(string ApplicationName)
        {
            return _Applications.Exists(a => a.Name.ToLower() == ApplicationName.Trim().ToLower());
        }

        public IApplication[] GetAvailableUserApplications()
        {
            return Applications.Where(a => a is UserApplication).OrderBy(a => a.Name).Cast<UserApplication>().ToArray();
        }

        public IgnoredApplication[] GetIgnoredApplications()
        {
            return Applications.Where(a => a is IgnoredApplication).OrderBy(a => a.Name).Cast<IgnoredApplication>().ToArray();
        }

        public IApplication GetGlobalApplication()
        {
            if (!_Applications.Exists(a => a is GlobalApplication))
                _Applications.Add(new GlobalApplication());

            return _Applications.First(a => a is GlobalApplication) as GlobalApplication;
        }

        public void RemoveGlobalAction(string ActionName)
        {
            RemoveAction(ActionName, true);
        }

        public void RemoveNonGlobalAction(string ActionName)
        {
            RemoveAction(ActionName, false);
        }

        public bool IsGlobalApplication(IApplication Application)
        {
            return (Application == GetGlobalApplication());
        }

        #endregion

        #region Private Methods

        private void RemoveAction(string ActionName, bool Global)
        {
            if (Global)
                // Attempt to remove action from global actions
                GetGlobalApplication().RemoveAllActions(a => a.Name.ToLower().Trim() == ActionName.ToLower().Trim());
            else
                // Select applications where this action may exist and delete them
                foreach (IApplication app in GetAvailableUserApplications().Where(a => a.Actions.Any(ac => ac.Name == ActionName)))
                    app.RemoveAllActions(a => a.Name.ToLower().Trim() == ActionName.ToLower().Trim());
        }

        #endregion
    }
}
