/*
 * ManagedWinapi - A collection of .NET components that wrap PInvoke calls to 
 * access native API by managed code. http://mwinapi.sourceforge.net/
 * Copyright (C) 2006 Michael Schierl
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; see the file COPYING. if not, visit
 * http://www.gnu.org/licenses/lgpl.html or write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using ManagedWinapi.Windows;

namespace ManagedWinapi
{

    /// <summary>
    /// Specifies a component that creates a global keyboard hotkey.
    /// </summary>
    [DefaultEvent("HotkeyPressed")]
    public class Hotkey : Component
    {

        /// <summary>
        /// Occurs when the hotkey is pressed.
        /// </summary>
        public event EventHandler HotkeyPressed;

        private static Object myStaticLock = new Object();
        private static int hotkeyCounter = 0xA000;

        private int hotkeyIndex;
        private bool isDisposed = false, isEnabled = false, isRegistered = false;
        private Keys _keyCode;
        private bool _ctrl, _alt, _shift, _windows;
        private readonly IntPtr hWnd;

        /// <summary>
        /// Initializes a new instance of this class with the specified container.
        /// </summary>
        /// <param name="container">The container to add it to.</param>
        public Hotkey(IContainer container) : this()
        {
            container.Add(this);
        }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        public Hotkey() 
        {
            EventDispatchingNativeWindow.Instance.EventHandler += nw_EventHandler;
            lock(myStaticLock) 
            {
                hotkeyIndex = ++hotkeyCounter;
            }
            hWnd = EventDispatchingNativeWindow.Instance.Handle;
        }

        /// <summary>
        /// Enables the hotkey. When the hotkey is enabled, pressing it causes a
        /// <c>HotkeyPressed</c> event instead of being handled by the active 
        /// application.
        /// </summary>
        public bool Enabled
        {
            get
            {
                return isEnabled;
            }
            set
            {
                isEnabled = value;
                updateHotkey(false);
            }
        }

        /// <summary>
        /// The key code of the hotkey.
        /// </summary>
        public Keys KeyCode
        {
            get
            {
                return _keyCode;
            }

            set
            {
                _keyCode = value;
                updateHotkey(true);
            }
        }

        /// <summary>
        /// Whether the shortcut includes the Control modifier.
        /// </summary>
        public bool Ctrl {
            get { return _ctrl; }
            set {_ctrl = value; updateHotkey(true);}
        }

        /// <summary>
        /// Whether this shortcut includes the Alt modifier.
        /// </summary>
        public bool Alt {
            get { return _alt; }
            set {_alt = value; updateHotkey(true);}
        }     
   
        /// <summary>
        /// Whether this shortcut includes the shift modifier.
        /// </summary>
        public bool Shift {
            get { return _shift; }
            set {_shift = value; updateHotkey(true);}
        }
        
        /// <summary>
        /// Whether this shortcut includes the Windows key modifier. The windows key
        /// is an addition by Microsoft to the keyboard layout. It is located between
        /// Control and Alt and depicts a Windows flag.
        /// </summary>
        public bool WindowsKey {
            get { return _windows; }
            set {_windows = value; updateHotkey(true);}
        }

        void nw_EventHandler(ref Message m, ref bool handled)
        {
            if (handled) return;
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == hotkeyIndex)
            {
                if (HotkeyPressed != null)
                    HotkeyPressed(this, EventArgs.Empty);
                handled = true;
            }
        }

        /// <summary>
        /// Releases all resources used by the System.ComponentModel.Component.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            isDisposed = true;
            updateHotkey(false);
            EventDispatchingNativeWindow.Instance.EventHandler -= nw_EventHandler;
            base.Dispose(disposing);
        }

        private void updateHotkey(bool reregister)
        {
            bool shouldBeRegistered = isEnabled && !isDisposed && !DesignMode;
            if (isRegistered && (!shouldBeRegistered || reregister))
            {
                // unregister hotkey
                UnregisterHotKey(hWnd, hotkeyIndex);
                isRegistered = false;
            }
            if (!isRegistered && shouldBeRegistered)
            {
                // register hotkey
                bool success = RegisterHotKey(hWnd, hotkeyIndex, 
                    (_shift ? MOD_SHIFT : 0) + (_ctrl ? MOD_CONTROL : 0) +
                    (_alt ? MOD_ALT : 0) + (_windows ? MOD_WIN : 0), (int)_keyCode);
                if (!success) throw new HotkeyAlreadyInUseException();
                isRegistered = true;
            }
        }

        #region PInvoke Declarations

        [DllImport("user32.dll", SetLastError=true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        [DllImport("user32.dll", SetLastError=true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private static readonly int MOD_ALT = 0x0001,
            MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004, MOD_WIN = 0x0008;

        private static readonly int WM_HOTKEY = 0x0312;

        #endregion
    }

    /// <summary>
    /// The exception is thrown when a hotkey should be registered that
    /// has already been registered by another application.
    /// </summary>
    public class HotkeyAlreadyInUseException : Exception { }
}
