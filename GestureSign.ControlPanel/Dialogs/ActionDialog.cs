﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using GestureSign.Common.Applications;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using GestureSign.ControlPanel.Common;
using MahApps.Metro.Controls;
using ManagedWinapi.Windows;
using Microsoft.Win32;
using Point = System.Drawing.Point;

namespace GestureSign.ControlPanel.Dialogs
{
    public partial class ActionDialog : MetroWindow
    {
        public ActionDialog()
        {
            InitializeComponent();
        }

        //Add action by new gesture
        public ActionDialog(string currentGestureName) : this()
        {
            _gestureName = currentGestureName;
        }

        //Add action by existing gesture
        public ActionDialog(string currentGestureName, IApplication selectedApplication) : this(currentGestureName)
        {
            _selectedApplication = selectedApplication;
        }

        //Edit action
        public ActionDialog(IAction selectedAction, IApplication selectedApplication) : this()
        {
            Title = LocalizationProvider.Instance.GetTextValue("ActionDialog.EditActionTitle");
            _selectedApplication = selectedApplication;
            _currentAction = selectedAction;
            if (_currentAction != null)
            {
                _gestureName = _currentAction.GestureName;
            }
        }

        #region Private Instance Fields

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out Point lpPoint);

        private readonly IApplication _selectedApplication;
        IApplication _newApplication;
        // Create variable to hold current selected plugin
        IPluginInfo _pluginInfo;
        IAction _currentAction;
        string _gestureName;
        #endregion

        #region Public Instance Properties

        public bool IsUsingExistingApplication { get { return cmbExistingApplication.SelectedIndex != 0; } }

        public static event EventHandler<ActionChangedEventArgs> ActionsChanged;
        #endregion


        #region Events

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            BindGroupComboBox();
            BindExistingApplications();

            chCrosshair.CrosshairDragging += chCrosshair_CrosshairDragging;
            BindPlugins();
            if (_currentAction != null)
            {
                //cmbExistingApplication.SelectedItem = ApplicationManager.Instance.CurrentApplication;
                foreach (object comboItem in cmbPlugins.Items)
                {
                    IPluginInfo pluginInfo = (IPluginInfo)comboItem;

                    if (pluginInfo.Class == _currentAction.PluginClass && pluginInfo.Filename == _currentAction.PluginFilename)
                    {
                        cmbPlugins.SelectedIndex = cmbPlugins.Items.IndexOf(comboItem);
                        return;
                    }
                }
            }

