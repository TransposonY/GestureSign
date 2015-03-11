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

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using GestureSign.Common.Drawing;
using GestureSign.Common.Gestures;

namespace GestureSign.UI
{
    /// <summary>
    /// AvailableGestures.xaml 的交互逻辑
    /// </summary>
    public partial class AvailableGestures : UserControl, IDisposable
    {

        public static event EventHandler StartCapture;
        public static event EventHandler GestureChanged;


        public AvailableGestures()
        {
            InitializeComponent();
            BindGestures();
            GestureDefinition.GesturesChanged += GestureDefinition_GesturesChanged;
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


        void GestureDefinition_GesturesChanged(object sender, EventArgs e)
        {
            BindGestures();
        }

        private void lstAvailableGestures_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.btnEditGesture.IsEnabled = this.btnDelGesture.IsEnabled = lstAvailableGestures.SelectedItems.Count > 0;
        }

        private async void btnDelGesture_Click(object sender, RoutedEventArgs e)
        {

            // Make sure at least one item is selected
            if (lstAvailableGestures.SelectedItems.Count == 0)
            {
                await Common.UI.WindowsHelper.GetParentWindow(this).ShowMessageAsync("请选择", "删除前需要选择至少一项手势", MessageDialogStyle.Affirmative,
                   new MetroDialogSettings() { AffirmativeButtonText = "确定" });
                //MessageBox.Show("You must select an item before deleting", "Please Select an Item", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (await Common.UI.WindowsHelper.GetParentWindow(this).ShowMessageAsync("删除确认", "确定删除这手势吗？",
                MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "确定", NegativeButtonText = "取消", AnimateHide = false }) == MessageDialogResult.Affirmative)
            {
                foreach (GestureItem listItem in lstAvailableGestures.SelectedItems)
                    Gestures.GestureManager.Instance.DeleteGesture(listItem.Name);
                if (GestureChanged != null)
                    GestureChanged(this, new EventArgs());

                BindGestures();
                Gestures.GestureManager.Instance.SaveGestures();
            }
        }
        private async void btnEditGesture_Click(object sender, RoutedEventArgs e)
        {
            // Make sure at least one item is selected
            if (lstAvailableGestures.SelectedItems.Count == 0)
            {
                await Common.UI.WindowsHelper.GetParentWindow(this).ShowMessageAsync("请选择", "编辑前需要选择至少一项手势", MessageDialogStyle.Affirmative,
                   new MetroDialogSettings() { AffirmativeButtonText = "确定" });
                //MessageBox.Show("You must select an item before deleting", "Please Select an Item", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            UI.GestureDefinition gd = new UI.GestureDefinition(
                Gestures.GestureManager.Instance.GetNewestGestureSample(((GestureItem)lstAvailableGestures.SelectedItems[0]).Name).Points,
                ((GestureItem)lstAvailableGestures.SelectedItems[0]).Name);
            gd.ShowDialog();
            if (GestureChanged != null)
                GestureChanged(this, new EventArgs());
        }

        private async void btnAddGesture_Click(object sender, RoutedEventArgs e)
        {
            if (await Common.UI.WindowsHelper.GetParentWindow(this).ShowMessageAsync(
                  "新建手势", "请点击“确定”后画出一个手势",
                  MessageDialogStyle.AffirmativeAndNegative,
                  new MetroDialogSettings() { AffirmativeButtonText = "确定", NegativeButtonText = "取消" }) == MessageDialogResult.Affirmative)
            {
                if (StartCapture != null)
                    StartCapture(this, new EventArgs());
            }
        }
        private void ImportGestureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofdGestures = new Microsoft.Win32.OpenFileDialog() { Filter = "手势文件|*.json", Title = "导入手势数据文件", CheckFileExists = true };
            if (ofdGestures.ShowDialog().Value)
            {
                int addcount = 0;
                List<IGesture> newGestures = Configuration.IO.FileManager.LoadObject<List<IGesture>>(ofdGestures.FileName, new Type[] { typeof(GestureSign.Gestures.Gesture) }, false);
                if (newGestures != null)
                    foreach (IGesture newGesture in newGestures)
                    {
                        if (GestureSign.Gestures.GestureManager.Instance.GestureExists(newGesture.Name))
                        {
                            var result = MessageBox.Show(String.Format("已经存在手势 \"{0}\" ，是否覆盖？", newGesture.Name), "已存在同名手势", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                            if (result == MessageBoxResult.Yes)
                            {
                                Gestures.GestureManager.Instance.DeleteGesture(newGesture.Name);
                                Gestures.GestureManager.Instance.AddGesture(newGesture);
                                addcount++;
                            }
                            else if (result == MessageBoxResult.Cancel) goto End;
                        }
                        else
                        {
                            Gestures.GestureManager.Instance.AddGesture(newGesture);
                            addcount++;
                        }
                    }
            End:
                if (addcount != 0)
                {
                    Gestures.GestureManager.Instance.SaveGestures();
                    BindGestures();
                }
                MessageBox.Show(String.Format("已添加 {0} 个手势", addcount), "导入完成");
            }
        }

        private void ExportGestureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfdGestures = new Microsoft.Win32.SaveFileDialog() { Filter = "手势文件|*.json", Title = "导出手势数据文件", AddExtension = true, DefaultExt = "json", ValidateNames = true };
            if (sfdGestures.ShowDialog().Value)
            {
                Configuration.IO.FileManager.SaveObject<List<IGesture>>(Gestures.GestureManager.Instance.Gestures, sfdGestures.FileName);
            }
        }

        #region Private Methods

        private void BindGestures()
        {
            // Clear existing gestures in list
            lstAvailableGestures.Items.Clear();
            Task task = new Task(AddAvailableGesturesItems);
            task.Start();
        }

        private void AddAvailableGesturesItems()
        {
            // Get all available gestures from gesture manager
            IEnumerable<IGesture> results = Gestures.GestureManager.Instance.Gestures.OrderBy(g => g.Name);//.GroupBy(g => g.Name).Select(g => g.First().Name);
            System.Threading.Thread.Sleep(300);
            foreach (IGesture gesture in results)
            {
                lstAvailableGestures.Dispatcher.BeginInvoke(new Action(() =>
                 {  // Create new listviewitem to represent gestures, create a thumbnail of the latest version of each gesture
                     // and add it to image list, then to the output list      gestureName
                     GestureItem newItem = new GestureItem()
                     {
                         Image = GestureImage.CreateImage(gesture.Points, new Size(65, 65)),
                         Name = gesture.Name
                     };
                     lstAvailableGestures.Items.Add(newItem);
                 }));
            }
        }

        #endregion






    }
}
