using System;
using System.Threading;
using System.Windows.Forms;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Plugins;
using GestureSignDaemon.Input;

namespace GestureSignDaemon
{
    static class Program
    {
        private static Surface surface;
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "GestureSignDaemon", out createdNew))
            {
                if (createdNew)
                {
                    Application.EnableVisualStyles();
                    //Application.SetCompatibleTextRenderingDefault(false);
                    TouchCapture.Instance.Load();
                    surface = new Surface();
                    TouchCapture.Instance.EnableTouchCapture();

                    GestureManager.Instance.Load(TouchCapture.Instance);
                    ApplicationManager.Instance.Load(TouchCapture.Instance);
                    // Create host control class and pass to plugins
                    HostControl hostControl = new HostControl()
                    {
                        _ApplicationManager = ApplicationManager.Instance,
                        _GestureManager = GestureManager.Instance,
                        _TouchCapture = TouchCapture.Instance,
                        _PluginManager = PluginManager.Instance,
                        _TrayManager = TrayManager.Instance
                    };
                    PluginManager.Instance.Load(hostControl);
                    TrayManager.Instance.Load();

                    AppConfig.ToggleWatcher();
                    try
                    {
                        NamedPipe.Instance.RunNamedPipeServer("GestureSignDaemon", new MessageProcessor());
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString(), "错误", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    if (TouchCapture.Instance.MessageWindow.NumberOfTouchscreens == 0)
                    {
                        MessageBox.Show("未检测到触摸屏设备，本软件或无法正常使用！", "错误");
                        //return;
                    }
                    Application.Run();
                }
                else
                {
                    Application.Exit();
                }
            }


        }


    }
}
