/*
 * ManagedWinapi - A collection of .NET components that wrap PInvoke calls to 
 * access native API by managed code. http://mwinapi.sourceforge.net/
 * Copyright (C) 2006, 2007 Michael Schierl
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
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace ManagedWinapi.Hooks
{
    /// <summary>
    /// A hook is a point in the system message-handling mechanism where an application
    /// can install a subroutine to monitor the message traffic in the system and process 
    /// certain types of messages before they reach the target window procedure.
    /// </summary>
    public class Hook : Component
    {
        private HookType type;
        internal bool hooked = false;
        private IntPtr hHook;
        private bool wrapCallback, global;
        private IntPtr wrappedDelegate;
        private IntPtr hWrapperInstance;
        private readonly HookProc managedDelegate;

        /// <summary>
        /// Occurs when the hook's callback is called.
        /// </summary>
        public event HookCallback Callback;

        /// <summary>
        /// Represents a method that handles a callback from a hook.
        /// </summary>
        public delegate int HookCallback(int code, IntPtr wParam, IntPtr lParam, ref bool callNext);

        /// <summary>
        /// Creates a new hook and hooks it.
        /// </summary>
        public Hook(HookType type, HookCallback callback, bool wrapCallback, bool global)
            : this(type, wrapCallback, global)
        {
            this.Callback += callback;
            StartHook();
        }

        /// <summary>
        /// Creates a new hook.
        /// </summary>
        public Hook(HookType type, bool wrapCallback, bool global)
            : this()
        {
            this.type = type;
            this.wrapCallback = wrapCallback;
            this.global = global;
        }

        /// <summary>
        /// Creates a new hook.
        /// </summary>
        public Hook(IContainer container)
            : this()
        {
            container.Add(this);
        }

        /// <summary>
        /// Creates a new hook.
        /// </summary>
        public Hook()
        {
            managedDelegate = new HookProc(InternalCallback);
        }

        /// <summary>
        /// The type of the hook.
        /// </summary>
        public HookType Type
        {
            get { return type; }
            set { type = value; }
        }

        /// <summary>
        /// Whether this hook has been started.
        /// </summary>
        public bool Hooked
        {
            get { return hooked; }
        }

        /// <summary>
        /// Hooks the hook.
        /// </summary>
        public virtual void StartHook()
        {
            if (hooked) return;
            IntPtr delegt = Marshal.GetFunctionPointerForDelegate(managedDelegate);
            if (wrapCallback)
            {
                wrappedDelegate = AllocHookWrapper(delegt);
                hWrapperInstance = LoadLibrary("ManagedWinapiNativeHelper.dll");
                hHook = SetWindowsHookEx(type, wrappedDelegate, hWrapperInstance, 0);
            }
            else if (global)
            {
                hHook = SetWindowsHookEx(type, delegt, Marshal.GetHINSTANCE(typeof(Hook).Assembly.GetModules()[0]), 0);
            }
            else
            {
                hHook = SetWindowsHookEx(type, delegt, IntPtr.Zero, getThreadID());
            }
            if (hHook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            hooked = true;
        }

        private uint getThreadID()
        {
#pragma warning disable 0618
            return (uint)AppDomain.GetCurrentThreadId();
#pragma warning restore 0618
        }

        /// <summary>
        /// Unhooks the hook.
        /// </summary>
        public virtual void Unhook()
        {
            if (!hooked) return;
            if (!UnhookWindowsHookEx(hHook)) throw new Win32Exception(Marshal.GetLastWin32Error());
            if (wrapCallback)
            {
                if (!FreeHookWrapper(wrappedDelegate)) throw new Win32Exception();
                if (!FreeLibrary(hWrapperInstance)) throw new Win32Exception();
            }
            hooked = false;
        }

        /// <summary>
        /// Unhooks the hook if necessary.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (hooked)
            {
                Unhook();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Override this method if you want to prevent a call
        /// to the CallNextHookEx method or if you want to return
        /// a different return value. For most hooks this is not needed.
        /// </summary>
        protected virtual int InternalCallback(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0 && Callback != null)
            {
                bool callNext = true;
                int retval = Callback(code, wParam, lParam, ref callNext);
                if (!callNext) return retval;
            }
            return CallNextHookEx(hHook, code, wParam, lParam);
        }

        #region PInvoke Declarations

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(HookType hook, IntPtr callback,
           IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        internal static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam,
           IntPtr lParam);

        [DllImport("ManagedWinapiNativeHelper.dll")]
        private static extern IntPtr AllocHookWrapper(IntPtr callback);

        [DllImport("ManagedWinapiNativeHelper.dll")]
        private static extern bool FreeHookWrapper(IntPtr wrapper);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        private delegate int HookProc(int code, IntPtr wParam, IntPtr lParam);

        internal static readonly int HC_ACTION = 0,
            HC_GETNEXT = 1,
            HC_SKIP = 2,
            HC_NOREMOVE = 3,
            HC_SYSMODALON = 4,
            HC_SYSMODALOFF = 5;
        #endregion
    }

    /// <summary>
    /// A hook that intercepts local window messages.
    /// </summary>
    public class LocalMessageHook : Hook
    {
        /// <summary>
        /// Called when a message has been intercepted.
        /// </summary>
        public event MessageCallback MessageOccurred;

        /// <summary>
        /// Represents a method that handles a message from a message hook.
        /// </summary>
        /// <param name="msg"></param>
        public delegate void MessageCallback(Message msg);

        /// <summary>
        /// Creates a local message hook and hooks it.
        /// </summary>
        /// <param name="callback"></param>
        public LocalMessageHook(MessageCallback callback)
            : this()
        {
            this.MessageOccurred = callback;
            StartHook();
        }

        /// <summary>
        /// Creates a local message hook.
        /// </summary>
        public LocalMessageHook()
            : base(HookType.WH_GETMESSAGE, false, false)
        {
            base.Callback += MessageHookCallback;
        }

        private int MessageHookCallback(int code, IntPtr lParam, IntPtr wParam, ref bool callNext)
        {
            if (code == HC_ACTION)
            {
                Message msg = (Message)Marshal.PtrToStructure(wParam, typeof(Message));
                if (MessageOccurred != null)
                {
                    MessageOccurred(msg);
                }
            }
            return 0;
        }
    }

    /// <summary>
    /// Hook Types. See the documentation of SetWindowsHookEx for reference.
    /// </summary>
    public enum HookType : int
    {
        ///
        WH_JOURNALRECORD = 0,
        ///
        WH_JOURNALPLAYBACK = 1,
        ///
        WH_KEYBOARD = 2,
        ///
        WH_GETMESSAGE = 3,
        ///
        WH_CALLWNDPROC = 4,
        ///
        WH_CBT = 5,
        ///
        WH_SYSMSGFILTER = 6,
        ///
        WH_MOUSE = 7,
        ///
        WH_HARDWARE = 8,
        ///
        WH_DEBUG = 9,
        ///
        WH_SHELL = 10,
        ///
        WH_FOREGROUNDIDLE = 11,
        ///
        WH_CALLWNDPROCRET = 12,
        ///
        WH_KEYBOARD_LL = 13,
        ///
        WH_MOUSE_LL = 14
    }
}
