using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using GestureSign.Common;
using GestureSign.Common.UI;
using GestureSign.Common.Input;

using System.Threading;
using System.Diagnostics;
using GestureSign.Common.Localization;

namespace GestureSign.Daemon
{
    public class TrayManager : ILoadable, ITrayManager
    {

        #region Private Variables

        // Create variable to hold the only allowed instance of this class
        static readonly TrayManager _Instance = new TrayManager();

        #endregion

        #region Controls Initialization

        private NotifyIcon TrayIcon;
        private ContextMenuStrip TrayMenu;
        private ToolStripMenuItem miTrainingMode;
        private ToolStripMenuItem miDisableGestures;
        private ToolStripSeparator miSeperator1;
        private ToolStripMenuItem _controlPanelMenuItem;
        private ToolStripSeparator miSeperator2;
        private ToolStripMenuItem miExitGestureSign;

        #endregion

        #region Private Methods

        private void SetupTrayIconAndTrayMenu()
        {
            TrayIcon = new NotifyIcon();
            TrayMenu = new ContextMenuStrip();
            miTrainingMode = new ToolStripMenuItem();
            miDisableGestures = new ToolStripMenuItem();
            miSeperator1 = new ToolStripSeparator();
            _controlPanelMenuItem = new ToolStripMenuItem();
            miSeperator2 = new ToolStripSeparator();
            miExitGestureSign = new ToolStripMenuItem();

            // Tray Icon
            TrayIcon.ContextMenuStrip = TrayMenu;
            TrayIcon.Text = "GestureSign";
            TrayIcon.DoubleClick += (o, e) => { TrayIcon_Click(o, (MouseEventArgs)e); };
            TrayIcon.Click += (o, e) => { TrayIcon_Click(o, (MouseEventArgs)e); };
            TrayIcon.Icon = Properties.Resources.normal_daemon;

            // Tray Menu
            TrayMenu.Items.AddRange(new ToolStripItem[] { miTrainingMode, miDisableGestures, miSeperator1, _controlPanelMenuItem, miSeperator2, miExitGestureSign });
            TrayMenu.Name = "TrayMenu";
            TrayMenu.Size = new Size(194, 82);
            TrayMenu.Text = LocalizationProvider.Instance.GetTextValue("TrayMenu.Text");
            //TrayMenu.Opened += (o, e) => { Input.TouchCapture.Instance.DisableTouchCapture(); };
            //TrayMenu.Closed += (o, e) => { Input.TouchCapture.Instance.EnableTouchCapture(); };

            // Training Mode Menu Item
            miTrainingMode.CheckOnClick = true;
            miTrainingMode.Name = "TrainingModeMenuItem";
            miTrainingMode.Size = new Size(193, 22);
            miTrainingMode.Text = LocalizationProvider.Instance.GetTextValue("TrayMenu.TrainingMode");
            miTrainingMode.Click += (o, e) => { ToggleTeaching(); };

            // Disable Gestures Menu Item
            miDisableGestures.Checked = false;
            miDisableGestures.CheckOnClick = true;
            miDisableGestures.CheckState = CheckState.Unchecked;
            miDisableGestures.Name = "DisableGesturesMenuItem";
            miDisableGestures.Size = new Size(193, 22);
            miDisableGestures.Text = LocalizationProvider.Instance.GetTextValue("TrayMenu.Disable");
            miDisableGestures.Click += (o, e) => { ToggleDisableGestures(); };

            // First Seperator Menu Item
            miSeperator1.Name = "Seperator1";
            miSeperator1.Size = new Size(190, 6);




            // Control Panel Menu Item
            _controlPanelMenuItem.Name = "ControlPanel";
            _controlPanelMenuItem.Size = new Size(193, 22);
            _controlPanelMenuItem.Text = LocalizationProvider.Instance.GetTextValue("TrayMenu.ControlPanel");
            _controlPanelMenuItem.Click += (o, e) =>
            {
                bool createdSetting;
                using (new Mutex(false, "GestureSignControlPanel", out createdSetting)) { }
                if (createdSetting)
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSign.exe");
                    if (System.IO.File.Exists(path))
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
                GestureSign.Common.InterProcessCommunication.NamedPipe.SendMessageAsync("MainWindow", "GestureSignControlPanel");
            };

            // Second Seperator Menu Item
            miSeperator2.Name = "Seperator2";
            miSeperator2.Size = new Size(190, 6);

            // Exit High Sign Menu Item
            miExitGestureSign.Name = "ExitGestureSign";
            miExitGestureSign.Size = new Size(193, 22);
            miExitGestureSign.Text = LocalizationProvider.Instance.GetTextValue("TrayMenu.Exit");
            miExitGestureSign.Click += async (o, e) =>
            {
                await GestureSign.Common.InterProcessCommunication.NamedPipe.SendMessageAsync("Exit", "GestureSignControlPanel");
                Application.Exit();
            };
        }

