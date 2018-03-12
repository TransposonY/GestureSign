using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using GestureSign.Common.Configuration;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.Common.Log;

namespace GestureSign.TouchInputProvider
{
    static class Program
    {
        private const string TouchInputProvider = "GestureSign_TouchInputProvider";

        private static MessageWindow _messageWindow;
        private static NamedPipeClientStream _pipeClient;

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (new Mutex(true, TouchInputProvider, out createdNew))
            {
                if (createdNew)
                {
                    try
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);

                        Logging.OpenLogFile();

                        if (!Connect()) return;

                        _messageWindow = new MessageWindow();
                        _messageWindow.PointsIntercepted += MessageWindow_PointsIntercepted;

                        _messageWindow.RegisterTouchPad = AppConfig.RegisterTouchPad;
                        AppConfig.ConfigChanged += (o, e) => _messageWindow.RegisterTouchPad = AppConfig.RegisterTouchPad;
                        NamedPipe.Instance.RunNamedPipeServer("TouchInputProviderMessage", new MessageProcessor());

                        Application.Run();
                    }
                    catch (Exception e)
                    {
                        Logging.LogException(e);
                        MessageBox.Show(e.ToString(), LocalizationProvider.Instance.GetTextValue("Messages.Error"),
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        Application.Exit();
                    }
                }
            }
        }

        private static void MessageWindow_PointsIntercepted(object sender, Common.Input.RawPointsDataMessageEventArgs e)
        {
            if (!_pipeClient.IsConnected && !Connect())
            {
                Application.Exit();
                return;
            }
            try
            {
                const int size = 16;
                const int offset = 8;
                int rawDataCount = e.RawData.Count;

                byte[] buffer = new byte[rawDataCount * size + offset];

                BitConverter.GetBytes(e.RawData.Count).CopyTo(buffer, 0);
                BitConverter.GetBytes((int)e.SourceDevice).CopyTo(buffer, 4);
                for (int i = 0; i < rawDataCount; i++)
                {
                    var current = e.RawData[i];
                    BitConverter.GetBytes(current.Tip).CopyTo(buffer, offset + i * size);
                    BitConverter.GetBytes(current.ContactIdentifier).CopyTo(buffer, offset + 4 + i * size);
                    BitConverter.GetBytes(current.RawPoints.X).CopyTo(buffer, offset + 4 * 2 + i * size);
                    BitConverter.GetBytes(current.RawPoints.Y).CopyTo(buffer, offset + 4 * 3 + i * size);
                }

                _pipeClient.Write(buffer, 0, buffer.Length);
                _pipeClient.Flush();
                _pipeClient.WaitForPipeDrain();
            }
            catch (IOException) { }
            catch (Exception exception)
            {
                Logging.LogException(exception);
            }
        }

        private static bool Connect()
        {
            try
            {
                var pipeName = NamedPipe.GetUserPipeName(TouchInputProvider);
                if (_pipeClient == null)
                    _pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous, TokenImpersonationLevel.None);

                int i = 0;
                for (; i != 10; i++)
                {
                    if (!NamedPipe.NamedPipeDoesNotExist(pipeName)) break;
                    Thread.Sleep(100);
                }
                if (i == 10) return false;
                _pipeClient.Connect(10);
                return true;
            }
            catch (Exception e)
            {
                Logging.LogException(e);
                return false;
            }
        }
    }
}