            NamedPipe.SendMessageAsync("DisableTouchCapture", "GestureSignDaemon");
        }

        private void availableGesturesComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            SelectCurrentGesture(_gestureName);
        }

        protected void chCrosshair_CrosshairDragging(object sender, MouseEventArgs e)
        {
            Point cursorPosition;
            GetCursorPos(out cursorPosition);
            SystemWindow window = SystemWindow.FromPointEx(cursorPosition.X, cursorPosition.Y, true, true);

            // Set MatchUsings
            MatchUsing muCustom = matchUsingRadio.MatchUsing;
            // Which screen are we changing
            try
            {
                switch (muCustom)
                {
                    case MatchUsing.WindowClass:
                        txtMatchString.Text = window.ClassName;

                        break;
                    case MatchUsing.WindowTitle:
                        txtMatchString.Text = window.Title;

                        break;
                    case MatchUsing.ExecutableFilename:
                        txtMatchString.Text = window.Process.MainModule.ModuleName;//.FileName;
                        txtMatchString.SelectionStart = txtMatchString.Text.Length;
                        break;
                }

                // Set application name from filename
                txtApplicationName.Text = window.Process.MainModule.FileVersionInfo.FileDescription;
            }
            catch (Exception ex) { txtApplicationName.Text = ex.Message; }

        }


        private void cmdDone_Click(object sender, RoutedEventArgs e)
        {
            if (SaveApplication())
            {
                if (SaveAction())
                {
                    Close();
                }
            }
        }

        private void cmdBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofdExecutable = new OpenFileDialog
            {
                Filter = LocalizationProvider.Instance.GetTextValue("ActionDialog.ExecutableFile") + "|*.exe"
            };
            if (ValidateFilepath(txtMatchString.Text.Trim()))
            {
                ofdExecutable.InitialDirectory = Path.GetDirectoryName(txtMatchString.Text);
                ofdExecutable.FileName = Path.GetFileName(txtMatchString.Text);
            }
            if (ofdExecutable.ShowDialog().Value)
            {
                txtApplicationName.Text = txtMatchString.Text = Path.GetFileName(ofdExecutable.FileName);

            }
        }

        private async void MetroWindow_Closed(object sender, EventArgs e)
        {
            // User canceled dialog, re-enable gestures and hide form

            await NamedPipe.SendMessageAsync("EnableTouchCapture", "GestureSignDaemon");
        }

        private void cmbPlugins_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (cmbPlugins.SelectedItem == null) return;
            LoadPlugin((IPluginInfo)cmbPlugins.SelectedItem);
        }

        private void cmdCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion


        #region Private Instance Methods

        private bool SaveApplication()
        {

            if (IsUsingExistingApplication)
            {
                if (_currentAction != null && cmbExistingApplication.SelectedItem as IApplication != _selectedApplication)
                {
                    if (((IApplication)cmbExistingApplication.SelectedItem).Actions.Any(a => a.Name.Equals(TxtActionName.Text.Trim())))
                    {
                        return
                            ShowErrorMessage(
                                LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExistsTitle"),
                                String.Format(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExists"),
                                    TxtActionName.Text.Trim(), ((IApplication)cmbExistingApplication.SelectedItem).Name));
                    }
                }
                ApplicationManager.Instance.CurrentApplication = (IApplication)cmbExistingApplication.SelectedItem;
                return true;
            }
            else
            {
                string appName = txtApplicationName.Text.Trim();

                // Make sure we have a valid application name
                if (String.IsNullOrWhiteSpace(appName))
                    return
                        ShowErrorMessage(
                            LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoApplicationNameTitle"),
                            LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoApplicationName"));

                if (_currentAction == null && ApplicationManager.Instance.ApplicationExists(appName))
                    return ShowErrorMessage(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.AppExistsTitle"),
                        LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.AppExists"));


                string matchString = txtMatchString.Text.Trim();
                // Make sure the user entered a match string
                if (String.IsNullOrEmpty(matchString))
                    return
                        ShowErrorMessage(
                            LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoMatchStringTitle"),
                            LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoMatchString"));

                _newApplication = new UserApplication
                {
                    InterceptTouchInput = InterceptTouchInputCheckBox.IsChecked.Value,
                    AllowSingleStroke = AllowSingleCheckBox.IsChecked.Value,
                    Name = appName,
                    Group = GroupComboBox.Text.Trim(),
                    MatchString = matchString,
                    MatchUsing = matchUsingRadio.MatchUsing,
                    IsRegEx = chkRegex.IsChecked.Value
                };


                ApplicationManager.Instance.CurrentApplication = _newApplication;
                return true;
            }
        }


        private bool ShowErrorMessage(string title, string message)
        {
            MessageFlyoutText.Text = message;
            MessageFlyout.Header = title;
            MessageFlyout.IsOpen = true;
            return false;
        }


        private void BindExistingApplications()
        {
            // Clear existing items
            cmbExistingApplication.Items.Clear();

            // Add generic application listview item
            IApplication allApplicationsItem = ApplicationManager.Instance.GetGlobalApplication();

            IApplication[] existingApplications = ApplicationManager.Instance.GetAvailableUserApplications();

            var addNewApplicationItem = new UserApplication()
            {
                Name = LocalizationProvider.Instance.GetTextValue("ActionDialog.NewApplication")
            };

            // Add application items to the combobox
            cmbExistingApplication.Items.Add(addNewApplicationItem);
            cmbExistingApplication.Items.Add(allApplicationsItem);
            foreach (IApplication app in existingApplications)
                cmbExistingApplication.Items.Add(app);

            // Select new applications
            cmbExistingApplication.SelectedItem = _selectedApplication ?? allApplicationsItem;
        }

        private void SelectCurrentGesture(string currentGesture)
        {
            if (currentGesture == null) return;

            foreach (GestureItem item in availableGesturesComboBox.Items)
            {
                if (item.Name == currentGesture)
                    availableGesturesComboBox.SelectedIndex = availableGesturesComboBox.Items.IndexOf(item);
            }
        }

        private void BindGroupComboBox()
        {
            GroupComboBox.ItemsSource = ApplicationManager.Instance.Applications.Select(app => app.Group).Distinct();
        }
        #endregion

        #region Type Methods

        public static bool ValidateFilepath(string path)
        {
            if (path.Trim() == String.Empty)
                return false;

            string pathname;
            string filename;

            try
            {
                pathname = Path.GetPathRoot(path);
                filename = Path.GetFileName(path);
            }
            catch (ArgumentException)
            {
                // GetPathRoot() and GetFileName() above will throw exceptions
                // if pathname/filename could not be parsed.

                return false;
            }

            // Make sure the filename part was actually specified
            if (filename.Trim() == String.Empty)
                return false;

            // Not sure if additional checking below is needed, but no harm done
            if (pathname.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                return false;

            if (filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return false;

            return true;
        }

        #endregion






        public void RefreshApplications()
        {
            alvRunningApplications.Items.Clear();
            ThreadPool.QueueUserWorkItem(GetValidWindows);
            //  this.lstRunningApplications.Items.Clear();
        }
        private void GetValidWindows(object s)
        {
            // Get valid running windows
            var windows = SystemWindow.AllToplevelWindows.Where
                     (
                         w => w.Visible &&	// Must be a visible windows
                         w.Title != "" &&	// Must have a window title
                         IsProcessAccessible(w.Process) &&
                        Path.GetDirectoryName(w.Process.ProcessName) != Process.GetCurrentProcess().ProcessName &&	// Must not be a GestureSign window
                         (w.ExtendedStyle & WindowExStyleFlags.TOOLWINDOW) != WindowExStyleFlags.TOOLWINDOW	// Must not be a tool window
                     );
            //System.Threading.Thread.Sleep(500);
            foreach (SystemWindow sWind in windows)
            {
                alvRunningApplications.Dispatcher.BeginInvoke(new Action(() =>
               {
                   ApplicationListViewItem lItem = new ApplicationListViewItem();

                   try
                   {
                       // Store identifying information
                       lItem.WindowClass = sWind.ClassName;
                       lItem.WindowTitle = sWind.Title;
                       lItem.WindowFilename = Path.GetFileName(sWind.Process.MainModule.FileName);
                       lItem.ApplicationIcon = Imaging.CreateBitmapSourceFromHIcon(sWind.Icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                       alvRunningApplications.Items.Add(lItem);
                   }
                   catch
                   {
                   }
               }));
            }
        }


        private bool IsProcessAccessible(Process Process)
        {
            try
            {
                ProcessModule module = Process.MainModule;
                return true;
            }
            catch { return false; }
        }




        private bool SaveAction()
        {
            if (availableGesturesComboBox.SelectedItem == null)
                return
                    ShowErrorMessage(
                        LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoSelectedGestureTitle"),
                        LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoSelectedGesture"));
            // Check if we're creating a new action
            bool isNew = false;
            if (_currentAction == null)
            {
                _currentAction = new Applications.Action();
                isNew = true;
            }
            string newActionName = TxtActionName.Text.Trim();

            if (String.IsNullOrEmpty(newActionName))
                return
                    ShowErrorMessage(
                        LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoActionNameTitle"),
                        LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoActionName"));

            if (isNew)
            {
                if (ApplicationManager.Instance.CurrentApplication is GlobalApplication)
                {
                    if (ApplicationManager.Instance.IsGlobalAction(newActionName))
                    {
                        _currentAction = null;
                        return
                            ShowErrorMessage(
                                LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExistsTitle"),
                                String.Format(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExistsInGlobal"),
                                    newActionName));
                    }
                }
                else
                {
                    if (ApplicationManager.Instance.CurrentApplication.Actions.Any(a => a.Name.Equals(newActionName)))
                    {
                        _currentAction = null;
                        return
                            ShowErrorMessage(
                                LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExistsTitle"),
                                String.Format(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExists"),
                                    newActionName, ApplicationManager.Instance.CurrentApplication.Name));
                    }
                }
            }
            else
            {
                if (ApplicationManager.Instance.CurrentApplication is GlobalApplication)
                {
                    if (ApplicationManager.Instance.IsGlobalAction(newActionName) && newActionName != _currentAction.Name)
                    {
                        return
                            ShowErrorMessage(
                                LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExistsTitle"),
                                String.Format(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExistsInGlobal"),
                                    newActionName));
                    }
                }
                else
                {
                    if (ApplicationManager.Instance.CurrentApplication.Actions.Any(a => a.Name.Equals(newActionName)) && newActionName != _currentAction.Name)
                    {
                        return
                            ShowErrorMessage(
                                LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExistsTitle"),
                                String.Format(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExists"),
                                    newActionName, ApplicationManager.Instance.CurrentApplication.Name));
                    }
                }
            }

            // Store new values
            _currentAction.GestureName = (availableGesturesComboBox.SelectedItem as GestureItem).Name;
            _currentAction.Name = newActionName;
            _currentAction.PluginClass = _pluginInfo.Class;
            _currentAction.PluginFilename = _pluginInfo.Filename;
            _currentAction.ActionSettings = _pluginInfo.Plugin.Serialize();
            _currentAction.IsEnabled = true;

            if (isNew)
            {
                if (_newApplication is UserApplication)
                {
                    ApplicationManager.Instance.AddApplication(_newApplication);
                }
                // Save new action to specific application
                ApplicationManager.Instance.CurrentApplication.AddAction(_currentAction);

            }
            else
            {
                if (IsUsingExistingApplication)
                {
                    if (cmbExistingApplication.SelectedItem as IApplication != _selectedApplication)
                    {
                        _selectedApplication.RemoveAction(_currentAction);
                        ((IApplication)cmbExistingApplication.SelectedItem).AddAction(_currentAction);
                    }
                }
                else
                {
                    _selectedApplication.RemoveAction(_currentAction);
                    _newApplication.AddAction(_currentAction);
                    ApplicationManager.Instance.AddApplication(_newApplication);
                }

            }
            // Save entire list of applications
            ApplicationManager.Instance.SaveApplications();
            if (ActionsChanged != null)
                ActionsChanged(this, new ActionChangedEventArgs(ApplicationManager.Instance.CurrentApplication, _currentAction));

            return true;
        }

        private void BindPlugins()
        {
            // Compile list of PluginInfo objects to bind to drop down
            List<IPluginInfo> availablePlugins = new List<IPluginInfo>();
            availablePlugins.AddRange(PluginManager.Instance.Plugins.Where(pi => pi.Plugin.IsAction).OrderBy(pi => pi.ToString()));

            // Bind available plugin list to combo box
            // cmbPlugins.DisplayMemberPath = "DisplayText";
            cmbPlugins.ItemsSource = availablePlugins;
            cmbPlugins.SelectedIndex = 0;
        }

        private void LoadPlugin(IPluginInfo selectedPlugin)
        {
            // Try to load plugin, and set current plugin to newly selected plugin
            _pluginInfo = selectedPlugin;

            // Set action name
            if (IsPluginMatch(_currentAction, selectedPlugin.Class, selectedPlugin.Filename))
            {
                TxtActionName.Text = _currentAction.Name;
                // Load action settings or no settings
                _pluginInfo.Plugin.Deserialize(_currentAction.ActionSettings);
            }
            else
            {
                TxtActionName.Text = GetNextActionName(_pluginInfo.Plugin.Name);
                _pluginInfo.Plugin.Deserialize("");
            }
            // Does the plugin have a graphical interface
            if (_pluginInfo.Plugin.GUI != null)
                // Show plugins graphical interface
                ShowSettings(_pluginInfo);
            else
                // There is no interface for this plugin, hide settings but leave action name input box
                HideSettings();
        }

        private string GetNextActionName(string name, int i = 1)
        {
            var actionName = i == 1 ? name : String.Format("{0}({1})", name, i);
            var selectedItem = cmbExistingApplication.SelectedItem;
            if (selectedItem != null && (IsUsingExistingApplication && ((IApplication)selectedItem).Actions.Exists(a => a.Name.Equals(actionName))))
                return GetNextActionName(name, ++i);
            return actionName;
        }

        private void ShowSettings(IPluginInfo pluginInfo)
        {
            var PluginGUI = pluginInfo.Plugin.GUI;
            if (PluginGUI.Parent != null)
            {
                SettingsContent.Content = null;
            }
            // Add settings to settings panel
            SettingsContent.Content = PluginGUI;

            SettingsContent.Height = PluginGUI.Height;
            SettingsContent.Visibility = Visibility.Visible;
        }
        private void HideSettings()
        {
            SettingsContent.Height = 0;
            SettingsContent.Visibility = Visibility.Collapsed;
        }

        private bool IsPluginMatch(IAction action, string PluginClass, string PluginFilename)
        {
            return (action != null && action.PluginClass == PluginClass && action.PluginFilename == PluginFilename);
        }

        private void RunningApplicationsPopup_Opened(object sender, EventArgs e)
        {
            RunningApplicationsPopup.PlacementRectangle = SystemParameters.MenuDropAlignment ? new Rect(Width, 0, 0, 0) : new Rect(Width / 2, 0, 0, 0);
            RefreshApplications();
        }

        private void txtMatchString_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            MatchStringPopup.IsOpen = true;
            MatchStringPopupTextBox.Focus();
        }
    }
}
