using System;
using System.Collections.Generic;
using GestureSign.Common.Gestures;
using ManagedWinapi;

namespace GestureSign.Daemon.Triggers
{
    class HotKeyManager : Trigger
    {
        private Dictionary<Hotkey, List<string>> _hotKeyMap = new Dictionary<Hotkey, List<string>>();

        public override bool LoadConfiguration(IGesture[] gestures)
        {
            UnloadHotKeys();
            return LoadHotKeys(gestures);
        }

        private bool LoadHotKeys(IGesture[] gestures)
        {
            _hotKeyMap = new Dictionary<Hotkey, List<string>>();

            if (gestures == null || gestures.Length == 0) return false;

            foreach (var g in gestures)
            {
                var h = ((Gesture)g).Hotkey;

                if (h != null && h.ModifierKeys != 0 && h.KeyCode != 0)
                {
                    var hotKey = new Hotkey() { KeyCode = h.KeyCode, ModifierKeys = h.ModifierKeys };
                    if (_hotKeyMap.ContainsKey(hotKey))
                    {
                        var gestureNameList = _hotKeyMap[hotKey] ?? new List<string>();
                        if (!gestureNameList.Contains(g.Name))
                            gestureNameList.Add(g.Name);
                    }
                    else
                    {
                        InitializeHotKey(hotKey);
                        _hotKeyMap.Add(hotKey, new List<string>(new[] { g.Name }));
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
                OnTriggerFired(new GestureNameEventArgs(_hotKeyMap[hotkey]));
            }
        }
    }
}
