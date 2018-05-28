using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using GestureSign.ControlPanel.Common;
using GestureSign.ControlPanel.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GestureSign.ControlPanel.UserControls
{
    /// <summary>
    /// Interaction logic for ApplicationSelector.xaml
    /// </summary>
    public partial class ApplicationSelector : UserControl
    {
        public Dictionary<string, IGesture> GestureMap { get; set; }

        public List<AppListItem> UserAppList
        {
            get { return (List<AppListItem>)GetValue(UserAppListProperty); }
            set { SetValue(UserAppListProperty, value); }
        }

        public List<AppListItem> IgnoredAppList
        {
            get { return (List<AppListItem>)GetValue(IgnoredAppListProperty); }
            set { SetValue(IgnoredAppListProperty, value); }
        }

        public static readonly DependencyProperty UserAppListProperty =
            DependencyProperty.Register(nameof(UserAppList), typeof(List<AppListItem>), typeof(ApplicationSelector), new PropertyMetadata(new List<AppListItem>()));

        public static readonly DependencyProperty IgnoredAppListProperty =
            DependencyProperty.Register(nameof(IgnoredAppList), typeof(List<AppListItem>), typeof(ApplicationSelector), new PropertyMetadata(new List<AppListItem>()));

        public List<IApplication> SeletedApplications
        {
            get
            {
                var seletedApps = new List<IApplication>();
                foreach (var app in UserAppList)
                {
                    if (app.IsSelected.HasValue)
                    {
                        if (!app.IsSelected.Value) continue;

                        seletedApps.Add(app.Application);
                    }
                    else
                    {
                        var seletedApp = app.Application as UserApp;
                        if (seletedApp != null)
                        {
                            UserApp userApp = new UserApp()
                            {
                                BlockTouchInputThreshold = seletedApp.BlockTouchInputThreshold,
                                Group = seletedApp.Group,
                                IsRegEx = seletedApp.IsRegEx,
                                LimitNumberOfFingers = seletedApp.LimitNumberOfFingers,
                                MatchString = seletedApp.MatchString,
                                MatchUsing = seletedApp.MatchUsing,
                                Name = seletedApp.Name,
                                Actions = app.ActionItemList.Where(ail => ail.IsSelected).Select(ail => ail.Action).ToList()
                            };
                            seletedApps.Add(userApp);
                        }
                        else
                        {
                            GlobalApp globalApp = new GlobalApp()
                            {
                                Actions = app.ActionItemList.Where(ail => ail.IsSelected).Select(ail => ail.Action).ToList()
                            };
                            seletedApps.Add(globalApp);
                        }
                    }
                }
                foreach (var app in IgnoredAppList)
                {
                    if (app.IsSelected.HasValue && app.IsSelected.Value)
                    {
                        seletedApps.Add(app.Application);
                    }
                }

                return seletedApps;
            }
        }


        public ApplicationSelector()
        {
            InitializeComponent();
        }

        public void Initialize(IEnumerable<IApplication> apps, IEnumerable<IGesture> gestures, bool showIgnore = false, bool selecteAll = false)
        {
            if (showIgnore)
                AppTabControl.SelectedIndex = 1;

            var newUserAppList = new List<AppListItem>();
            var newIgnoredApp = new List<AppListItem>();
            foreach (var app in apps)
            {
                IgnoredApp ignoredApp = app as IgnoredApp;
                if (ignoredApp != null)
                {
                    newIgnoredApp.Add(new AppListItem(ignoredApp, null, selecteAll));
                }
                else
                {
                    var ali = new AppListItem(app, null, selecteAll);
                    ali.ActionItemList.ForEach(a => a.IsSelected = selecteAll);
                    newUserAppList.Add(ali);
                }
            }
            GestureMap = gestures.ToDictionary(g => g.Name);
            UserAppList = newUserAppList;
            IgnoredAppList = newIgnoredApp;
        }

        private void AppFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AppListBox == null) return;
            var view = CollectionViewSource.GetDefaultView(AppListBox.ItemsSource);
            if (view == null) return;
            view.Refresh();
        }

        private void ActionFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ActionListBox == null) return;
            var view = CollectionViewSource.GetDefaultView(ActionListBox.ItemsSource);
            if (view == null) return;
            view.Refresh();
        }

        private void AppCollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            string filter = AppFilterTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(filter))
            {
                e.Accepted = true;
            }
            else
            {
                var app = (AppListItem)e.Item;
                e.Accepted = (app.Application.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            string filter = ActionFilterTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(filter))
            {
                e.Accepted = true;
            }
            else
            {
                var listItem = (ActionListItem)e.Item;
                e.Accepted = (listItem.Action.Name?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        private void AllAppCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            bool? isChecked = checkBox.IsChecked;
            UserAppList.ForEach(ali => ali.IsSelected = isChecked);
        }

        private void ActionCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var seletedApp = AppListBox.SelectedItem as AppListItem;
            if (seletedApp == null) return;
            if (seletedApp.ActionItemList.All(a => a.IsSelected))
                seletedApp.IsSelected = true;
            else if (seletedApp.ActionItemList.All(a => !a.IsSelected))
                seletedApp.IsSelected = false;
            else seletedApp.IsSelected = null;
        }

        private void AppGroupCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            var groupItem = UIHelper.GetParentDependencyObject<GroupItem>(checkBox);
            var collectionViewGroup = groupItem.Content as CollectionViewGroup;
            if (collectionViewGroup != null)
                foreach (AppListItem item in collectionViewGroup.Items)
                {
                    item.IsSelected = checkBox.IsChecked;
                    item.ActionItemList.ForEach(a => a.IsSelected = checkBox.IsChecked.Value);
                }
        }
    }
}
