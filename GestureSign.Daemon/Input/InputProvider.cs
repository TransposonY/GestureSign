using GestureSign.Common.Configuration;
using GestureSign.Common.InterProcessCommunication;
using ManagedWinapi.Hooks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GestureSign.Daemon.Input
{
    internal class InputProvider : IDisposable
    {
        private bool disposedValue = false; // To detect redundant calls

        public TouchInputProcessor TouchInputProcessor;
        private const string TouchInputProvider = "GestureSign_TouchInputProvider";
        public LowLevelMouseHook LowLevelMouseHook;

        public InputProvider()
        {
            RunPipeServer();

            AppConfig.ConfigChanged += AppConfig_ConfigChanged;
            LowLevelMouseHook = new LowLevelMouseHook();
            if (AppConfig.DrawingButton != MouseActions.None)
                Task.Delay(1000).ContinueWith((t) =>
                {
                    LowLevelMouseHook.StartHook();
                }, TaskScheduler.FromCurrentSynchronizationContext());
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

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    AppConfig.ConfigChanged -= AppConfig_ConfigChanged;
                }

                LowLevelMouseHook?.Unhook();
                disposedValue = true;
            }
        }

        ~InputProvider()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
