using GestureSign.Common.Applications;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;

namespace GestureSign.ControlPanel.ViewModel
{
    public class ApplicationItemProvider
    {
        static ApplicationItemProvider()
        {
            ApplicationItems = new ObservableCollection<IApplication>();
            IgnoredApplicationItems = new ObservableCollection<IgnoredApp>();

            ApplicationManager.Instance.CollectionChanged += ApplicationManager_CollectionChanged;

            ApplicationManager.Instance.LoadingTask.ContinueWith((task) =>
            {
                Application.Current.Dispatcher.Invoke(ReloadApplicationItems);
            });
        }

        public static ObservableCollection<IApplication> ApplicationItems { get; set; }

        public static ObservableCollection<IgnoredApp> IgnoredApplicationItems { get; set; }

        private static void ReloadApplicationItems()
        {
            if (ApplicationItems == null)
                ApplicationItems = new ObservableCollection<IApplication>();
            if (IgnoredApplicationItems == null)
                IgnoredApplicationItems = new ObservableCollection<IgnoredApp>();

            ApplicationItems.Clear();
            IgnoredApplicationItems.Clear();

            ApplicationItems.Add(ApplicationManager.Instance.GetGlobalApplication());
            foreach (IApplication app in ApplicationManager.Instance.Applications)
            {
                if (app is UserApp)
                {
                    ApplicationItems.Add(app);
                }
                else if (app is IgnoredApp)
                {
                    IgnoredApplicationItems.Add((IgnoredApp)app);
                }
            }
        }

        private static void ApplicationManager_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ApplicationItems.Clear();
                IgnoredApplicationItems.Clear();
                return;
            }

            if (e.NewItems != null)
                foreach (var item in e.NewItems)
                {
                    AddApplication((IApplication)item);
                }

            if (e.OldItems != null)
                foreach (var item in e.OldItems)
                {
                    RemoveApplication((IApplication)item);
                }
        }

        private static void AddApplication(IApplication application)
        {
            if (application is IgnoredApp)
            {
                IgnoredApplicationItems.Add((IgnoredApp)application);
            }
            else
            {
                ApplicationItems.Add(application);
            }
        }

        private static void RemoveApplication(IApplication application)
        {
            if (application is IgnoredApp)
            {
                IgnoredApplicationItems.Remove((IgnoredApp)application);
            }
            else
            {
                ApplicationItems.Remove(application);
            }
        }
    }
}
