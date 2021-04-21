using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.Input;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Daemon.Input;
using System.Threading;

namespace GestureSign.Daemon
{
    class MessageProcessor : IMessageProcessor
    {
        private SynchronizationContext _synchronizationContext;

        public MessageProcessor(SynchronizationContext synchronizationContext)
        {
            _synchronizationContext = synchronizationContext;
        }

        public bool ProcessMessages(IpcCommands command, object data)
        {
            _synchronizationContext.Post(state =>
            {
                switch (command)
                {
                    case IpcCommands.StartTeaching:
                        PointCapture.Instance.Mode = CaptureMode.Training;
                        break;
                    case IpcCommands.StopTraining:
                        if (PointCapture.Instance.Mode != CaptureMode.UserDisabled)
                            PointCapture.Instance.Mode = CaptureMode.Normal;
                        break;
                    case IpcCommands.LoadApplications:
                        ApplicationManager.Instance.LoadApplications().Wait();
                        break;
                    case IpcCommands.LoadGestures:
                        GestureManager.Instance.LoadGestures().Wait();
                        break;
                    case IpcCommands.LoadConfiguration:
                        AppConfig.Reload();
                        break;
                    case IpcCommands.StartControlPanel:
                        TrayManager.StartControlPanel();
                        break;
                    case IpcCommands.SynTouchPadState:
                        NamedPipe.SendMessageAsync(IpcCommands.SynTouchPadState, Common.Constants.ControlPanel, AppConfig.IsSynTouchPadAvailable, false).Wait();
                        break;
                }
            }, null);

            return true;
        }

    }
}
