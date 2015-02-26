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
using System.Windows.Forms;

namespace ManagedWinapi.Windows
{

    /// <summary>
    /// Called by an EventDispatchingNativeWindow when a window message is received
    /// </summary>
    /// <param name="m">The message to handle.</param>
    /// <param name="handled">Whether the event has already been handled. If this value is true, the handler
    /// should return immediately. It may set the value to true to indicate that no others 
    /// should handle it. If the event is not handled by any handler, it is passed to the
    /// default WindowProc.</param>
    public delegate void WndProcEventHandler(ref Message m, ref bool handled);

    /// <summary>
    /// A Win32 native window that delegates window messages to handlers. So several
    /// components can use the same native window to save "USER resources". This class
    /// is useful when writing your own components.
    /// </summary>
    public class EventDispatchingNativeWindow : NativeWindow
    {

        private static Object myLock = new Object();
        private static EventDispatchingNativeWindow _instance;

        /// <summary>
        /// A global instance which can be used by components that do not need
        /// their own window.
        /// </summary>
        public static EventDispatchingNativeWindow Instance
        {
            get
            {
                lock (myLock)
                {
                    if (_instance == null)
                        _instance = new EventDispatchingNativeWindow();
                    return _instance;
                }
            }
        }

        /// <summary>
        /// Attach your event handlers here.
        /// </summary>
        public event WndProcEventHandler EventHandler;

        /// <summary>
        /// Create your own event dispatching window.
        /// </summary>
        public EventDispatchingNativeWindow()
        {
            CreateHandle(new CreateParams());
        }

        /// <summary>
        /// Parse messages passed to this window and send them to the event handlers.
        /// </summary>
        /// <param name="m">A System.Windows.Forms.Message that is associated with the 
        /// current Windows message.</param>
        protected override void WndProc(ref Message m)
        {
            bool handled = false;
            if (EventHandler != null)
                EventHandler(ref m, ref handled);
            if (!handled)
                base.WndProc(ref m);
        }
    }
}