        private void TrayIcon_Click(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (e.Clicks == 2)
                        if (miTrainingMode.Checked) StopTeaching();
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
            Input.TouchCapture.Instance.StateChanged += new StateChangedEventHandler(CaptureState_Changed);
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
            get { return TrayIcon.Visible; }
            set { TrayIcon.Visible = value; }
        }

        public void Load()
        {
            SetupTrayIconAndTrayMenu();
            TrayIcon.Visible = GestureSign.Common.Configuration.AppConfig.ShowTrayIcon;
            if (GestureSign.Common.Configuration.AppConfig.ShowBalloonTip)
                this.TrayIcon.ShowBalloonTip(1000, LocalizationProvider.Instance.GetTextValue("TrayMenu.BalloonTipTitle"),
                    LocalizationProvider.Instance.GetTextValue("TrayMenu.BalloonTip"), ToolTipIcon.Info);

        }

        #endregion

        #region Events

        void Application_ApplicationExit(object sender, EventArgs e)
        {
            if (TrayIcon != null) TrayIcon.Visible = false;
            Environment.Exit(Environment.ExitCode);
        }


        protected void CaptureState_Changed(object sender, StateChangedEventArgs e)
        {
            // Update tray icon based on new state
            if (e.State == CaptureState.UserDisabled)
            {
                miTrainingMode.Enabled = false;
                miDisableGestures.Checked = true;
                TrayIcon.Icon = Properties.Resources.stop;
            }
            else
            {
                miTrainingMode.Enabled = true;
                miDisableGestures.Checked = false;
                // Consider state of Training Mode and load according icon
                if (miTrainingMode.Checked)
                    TrayIcon.Icon = Properties.Resources.add;
                else
                    TrayIcon.Icon = Properties.Resources.normal_daemon;
            }
        }

        #endregion

        #region Public Methods


        public void ToggleTeaching()
        {
            // Toggle teaching mode, unless is UserDisable gestures mode
            if (Input.TouchCapture.Instance.State != CaptureState.UserDisabled)
                if (GestureSign.Common.Configuration.AppConfig.Teaching)
                    StopTeaching();
                else
                    StartTeaching();
            else ToggleDisableGestures();
        }

        public void ToggleDisableGestures()
        {
            Input.TouchCapture.Instance.ToggleUserDisableTouchCapture();
        }

        public void StartTeaching()
        {
            GestureSign.Common.Configuration.AppConfig.Teaching = miTrainingMode.Checked = true;

            // Assign resource icon as tray icon	
            TrayIcon.Icon = Properties.Resources.add;
        }

        public void StopTeaching()
        {
            GestureSign.Common.Configuration.AppConfig.Teaching = miTrainingMode.Checked = false;

            // Assign resource icon as tray icon
            TrayIcon.Icon = Properties.Resources.normal_daemon;
        }

        #endregion

    }
}
