using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using ManagedWinapi;
using System;
using System.Collections.Generic;

namespace GestureSign.Daemon.Triggers
{
    class HotKeyManager : Trigger
    {
        private Dictionary<Hotkey, List<IAction>> _hotKeyMap = new Dictionary<Hotkey, List<IAction>>();

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
                        InitializeHotKey(hotKey);
                        _hotKeyMap.Add(hotKey, new List<IAction>() { action });
                    }
                }
            }
            return true;
        }

        private void InitializeHotKey(Hotkey hotkey)
        {
            try
            {
                hotkey.HotkeyPressed += Hotkey_HotkeyPressed;
                hotkey.Register();
            }
            catch (HotkeyAlreadyInUseException)
            {
                hotkey.Unregister();
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
