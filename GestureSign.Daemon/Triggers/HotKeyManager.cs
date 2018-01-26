using GestureSign.Common.Applications;
using GestureSign.Daemon.Input;
using ManagedWinapi;
using ManagedWinapi.Windows;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GestureSign.Daemon.Triggers
{
    class HotKeyManager : Trigger
    {
        private List<KeyValuePair<Hotkey, List<IAction>>> _hotKeyMap = new List<KeyValuePair<Hotkey, List<IAction>>>();

        public HotKeyManager()
        {
            PointCapture.Instance.ForegroundApplicationsChanged += Instance_ForegroundApplicationsChanged;
            PointCapture.Instance.ModeChanged += Instance_ModeChanged;

            var hotKeyActions = ApplicationManager.Instance.GetApplicationFromWindow(SystemWindow.ForegroundWindow).Where(app => !(app is IgnoredApp)).SelectMany(app => app.Actions).Where(a => a.Hotkey != null).ToList();
            hotKeyActions.AddRange(ApplicationManager.Instance.GetGlobalApplication().Actions.Where(a => a.Hotkey != null));

            if (hotKeyActions.Count != 0)
                RegisterHotKeys(hotKeyActions);
        }

        private void Instance_ForegroundApplicationsChanged(object sender, IApplication[] apps)
        {
            var hotKeyActions = apps.Where(application => application is UserApp && application.Actions != null).SelectMany(app => app.Actions).Where(a => a != null && a.Hotkey != null).ToList();
            hotKeyActions.AddRange(ApplicationManager.Instance.GetGlobalApplication().Actions.Where(a => a.Hotkey != null));

            if (hotKeyActions.Count == 0)
                UnloadHotKeys();
            else
                RegisterHotKeys(hotKeyActions);
        }

        private void Instance_ModeChanged(object sender, Common.Input.ModeChangedEventArgs e)
        {
            if (e.Mode == Common.Input.CaptureMode.UserDisabled)
                UnloadHotKeys();
        }

        private void RegisterHotKeys(List<IAction> actions)
        {
            UnloadHotKeys();
            _hotKeyMap = new List<KeyValuePair<Hotkey, List<IAction>>>();
            foreach (var action in actions)
            {
                var h = action.Hotkey;

                if (h != null && h.ModifierKeys != 0 && h.KeyCode != 0)
                {
                    int index = _hotKeyMap.FindIndex(p => p.Key.KeyCode == h.KeyCode && p.Key.ModifierKeys == h.ModifierKeys);
                    if (index >= 0)
                    {
                        var actionList = _hotKeyMap[index].Value;
                        if (!actionList.Contains(action))
                            actionList.Add(action);
                    }
                    else
                    {
                        var hotKey = new Hotkey() { KeyCode = h.KeyCode, ModifierKeys = h.ModifierKeys };
                        hotKey.HotkeyPressed += Hotkey_HotkeyPressed;
                        _hotKeyMap.Add(new KeyValuePair<Hotkey, List<IAction>>(hotKey, new List<IAction>() { action }));
                        try
                        {
                            hotKey.Register();
                        }
                        catch (HotkeyAlreadyInUseException)
                        {
                            hotKey.Unregister();
                        }
                    }
                }
            }
        }

        private void UnloadHotKeys()
        {
            if (_hotKeyMap != null)
                foreach (var hotKeyPair in _hotKeyMap)
                {
                    hotKeyPair.Key.HotkeyPressed -= Hotkey_HotkeyPressed;
                    hotKeyPair.Key.Dispose();
                }
            _hotKeyMap = null;
        }

        private void Hotkey_HotkeyPressed(object sender, EventArgs e)
        {
            Hotkey hotkey = (Hotkey)sender;
            int index = _hotKeyMap.FindIndex(p => p.Key.KeyCode == hotkey.KeyCode && p.Key.ModifierKeys == hotkey.ModifierKeys);
            if (index >= 0)
            {
                var window = ApplicationManager.Instance.GetForegroundApplications();
                OnTriggerFired(new TriggerFiredEventArgs(_hotKeyMap[index].Value, window.Rectangle.Location));
            }
        }
    }
}
