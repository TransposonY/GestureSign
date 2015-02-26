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
using GestureSign.Applications;

using MahApps.Metro.Controls;

namespace GestureSign.UI
{
    /// <summary>
    /// AvailableApplications.xaml 的交互逻辑
    /// </summary>
    public partial class IgnoredApplications : UserControl, IDisposable
    {
        public static event ApplicationChangedEventHandler IgnoredCustomFlyout;
        public static event ApplicationChangedEventHandler IgnoredRuningFlyout;
        public IgnoredApplications()
        {
            InitializeComponent();
            CustomApplicationsFlyout.BindIgnoredApplications += ApplicationsFlyout_BindIgnoredApplications;
            RuningApplicationsFlyout.BindIgnoredApplications += ApplicationsFlyout_BindIgnoredApplications;

            BindIgnoredApplications();
        }

        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here.

            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }
        void ApplicationsFlyout_BindIgnoredApplications(object sender, EventArgs e)
        {
            BindIgnoredApplications();
        }

        private void BindIgnoredApplications()
        {
            this.lstIgnoredApplications.ItemsSource = null;
            IgnoredApplication[] lstApplications = Applications.ApplicationManager.Instance.GetIgnoredApplications();


            var sourceView = new ListCollectionView(lstApplications);//创建数据源的视图

            var groupDesctrption = new PropertyGroupDescription("MatchUsing");//设置分组列

            sourceView.GroupDescriptions.Add(groupDesctrption);//在图中添加分组
            this.lstIgnoredApplications.ItemsSource = sourceView;//绑定数据源
        }

        private void btnDeleteIgnoredApp_Click(object sender, RoutedEventArgs e)
        {
            foreach (IgnoredApplication lvItem in this.lstIgnoredApplications.SelectedItems)
                RemoveIgnoredApplication(lvItem.Name);
            ApplicationManager.Instance.SaveApplications();
            BindIgnoredApplications();
        }

        private void RemoveIgnoredApplication(string Name)
        {
            ApplicationManager.Instance.RemoveApplication(ApplicationManager.Instance.GetIgnoredApplications().Single(a => a.Name == Name));
        }

        private void btnEditIgnoredApp_Click(object sender, RoutedEventArgs e)
        {
            IgnoredApplication ia = this.lstIgnoredApplications.SelectedItem as IgnoredApplication;
            if (ia == null) return;
            if (IgnoredCustomFlyout != null)
                IgnoredCustomFlyout(this, new ApplicationChangedEventArgs(ia));
        }

        private void btnAddIgnoredApp_Click(object sender, RoutedEventArgs e)
        {
            this.lstIgnoredApplications.SelectedIndex = -1;
            if (IgnoredRuningFlyout != null)
                IgnoredRuningFlyout(this, new ApplicationChangedEventArgs());
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
    }

    [ValueConversion(typeof(MatchUsing), typeof(string))]
    public class DataConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            MatchUsing mu = (MatchUsing)value;
            switch (mu)
            {
                case MatchUsing.All:
                    return "所有程序";
                case MatchUsing.ExecutableFilename:
                    return "匹配类型： 文件名";
                case MatchUsing.WindowClass:
                    return "匹配类型： 窗口类名称";
                case MatchUsing.WindowTitle:
                    return "匹配类型： 窗口标题";
                default: return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
          
        }
    }
    [ValueConversion(typeof(bool), typeof(string))]
    public class BoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool v = (bool)value;
            if (v) return "是";
            else return "否";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string strValue = value as string;
            switch (strValue)
            {
                case "是": return true;
                case "否": return false;
                default: return DependencyProperty.UnsetValue;
            }

        }
    }
}
