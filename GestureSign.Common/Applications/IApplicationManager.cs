using System;
using System.Collections.Generic;

namespace GestureSign.Common.Applications
{
    public interface IApplicationManager
    {
        event ApplicationChangedEventHandler ApplicationChanged;
        bool ApplicationExists(string ApplicationName);
        List<IApplication> Applications { get; }
        ManagedWinapi.Windows.SystemWindow CaptureWindow { get; }
        IApplication CurrentApplication { get; set; }
        void AddApplication(IApplication Application);
        IEnumerable<IAction> GetRecognizedDefinedAction(string GestureName);
        IEnumerable<IApplication> GetApplicationFromPoint(System.Drawing.PointF TestPoint);
        IApplication[] GetApplicationFromWindow(ManagedWinapi.Windows.SystemWindow Window);
        IApplication[] GetAvailableUserApplications();
        IEnumerable<IAction> GetDefinedAction(string GestureName, IEnumerable<IApplication> Application, bool UseGlobal);
        IEnumerable<IAction> GetDefinedAction(string GestureName);
        IApplication GetExistingUserApplication(string ApplicationName);
        IApplication GetGlobalApplication();
        ManagedWinapi.Windows.SystemWindow GetWindowFromPoint(System.Drawing.PointF Point);
        bool IsGlobalGesture(string GestureName);
        bool IsUserGesture(string GestureName);
        bool LoadApplications();
        void RemoveGlobalAction(string ActionName);
        void RemoveNonGlobalAction(string ActionName);
        bool SaveApplications();
    }
}
