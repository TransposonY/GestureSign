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

namespace GestureSign.UI
{
    /// <summary>
    /// Options.xaml 的交互逻辑
    /// </summary>
    public partial class Options : UserControl
    {
        Color _VisualFeedbackColor;
        public Options()
        {
            InitializeComponent();
            LoadSettings();
        }

        #region Custom Events

        // Create event to notify subscribers that options have been saved
        static public event OptionsSavedEventHandler OptionsChanged;

        protected virtual void OnOptionsChanged()
        {
            if (OptionsChanged != null) OptionsChanged(this, new EventArgs());
        }

        #endregion
        private bool LoadSettings()
        {
            try
            {
                // Try to load saved settings
              //  GestureSign.Configuration.AppConfig.Reload();

                _VisualFeedbackColor = GestureSign.Configuration.AppConfig.VisualFeedbackColor;
                VisualFeedbackWidthSlider.Value = GestureSign.Configuration.AppConfig.VisualFeedbackWidth;
                MinimumPointDistanceSlider.Value = GestureSign.Configuration.AppConfig.MinimumPointDistance;
                chkWindowsStartup.IsChecked = GetStartupStatus();
                OpacitySlider.Value = GestureSign.Configuration.AppConfig.Opacity;

                return true;
            }
            catch
            {
                MessageBox.Show("无法载入设置", "发生错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
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
            cdColorPicker.Color = System.Drawing.Color.FromArgb(_VisualFeedbackColor.A, _VisualFeedbackColor.R, _VisualFeedbackColor.G, _VisualFeedbackColor.B);

            // Show color picker dialog
            if (cdColorPicker.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            // Change color of visual feedback and refresh example
            _VisualFeedbackColor.A = cdColorPicker.Color.A;
            _VisualFeedbackColor.R = cdColorPicker.Color.R;
            _VisualFeedbackColor.B = cdColorPicker.Color.B;
            _VisualFeedbackColor.G = cdColorPicker.Color.G;
            GestureSign.Configuration.AppConfig.VisualFeedbackColor = _VisualFeedbackColor;
            UpdateVisualFeedbackExample();
        }

        private void VisualFeedbackWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateVisualFeedbackExample();

          GestureSign.Configuration.AppConfig.VisualFeedbackWidth = (int)Math.Round(VisualFeedbackWidthSlider.Value);
            OnOptionsChanged();
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Change opacity display text with new value
            OpacityText.Text = String.Format("不透明度: {0}%", GetAlphaPercentage(OpacitySlider.Value));
            GestureSign.Configuration.AppConfig.Opacity = OpacitySlider.Value;
            OnOptionsChanged();
        }

        private void MinimumPointDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.OldValue == 0) return;
            GestureSign.Configuration.AppConfig.MinimumPointDistance = (int)Math.Round(e.NewValue);
            OnOptionsChanged();
        }
        private void UserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            GestureSign.Configuration.AppConfig.Save();
            OnOptionsChanged();
        }



        private void UpdateVisualFeedbackExample()
        {
            // Show new example graphic if visual feedback is enabled
            if (VisualFeedbackWidthSlider.Value > 0)
            {
                VisualFeedbackExample.Stroke = new SolidColorBrush(_VisualFeedbackColor);

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
            if (System.IO.File.Exists(lnkPath))
                return true;
            else return false;
        }


        private int GetAlphaPercentage(double Alpha)
        {
            return (int)Math.Round(Alpha * 100d);
        }

        private void DisableIncompatibleControls()
        {
            OpacitySlider.IsEnabled = ManagedWinapi.Windows.DesktopWindowManager.IsCompositionEnabled();
        }

        private void chkWindowsStartup_Click(object sender, RoutedEventArgs e)
        {
            string lnkPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Startup) +
                "\\" + Application.ResourceAssembly.GetName().Name + ".lnk";
            try
            {
                if (chkWindowsStartup.IsChecked.Value)
                {
                    CreateLnk(lnkPath);
                }
                else System.IO.File.Delete(lnkPath);
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        private void CreateLnk(string lnkPath)
        {
            if (!System.IO.File.Exists(lnkPath))
            {
                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                IWshRuntimeLibrary.IWshShortcut shortCut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(lnkPath);
                shortCut.TargetPath = Application.ResourceAssembly.Location;// System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                shortCut.WindowStyle = 7;
                shortCut.Arguments = "";
                shortCut.Description = Application.ResourceAssembly.GetName().Version.ToString();// Application.ProductName + Application.ProductVersion;
                shortCut.IconLocation = Application.ResourceAssembly.Location;// Application.ExecutablePath;
                shortCut.WorkingDirectory = System.IO.Directory.GetCurrentDirectory();// Application.ResourceAssembly.;
                shortCut.Save();
            }
        }

    }
}
