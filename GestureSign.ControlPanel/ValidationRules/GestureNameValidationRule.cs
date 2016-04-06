using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using System.Globalization;
using System.Windows.Controls;

namespace GestureSign.ControlPanel.ValidationRules
{
    public class GestureNameValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string newGestureName = value as string;

            if (GestureManager.Instance.GestureExists(newGestureName.Trim()))
                return new ValidationResult(false, string.Format(LocalizationProvider.Instance.GetTextValue("GestureDefinition.Messages.GestureExists"), newGestureName));

            return new ValidationResult(true, null);
        }
    }
}
