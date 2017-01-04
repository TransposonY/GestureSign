using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using GestureSign.Common.Applications;

namespace GestureSign.ControlPanel.ViewModel
{
    public class ApplicationItemProvider
    {
        static ApplicationItemProvider()
        {
            ApplicationItems = new ObservableCollection<IApplication>();
            IgnoredApplicationItems = new ObservableCollection<IgnoredApplication>();

            ApplicationManager.ApplicationChanged += (o, e) =>
            {
                if (e.Application is IgnoredApplication)
                    UpdateIgnoredApplicationItems();
                else
                    UpdateApplicationItems();
            };

            ApplicationManager.OnLoadApplicationsCompleted += (o, e) =>
            {
                Application.Current.Dispatcher.Invoke(UpdateApplicationItems);
                Application.Current.Dispatcher.Invoke(UpdateIgnoredApplicationItems);
            };

            if (ApplicationManager.FinishedLoading)
            {
                UpdateApplicationItems();
                UpdateIgnoredApplicationItems();
            }
        }

        public static ObservableCollection<IApplication> ApplicationItems { get; set; }

        public static ObservableCollection<IgnoredApplication> IgnoredApplicationItems { get; set; }

        private static void UpdateApplicationItems()
        {
            if (ApplicationItems == null)
                ApplicationItems = new ObservableCollection<IApplication>();
            else
                ApplicationItems.Clear();

            var userApplications = ApplicationManager.Instance.Applications.Where(app => (app is UserApplication)).OrderBy(app => app.Name);
            var globalApplication = ApplicationManager.Instance.GetAllGlobalApplication();

            foreach (var app in globalApplication.Union(userApplications))
            {
                ApplicationItems.Add(app);
            }
        }

        private static void UpdateIgnoredApplicationItems()
        {
            if (IgnoredApplicationItems == null)
                IgnoredApplicationItems = new ObservableCollection<IgnoredApplication>();
            else
                IgnoredApplicationItems.Clear();

            foreach (var app in ApplicationManager.Instance.GetIgnoredApplications())
            {
                IgnoredApplicationItems.Add(app);
            }
        }
    }
}
