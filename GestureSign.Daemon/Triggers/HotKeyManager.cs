using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
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
        private Dictionary<Hotkey, List<IAction>> _hotKeyMap = new Dictionary<Hotkey, List<IAction>>();

        public HotKeyManager()
        {
            PointCapture.Instance.ForegroundApplicationsChanged += Instance_ForegroundApplicationsChanged;
        }

        private void Instance_ForegroundApplicationsChanged(object sender, IApplication[] apps)
        {
            var userAppList = apps.Where(application => application is UserApp).Union(ApplicationManager.Instance.GetAllGlobalApplication()).ToArray();
            if (userAppList.Length == 0) return;
            RegisterHotKeys(userAppList);
        }

        public override bool LoadConfiguration(List<IAction> actions)
        {
            UnloadHotKeys();
            return LoadHotKeys(actions);
        }

        private bool LoadHotKeys(List<IAction> actions)
        {
            _hotKeyMap = new Dictionary<Hotkey, List<IAction>>();

            if (actions == null || actions.Count == 0) return false;

            foreach (var action in actions)
            {
                var h = action.Hotkey;

                if (h != null && h.ModifierKeys != 0 && h.KeyCode != 0)
                {
                    var hotKey = new Hotkey() { KeyCode = h.KeyCode, ModifierKeys = h.ModifierKeys };
                    if (_hotKeyMap.ContainsKey(hotKey))
                    {
                        var actionList = _hotKeyMap[hotKey];
                        if (!actionList.Contains(action))
                            actionList.Add(action);
                    }
                    else
                    {
                        hotKey.HotkeyPressed += Hotkey_HotkeyPressed;
                        _hotKeyMap.Add(hotKey, new List<IAction>() { action });
                    }
                }
            }
            var apps = ApplicationManager.Instance.GetApplicationFromWindow(SystemWindow.ForegroundWindow);
            RegisterHotKeys(apps);
            return true;
        }

        private void RegisterHotKeys(params IApplication[] apps)
        {
            foreach (var hotKeyPair in _hotKeyMap)
            {
                if (apps.Any(app => hotKeyPair.Value.Intersect(app.Actions).Any()))
                {
                    try
                    {
                        hotKeyPair.Key.Register();
                    }
                    catch (HotkeyAlreadyInUseException)
                    {
                        hotKeyPair.Key.Unregister();
                    }
                }
                else hotKeyPair.Key.Unregister();
            }
        }

        private void UnloadHotKeys()
        {
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
            if (_hotKeyMap.ContainsKey(hotkey))
            {
                var window = ApplicationManager.Instance.GetForegroundApplications();
                OnTriggerFired(new TriggerFiredEventArgs(_hotKeyMap[hotkey], window.Rectangle.Location));
            }
        }
    }
}
