using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Threading;
using System.Diagnostics;

using System.IO;
using GestureSign.Common.Configuration;

namespace GestureSignDaemon
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

            if (!System.IO.File.Exists("GestureSign.exe"))
            {
                MessageBox.Show("未找到本软件组件\"GestureSign.exe\"，请重新下载或安装本软件.", "错误", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            using (Mutex mutex = new Mutex(true, "GestureSignDaemon", out createdNew))
            {
                if (createdNew)
                {
                    Application.EnableVisualStyles();
                    //Application.SetCompatibleTextRenderingDefault(false);

                    Input.TouchCapture.Instance.Load();
                    Surface surface = new Surface();
                    Input.TouchCapture.Instance.EnableTouchCapture();

                    GestureSign.Common.Gestures.GestureManager.Instance.Load(Input.TouchCapture.Instance);
                    GestureSign.Common.Applications.ApplicationManager.Instance.Load(Input.TouchCapture.Instance);
                    // Create host control class and pass to plugins
                    GestureSign.Common.Plugins.HostControl hostControl = new GestureSign.Common.Plugins.HostControl()
                    {
                        _ApplicationManager = global::GestureSign.Common.Applications.ApplicationManager.Instance,
                        _GestureManager = global::GestureSign.Common.Gestures.GestureManager.Instance,
                        _TouchCapture = Input.TouchCapture.Instance,
                        _PluginManager = GestureSign.Common.Plugins.PluginManager.Instance,
                        _TrayManager = TrayManager.Instance
                    };
                    GestureSign.Common.Plugins.PluginManager.Instance.Load(hostControl);
                    TrayManager.Instance.Load();

                    MessageProcessor messageProcessor = new MessageProcessor();
                    GestureSign.Common.InterProcessCommunication.NamedPipe.Instance.RunNamedPipeServer("GestureSignDaemon", messageProcessor.ProcessMessages);

                    AppConfig.ToggleWatcher();
                    if (Input.TouchCapture.Instance.MessageWindow.NumberOfTouchscreens == 0)
                    {
                        MessageBox.Show("未检测到触摸屏设备，本软件或无法正常使用！", "错误");
                    }
                    else if (AppConfig.XRatio == 0 && !Input.TouchCapture.Instance.MessageWindow.IsRegistered)
                    {
                        try
                        {
                            bool createdSetting;
                            using (new Mutex(false, "GestureSignSetting", out createdSetting)) { }
                            if (createdSetting)
                            {
                                using (Process daemon = new Process())
                                {
                                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSign.exe");
                                    daemon.StartInfo.FileName = path;

                                    // pipeClient.StartInfo.Arguments =            
                                    //daemon.StartInfo.UseShellExecute = false;
                                    daemon.Start();
                                    daemon.WaitForInputIdle(1000);
                                }
                            }
                            GestureSign.Common.InterProcessCommunication.NamedPipe.SendMessageAsync("Guide", "GestureSignSetting");
                        }
                        catch (Exception exception) { MessageBox.Show(exception.ToString(), "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); }

                    }
                    Application.Run();
                }
                else
                {
                    //MessageBox.Show("本程序已经运行", "提示");
                    Application.Exit();
                }
            }


        }


    }
}
