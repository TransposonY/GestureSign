using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using GestureSign.Common;
using GestureSign.Common.Applications;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System.Linq;
using Point = System.Drawing.Point;

namespace GestureSign.UI
{
    /// <summary>
    /// CustomApplicationsFlyout.xaml 的交互逻辑
    /// </summary>
    public partial class CustomApplicationsFlyout : Flyout
    {
        public static event EventHandler OpenIgnoredRuningFlyout;
        public static event EventHandler RefreshIgnoredApplications;
        public static event EventHandler RefreshApplications;


        private bool EditMode;
        private IApplication CurrentApplication;
        private bool isUserApp;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out Point lpPoint);


        public CustomApplicationsFlyout()
        {
            InitializeComponent();
            crosshairMain.CrosshairDragged += crosshairMain_CrosshairDragged;
            crosshairMain.CrosshairDragging += crosshairMain_CrosshairDragging;
            IgnoredApplications.ShowIgnoredCustomFlyout += ShowEditApplicationFlyout;
            AvailableAction.ShowEditApplicationFlyout += ShowEditApplicationFlyout;
            RuningApplicationsFlyout.RuningAppSelectionChanged += RuningApplicationsFlyout_RuningAppSelectionChanged;
        }


        private void Flyout_ClosingFinished(object sender, RoutedEventArgs e)
        {
            ClearManualFields();
        }
        void ShowEditApplicationFlyout(object sender, ApplicationChangedEventArgs e)
        {
            CurrentApplication = e.Application;
            //CurrentApplication may be null or IgnoredApplication
            isUserApp = CurrentApplication is UserApplication;

            if (isUserApp)
            {
                ApplicationNameTextBox.Text = CurrentApplication.Name;
                GroupNameTextBox.Text = CurrentApplication.Group;
            }

            chkAllowSingleStroke.Visibility = ApplicationNameTextBlock.Visibility = ApplicationNameTextBox.Visibility =
                   GroupNameTextBlock.Visibility = GroupNameTextBox.Visibility =
                  isUserApp ? Visibility.Visible : Visibility.Collapsed;

            Theme = isUserApp ? FlyoutTheme.Adapt : FlyoutTheme.Inverse;
            if (CurrentApplication != null)
                SetFields(CurrentApplication.MatchString, CurrentApplication.MatchUsing, CurrentApplication.IsRegEx);

            IsOpen = true;
        }
        void RuningApplicationsFlyout_RuningAppSelectionChanged(object sender, ApplicationListViewItem e)
        {
            if (e != null)
            {
                txtFile.Text = e.WindowFilename;
                txtClass.Text = e.WindowClass;
                txtTitle.Text = e.WindowTitle;
            }
        }
        private void ShowRunningButton_Click(object sender, RoutedEventArgs e)
        {
            if (OpenIgnoredRuningFlyout != null) OpenIgnoredRuningFlyout(this, new EventArgs());
        }


        void crosshairMain_CrosshairDragging(object sender, MouseEventArgs e)
        {
            Point cursorPosition; //(e.OriginalSource as Image).PointToScreen(e.GetPosition(null));
            GetCursorPos(out cursorPosition);
            if (Visibility == Visibility.Visible && chkCrosshairHide.IsChecked.Value)
                Opacity = 0.00;
            try
            {
                txtFile.Text = Path.GetFileName(ApplicationManager.Instance.GetWindowFromPoint(cursorPosition).Process.MainModule.FileName);
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
                Opacity = 1.00;
        }



        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog { Filter = "可执行文件|*.exe" };
            if (op.ShowDialog().Value)
            {
                txtClass.Text = txtTitle.Text = "";
                txtFile.Text = op.SafeFileName;
            }
        }

