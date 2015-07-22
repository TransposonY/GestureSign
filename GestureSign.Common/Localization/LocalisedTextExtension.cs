using System;
using System.Windows.Markup;

namespace GestureSign.Common.Localization
{
    [MarkupExtensionReturnType(typeof(string))]
    public class LocalisedTextExtension : MarkupExtension
    {

        private string _key;

        public LocalisedTextExtension(string key)
        {
            _key = key;
        }
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return LanguageDataManager.Instance.GetTextValue(_key);
        }

    }
}
