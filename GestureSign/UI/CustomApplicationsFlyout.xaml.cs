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

using System.Runtime.CompilerServices;
using System.ComponentModel;
using GestureSign.Common.Applications;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace GestureSign.UI
{
    /// <summary>
    /// CustomApplicationsFlyout.xaml 的交互逻辑
    /// </summary>
    public partial class CustomApplicationsFlyout : Flyout
    {
        public static event EventHandler OpenIgnoredRuningFlyout;
        public event EventHandler RemoveApplication;
        public static event EventHandler BindIgnoredApplications;


        private bool EditMode = false;
        private IgnoredApplication CurrentIgnoredApplication;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        static extern bool GetCursorPos(out System.Drawing.Point lpPoint);


        public CustomApplicationsFlyout()
        {
            InitializeComponent();
            this.crosshairMain.CrosshairDragged += crosshairMain_CrosshairDragged;
            this.crosshairMain.CrosshairDragging += crosshairMain_CrosshairDragging;
            IgnoredApplications.IgnoredCustomFlyout += IgnoredApplications_IgnoredCustomFlyout;
            RuningApplicationsFlyout.OpenIgnoredCustomFlyout += RuningApplicationsFlyout_IgnoredCustomFlyout;
        }

        void RuningApplicationsFlyout_IgnoredCustomFlyout(object sender, EventArgs e)
        {
            this.IsOpen = true;
            ClearManualFields();
        }

        void IgnoredApplications_IgnoredCustomFlyout(object sender, ApplicationChangedEventArgs e)
        {
            this.IsOpen = true;
            CurrentIgnoredApplication = e.Application as IgnoredApplication;
            SetFields(e.Application.MatchString, e.Application.MatchUsing, e.Application.IsRegEx);
        }


        private void SwitchToRunning_Click(object sender, RoutedEventArgs e)
        {
            this.IsOpen = false;
            OpenIgnoredRuningFlyout(this, new EventArgs());
        }


        void crosshairMain_CrosshairDragging(object sender, MouseEventArgs e)
        {
            System.Drawing.Point cursorPosition; //(e.OriginalSource as Image).PointToScreen(e.GetPosition(null));
            GetCursorPos(out cursorPosition);
            if (this.Visibility == Visibility.Visible && chkCrosshairHide.IsChecked.Value)
                this.Opacity = 0.00;
            try
            {
                txtFile.Text = System.IO.Path.GetFileName(ApplicationManager.Instance.GetWindowFromPoint(cursorPosition).Process.MainModule.FileName);
                txtClass.Text = ApplicationManager.Instance.GetWindowFromPoint(cursorPosition).ClassName;
                txtTitle.Text = ApplicationManager.Instance.GetWindowFromPoint(cursorPosition).Title;
            }
            catch (Exception ex)
            {
                txtFile.Text = txtClass.Text = txtTitle.Text = "错误：" + ex.Message;
            }
        }

        void crosshairMain_CrosshairDragged(object sender, MouseButtonEventArgs e)
        {
            if (chkCrosshairHide.IsChecked.Value)
                this.Opacity = 1.00;
        }



        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog op = new Microsoft.Win32.OpenFileDialog();
            op.Filter = "可执行文件|*.exe";
            if (op.ShowDialog().Value)
            {
                txtClass.Text = txtTitle.Text = "";
                txtFile.Text = op.SafeFileName;
            }
        }

        private void btnAddCustom_Click(object sender, RoutedEventArgs e)
        {
            if (RemoveApplication != null)
                RemoveApplication(this, new EventArgs());
            MatchUsing matchUsing;
            string matchString;

            if (this.RadioButton1.IsChecked.Value)
            {
                matchUsing = MatchUsing.ExecutableFilename;
                matchString = txtFile.Text.Trim();
            }
            else if (this.RadioButton2.IsChecked.Value)
            {
                matchUsing = MatchUsing.WindowClass;
                matchString = txtClass.Text.Trim();
            }
            else
            {
                matchUsing = MatchUsing.WindowTitle;
                matchString = txtTitle.Text.Trim();
            }
            if (String.IsNullOrEmpty(matchString))
            {
                Common.UI.WindowsHelper.GetParentWindow(this).ShowMessageAsync("字段为空", "匹配字段不能为空，请重新输入匹配字段", settings: new MetroDialogSettings() { AffirmativeButtonText = "确定" });
                return;
            } AddIgnoredApplication(matchUsing.ToString() + matchString, matchString, matchUsing, this.chkPattern.IsChecked.Value);
            this.ClearManualFields();
            EditMode = false;
            this.IsOpen = false;
        }

        public void ClearManualFields()
        {
            txtFile.Text = "";
            txtClass.Text = "";
            txtTitle.Text = "";
            CurrentIgnoredApplication = null;
            this.chkPattern.IsChecked = false;
        }

        public void SetFields(string matchString, MatchUsing matchUsing, bool isRegEx)
        {
            EditMode = true;
            this.chkPattern.IsChecked = isRegEx;
            switch (matchUsing)
            {
                case Common.Applications.MatchUsing.ExecutableFilename:
                    this.RadioButton1.IsChecked = true;
                    this.txtFile.Text = matchString;
                    break;
                case Common.Applications.MatchUsing.WindowClass:
                    this.RadioButton2.IsChecked = true;
                    this.txtClass.Text = matchString;
                    break;
                case Common.Applications.MatchUsing.WindowTitle:
                    this.RadioButton3.IsChecked = true;
                    this.txtTitle.Text = matchString;
                    break;
            }
        }





        private void AddIgnoredApplication(String Name, String MatchString, MatchUsing MatchUsing, bool IsRegEx)
        {
            if (ApplicationManager.Instance.ApplicationExists(Name))
            {
                Common.UI.WindowsHelper.GetParentWindow(this).ShowMessageAsync("该忽略程序已存在", "该忽略程序已存在，请重新输入匹配字段", settings: new MetroDialogSettings() { AffirmativeButtonText = "确定" });
                return;

            }

            if (EditMode) { ApplicationManager.Instance.RemoveApplication(CurrentIgnoredApplication); }
            ApplicationManager.Instance.AddApplication(new IgnoredApplication(Name, MatchUsing, MatchString, IsRegEx, true));
            ApplicationManager.Instance.SaveApplications();
            BindIgnoredApplications(this, new EventArgs());
        }
    }
}
