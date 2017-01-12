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
    public class Hotkey : IDisposable//: Component
    {

        /// <summary>
        /// Occurs when the hotkey is pressed.
        /// </summary>
        public event EventHandler HotkeyPressed;

        private static Object _myStaticLock = new Object();
        private static int _hotkeyCounter = 0xA000;

        private readonly int _hotkeyIndex;
        private bool _isDisposed = false, _isEnabled = false, _isRegistered = false;
        private int _keyCode;
        //private bool _ctrl, _alt, _shift, _windows;
        private int _modifierKeys;
        private readonly IntPtr hWnd;
        private readonly EventDispatchingNativeWindow nativeWindow;

        ///// <summary>
        ///// Initializes a new instance of this class with the specified container.
        ///// </summary>
        ///// <param name="container">The container to add it to.</param>
        //public Hotkey(IContainer container) : this()
        //{
        //    container.Add(this);
        //}

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        public Hotkey()
        {
            nativeWindow = EventDispatchingNativeWindow.Instance;
            nativeWindow.EventHandler += nw_EventHandler;
            lock (_myStaticLock)
            {
                _hotkeyIndex = ++_hotkeyCounter;
            }
            hWnd = nativeWindow.Handle;
        }

        /// <summary>
        /// Enables the hotkey. When the hotkey is enabled, pressing it causes a
        /// <c>HotkeyPressed</c> event instead of being handled by the active 
        /// application.
        /// </summary>
        private bool Enabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                _isEnabled = value;
                updateHotkey(false);
            }
        }

        /// <summary>
        /// The key code of the hotkey.
        /// </summary>
        public int KeyCode
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

        public int ModifierKeys
        {
            get { return _modifierKeys; }
            set
            {
                _modifierKeys = value;
                updateHotkey(true);
            }
        }

        ///// <summary>
        ///// Whether the shortcut includes the Control modifier.
        ///// </summary>
        //public bool Ctrl {
        //    get { return _ctrl; }
        //    set {_ctrl = value; updateHotkey(true);}
        //}

        ///// <summary>
        ///// Whether this shortcut includes the Alt modifier.
        ///// </summary>
        //public bool Alt {
        //    get { return _alt; }
        //    set {_alt = value; updateHotkey(true);}
        //}     

        ///// <summary>
        ///// Whether this shortcut includes the shift modifier.
        ///// </summary>
        //public bool Shift {
        //    get { return _shift; }
        //    set {_shift = value; updateHotkey(true);}
        //}

        ///// <summary>
        ///// Whether this shortcut includes the Windows key modifier. The windows key
        ///// is an addition by Microsoft to the keyboard layout. It is located between
        ///// Control and Alt and depicts a Windows flag.
        ///// </summary>
        //public bool WindowsKey {
        //    get { return _windows; }
        //    set {_windows = value; updateHotkey(true);}
        //}

        public void Register()
        {
            Enabled = true;
        }

        public void Unregister()
        {
            Enabled = false;
        }

        public bool Equals(Hotkey other)
        {
            if (object.ReferenceEquals(null, other))
                return false;
            if (object.ReferenceEquals(this, other))
                return true;
            return Equals(other.KeyCode, this.KeyCode) && Equals(other.ModifierKeys, this.ModifierKeys);
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(null, obj))
                return false;
            if (object.ReferenceEquals(this, obj))
                return true;
            return obj.GetType() == typeof(Hotkey) && this.Equals((Hotkey)obj);
        }

        public override int GetHashCode()
        {
            return this.KeyCode.GetHashCode() * 397 ^ this.ModifierKeys.GetHashCode();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void nw_EventHandler(ref Message m, ref bool handled)
        {
            if (handled) return;
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == _hotkeyIndex)
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
        protected void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Release managed resources
                }
                _isDisposed = true;
                updateHotkey(false);
                nativeWindow.EventHandler -= nw_EventHandler;
            }
        }

        private void updateHotkey(bool reregister)
        {
            bool shouldBeRegistered = _isEnabled && !_isDisposed;
            if (_isRegistered && (!shouldBeRegistered || reregister))
            {
                // unregister hotkey
                UnregisterHotKey(hWnd, _hotkeyIndex);
                _isRegistered = false;
            }
            if (!_isRegistered && shouldBeRegistered)
            {
                // register hotkey
                bool success = RegisterHotKey(hWnd, _hotkeyIndex, _modifierKeys, _keyCode);
                if (!success) throw new HotkeyAlreadyInUseException();
                _isRegistered = true;
            }
        }

        ~Hotkey()
        {
            Dispose(false);
        }

        #region PInvoke Declarations

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private static readonly int WM_HOTKEY = 0x0312;

        #endregion
    }

    /// <summary>
    /// The exception is thrown when a hotkey should be registered that
    /// has already been registered by another application.
    /// </summary>
    public class HotkeyAlreadyInUseException : Exception { }
}
