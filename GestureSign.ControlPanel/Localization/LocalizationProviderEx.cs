using System;
using System.Windows;
using System.Windows.Media;
using GestureSign.Common.Localization;

namespace GestureSign.ControlPanel.Localization
{
    class LocalizationProviderEx : LocalizationProvider
    {
        public static FlowDirection FlowDirection
        {
            get
            {
                string value;
                if (Texts.TryGetValue("IsRightToLeft", out value))
                {
                    try
                    {
                        return Boolean.Parse(value) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                    }
                    catch
                    {
                        return FlowDirection.LeftToRight;
                    }
                }
                return FlowDirection.LeftToRight;
            }
        }

        public static FontFamily Font
        {
            get
            {
                string value;
                if (Texts.TryGetValue("Font", out value))
                    return new FontFamily(value);
                return null;
            }
        }

        public static FontFamily HeaderFontFamily
        {
            get
            {
                string value;
                if (Texts.TryGetValue(nameof(HeaderFontFamily), out value))
                    return new FontFamily(value);
                return null;
            }
        }
    }
}
