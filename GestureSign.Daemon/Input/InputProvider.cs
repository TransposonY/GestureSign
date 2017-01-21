using System.Threading;
using GestureSign.Common.Configuration;
using GestureSign.Common.InterProcessCommunication;
using ManagedWinapi.Hooks;

namespace GestureSign.Daemon.Input
{
    internal class InputProvider
    {
        public TouchInputProcessor TouchInputProcessor;
        private const string TouchInputProvider = "GestureSign_TouchInputProvider";
        public LowLevelMouseHook LowLevelMouseHook;

        public InputProvider()
        {
            RunPipeServer();

            AppConfig.ConfigChanged += AppConfig_ConfigChanged;
            LowLevelMouseHook = new LowLevelMouseHook();

            if (AppConfig.DrawingButton != MouseActions.None)
                LowLevelMouseHook.StartHook();
        }

        private void AppConfig_ConfigChanged(object sender, System.EventArgs e)
        {
            if (AppConfig.DrawingButton != MouseActions.None)
                LowLevelMouseHook.StartHook();
            else LowLevelMouseHook.Unhook();
        }

        private void RunPipeServer()
        {
            SynchronizationContext uiContext = SynchronizationContext.Current;
            TouchInputProcessor = new TouchInputProcessor(uiContext);

            NamedPipe.Instance.RunPersistentPipeConnection(TouchInputProvider, TouchInputProcessor);
        }
    }
}
