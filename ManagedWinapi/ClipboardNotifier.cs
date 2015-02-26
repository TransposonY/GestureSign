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
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ManagedWinapi.Windows;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ManagedWinapi
{
    /// <summary>
    /// Specifies a component that monitors the system clipboard for changes.
    /// </summary>
    [DefaultEvent("ClipboardChanged")]
    public class ClipboardNotifier : Component
    {

        /// <summary>
        /// Occurs when the clipboard contents have changed.
        /// </summary>
        public event EventHandler ClipboardChanged;

        private readonly IntPtr hWnd;
        private IntPtr nextHWnd;
        private readonly EventDispatchingNativeWindow ednw;

        private static Boolean instantiated = false;

        /// <summary>
        /// Creates a new clipboard notifier.
        /// </summary>
        /// <param name="container">The container.</param>
        public ClipboardNotifier(IContainer container)
            : this()
        {
            container.Add(this);
        }

        /// <summary>
        /// Creates a new clipboard notifier.
        /// </summary>
        public ClipboardNotifier()
        {
            if (instantiated)
            {
                // use new windows if more than one instance is used.
                System.Diagnostics.Debug.WriteLine("WARNING: More than one ClipboardNotifier used!");
                ednw = new EventDispatchingNativeWindow();
            }
            else
            {
                ednw = EventDispatchingNativeWindow.Instance;
                instantiated = true;
            }
            ednw.EventHandler += clipboardEventHandler;
            hWnd = ednw.Handle;
            nextHWnd = SetClipboardViewer(hWnd);
        }

        /// <summary>
        /// Frees resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            ChangeClipboardChain(hWnd, nextHWnd);
            ednw.EventHandler -= clipboardEventHandler;
            base.Dispose(disposing);
        }

        void clipboardEventHandler(ref System.Windows.Forms.Message m, ref bool handled)
        {
            if (handled) return;
            if (m.Msg == WM_DRAWCLIPBOARD)
            {
                // notify me
                if (ClipboardChanged != null)
                    ClipboardChanged(this, EventArgs.Empty);
                // pass on message
                SendMessage(nextHWnd, m.Msg, m.WParam, m.LParam);
                handled = true;
            }
            else if (m.Msg == WM_CHANGECBCHAIN)
            {
                if (m.WParam == nextHWnd)
                {
                    nextHWnd = m.LParam;
                }
                else
                {
                    SendMessage(nextHWnd, m.Msg, m.WParam, m.LParam);
                }
            }
        }

        #region PInvoke Declarations

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("user32.dll")]
        private static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        private static readonly int 
            WM_DRAWCLIPBOARD = 0x0308, 
            WM_CHANGECBCHAIN = 0x030D;

        #endregion
    }
}
