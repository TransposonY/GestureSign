using GestureSign.Common.Plugins;
using System;
using System.Linq;
using System.Windows.Forms;

namespace GestureSign.CorePlugins.Common
{
    public class EnvironmentVariablesParser
    {
        PointInfo _pointInfo;
        public EnvironmentVariablesParser(PointInfo pointInfo)
        {
            _pointInfo = pointInfo;
        }

        public string ExpandEnvironmentVariables(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return command;

            command = Environment.ExpandEnvironmentVariables(command);

            if (command.Contains("%GS_Clipboard%"))
            {
                string clipboardString = string.Empty;
                _pointInfo.Invoke(() =>
                {
                    IDataObject iData = Clipboard.GetDataObject();
                    if (iData != null && iData.GetDataPresent(DataFormats.Text))
                    {
                        clipboardString = (string)iData.GetData(DataFormats.Text);
                    }
                });
                if (!string.IsNullOrEmpty(clipboardString))
                    command = command.Replace("%GS_Clipboard%", clipboardString);
            }

            if (command.Contains("%GS_ClassName%") && !string.IsNullOrEmpty(_pointInfo.Window.ClassName))
            {
                command = command.Replace("%GS_ClassName%", _pointInfo.Window.ClassName);
            }
            if (command.Contains("%GS_Title%") && !string.IsNullOrEmpty(_pointInfo.Window.Title))
            {
                command = command.Replace("%GS_Title%", _pointInfo.Window.Title);
            }
            if (command.Contains("%GS_PID%"))
            {
                command = command.Replace("%GS_PID%", _pointInfo.Window.ProcessId.ToString());
            }

            return command.Replace("%GS_StartPoint_X%", _pointInfo.PointLocation.First().X.ToString()).
              Replace("%GS_StartPoint_Y%", _pointInfo.PointLocation.First().Y.ToString()).
              Replace("%GS_EndPoint_X%", _pointInfo.Points[0].Last().X.ToString()).
              Replace("%GS_EndPoint_Y%", _pointInfo.Points[0].Last().Y.ToString()).
              Replace("%GS_WindowHandle%", _pointInfo.WindowHandle.ToString());
        }
    }
}
