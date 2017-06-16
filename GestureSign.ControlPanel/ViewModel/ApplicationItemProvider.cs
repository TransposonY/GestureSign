using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using GestureSign.Common.Applications;
using System.Collections.Generic;

namespace GestureSign.ControlPanel.ViewModel
{
    public class ApplicationItemProvider
    {
        static ApplicationItemProvider()
        {
            ApplicationItems = new ObservableCollection<IApplication>();
            IgnoredApplicationItems = new ObservableCollection<IgnoredApp>();

            ApplicationManager.ApplicationChanged += (o, e) =>
            {
                if (e.Application is IgnoredApp)
                    UpdateIgnoredApplicationItems();
                else
                    UpdateApplicationItems();
            };

            ApplicationManager.Instance.LoadingTask.ContinueWith((task) =>
            {
                Application.Current.Dispatcher.Invoke(UpdateApplicationItems);
                Application.Current.Dispatcher.Invoke(UpdateIgnoredApplicationItems);
            });
        }

        public static ObservableCollection<IApplication> ApplicationItems { get; set; }

        public static ObservableCollection<IgnoredApp> IgnoredApplicationItems { get; set; }

        private static void UpdateApplicationItems()
        {
            if (ApplicationItems == null)
                ApplicationItems = new ObservableCollection<IApplication>();
            else
                ApplicationItems.Clear();

            var newApps = new List<IApplication>();
            newApps.Add(ApplicationManager.Instance.GetGlobalApplication());
            newApps.AddRange(ApplicationManager.Instance.Applications.Where(app => (app is UserApp)).OrderBy(app => app.Name));

            foreach (var app in newApps)
            {
                ApplicationItems.Add(app);
            }
        }

        private static void UpdateIgnoredApplicationItems()
        {
            if (IgnoredApplicationItems == null)
                IgnoredApplicationItems = new ObservableCollection<IgnoredApp>();
            else
                IgnoredApplicationItems.Clear();

            foreach (var app in ApplicationManager.Instance.GetIgnoredApplications())
            {
                IgnoredApplicationItems.Add(app);
            }
        }
    }
}
