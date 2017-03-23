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

        public ClipboardMatchControl TypedGUI
        {
            get { return (ClipboardMatchControl)GUI; }
        }

        public IHostControl HostControl { get; set; }

        #endregion

        #region IPlugin Instance Methods

        public void Initialize()
        {

        }

        public bool Gestured(PointInfo actionPoint)
        {
            bool success = false;
            actionPoint.SyncContext.Send(state =>
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
            }, null);

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
