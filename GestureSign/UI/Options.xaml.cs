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

using GestureSign.Common.UI;
using Microsoft.Win32;

using Microsoft.Win32.TaskScheduler;
using System.Security.Principal;

namespace GestureSign.UI
{
    /// <summary>
    /// Options.xaml 的交互逻辑
    /// </summary>
    public partial class Options : UserControl
    {
        System.Drawing.Color _VisualFeedbackColor;
        private const string TaskName = "GestureSignAutoRunTask";

        public Options()
        {
            InitializeComponent();
            LoadSettings();
        }

        #region Custom Events

        #endregion
        private void LoadSettings()
        {
            try
            {
                // Try to load saved settings
                //  Common.Configuration.AppConfig.Reload();

                _VisualFeedbackColor = Common.Configuration.AppConfig.VisualFeedbackColor;
                VisualFeedbackWidthSlider.Value = Common.Configuration.AppConfig.VisualFeedbackWidth;
                MinimumPointDistanceSlider.Value = Common.Configuration.AppConfig.MinimumPointDistance;
                chkWindowsStartup.IsChecked = GetStartupStatus();
                OpacitySlider.Value = Common.Configuration.AppConfig.Opacity;
                chkOrderByLocation.IsChecked = Common.Configuration.AppConfig.IsOrderByLocation;
                if (Common.Configuration.AppConfig.UiAccess)
                {
                    chkCompatibilityMode.IsChecked = Common.Configuration.AppConfig.CompatibilityMode;
                    chkInterceptTouchInput.IsChecked = Common.Configuration.AppConfig.InterceptTouchInput;
                }
                else
                {
                    chkCompatibilityMode.IsChecked = chkInterceptTouchInput.IsChecked =
                        chkInterceptTouchInput.IsEnabled = false;
                }

            }
            catch
            {
                MessageBox.Show("无法载入设置", "发生错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Disable gestures while options are open
            //Input.MouseCapture.Instance.DisableMouseCapture();

            UpdateVisualFeedbackExample();
            DisableIncompatibleControls();
        }

        private void btnPickColor_Click(object sender, RoutedEventArgs e)
        {
            // Set color picker dialog color to current visual feedback color
            System.Windows.Forms.ColorDialog cdColorPicker = new System.Windows.Forms.ColorDialog();
            cdColorPicker.AllowFullOpen = true;
            cdColorPicker.Color = _VisualFeedbackColor;// System.Drawing.Color.FromArgb(_VisualFeedbackColor.A, _VisualFeedbackColor.R, _VisualFeedbackColor.G, _VisualFeedbackColor.B);

            // Show color picker dialog
            if (cdColorPicker.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            // Change color of visual feedback and refresh example
            //_VisualFeedbackColor.A = cdColorPicker.Color.A;
            //_VisualFeedbackColor.R = cdColorPicker.Color.R;
            //_VisualFeedbackColor.B = cdColorPicker.Color.B;
            //_VisualFeedbackColor.G = cdColorPicker.Color.G;
            _VisualFeedbackColor = cdColorPicker.Color;
            Common.Configuration.AppConfig.VisualFeedbackColor = _VisualFeedbackColor;
            UpdateVisualFeedbackExample();

            Common.Configuration.AppConfig.Save();
        }

        private void VisualFeedbackWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateVisualFeedbackExample();

            Common.Configuration.AppConfig.VisualFeedbackWidth = (int)Math.Round(VisualFeedbackWidthSlider.Value);

            Common.Configuration.AppConfig.Save();
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Change opacity display text with new value
            OpacityText.Text = String.Format("不透明度: {0}%", GetAlphaPercentage(OpacitySlider.Value));
            Common.Configuration.AppConfig.Opacity = OpacitySlider.Value;

            Common.Configuration.AppConfig.Save();
        }

        private void MinimumPointDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.OldValue == 0) return;
            Common.Configuration.AppConfig.MinimumPointDistance = (int)Math.Round(e.NewValue);

            Common.Configuration.AppConfig.Save();
        }


        private void UpdateVisualFeedbackExample()
        {
            // Show new example graphic if visual feedback is enabled
            if (VisualFeedbackWidthSlider.Value > 0)
            {
                VisualFeedbackExample.Stroke = new SolidColorBrush(
                    Color.FromArgb(_VisualFeedbackColor.A, _VisualFeedbackColor.R, _VisualFeedbackColor.G, _VisualFeedbackColor.B));

                VisualFeedbackWidthText.Text = String.Format("轨迹宽度 {0:0} px", VisualFeedbackWidthSlider.Value);
            }
            else
            {
                VisualFeedbackWidthText.Text = "关闭";
            }

        }



