using System;
using System.Threading;
using System.Windows.Forms;
using GestureSign.Common;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.Common.Log;
using GestureSign.Common.Plugins;
using GestureSign.Daemon.Input;
using GestureSign.Daemon.Triggers;

namespace GestureSign.Daemon
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (new Mutex(true, Constants.Daemon, out createdNew))
            {
                if (createdNew)
                {
                    Application.EnableVisualStyles();
                    //Application.SetCompatibleTextRenderingDefault(false);
                    try
                    {
                        Application.ThreadException += Application_ThreadException;
                        Logging.LoggedExceptionOccurred += (o, e) => ShowException(e);
                        Logging.OpenLogFile();

                        if (!LocalizationProvider.Instance.LoadFromFile("Daemon"))
                        {
                            LocalizationProvider.Instance.LoadFromResource(Properties.Resources.en);
                        }

                        PointCapture.Instance.Load();
                        SynchronizationContext uiContext = SynchronizationContext.Current;
                        TriggerManager.Instance.Load();

                        GestureManager.Instance.Load(PointCapture.Instance);
                        ApplicationManager.Instance.Load(PointCapture.Instance);
                        // Create host control class and pass to plugins
                        HostControl hostControl = new HostControl()
                        {
                            _ApplicationManager = ApplicationManager.Instance,
                            _GestureManager = GestureManager.Instance,
                            _PointCapture = PointCapture.Instance,
                            _PluginManager = PluginManager.Instance,
                            _TrayManager = TrayManager.Instance
                        };
                        PluginManager.Instance.Load(hostControl, uiContext);
                        TrayManager.Instance.Load();

                        AppConfig.ConfigChanged += (o, e) => NamedPipe.SendMessageAsync(CommandEnum.ConfigReload, Constants.ControlPanel);
                        NamedPipe.Instance.RunNamedPipeServer(Constants.Daemon, new MessageProcessor(uiContext));

                        Application.ApplicationExit += Application_ApplicationExit;

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
                    NamedPipe.SendMessageAsync(CommandEnum.StartControlPanel, Constants.Daemon, wait: false).Wait();
                }
            }
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            NamedPipe.Instance.Dispose();
            PointCapture.Instance.Dispose();
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            DialogResult result = DialogResult.Abort;
            try
            {
                Logging.LogException(e.Exception);
                string errorMsg = "An application error occurred. Please contact the author with the following information:\n\n";
                errorMsg = errorMsg + e.Exception;
                result = MessageBox.Show(errorMsg, "Error", MessageBoxButtons.AbortRetryIgnore,
                   MessageBoxIcon.Stop);
            }
            catch (Exception fe)
            {
                try
                {
                    MessageBox.Show(fe.ToString(), "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
                finally
                {
                    Application.Exit();
                }
            }

            // Exits the program when the user clicks Abort.
            if (result == DialogResult.Abort)
                Application.Exit();
        }

        private static void ShowException(Exception exception)
        {
            while (exception.InnerException != null)
                exception = exception.InnerException;

            MessageBox.Show(exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }
}
