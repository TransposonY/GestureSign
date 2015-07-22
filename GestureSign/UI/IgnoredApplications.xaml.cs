using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Localization;
using MahApps.Metro.Controls;

namespace GestureSign.UI
{
    /// <summary>
    /// AvailableApplications.xaml 的交互逻辑
    /// </summary>
    public partial class IgnoredApplications : UserControl
    {
        public static event ApplicationChangedEventHandler ShowIgnoredCustomFlyout;
        public IgnoredApplications()
        {
            InitializeComponent();
            EditApplicationFlyout.RefreshIgnoredApplications += ApplicationsFlyout_BindIgnoredApplications;

            if (ApplicationManager.Instance.FinishedLoading) BindIgnoredApplications();
            ApplicationManager.Instance.OnLoadApplicationsCompleted += (o, e) => { this.Dispatcher.Invoke(BindIgnoredApplications); };
        }

        void ApplicationsFlyout_BindIgnoredApplications(object sender, EventArgs e)
        {
            BindIgnoredApplications();
        }

        private void BindIgnoredApplications()
        {
            this.lstIgnoredApplications.ItemsSource = null;
            var lstApplications = ApplicationManager.Instance.GetIgnoredApplications().ToList();


            var sourceView = new ListCollectionView(lstApplications);//创建数据源的视图

            var groupDesctrption = new PropertyGroupDescription("MatchUsing");//设置分组列

            sourceView.GroupDescriptions.Add(groupDesctrption);//在图中添加分组
            this.lstIgnoredApplications.ItemsSource = sourceView;//绑定数据源
        }

        private void btnDeleteIgnoredApp_Click(object sender, RoutedEventArgs e)
        {
            foreach (IgnoredApplication lvItem in this.lstIgnoredApplications.SelectedItems)
                ApplicationManager.Instance.RemoveIgnoredApplications(lvItem.Name);
            ApplicationManager.Instance.SaveApplications();
            BindIgnoredApplications();
        }

        private void btnEditIgnoredApp_Click(object sender, RoutedEventArgs e)
        {
            IgnoredApplication ia = this.lstIgnoredApplications.SelectedItem as IgnoredApplication;
            if (ia == null) return;
            if (ShowIgnoredCustomFlyout != null)
                ShowIgnoredCustomFlyout(this, new ApplicationChangedEventArgs(ia));
        }

        private void btnAddIgnoredApp_Click(object sender, RoutedEventArgs e)
        {
            this.lstIgnoredApplications.SelectedIndex = -1;
            if (ShowIgnoredCustomFlyout != null)
                ShowIgnoredCustomFlyout(this, new ApplicationChangedEventArgs());
        }



        private void lstIgnoredApplications_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.btnEditIgnoredApp.IsEnabled = this.btnDeleteIgnoredApp.IsEnabled =
                this.lstIgnoredApplications.SelectedItem != null;
        }

        private void EnabledIgnoredAppCheckBoxs_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = (sender as CheckBox).IsChecked.Value;
            foreach (IgnoredApplication ia in this.lstIgnoredApplications.Items)
                ia.IsEnabled = isChecked;
        }

        private void IgnoredAppCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            (ApplicationManager.Instance.Applications.Find(app => app.Name == ((CheckBox)sender).Tag as string)
                as IgnoredApplication).IsEnabled = true;
            ApplicationManager.Instance.SaveApplications();
        }

        private void IgnoredAppCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            (ApplicationManager.Instance.Applications.Find(app => app.Name == ((CheckBox)sender).Tag as string)
                as IgnoredApplication).IsEnabled = false;
            ApplicationManager.Instance.SaveApplications();
        }

        private void ImportIgnoredAppsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofdApplications = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = LanguageDataManager.Instance.GetTextValue("Ignored.IgnoredAppFile") + "|*.json;*.ign",
                Title = LanguageDataManager.Instance.GetTextValue("Ignored.ImportIgnoredApps"),
                CheckFileExists = true
            };
            if (ofdApplications.ShowDialog().Value)
            {
                int addcount = 0;
                List<IApplication> newApps = System.IO.Path.GetExtension(ofdApplications.FileName).Equals(".ign", StringComparison.OrdinalIgnoreCase) ?
                    FileManager.LoadObject<List<IApplication>>(ofdApplications.FileName, false, true) :
                    FileManager.LoadObject<List<IApplication>>(ofdApplications.FileName, new Type[] { typeof(GlobalApplication), typeof(UserApplication), typeof(IgnoredApplication), typeof(Applications.Action) }, false);
                if (newApps != null)
                    foreach (IApplication newApp in newApps)
                    {
                        if (newApp is IgnoredApplication &&
                            !ApplicationManager.Instance.ApplicationExists(newApp.Name))
                        {
                            ApplicationManager.Instance.AddApplication(newApp);
                            addcount++;
                        }
                    }
            End:
                if (addcount != 0)
                {
                    BindIgnoredApplications();
                    ApplicationManager.Instance.SaveApplications();
                }
                MessageBox.Show(
                    String.Format(LanguageDataManager.Instance.GetTextValue("Ignored.Messages.ImportComplete"),
                        addcount),
                    LanguageDataManager.Instance.GetTextValue("Ignored.Messages.ImportCompleteTitle"));
            }
        }

        private void ExportIgnoredAppsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfdApplications = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = LanguageDataManager.Instance.GetTextValue("Ignored.IgnoredAppFile") + "|*.ign",
                Title = LanguageDataManager.Instance.GetTextValue("Ignored.ExportIgnoredApps"),
                AddExtension = true,
                DefaultExt = "ign",
                ValidateNames = true
            };
            if (sfdApplications.ShowDialog().Value)
            {
                FileManager.SaveObject(ApplicationManager.Instance.Applications.Where(app => (app is IgnoredApplication)).ToList(), sfdApplications.FileName, true);
            }
        }
    }


    [ValueConversion(typeof(bool), typeof(string))]
    public class BoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool v = (bool)value;
            if (v) return LanguageDataManager.Instance.GetTextValue("Common.Yes");
            else return LanguageDataManager.Instance.GetTextValue("Common.No");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string strValue = value as string;
            if (strValue == LanguageDataManager.Instance.GetTextValue("Common.Yes")) return true;
            return false;
        }
    }
}
