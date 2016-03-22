using System.Collections.Generic;
using System.Windows.Forms;
using GestureSign.Common.Localization;

namespace GestureSign.CorePlugins.HotKey
{
    public class HotKeySettings
    {
        #region Public Properties

        public bool Windows { get; set; }

        public bool Control { get; set; }

        public bool Shift { get; set; }

        public bool Alt { get; set; }

        public List<Keys> KeyCode { get; set; }

        public bool SendByKeybdEvent { get; set; }

        #endregion
    }

    public class ExtraKeysDescription
    {
        static ExtraKeysDescription()
        {
            DescriptionDict = new Dictionary<Keys, string>(14)
            {
                {Keys.None, LocalizationProvider.Instance.GetTextValue("CorePlugins.HotKey.AddExtraKey")}
            };

            AddDescription(Keys.BrowserBack, Keys.BrowserForward, Keys.BrowserHome, Keys.BrowserRefresh,
                Keys.BrowserSearch, Keys.BrowserStop,
                Keys.MediaNextTrack, Keys.MediaPlayPause, Keys.MediaPreviousTrack,
                Keys.MediaStop, Keys.VolumeDown, Keys.VolumeMute, Keys.VolumeUp);
        }
        public static Dictionary<Keys, string> DescriptionDict { get; private set; }

        private static void AddDescription(params Keys[] keys)
        {
            foreach (Keys code in keys)
                DescriptionDict.Add(code,
                    LocalizationProvider.Instance.GetTextValue("CorePlugins.HotKey.ExtraKeys." + code));
        }
    }
}
