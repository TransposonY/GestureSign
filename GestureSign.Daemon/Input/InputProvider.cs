using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using ManagedWinapi.Hooks;
using System;
using System.Threading.Tasks;

namespace GestureSign.Daemon.Input
{
    internal class InputProvider : IDisposable
    {
        private bool disposedValue = false; // To detect redundant calls
        private MessageWindow _messageWindow;
        private SynTouchPad _synTouchPad;

        public LowLevelMouseHook LowLevelMouseHook;
        public event RawPointsDataMessageEventHandler PointsIntercepted;

        public InputProvider()
        {
            _messageWindow = new MessageWindow();
            _messageWindow.PointsIntercepted += MessageWindow_PointsIntercepted;

            AppConfig.ConfigChanged += AppConfig_ConfigChanged;
            LowLevelMouseHook = new LowLevelMouseHook();
            if (AppConfig.DrawingButton != MouseActions.None)
                Task.Delay(1000).ContinueWith((t) =>
                {
                    LowLevelMouseHook.StartHook();
                }, TaskScheduler.FromCurrentSynchronizationContext());

            UpdateSynTouchPadState();
        }

        private void AppConfig_ConfigChanged(object sender, System.EventArgs e)
        {
            if (AppConfig.DrawingButton != MouseActions.None)
                LowLevelMouseHook.StartHook();
            else LowLevelMouseHook.Unhook();

            _messageWindow.UpdateRegistration();
        }

        private void MessageWindow_PointsIntercepted(object sender, RawPointsDataMessageEventArgs e)
        {
            if (e.RawData.Count == 0)
                return;
            PointsIntercepted?.Invoke(this, e);
        }

        private void UpdateSynTouchPadState()
        {
            if (_synTouchPad != null)
            {
                _synTouchPad.PointsIntercepted -= MessageWindow_PointsIntercepted;
                _synTouchPad.Dispose();
                _synTouchPad = null;
            }
            if (AppConfig.RegisterTouchPad)
            {
                _synTouchPad = new SynTouchPad();
                if (_synTouchPad.IsAvailable)
                {
                    _synTouchPad.PointsIntercepted += MessageWindow_PointsIntercepted;
                    _synTouchPad.Initialize();
                }
                else
                {
                    _synTouchPad.Dispose();
                    _synTouchPad = null;
                }
            }
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
