using System;
using System.Windows;
using System.Windows.Media;
using GestureSign.Common.Localization;

namespace GestureSign.ControlPanel.Localization
{
    class LocalizationProviderEx : LocalizationProvider
    {
        private static LocalizationProviderEx _instance;

        public FlowDirection FlowDirection
        {
            get
            {
                if (Texts.ContainsKey("IsRightToLeft"))
                    return Boolean.Parse(Texts["IsRightToLeft"]) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                return FlowDirection.LeftToRight;
            }
        }

        public FontFamily Font
        {
            get
            {
                if (Texts.ContainsKey("Font"))
                    return new FontFamily(Texts["Font"]);
                return null;
            }
        }

        public new static LocalizationProviderEx Instance
        {
            get { return _instance ?? (_instance = new LocalizationProviderEx()); }
        }
    }
}
