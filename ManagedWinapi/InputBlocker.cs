using System;
using System.Runtime.InteropServices;

namespace ManagedWinapi
{
    /// <summary>
    /// Blocks keyboard and mouse input until this object is disposed.
    /// Unlike <see cref="ManagedWinapi.Hooks.InputLocker"/>, you cannot detect when the systems
    /// removes the block (which happens when the user presses CTRL+ALT+DEL),
    /// but it works on Windows Vista as well.
    /// </summary>
    public class InputBlocker : IDisposable
    {
        bool needUnblock;

        /// <summary>
        /// Blocks keyboard and mouse input until this object is disposed.
        /// </summary>
        public InputBlocker()
        {
            needUnblock = BlockInput(true);
        }

        /// <summary>
        /// Unblocks keyboard and mouse input.
        /// </summary>
        public void Dispose()
        {
            if (needUnblock) BlockInput(false);
            needUnblock = false;
        }

        #region PInvoke Declarations
        [DllImport("user32.dll")]
        static extern bool BlockInput(bool fBlockIt);
        #endregion
    }
}
