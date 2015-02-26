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

namespace ManagedWinapi.Hooks
{
    /// <summary>
    /// Abstract base class for hooks that can be used to create or playback 
    /// a log of keyboard and mouse events.
    /// </summary>
    public abstract class JournalHook : Hook
    {
        /// <summary>
        /// Occurs when the journal activity has been cancelled by
        /// CTRL+ALT+DEL or CTRL+ESC.
        /// </summary>
        public event EventHandler JournalCancelled;
        private readonly LocalMessageHook lmh;

        /// <summary>
        /// Creates a new journal hook.
        /// </summary>
        public JournalHook(HookType type)
            : base(type, true, false)
        {
            lmh = new LocalMessageHook();
            lmh.MessageOccurred += new LocalMessageHook.MessageCallback(lmh_Callback);
        }

        private void lmh_Callback(System.Windows.Forms.Message msg)
        {
            if (msg.Msg == WM_CANCELJOURNAL)
            {
                hooked = false;
                lmh.Unhook();
                if (JournalCancelled != null)
                {
                    JournalCancelled(this, new EventArgs());
                }
            }
        }

        /// <summary>
        /// Hooks the hook.
        /// </summary>
        public override void StartHook()
        {
            if (Hooked) return;
            lmh.StartHook();
            base.StartHook();
        }

        /// <summary>
        /// Unhooks the hook.
        /// </summary>
        public override void Unhook()
        {
            if (!Hooked) return;
            base.Unhook();
            lmh.Unhook();
        }

        #region PInvoke Declarations

        private static readonly int WM_CANCELJOURNAL = 0x4B;

        [StructLayout(LayoutKind.Sequential)]
        internal struct EVENTMSG
        {
            public uint message;
            public uint paramL;
            public uint paramH;
            public int time;
            public IntPtr hWnd;
        }
        #endregion
    }

    /// <summary>
    /// An event that has been recorded by a journal hook.
    /// </summary>
    public class JournalMessage
    {
        internal static JournalMessage Create(JournalHook.EVENTMSG msg)
        {
            return new JournalMessage(msg);
        }

        private JournalHook.EVENTMSG msg;
        private JournalMessage(JournalHook.EVENTMSG msg)
        {
            this.msg = msg;
        }

        /// <summary>
        /// Creates a new journal message.
        /// </summary>
        public JournalMessage(IntPtr hWnd, uint message, uint paramL, uint paramH, uint time)
        {
            msg = new JournalHook.EVENTMSG();
            msg.hWnd = hWnd;
            msg.message = message;
            msg.paramL = paramL;
            msg.paramH = paramH;
            msg.time = 0;
        }

        /// <summary>
        /// The window this message has been sent to.
        /// </summary>
        public IntPtr HWnd { get { return msg.hWnd; } }

        /// <summary>
        /// The message.
        /// </summary>
        public uint Message { get { return msg.message; } }

        /// <summary>
        /// The first parameter of the message.
        /// </summary>
        public uint ParamL { get { return msg.paramL; } }

        /// <summary>
        /// The second parameter of the message.
        /// </summary>
        public uint ParamH { get { return msg.paramH; } }

        /// <summary>
        /// The timestamp of the message.
        /// </summary>
        public int Time
        {
            get { return msg.time; }
            set { msg.time = value; }
        }

        /// <summary>
        /// Returns a System.String that represents the current System.Object.
        /// </summary>
        public override string ToString()
        {
            return "JournalMessage[hWnd=" + msg.hWnd + ",message=" + msg.message + ",L=" + msg.paramL +
                ",H=" + msg.paramH + ",time=" + msg.time + "]";
        }
    }

    /// <summary>
    /// Event data for a journal record event.
    /// </summary>
    public class JournalRecordEventArgs : EventArgs
    {
        private JournalMessage msg;

        internal JournalRecordEventArgs(JournalMessage msg)
        {
            this.msg = msg;
        }

        /// <summary>
        /// The recorded message.
        /// </summary>
        public JournalMessage RecordedMessage
        {
            get { return msg; }
        }
    }

    /// <summary>
    /// A hook that can be used to create a log of keyboard and mouse events.
    /// </summary>
    public class JournalRecordHook : JournalHook
    {
        /// <summary>
        /// Occurs when a system modal dialog appears. This may be used
        /// to stop recording.
        /// </summary>
        public event EventHandler SystemModalDialogAppeared;

        /// <summary>
        /// Occurs when a system modal dialog disappears. This may be used
        /// to continue recording.
        /// </summary>
        public event EventHandler SystemModalDialogDisappeared;

        /// <summary>
        /// Occurs when an event can be recorded.
        /// </summary>
        public event EventHandler<JournalRecordEventArgs> RecordEvent;

        /// <summary>
        /// Creates a new journal record hook.
        /// </summary>
        public JournalRecordHook()
            : base(HookType.WH_JOURNALRECORD)
        {
            base.Callback += JournalRecordHook_Callback;
        }

        private int JournalRecordHook_Callback(int code, IntPtr wParam, IntPtr lParam, ref bool callNext)
        {
            if (code == HC_ACTION)
            {
                EVENTMSG em = (EVENTMSG)Marshal.PtrToStructure(lParam, typeof(EVENTMSG));
                JournalMessage jm = JournalMessage.Create(em);
                if (RecordEvent != null)
                {
                    RecordEvent(this, new JournalRecordEventArgs(jm));
                }
            }
            else if (code == HC_SYSMODALON)
            {
                if (SystemModalDialogAppeared != null)
                {
                    SystemModalDialogAppeared(this, new EventArgs());
                }
            }
            else if (code == HC_SYSMODALOFF)
            {
                if (SystemModalDialogDisappeared != null)
                {
                    SystemModalDialogDisappeared(this, new EventArgs());
                }
            }
            return 0;
        }
    }

