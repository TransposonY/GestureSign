using System;

namespace GestureSign.Daemon.Input
{
    public class ForegroundChangedEventArgs : EventArgs
    {
        #region Constructors

        public ForegroundChangedEventArgs(IntPtr hwnd)
        {
            Hwnd = hwnd;
        }

        #endregion

        #region Public Properties

        public IntPtr Hwnd { get; set; }

        #endregion
    }
}
