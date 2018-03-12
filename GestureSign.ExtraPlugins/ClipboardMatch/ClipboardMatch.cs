using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GestureSign.Common.Plugins;

namespace GestureSign.ExtraPlugins.ClipboardMatch
{
    public class ClipboardMatch : IPlugin
    {
        #region IPlugin Instance Fields

        private ClipboardMatchControl _gui = null;
        private string _matchString;

        #endregion

        #region IPlugin Instance Properties

        public string Name
        {
            get { return "ClipboardMatch"; }
        }

        public string Category
        {
            get { return "Clipboard"; }
        }

        public string Description
        {
            get { return "Match " + _matchString; }
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

        public ClipboardMatchControl TypedGUI
        {
            get { return (ClipboardMatchControl)GUI; }
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
            bool success = false;
            actionPoint.Invoke(() =>
            {
                IDataObject iData = Clipboard.GetDataObject();
                if (iData != null && iData.GetDataPresent(DataFormats.Text))
                {
                    string source = (string)iData.GetData(DataFormats.Text);
                    Regex reg = new Regex(_matchString);
                    var match = reg.Match(source);
                    if (match.Success)
                    {
                        string result = match.Value;
                        if (!string.IsNullOrWhiteSpace(result))
                        {
                            Clipboard.SetDataObject(result);
                            success = true;
                        }
                    }
                }
            });

            return success;
        }

        public bool Deserialize(string serializedData)
        {
            _matchString = serializedData;
            return true;
        }

        public string Serialize()
        {
            if (_gui != null)
            {
                _matchString = _gui.MatchTextBox.Text;
                return _matchString;
            }
            else return _matchString ?? String.Empty;
        }

        #endregion

        #region Private Instance Methods

        private ClipboardMatchControl CreateGUI()
        {
            ClipboardMatchControl sendKeystrokesControl = new ClipboardMatchControl();
            sendKeystrokesControl.Loaded += (s, o) =>
            {
                TypedGUI.MatchTextBox.Text = _matchString;
            };
            return sendKeystrokesControl;
        }

        #endregion
    }
}