    /// <summary>
    /// A hook that can be used to playback a log of keyboard and mouse events.
    /// </summary>
    public class JournalPlaybackHook : JournalHook
    {
        /// <summary>
        /// Occurs when a system modal dialog appears. This may be used to 
        /// stop playback.
        /// </summary>
        public event EventHandler SystemModalDialogAppeared;

        /// <summary>
        /// Occurs when a system modal dialog disappears. This may be used
        /// to continue playback.
        /// </summary>
        public event EventHandler SystemModalDialogDisappeared;

        /// <summary>
        /// Occurs when the next journal message is needed. If the message is
        /// <null/> and a timestamp in the future, it just waits for that time and
        /// asks for a message again. If the message is <null/> and the timestamp is
        /// in the past, playback stops.
        /// </summary>
        public event JournalQuery GetNextJournalMessage;
        private int nextEventTime = 0;
        private JournalMessage nextEvent = null;

        /// <summary>
        /// Represents a method that yields the next journal message.
        /// </summary>
        public delegate JournalMessage JournalQuery(ref int timestamp);

        /// <summary>
        /// Creates a new journal playback hook.
        /// </summary>
        public JournalPlaybackHook()
            : base(HookType.WH_JOURNALPLAYBACK)
        {
            base.Callback += JournalPlaybackHook_Callback;
        }

        private int JournalPlaybackHook_Callback(int code, IntPtr wParam, IntPtr lParam, ref bool callNext)
        {
            if (code == HC_GETNEXT)
            {
                callNext = false;
                int tick = Environment.TickCount;
                if (nextEventTime > tick)
                {
                    return nextEventTime - tick;
                }
                if (nextEvent == null)
                {
                    nextEventTime = 0;
                    nextEvent = GetNextJournalMessage(ref nextEventTime);
                    if (nextEventTime <= tick)
                    {
                        if (nextEvent == null)
                        {
                            // shutdown the hook
                            Unhook();
                            return 1;
                        }
                        else
                        {
                            nextEventTime = nextEvent.Time;
                        }
                    }
                    if (nextEventTime > tick)
                    {
                        return nextEventTime - tick;
                    }
                }
                // now we have the next event, which should be sent
                EVENTMSG em = (EVENTMSG)Marshal.PtrToStructure(lParam, typeof(EVENTMSG));
                em.hWnd = nextEvent.HWnd;
                em.time = nextEvent.Time;
                em.message = nextEvent.Message;
                em.paramH = nextEvent.ParamH;
                em.paramL = nextEvent.ParamL;
                Marshal.StructureToPtr(em, lParam, false);
                return 0;
            }
            else if (code == HC_SKIP)
            {
                nextEvent = null;
                nextEventTime = 0;
            }
            else if (code == HC_SYSMODALON)
            {
                if (SystemModalDialogAppeared != null)
                    SystemModalDialogAppeared(this, new EventArgs());
            }
            else if (code == HC_SYSMODALOFF)
            {
                if (SystemModalDialogDisappeared != null)
                    SystemModalDialogDisappeared(this, new EventArgs());
            }
            return 0;
        }
    }

    /// <summary>
    /// Convenience class that uses a journal playback hook to block keyboard
    /// and mouse input for some time.
    /// </summary>
    public class InputLocker : IDisposable
    {

        private int interval, count;
        private JournalPlaybackHook hook;

        /// <summary>
        /// Locks the input for <code>interval * count</code> milliseconds. The
        /// lock can be canceled every <code>interval</code> milliseconds. If count is
        /// negative, the lock will be active until cancelled.
        /// </summary>
        /// <param name="interval">The interval to lock the input.</param>
        /// <param name="count">How often to lock the input.</param>
        /// <param name="force">If <code>true</code>, the lock cannot be canceled
        /// by pressing Control+Alt+Delete</param>
        public InputLocker(int interval, int count, bool force)
        {
            this.interval = interval;
            this.count = count;
            hook = new JournalPlaybackHook();
            hook.GetNextJournalMessage += new JournalPlaybackHook.JournalQuery(hook_GetNextJournalMessage);
            if (force)
                hook.JournalCancelled += new EventHandler(hook_JournalCancelled);
            hook.StartHook();
        }

        private void hook_JournalCancelled(object sender, EventArgs e)
        {
            if (count >= 0) count++;
            hook.StartHook();
        }

        private JournalMessage hook_GetNextJournalMessage(ref int timestamp)
        {
            if (count == 0) return null;
            timestamp = Environment.TickCount + interval;
            if (count > 0) count--;
            return null;
        }

        /// <summary>
        /// Unlocks the input.
        /// </summary>
        public void Unlock()
        {
            count = 0;
        }

        /// <summary>
        /// Unlocks the input.
        /// </summary>
        public void Dispose()
        {
            Unlock();
            hook.Dispose();
        }

        /// <summary>
        /// Lock input for given number of milliseconds
        /// </summary>
        /// <param name="millis">Number of milliseconds to lock</param>
        /// <param name="force">If <code>true</code>, the lock cannot be canceled
        /// by pressing Control+Alt+Delete</param>
        public static void LockInputFor(int millis, bool force)
        {
            new InputLocker(millis, 1, force);
        }
    }
}
