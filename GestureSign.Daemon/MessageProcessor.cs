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

        public bool ProcessMessages(CommandEnum command, object data)
        {
            _synchronizationContext.Post(state =>
            {
                switch (command)
                {
                    case CommandEnum.StartTeaching:
                        PointCapture.Instance.Mode = CaptureMode.Training;
                        break;
                    case CommandEnum.StopTraining:
                        if (PointCapture.Instance.Mode != CaptureMode.UserDisabled)
                            PointCapture.Instance.Mode = CaptureMode.Normal;
                        break;
                    case CommandEnum.LoadApplications:
                        ApplicationManager.Instance.LoadApplications().Wait();
                        break;
                    case CommandEnum.LoadGestures:
                        GestureManager.Instance.LoadGestures().Wait();
                        break;
                    case CommandEnum.LoadConfiguration:
                        AppConfig.Reload();
                        break;
                    case CommandEnum.StartControlPanel:
                        TrayManager.StartControlPanel();
                        break;
                    case CommandEnum.SynTouchPadState:
                        NamedPipe.SendMessageAsync(CommandEnum.SynTouchPadState, Common.Constants.ControlPanel, AppConfig.IsSynTouchPadAvailable, false).Wait();
                        break;
                }
            }, null);

            return true;
        }

    }
}
