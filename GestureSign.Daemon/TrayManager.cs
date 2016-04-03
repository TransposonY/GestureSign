using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using GestureSign.Common;
using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.Common.UI;
using GestureSign.Daemon.Input;
using GestureSign.Daemon.Properties;

namespace GestureSign.Daemon
{
    public class TrayManager : ILoadable, ITrayManager
    {

        #region Private Variables

        // Create variable to hold the only allowed instance of this class
        static readonly TrayManager _Instance = new TrayManager();

        #endregion

        #region Controls Initialization

        private NotifyIcon _trayIcon;
        private ContextMenu _trayMenu;
        private MenuItem _trainingModeMenuItem;
        private MenuItem _disableGesturesMenuItem;
        private MenuItem _controlPanelMenuItem;
        private MenuItem _exitGestureSignMenuItem;

        #endregion

        #region Private Methods

        private void SetupTrayIconAndTrayMenu()
        {
            _trayIcon = new NotifyIcon();
            _trayMenu = new ContextMenu();
            _trainingModeMenuItem = new MenuItem();
            _disableGesturesMenuItem = new MenuItem();
            _controlPanelMenuItem = new MenuItem();
            _exitGestureSignMenuItem = new MenuItem();

            // Tray Icon
            _trayIcon.ContextMenu = _trayMenu;
            _trayIcon.Text = "GestureSign";
            _trayIcon.DoubleClick += (o, e) => { TrayIcon_Click(o, (MouseEventArgs)e); };
            _trayIcon.Click += (o, e) => { TrayIcon_Click(o, (MouseEventArgs)e); };
            _trayIcon.Icon = Resources.normal_daemon;

            // Tray Menu
            _trayMenu.MenuItems.AddRange(new MenuItem[] { _trainingModeMenuItem, _disableGesturesMenuItem, new MenuItem("-"), _controlPanelMenuItem, new MenuItem("-"), _exitGestureSignMenuItem });
            _trayMenu.Name = "TrayMenu";
            //TrayMenu.Size = new Size(194, 82);
            //TrayMenu.Opened += (o, e) => { Input.TouchCapture.Instance.DisableTouchCapture(); };
            //TrayMenu.Closed += (o, e) => { Input.TouchCapture.Instance.EnableTouchCapture(); };

            // Training Mode Menu Item
            //miTrainingMode.CheckOnClick = true;
            _trainingModeMenuItem.Name = "TrainingModeMenuItem";
            //miTrainingMode.Size = new Size(193, 22);
            _trainingModeMenuItem.Text = LocalizationProvider.Instance.GetTextValue("TrayMenu.TrainingMode");
            _trainingModeMenuItem.Click += (o, e) =>
            {
                TouchCapture.Instance.Mode = TouchCapture.Instance.Mode != CaptureMode.Training
                    ? CaptureMode.Training
                    : CaptureMode.Normal;
            };

            // Disable Gestures Menu Item
            _disableGesturesMenuItem.Checked = false;
            //miDisableGestures.CheckOnClick = true;
            _disableGesturesMenuItem.Name = "DisableGesturesMenuItem";
            //miDisableGestures.Size = new Size(193, 22);
            _disableGesturesMenuItem.Text = LocalizationProvider.Instance.GetTextValue("TrayMenu.Disable");
            _disableGesturesMenuItem.Click += (o, e) => { ToggleDisableGestures(); };


            // Control Panel Menu Item
            _controlPanelMenuItem.Name = "ControlPanel";
            //_controlPanelMenuItem.Size = new Size(193, 22);
            _controlPanelMenuItem.Text = LocalizationProvider.Instance.GetTextValue("TrayMenu.ControlPanel");
            _controlPanelMenuItem.Click += (o, e) =>
            {
                bool createdSetting;
                using (new Mutex(false, "GestureSignControlPanel", out createdSetting)) { }
                if (createdSetting)
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSign.exe");
                    if (File.Exists(path))
                        using (Process daemon = new Process())
                        {
                            try
                            {
                                daemon.StartInfo.FileName = path;

                                //daemon.StartInfo.UseShellExecute = false;
                                daemon.Start();
                                daemon.WaitForInputIdle(500);
                            }
                            catch (Exception exception)
                            {
                                Logging.LogException(exception);
                                MessageBox.Show(exception.ToString(),
                                    LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                            }

                        }
                }
                NamedPipe.SendMessageAsync("MainWindow", "GestureSignControlPanel");
            };

            _exitGestureSignMenuItem.Name = "ExitGestureSign";
            //miExitGestureSign.Size = new Size(193, 22);
            _exitGestureSignMenuItem.Text = LocalizationProvider.Instance.GetTextValue("TrayMenu.Exit");
            _exitGestureSignMenuItem.Click += async (o, e) =>
            {
                await NamedPipe.SendMessageAsync("Exit", "GestureSignControlPanel", false);
                Application.Exit();
            };
        }

        private void TrayIcon_Click(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (e.Clicks == 2)
                        if (Input.TouchCapture.Instance.Mode == CaptureMode.Training)
                        {
                            Input.TouchCapture.Instance.Mode = CaptureMode.Normal;
                        }
                        else ToggleDisableGestures();
                    break;
                case MouseButtons.Right:
                    break;
                case MouseButtons.Middle:
                    ToggleDisableGestures();
                    break;
            }
        }

        #endregion

        #region Constructors

        protected TrayManager()
        {
            TouchCapture.Instance.ModeChanged += CaptureMode_Changed;
            Application.ApplicationExit += Application_ApplicationExit;
        }
        #endregion


        #region Public Properties

        public static TrayManager Instance
        {
            get { return _Instance; }
        }

        public bool TrayIconVisible
        {
            get { return _trayIcon.Visible; }
            set { _trayIcon.Visible = value; }
        }

        public void Load()
        {
            SetupTrayIconAndTrayMenu();
            _trayIcon.Visible = AppConfig.ShowTrayIcon;
            if (AppConfig.ShowBalloonTip)
                _trayIcon.ShowBalloonTip(1000, LocalizationProvider.Instance.GetTextValue("TrayMenu.BalloonTipTitle"),
                    LocalizationProvider.Instance.GetTextValue("TrayMenu.BalloonTip"), ToolTipIcon.Info);

        }

        #endregion

        #region Events

        void Application_ApplicationExit(object sender, EventArgs e)
        {
            if (_trayIcon != null) _trayIcon.Visible = false;
            Environment.Exit(Environment.ExitCode);
        }


        protected void CaptureMode_Changed(object sender, ModeChangedEventArgs e)
        {
            // Update tray icon based on new state
            if (e.Mode == CaptureMode.UserDisabled)
            {
                _trainingModeMenuItem.Enabled = false;
                _disableGesturesMenuItem.Checked = true;
                _trayIcon.Icon = Resources.stop;
            }
            else
            {
                _trainingModeMenuItem.Enabled = true;
                _disableGesturesMenuItem.Checked = false;
                // Consider state of Training Mode and load according icon
                if (e.Mode == CaptureMode.Training)
                {
                    _trayIcon.Icon = Resources.add;
                    _trainingModeMenuItem.Checked = true;
                }
                else
                {
                    _trayIcon.Icon = Resources.normal_daemon;
                    _trainingModeMenuItem.Checked = false;
                }
            }
        }

        #endregion

        #region Public Methods

        public void ToggleDisableGestures()
        {
            TouchCapture.Instance.ToggleUserDisableTouchCapture();
        }

        #endregion

    }
}
