using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using GestureSign.Common;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using Timer = System.Threading.Timer;

namespace GestureSign.TouchInputProvider
{
    static class Program
    {
        private const string TouchInputProvider = "GestureSign_TouchInputProvider";

        private static MessageWindow _messageWindow;
        private static NamedPipeClientStream _pipeClient;
        private static Timer _connectTimer;

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

                        _connectTimer = new Timer(o =>
                         {
                             if (!_pipeClient.IsConnected && !Connect()) Application.Exit();
                         }, null, 10000, 10000);

                        _messageWindow = new MessageWindow();
                        _messageWindow.PointsIntercepted += MessageWindow_PointsIntercepted;

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
                else
                {
                    Application.Exit();
                }
            }
        }

        private static void MessageWindow_PointsIntercepted(object sender, Common.Input.RawPointsDataMessageEventArgs e)
        {
            if (!_pipeClient.IsConnected) return;
            try
            {
                const int size = 16;
                int rawDataCount = e.RawData.Count;

                byte[] buffer = new byte[rawDataCount * size + 4];

                BitConverter.GetBytes(e.RawData.Count).CopyTo(buffer, 0);
                for (int i = 0; i < rawDataCount; i++)
                {
                    var current = e.RawData[i];
                    BitConverter.GetBytes(current.Tip).CopyTo(buffer, i * size + 4);
                    BitConverter.GetBytes(current.ContactIdentifier).CopyTo(buffer, i * size + 8);
                    BitConverter.GetBytes(current.RawPoints.X).CopyTo(buffer, i * size + 12);
                    BitConverter.GetBytes(current.RawPoints.Y).CopyTo(buffer, i * size + 16);
                }

                _pipeClient.Write(buffer, 0, buffer.Length);
                _pipeClient.Flush();
                _pipeClient.WaitForPipeDrain();
            }
            catch (Exception exception)
            {
                Logging.LogException(exception);
            }
        }

        private static bool Connect()
        {
            try
            {
                if (_pipeClient == null)
                    _pipeClient = new NamedPipeClientStream(".", TouchInputProvider, PipeDirection.InOut,
                        PipeOptions.Asynchronous, TokenImpersonationLevel.None);

                int i = 0;
                for (; i != 10; i++)
                {
                    if (!NamedPipe.NamedPipeDoesNotExist(TouchInputProvider)) break;
                    Thread.Sleep(500);
                }
                if (i == 10) return false;
                _pipeClient.Connect(10);

                ThreadPool.QueueUserWorkItem(delegate
                {
                    StreamReader sr = new StreamReader(_pipeClient);
                    while (true)
                    {
                        var result = sr.ReadLine();
                        if (result == null || result == "Exit")
                            break;
                    }
                    Application.Exit();
                });
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
