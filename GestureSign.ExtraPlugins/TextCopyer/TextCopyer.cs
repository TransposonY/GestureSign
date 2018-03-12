using GestureSign.Common.Plugins;
using System.Windows;
using System.Windows.Automation;

namespace GestureSign.ExtraPlugins.TextCopyer
{
    public class TextCopyer : IPlugin
    {
        #region IPlugin Instance Fields

        private TextCopyerPanel _gui = null;
        private Point? _position = null;

        #endregion

        #region IPlugin Instance Properties

        public string Name
        {
            get { return "TextCopyer"; }
        }

        public string Category
        {
            get { return "Clipboard"; }
        }

        public string Description
        {
            get { return "Copy text from " + (_position == null ? "First Point Down" : $"({_position.Value})"); }
        }

        public bool IsAction
        {
            get { return true; }
        }

        public object GUI
        {
            get
            {
                if (_gui == null)
                    _gui = CreateGUI();

                return _gui;
            }
        }

        public bool ActivateWindowDefault
        {
            get { return false; }
        }

        public TextCopyerPanel TypedGUI
        {
            get { return (TextCopyerPanel)GUI; }
        }

        public object Icon => System.Windows.Media.Geometry.Parse("M928 128l-288 0c0-70.688-57.312-128-128-128s-128 57.312-128 128l-288 0c-17.664 0-32 14.336-32 32l0 832c0 17.664 14.336 32 32 32l832 0c17.664 0 32-14.336 32-32l0-832c0-17.664-14.336-32-32-32zM512 64c35.36 0 64 28.64 64 64s-28.64 64-64 64c-35.36 0-64-28.64-64-64s28.64-64 64-64zM896 960l-768 0 0-768 128 0 0 96c0 17.664 14.336 32 32 32l448 0c17.664 0 32-14.336 32-32l0-96 128 0 0 768zM448 858.496l-205.248-237.248 58.496-58.496 146.752 114.752 274.752-242.752 58.528 58.496z");

        public IHostControl HostControl { get; set; }

        #endregion

        #region IPlugin Instance Methods

        public void Initialize()
        {

        }

        public bool Gestured(PointInfo actionPoint)
        {
            Point target = _position == null ? new Point(actionPoint.PointLocation[0].X, actionPoint.PointLocation[0].Y) : _position.Value;
            AutomationElement targetTextElement = GetTargetTextElement(target);
            if (targetTextElement == null) return false;

            string text = ExtractText(targetTextElement);
            if (string.IsNullOrEmpty(text)) return false;

            bool success = false;
            actionPoint.Invoke(() =>
            {
                Clipboard.SetText(text);
                Clipboard.Flush();
                success = true;
            });

            return success;
        }

        public bool Deserialize(string serializedData)
        {
            if (string.IsNullOrEmpty(serializedData))
            {
                _position = null;
            }
            else
            {
                try
                {
                    _position = Point.Parse(serializedData);
                }
                catch
                {
                    _position = null;
                    return false;
                }
            }
            return true;
        }

        public string Serialize()
        {
            if (_gui != null)
            {
                _position = _gui.Position;
                return _position?.ToString();
            }
            else return _position?.ToString();
        }

        #endregion

        #region Private Instance Methods

        private AutomationElement GetTargetTextElement(Point p)
        {
            AutomationElement element = AutomationElement.FromPoint(p);
            if (element == null)
            {
                return null;
            }

            var cond1 = new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document),
                new PropertyCondition(AutomationElement.IsTextPatternAvailableProperty, true));
            var cond2 = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text);
            OrCondition textCondition = new OrCondition(cond1, cond2);

            var textElements = element.FindAll(TreeScope.Descendants | TreeScope.Element, textCondition);
            if (textElements.Count == 0)
                return element;

            int i = 0;
            do
            {
                foreach (AutomationElement e in textElements)
                {
                    Rect bound = e.Current.BoundingRectangle;
                    if (bound.IsEmpty) continue;
                    bound.Inflate(i * 2, i * 2);
                    if (bound.Contains(p))
                        return e;
                }
            }
            while (i++ < 10);

            return null;
        }

        private string ExtractText(AutomationElement element)
        {
            object pattern;
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out pattern))
            {
                ValuePattern valuePattern = (ValuePattern)pattern;
                return valuePattern.Current.Value;
            }
            if (element.TryGetCurrentPattern(TextPattern.Pattern, out pattern))
            {
                TextPattern textPattern = (TextPattern)pattern;
                foreach (var range in textPattern.GetSelection())
                {
                    return range.GetText(-1);
                }
            }
            return element.Current.Name;
        }

        private TextCopyerPanel CreateGUI()
        {
            var panel = new TextCopyerPanel();
            panel.Loaded += (s, o) =>
            {
                TypedGUI.Position = _position;
            };
            return panel;
        }

        #endregion
    }
}
