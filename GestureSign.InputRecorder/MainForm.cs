using GestureSign.Daemon.Native;
using GestureSign.InputRecorder.Recording;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GestureSign.InputRecorder
{
    internal sealed class MainForm : Form
    {
        private const int WM_LBUTTONDOWN = 0x201, WM_LBUTTONUP = 0x202,
            WM_RBUTTONDOWN = 0x204, WM_RBUTTONUP = 0x205,
            WM_MBUTTONDOWN = 0x207, WM_MBUTTONUP = 0x208,
            WM_XBUTTONDOWN = 0x20B, WM_XBUTTONUP = 0x20C;

        /// <summary>Live paths are cleared when a new touch starts after this much idle time.</summary>
        private const int LiveClearIdleMs = 1000;

        private readonly ListView _deviceList = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HeaderStyle = ColumnHeaderStyle.Nonclickable };
        private readonly ListBox _scenarioList = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
        private readonly Label _instruction = new Label { Dock = DockStyle.Top, Height = 64, Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 11f), Padding = new Padding(4) };
        private readonly TextBox _takeName = new TextBox { Width = 240 };
        private readonly TextBox _description = new TextBox { Width = 520 };
        private readonly Button _recordButton = new Button { Text = "Record (F5)", AutoSize = true };
        private readonly Button _stopButton = new Button { Text = "Stop (F6)", AutoSize = true, Enabled = false };
        private readonly Button _saveButton = new Button { Text = "Save (Ctrl+S)", AutoSize = true, Enabled = false };
        private readonly CheckBox _autoTake = new CheckBox { Text = "Start on touch, stop when released for", AutoSize = true, Checked = true, Margin = new Padding(12, 6, 0, 3) };
        private readonly NumericUpDown _liftDelay = new NumericUpDown { Minimum = 200, Maximum = 5000, Increment = 100, Value = 800, Width = 60, Margin = new Padding(0, 4, 0, 3) };
        private readonly CheckBox _autoSave = new CheckBox { Text = "Auto-save", AutoSize = true, Margin = new Padding(12, 6, 3, 3) };
        private readonly CheckBox _mouseMoves = new CheckBox { Text = "Record mouse moves", AutoSize = true, Checked = true, Margin = new Padding(12, 6, 3, 3) };
        private readonly TextBox _folder = new TextBox { Width = 420 };
        private readonly Panel _warning = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.LightYellow, Visible = false, Padding = new Padding(4) };
        private readonly Label _warningText = new Label { Dock = DockStyle.Fill };
        private readonly Label _status = new Label { Dock = DockStyle.Top, Height = 48, Padding = new Padding(4) };
        private readonly ContactView _contactView = new ContactView { Dock = DockStyle.Fill };
        private readonly PreviewPanel _preview = new PreviewPanel { Dock = DockStyle.Fill };
        private readonly Timer _refreshTimer = new Timer { Interval = 250 };
        private readonly Timer _liftTimer = new Timer();

        private readonly Dictionary<IntPtr, DeviceMonitor> _monitors = new Dictionary<IntPtr, DeviceMonitor>();
        private InputCapture _capture;
        private RecordingSession _session;
        private RecordingSession _pending;
        private string _sessionName;
        private bool _saved;
        private bool _devicesChanged;
        private int _mouseButtonsDown;
        private bool _takeHadMouseDown;
        private int _refreshTicks;

        public MainForm()
        {
            Text = "GestureSign Input Recorder";
            ClientSize = new Size(1200, 820);
            StartPosition = FormStartPosition.CenterScreen;

            _folder.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GestureSignRecordings");

            _deviceList.Columns.Add("Kind", 90);
            _deviceList.Columns.Add("VID:PID", 80);
            _deviceList.Columns.Add("Slots/report", 85);
            _deviceList.Columns.Add("Surface", 110);
            _deviceList.Columns.Add("Reports/s", 75);
            _deviceList.Columns.Add("Reports", 70);
            _deviceList.Columns.Add("Device name", 640);
            var refreshDevices = new Button { Text = "Refresh", Dock = DockStyle.Right, Width = 80 };
            refreshDevices.Click += (s, e) => RefreshDevices();
            var deviceGroup = new GroupBox { Text = "Detected digitizers (raw input HID, usage page 0x0D)", Dock = DockStyle.Top, Height = 130 };
            deviceGroup.Controls.Add(_deviceList);
            deviceGroup.Controls.Add(refreshDevices);

            _scenarioList.Items.AddRange(Scenarios.All.Cast<object>().ToArray());
            _scenarioList.SelectedIndexChanged += (s, e) => SelectScenario();
            var scenarioGroup = new GroupBox { Text = "Scenarios", Dock = DockStyle.Left, Width = 250 };
            scenarioGroup.Controls.Add(_scenarioList);

            var nameRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, WrapContents = false };
            nameRow.Controls.Add(new Label { Text = "File name:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
            nameRow.Controls.Add(_takeName);
            nameRow.Controls.Add(new Label { Text = "Description:", AutoSize = true, Margin = new Padding(12, 8, 3, 3) });
            nameRow.Controls.Add(_description);

            var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, WrapContents = false };
            buttonRow.Controls.AddRange(new Control[]
            {
                _recordButton, _stopButton, _saveButton, _autoTake, _liftDelay,
                new Label { Text = "ms", AutoSize = true, Margin = new Padding(2, 8, 3, 3) },
                _autoSave, _mouseMoves,
            });
            _recordButton.Click += (s, e) => StartRecording();
            _stopButton.Click += (s, e) => StopRecording();
            _saveButton.Click += (s, e) => Save();
            _mouseMoves.CheckedChanged += (s, e) =>
            {
                if (_session != null) _session.RecordMouseMoves = _mouseMoves.Checked;
                if (_pending != null) _pending.RecordMouseMoves = _mouseMoves.Checked;
            };
            _autoTake.CheckedChanged += (s, e) => UpdateAutoStop();

            var browse = new Button { Text = "Browse...", AutoSize = true };
            browse.Click += (s, e) => BrowseFolder();
            var folderRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, WrapContents = false };
            folderRow.Controls.Add(new Label { Text = "Output folder:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
            folderRow.Controls.Add(_folder);
            folderRow.Controls.Add(browse);

            var openSettings = new LinkLabel { Text = "Open touchpad settings", Dock = DockStyle.Right, AutoSize = true, Padding = new Padding(8, 0, 0, 0) };
            openSettings.LinkClicked += (s, e) => OpenTouchpadSettings();
            _warning.Controls.Add(_warningText);
            _warning.Controls.Add(openSettings);

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
            split.Panel1.Controls.Add(_contactView);
            split.Panel2.Controls.Add(_preview);

            var right = new Panel { Dock = DockStyle.Fill };
            // Docked controls are laid out in reverse z-order: add the fill control first.
            right.Controls.Add(split);
            right.Controls.Add(_status);
            right.Controls.Add(_warning);
            right.Controls.Add(folderRow);
            right.Controls.Add(buttonRow);
            right.Controls.Add(nameRow);
            right.Controls.Add(_instruction);

            Controls.Add(right);
            Controls.Add(scenarioGroup);
            Controls.Add(deviceGroup);

            // Set after layout so the distance is not clamped by the default size.
            Load += (s, e) => split.SplitterDistance = split.Width * 3 / 5;

            _refreshTimer.Tick += (s, e) => OnRefreshTick();
            _liftTimer.Tick += (s, e) => OnLiftTimeout();
            _scenarioList.SelectedIndex = 0;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _capture = new InputCapture();
            _capture.RawInput += OnRawInput;
            _capture.DeviceChanged += (change, hDevice) => _devicesChanged = true;
            _capture.Mouse += OnMouse;
            RefreshDevices();
            UpdateStatus();
            _refreshTimer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (HasUnsavedTake() &&
                MessageBox.Show(this, "The last recording has not been saved. Quit anyway?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _refreshTimer.Stop();
            _liftTimer.Stop();
            _capture?.Dispose();
            foreach (DeviceMonitor monitor in _monitors.Values)
                monitor.Dispose();
            _monitors.Clear();
            base.OnFormClosed(e);
        }

        /// <summary>Keyboard control, so starting and stopping a take never touches the device being recorded.</summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F5:
                    StartRecording();
                    return true;
                case Keys.F6:
                case Keys.Escape:
                    if (IsRecording || IsArmed)
                    {
                        StopRecording();
                        return true;
                    }
                    break;
                case Keys.Control | Keys.S:
                    Save();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private bool IsRecording => _capture?.Session != null;

        private bool IsArmed => _pending != null;

        private bool HasUnsavedTake() => _session != null && !_saved && _session.Recording.Events.Count > 0;

        private void RefreshDevices()
        {
            var present = new HashSet<IntPtr>();
            _deviceList.BeginUpdate();
            _deviceList.Items.Clear();
            foreach (KeyValuePair<IntPtr, RecordedDevice> device in DeviceCatalog.EnumerateDigitizers())
            {
                present.Add(device.Key);
                DeviceMonitor monitor = GetMonitor(device.Key, device.Value);
                _deviceList.Items.Add(new ListViewItem(new[]
                {
                    monitor.Kind,
                    monitor.VidPid,
                    monitor.Decoder == null ? "not decodable" : monitor.Decoder.ContactSlots.ToString(),
                    monitor.SurfaceText,
                    monitor.ReportsPerSecond.ToString(),
                    monitor.Reports.ToString(),
                    monitor.Info.Name ?? "(unknown)",
                })
                { Tag = monitor });
            }
            if (_deviceList.Items.Count == 0)
                _deviceList.Items.Add(new ListViewItem(new[] { "-", "", "", "", "", "", "No digitizer found. Mouse scenarios can still be recorded." }));
            _deviceList.EndUpdate();

            foreach (IntPtr gone in _monitors.Keys.Where(h => !present.Contains(h)).ToList())
            {
                DeviceMonitor monitor = _monitors[gone];
                _monitors.Remove(gone);
                if (_contactView.Monitor == monitor)
                    _contactView.Monitor = null;
                monitor.Dispose();
            }
            UpdateWarning();
            // GetMonitor flags new handles; they are listed now.
            _devicesChanged = false;
        }

        private DeviceMonitor GetMonitor(IntPtr handle, RecordedDevice info = null)
        {
            DeviceMonitor monitor;
            if (!_monitors.TryGetValue(handle, out monitor))
            {
                monitor = new DeviceMonitor(handle, info ?? DeviceCatalog.Describe(handle, 0, includePreparsedData: false));
                _monitors.Add(handle, monitor);
                _devicesChanged = true;
            }
            return monitor;
        }

        private void UpdateWarning()
        {
            string warning = SystemGestureCheck.Describe(_monitors.Values.Any(m => m.Info.Usage == NativeMethods.TouchPadUsage));
            _warningText.Text = warning ?? "";
            _warning.Visible = warning != null;
        }

        private static void OpenTouchpadSettings()
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-settings:devices-touchpad") { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
            {
                MessageBox.Show("Open Settings > Bluetooth & devices > Touchpad manually: " + ex.Message);
            }
        }

        private void OnRawInput(byte[] buffer)
        {
            int offset, sizeHid, count;
            try
            {
                if (!RawInputConverter.TryGetHidPayload(buffer, out offset, out sizeHid, out count))
                    return;
            }
            catch (InvalidDataException)
            {
                // Counted by the session as a bad WM_INPUT.
                return;
            }

            DeviceMonitor monitor = GetMonitor(RawInputConverter.GetDeviceHandle(buffer));
            monitor.Reports += count;
            if (monitor.Decoder == null)
                return;

            int now = Environment.TickCount;
            bool decoded = false;
            for (int i = 0; i < count; i++)
            {
                DecodedReport report = monitor.Decoder.Decode(buffer, offset + i * sizeHid, sizeHid);
                if (report == null)
                    continue;
                decoded = true;

                int before = monitor.Live.ActiveCount;
                if (before == 0 && unchecked(now - monitor.LastActiveTick) > LiveClearIdleMs)
                    monitor.Live.Clear();
                monitor.Live.Apply(report);
                if (monitor.Live.ActiveCount > 0)
                    monitor.LastActiveTick = now;

                // A take starts on a touch-down edge: a finger still resting from clicking Record does not count.
                if (IsArmed && before == 0 && monitor.Live.ActiveCount > 0)
                    BeginTake();
                if (IsRecording)
                {
                    monitor.Take.Apply(report);
                    _takeHadMouseDown |= report.ButtonDown;
                }
                UpdateAutoStop();
            }
            if (!decoded)
                return;

            if (IsRecording || !_contactView.ShowTake || _contactView.Monitor == null)
                _contactView.Monitor = monitor;
            _contactView.Invalidate();
        }

        private void OnMouse(int msg, int x, int y)
        {
            switch (msg)
            {
                case WM_LBUTTONDOWN:
                case WM_RBUTTONDOWN:
                case WM_MBUTTONDOWN:
                case WM_XBUTTONDOWN:
                    _mouseButtonsDown++;
                    // Clicks on the recorder itself (e.g. Stop) never start a take.
                    if (IsArmed && !Bounds.Contains(x, y))
                        BeginTake();
                    if (IsRecording)
                        _takeHadMouseDown = true;
                    UpdateAutoStop();
                    break;
                case WM_LBUTTONUP:
                case WM_RBUTTONUP:
                case WM_MBUTTONUP:
                case WM_XBUTTONUP:
                    if (_mouseButtonsDown > 0)
                        _mouseButtonsDown--;
                    UpdateAutoStop();
                    break;
            }
        }

        private void SelectScenario()
        {
            var scenario = _scenarioList.SelectedItem as Scenario;
            if (scenario == null)
            {
                _instruction.Text = "";
                return;
            }
            _instruction.Text = scenario.Instruction + "\r\n" +
                "Press Record (F5), perform the action, then Stop (F6) and Save (Ctrl+S). Edit the file name to record your own gestures.";
            _takeName.Text = scenario.Id;
            _description.Text = scenario.Instruction;
        }

        private void StartRecording()
        {
            if (IsRecording || IsArmed)
                return;
            if (HasUnsavedTake() &&
                MessageBox.Show(this, "Discard the unsaved recording?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            _liftTimer.Stop();

            _sessionName = ToFileName(_takeName.Text);
            _takeName.Text = _sessionName;
            var session = new RecordingSession(_description.Text.Trim(), _mouseMoves.Checked);
            if (_autoTake.Checked)
            {
                _pending = session;
                _contactView.ShowTake = false;
                _contactView.Invalidate();
                SetButtons();
                UpdateStatus();
            }
            else
            {
                _pending = session;
                BeginTake();
            }
        }

        /// <summary>Starts capturing into the armed session.</summary>
        private void BeginTake()
        {
            _liftTimer.Stop();
            RecordingSession session = _pending;
            _pending = null;
            foreach (DeviceMonitor monitor in _monitors.Values)
                monitor.Take.Clear();
            _takeHadMouseDown = false;
            _session = _capture.Start(session);
            _saved = false;
            _preview.Recording = _session.Recording;
            _contactView.ShowTake = true;
            _contactView.Invalidate();
            SetButtons();
            UpdateStatus();
        }

        private void StopRecording()
        {
            _liftTimer.Stop();
            if (IsArmed)
                _pending = null;
            else
                _capture.Stop();
            SetButtons();
            UpdateStatus();
        }

        /// <summary>Auto take: starts the lift delay on release, without extending it for idle reports.</summary>
        private void UpdateAutoStop()
        {
            if (!IsRecording || !_autoTake.Checked)
            {
                _liftTimer.Stop();
                return;
            }
            bool active = _mouseButtonsDown > 0;
            bool touched = _takeHadMouseDown;
            foreach (DeviceMonitor monitor in _monitors.Values)
            {
                active |= monitor.Take.ActiveCount > 0 || monitor.Take.ButtonDown;
                touched |= monitor.Take.Strokes.Count > 0;
            }
            if (active || !touched)
            {
                _liftTimer.Stop();
                return;
            }
            if (_liftTimer.Enabled)
                return;
            _liftTimer.Interval = (int)_liftDelay.Value;
            _liftTimer.Start();
        }

        private void OnLiftTimeout()
        {
            _liftTimer.Stop();
            if (!IsRecording)
                return;
            StopRecording();
            if (_autoSave.Checked)
                Save();
        }

        private void Save()
        {
            if (_session == null || IsRecording || _saved) return;
            string folder = _folder.Text.Trim();
            try
            {
                Directory.CreateDirectory(folder);
                string path = NextFreePath(folder, _sessionName);
                _session.Recording.Save(path);
                _saved = true;
                _status.Text = "Saved " + path;
                SetButtons();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>&lt;name&gt;.gsrec.json, or &lt;name&gt;-2.gsrec.json, -3, ... so repeated takes never overwrite each other.</summary>
        private static string NextFreePath(string folder, string name)
        {
            string path = Path.Combine(folder, name + ".gsrec.json");
            for (int take = 2; File.Exists(path); take++)
                path = Path.Combine(folder, name + "-" + take + ".gsrec.json");
            return path;
        }

        private static string ToFileName(string text)
        {
            var name = new StringBuilder(text.Trim());
            foreach (char invalid in Path.GetInvalidFileNameChars())
                name.Replace(invalid, '-');
            return name.Length == 0 ? "recording" : name.ToString();
        }

        private void BrowseFolder()
        {
            using (var dialog = new FolderBrowserDialog { SelectedPath = _folder.Text, ShowNewFolderButton = true })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    _folder.Text = dialog.SelectedPath;
            }
        }

        private void SetButtons()
        {
            bool busy = IsRecording || IsArmed;
            _recordButton.Enabled = !busy;
            _stopButton.Enabled = busy;
            _saveButton.Enabled = !busy && _session != null && !_saved;
            _scenarioList.Enabled = !busy;
            _takeName.Enabled = !busy;
            _description.Enabled = !busy;
        }

        private void OnRefreshTick()
        {
            if (_devicesChanged)
                RefreshDevices();
            int now = Environment.TickCount;
            foreach (ListViewItem item in _deviceList.Items)
            {
                var monitor = item.Tag as DeviceMonitor;
                if (monitor == null) continue;
                monitor.UpdateRate(now);
                SetText(item.SubItems[4], monitor.ReportsPerSecond.ToString());
                SetText(item.SubItems[5], monitor.Reports.ToString());
            }
            // Pick up changes made in the touchpad settings while the recorder is open.
            if (++_refreshTicks % 8 == 0)
                UpdateWarning();
            UpdateStatus();
            _contactView.Invalidate();
        }

        private static void SetText(ListViewItem.ListViewSubItem item, string text)
        {
            if (item.Text != text)
                item.Text = text;
        }

        private void UpdateStatus()
        {
            if (IsArmed)
            {
                _status.Text = string.Format(
                    "ARMED {0}: the take starts at the next touch-down (or mouse button press outside this window){1}. F6/Esc cancels.",
                    _sessionName, _autoTake.Checked ? string.Format(" and stops {0} ms after everything is released", _liftDelay.Value) : "");
                return;
            }
            if (_session == null)
            {
                _status.Text = "Idle. Select a scenario and press Record (F5). Touch the trackpad to see its contacts live.";
                return;
            }
            if (_saved && !IsRecording)
                return;

            var text = new StringBuilder();
            text.Append(IsRecording ? "RECORDING " : "Stopped ").Append(_sessionName);
            text.AppendFormat(" | events {0} | mouse {1} | device changes {2}", _session.Recording.Events.Count, _session.MouseEventCount, _session.DeviceChangeCount);
            if (_session.ErrorCount > 0)
                text.AppendFormat(" | bad WM_INPUT {0}", _session.ErrorCount);
            if (!IsRecording)
                text.Append(" | Save with Ctrl+S");
            text.AppendLine();
            foreach (RecordedDevice device in _session.Devices)
                text.AppendFormat("#{0} {1} {2:X4}:{3:X4}: {4} hid   ", device.Id, DeviceCatalog.UsageName(device.UsagePage, device.Usage), device.VendorId, device.ProductId, _session.GetHidCount(device.Id));
            if (_session.Warnings.Count > 0)
                text.Append(string.Join(" ", _session.Warnings));
            _status.Text = text.ToString();
            _preview.Invalidate();
        }

        /// <summary>Draws screens, the cursor position at each HID report and the mouse hook points.</summary>
        private sealed class PreviewPanel : Panel
        {
            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public InputRecording Recording { get; set; }

            public PreviewPanel()
            {
                DoubleBuffered = true;
                BackColor = Color.White;
                BorderStyle = BorderStyle.FixedSingle;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Rectangle desktop = SystemInformation.VirtualScreen;
                if (desktop.Width <= 0 || desktop.Height <= 0 || ClientSize.Width < 10 || ClientSize.Height < 10)
                    return;
                float scale = Math.Min((ClientSize.Width - 8f) / desktop.Width, (ClientSize.Height - 8f) / desktop.Height);
                Func<int, int, PointF> map = (x, y) => new PointF(4 + (x - desktop.X) * scale, 4 + (y - desktop.Y) * scale);

                foreach (Screen screen in Screen.AllScreens)
                {
                    PointF p = map(screen.Bounds.X, screen.Bounds.Y);
                    e.Graphics.DrawRectangle(Pens.Silver, p.X, p.Y, screen.Bounds.Width * scale, screen.Bounds.Height * scale);
                }

                if (Recording == null)
                    return;
                foreach (RecordedEvent evt in Recording.Events)
                {
                    PointF p;
                    Brush brush;
                    float size = 2f;
                    if (evt.Type == RecordedEventTypes.Hid)
                    {
                        p = map(evt.CursorX, evt.CursorY);
                        brush = Brushes.RoyalBlue;
                    }
                    else if (evt.Type == RecordedEventTypes.Mouse)
                    {
                        p = map(evt.X, evt.Y);
                        if (evt.MouseMessage == RecordedMouseMessages.Down) { brush = Brushes.Red; size = 6f; }
                        else if (evt.MouseMessage == RecordedMouseMessages.Up) { brush = Brushes.Green; size = 6f; }
                        else brush = Brushes.Gray;
                    }
                    else
                    {
                        continue;
                    }
                    e.Graphics.FillEllipse(brush, p.X - size / 2, p.Y - size / 2, size, size);
                }
                e.Graphics.DrawString("screens: blue = cursor at HID report, gray = mouse move, red/green = button down/up", Font, Brushes.DimGray, 4, ClientSize.Height - Font.Height - 4);
            }
        }
    }
}