        private void btnAddCustom_Click(object sender, RoutedEventArgs e)
        {
            MatchUsing matchUsing;
            string matchString;

            if (RadioButton1.IsChecked.Value)
            {
                matchUsing = MatchUsing.ExecutableFilename;
                matchString = txtFile.Text.Trim();
            }
            else if (RadioButton2.IsChecked.Value)
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
                UIHelper.GetParentWindow(this).ShowMessageAsync("字段为空", "匹配字段不能为空，请重新输入匹配字段", settings: new MetroDialogSettings { AffirmativeButtonText = "确定" });
                return;
            }
            try
            {
                if (chkPattern.IsChecked.Value)
                    System.Text.RegularExpressions.Regex.IsMatch(matchString, "teststring");
            }
            catch
            {
                UIHelper.GetParentWindow(this).ShowMessageAsync("格式错误", "正则表达式格式错误，请重新检查", settings: new MetroDialogSettings { AffirmativeButtonText = "确定" });
                return;
            }
            string name;
            if (isUserApp)
            {
                name = ApplicationNameTextBox.Text.Trim();
                string groupName = GroupNameTextBox.Text.Trim();
                if (name.Length == 0)
                {
                    UIHelper.GetParentWindow(this).ShowMessageAsync("无程序名", "请定义程序名", settings: new MetroDialogSettings { AffirmativeButtonText = "确定" });
                    return;
                }
                if (!name.Equals(CurrentApplication.Name) && ApplicationManager.Instance.ApplicationExists(name))
                {
                    UIHelper.GetParentWindow(this).ShowMessageAsync("该程序名已经存在", "程序名称已经存在，请输入其他名字", settings: new MetroDialogSettings { AffirmativeButtonText = "确定" });
                    return;
                }
                CurrentApplication.Name = name;
                CurrentApplication.Group = groupName;
                CurrentApplication.MatchUsing = matchUsing;
                CurrentApplication.MatchString = matchString;
                CurrentApplication.IsRegEx = chkPattern.IsChecked.Value;
                CurrentApplication.AllowSingleStroke = chkAllowSingleStroke.IsChecked.Value;
                if (RefreshApplications != null) RefreshApplications(this, EventArgs.Empty);
            }
            else
            {
                name = matchUsing + "$" + matchString;
                if (!name.Equals(CurrentApplication.Name) && ApplicationManager.Instance.GetIgnoredApplications().Any(app => app.Name.Equals(name)))
                {
                    UIHelper.GetParentWindow(this).ShowMessageAsync("该忽略程序已存在", "该忽略程序已存在，请重新输入匹配字段", settings: new MetroDialogSettings { AffirmativeButtonText = "确定" });
                    return;
                }

                if (EditMode) { ApplicationManager.Instance.RemoveApplication(CurrentApplication); }
                ApplicationManager.Instance.AddApplication(new IgnoredApplication(name, matchUsing, matchString, chkPattern.IsChecked.Value, true));
                if (RefreshIgnoredApplications != null) RefreshIgnoredApplications(this, EventArgs.Empty);
            }
            ApplicationManager.Instance.SaveApplications();
            EditMode = false;
            IsOpen = false;
        }

        public void ClearManualFields()
        {
            GroupNameTextBox.Text = ApplicationNameTextBox.Text = txtClass.Text = txtTitle.Text = txtFile.Text = "";
            CurrentApplication = null;
            chkPattern.IsChecked = false;
        }

        public void SetFields(string matchString, MatchUsing matchUsing, bool isRegEx)
        {
            EditMode = true;
            chkPattern.IsChecked = isRegEx;
            switch (matchUsing)
            {
                case MatchUsing.ExecutableFilename:
                    RadioButton1.IsChecked = true;
                    txtFile.Text = matchString;
                    break;
                case MatchUsing.WindowClass:
                    RadioButton2.IsChecked = true;
                    txtClass.Text = matchString;
                    break;
                case MatchUsing.WindowTitle:
                    RadioButton3.IsChecked = true;
                    txtTitle.Text = matchString;
                    break;
            }
        }


    }
}
