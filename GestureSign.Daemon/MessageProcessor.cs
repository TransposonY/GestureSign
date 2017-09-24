using System;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.Input;
using GestureSign.Daemon.Input;
using GestureSign.Common.InterProcessCommunication;

namespace GestureSign.Daemon
{
    class MessageProcessor : IMessageProcessor
    {
        private SynchronizationContext _synchronizationContext;

        public MessageProcessor(SynchronizationContext synchronizationContext)
        {
            _synchronizationContext = synchronizationContext;
        }

        public bool ProcessMessages(NamedPipeServerStream server)
        {
            BinaryFormatter binForm = new BinaryFormatter();

            using (MemoryStream memoryStream = new MemoryStream())
            {
                server.CopyTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                object data = binForm.Deserialize(memoryStream);
                string message = data as string;
                if (message != null)
                {
                    _synchronizationContext.Post(state =>
                    {
                        switch (message)
                        {
                            case "StartTeaching":
                                PointCapture.Instance.Mode = CaptureMode.Training;
                                break;
                            case "StopTraining":
                                if (PointCapture.Instance.Mode != CaptureMode.UserDisabled)
                                    PointCapture.Instance.Mode = CaptureMode.Normal;
                                break;
                            case "LoadApplications":
                                ApplicationManager.Instance.LoadApplications().Wait();
                                break;
                            case "LoadGestures":
                                GestureManager.Instance.LoadGestures().Wait();
                                break;
                            case "LoadConfiguration":
                                AppConfig.Reload();
                                break;
                            case "ShowTrayIcon":
                                TrayManager.Instance.TrayIconVisible = true;
                                break;
                            case "HideTrayIcon":
                                TrayManager.Instance.TrayIconVisible = false;
                                break;
                        }
                    }, null);
                }
            }
            return true;
        }

    }
}