        private bool GetStartupStatus()
        {
            string lnkPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Startup) +
                "\\" + Application.ResourceAssembly.GetName().Name + ".lnk";
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        // Get the service on the local machine
                        using (TaskService ts = new TaskService())
                        {
                            var tasks = ts.RootFolder.GetTasks(new System.Text.RegularExpressions.Regex(TaskName));
                            return tasks.Count != 0 || System.IO.File.Exists(lnkPath);
                        }
                    }
                    else return System.IO.File.Exists(lnkPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }


        private int GetAlphaPercentage(double Alpha)
        {
            return (int)Math.Round(Alpha * 100d);
        }

        private void DisableIncompatibleControls()
        {
            OpacitySlider.IsEnabled = ManagedWinapi.Windows.DesktopWindowManager.IsCompositionEnabled();
        }

        private void CreateLnk(string lnkPath)
        {
            if (!System.IO.File.Exists(lnkPath))
            {
                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                IWshRuntimeLibrary.IWshShortcut shortCut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(lnkPath);
                shortCut.TargetPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSignDaemon.exe");
                //Application.ResourceAssembly.Location;// System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                shortCut.WindowStyle = 7;
                shortCut.Arguments = "";
                shortCut.Description = Application.ResourceAssembly.GetName().Version.ToString();// Application.ProductName + Application.ProductVersion;
                shortCut.IconLocation = Application.ResourceAssembly.Location;// Application.ExecutablePath;
                shortCut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;// Application.ResourceAssembly.;
                shortCut.Save();
            }
        }

        private void chkWindowsStartup_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        // Get the service on the local machine
                        using (TaskService ts = new TaskService())
                        {
                            var tasks = ts.RootFolder.GetTasks(new System.Text.RegularExpressions.Regex(TaskName));

                            if (tasks.Count == 0)
                            {
                                // Create a new task definition and assign properties
                                TaskDefinition td = ts.NewTask();
                                td.Settings.DisallowStartIfOnBatteries = false;
                                td.RegistrationInfo.Description = "Launch GestureSign when user login";

                                td.Principal.RunLevel = TaskRunLevel.Highest;

                                LogonTrigger lt = new LogonTrigger();
                                lt.Enabled = true;
                                td.Triggers.Add(lt);
                                // Create an action that will launch Notepad whenever the trigger fires
                                td.Actions.Add(new ExecAction(
                                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSignDaemon.exe"), null, AppDomain.CurrentDomain.BaseDirectory));

                                // Register the task in the root folder
                                ts.RootFolder.RegisterTaskDefinition(TaskName, td);
                            }
                        }
                    }
                    else
                    {
                        string lnkPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Startup) +
                            "\\" + Application.ResourceAssembly.GetName().Name + ".lnk";

                        CreateLnk(lnkPath);
                    }
                }
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error); }

        }

        private void chkWindowsStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                    {  // Get the service on the local machine
                        using (TaskService ts = new TaskService())
                        {
                            var tasks = ts.RootFolder.GetTasks(new System.Text.RegularExpressions.Regex(TaskName));

                            if (tasks.Count != 0)
                            {
                                ts.RootFolder.DeleteTask(TaskName);
                            }
                        }
                    }

                    string lnkPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Startup) +
                      "\\" + Application.ResourceAssembly.GetName().Name + ".lnk";

                    if (System.IO.File.Exists(lnkPath))
                        System.IO.File.Delete(lnkPath);

                }
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void chkOrderByLocation_Checked(object sender, RoutedEventArgs e)
        {
            Common.Configuration.AppConfig.IsOrderByLocation = true;
            Common.Configuration.AppConfig.Save();
        }

        private void chkOrderByLocation_Unchecked(object sender, RoutedEventArgs e)
        {
            Common.Configuration.AppConfig.IsOrderByLocation = false;
            Common.Configuration.AppConfig.Save();
        }

        private void chkInterceptTouchInput_Checked(object sender, RoutedEventArgs e)
        {
            Common.Configuration.AppConfig.InterceptTouchInput = true;
            Common.Configuration.AppConfig.Save();
        }

        private void chkInterceptTouchInput_Unchecked(object sender, RoutedEventArgs e)
        {
            Common.Configuration.AppConfig.InterceptTouchInput = false;
            Common.Configuration.AppConfig.Save();
        }

        private void chkCompatibilityMode_Checked(object sender, RoutedEventArgs e)
        {
            Common.Configuration.AppConfig.CompatibilityMode = true;
            Common.Configuration.AppConfig.Save();
        }

        private void chkCompatibilityMode_Unchecked(object sender, RoutedEventArgs e)
        {
            Common.Configuration.AppConfig.CompatibilityMode = false;
            Common.Configuration.AppConfig.Save();
        }

        private void btnOpenApplicationData_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestureSign"));
        }

    }
}
