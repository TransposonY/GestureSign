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
using System.ComponentModel;
using System.Runtime.InteropServices;
using Accessibility;

namespace ManagedWinapi.Accessibility
{
    /// <summary>
    /// Listens to events from the Windows accessibility system. These events are useful
    /// if you want to write a screenreader or similar program.
    /// </summary>
    public class AccessibleEventListener : Component
    {
        /// <summary>
        /// Occurs when an accessible event is received.
        /// </summary>
        public event AccessibleEventHandler EventOccurred;

        private bool enabled;
        private IntPtr handle = IntPtr.Zero;
        private AccessibleEventType min = AccessibleEventType.EVENT_MIN;
        private AccessibleEventType max = AccessibleEventType.EVENT_MAX;
        private WinEventDelegate internalDelegate;
        private GCHandle gch;
        private UInt32 processId = 0;
        private UInt32 threadId = 0;

        /// <summary>
        /// Initializes a new instance of this class with the specified container.
        /// </summary>
        /// <param name="container">The container to add it to.</param>
        public AccessibleEventListener(IContainer container)
            : this()
        {
            container.Add(this);
        }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        public AccessibleEventListener()
        {
            internalDelegate = new WinEventDelegate(InternalCallback);
            gch = GCHandle.Alloc(internalDelegate);
        }

        /// <summary>
        /// Enables this listener so that it reports accessible events.
        /// </summary>
        public bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                enabled = value;
                updateListener();
            }
        }

        /// <summary>
        /// The minimal event type to listen to.
        /// </summary>
        public AccessibleEventType MinimalEventType
        {
            get { return min; }
            set { min = value; updateListener(); }
        }

        /// <summary>
        /// The maximal event type to listen to.
        /// </summary>
        public AccessibleEventType MaximalEventType
        {
            get { return max; }
            set { max = value; updateListener(); }
        }

        /// <summary>
        /// The Process ID to listen to.
        /// Default 0 listens to all processes.
        /// </summary>
        public UInt32 ProcessId
        {
            get { return processId; }
            set { processId = value; updateListener(); }
        }

        /// <summary>
        /// The Thread ID to listen to.
        /// Default 0 listens to all threads.
        /// </summary> 
        public UInt32 ThreadId
        {
            get { return threadId; }
            set { threadId = value; updateListener(); }
        }

        private void updateListener()
        {
            if (handle != IntPtr.Zero)
            {
                UnhookWinEvent(handle);
                handle = IntPtr.Zero;
            }
            if (enabled)
            {
                handle = SetWinEventHook(min, max, IntPtr.Zero, internalDelegate, processId, threadId, 0);
            }
        }

        /// <summary>
        /// Releases all resources used by the System.ComponentModel.Component.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (enabled)
            {
                enabled = false;
                updateListener();
            }
            gch.Free();
            base.Dispose(disposing);
        }

        private void InternalCallback(IntPtr hWinEventHook, AccessibleEventType eventType,
            IntPtr hwnd, uint idObject, uint idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hWinEventHook != handle) return;
            AccessibleEventArgs aea = new AccessibleEventArgs(eventType, hwnd, idObject, idChild, dwEventThread, dwmsEventTime);
            if (EventOccurred != null)
                EventOccurred(this, aea);
        }

        internal static SystemAccessibleObject GetAccessibleObject(AccessibleEventArgs e)
        {
            IAccessible iacc;
            object child;
            uint result = AccessibleObjectFromEvent(e.HWnd, e.ObjectID, e.ChildID, out iacc, out child);
            if (result != 0) throw new Exception("AccessibleObjectFromPoint returned " + result);
            return new SystemAccessibleObject(iacc, (int)child);
        }

        #region PInvoke Declarations

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(AccessibleEventType eventMin, AccessibleEventType eventMax, IntPtr
           hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess,
           uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        private delegate void WinEventDelegate(IntPtr hWinEventHook, AccessibleEventType eventType,
            IntPtr hwnd, uint idObject, uint idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("oleacc.dll")]
        private static extern uint AccessibleObjectFromEvent(IntPtr hwnd, uint dwObjectID, uint dwChildID, out IAccessible ppacc, [MarshalAs(UnmanagedType.Struct)] out object pvarChild);

        #endregion
    }

    /// <summary>
    /// Represents the method that will handle accessibility events.
    /// </summary>
    public delegate void AccessibleEventHandler(object sender, AccessibleEventArgs e);

    /// <summary>
    /// Provides data for accessible events.
    /// </summary>
    public class AccessibleEventArgs : EventArgs
    {
        private AccessibleEventType eventType;
        private IntPtr hWnd;
        private uint idObject;
        private uint idChild;
        private uint dwEventThread;
        private uint dwmsEventTime;

        /// <summary>
        /// Initializes a new instance of the AccessibleEventArgs class.
        /// </summary>
        public AccessibleEventArgs(AccessibleEventType eventType,
            IntPtr hwnd, uint idObject, uint idChild, uint dwEventThread, uint dwmsEventTime)
        {
            this.eventType = eventType;
            this.hWnd = hwnd;
            this.idObject = idObject;
            this.idChild = idChild;
            this.dwEventThread = dwEventThread;
            this.dwmsEventTime = dwmsEventTime;
        }

        /// <summary>
        /// Type of this accessible event
        /// </summary>
        public AccessibleEventType EventType
        {
            get { return eventType; }
        }

        /// <summary>
        /// Handle of the affected window, if any.
        /// </summary>
        public IntPtr HWnd
        {
            get { return hWnd; }
        }

        /// <summary>
        /// Object ID.
        /// </summary>
        public uint ObjectID
        {
            get { return idObject; }
        }

        /// <summary>
        /// Child ID.
        /// </summary>
        public uint ChildID
        {
            get { return idChild; }
        }

        /// <summary>
        /// The thread that generated this event.
        /// </summary>
        public uint Thread
        {
            get { return dwEventThread; }
        }

        /// <summary>
        /// Time in milliseconds when the event was generated.
        /// </summary>
        public uint Time
        {
            get { return dwmsEventTime; }
        }

        /// <summary>
        /// The accessible object related to this event.
        /// </summary>
        public SystemAccessibleObject AccessibleObject
        {
            get
            {
                return AccessibleEventListener.GetAccessibleObject(this);
            }
        }
    }

    /// <summary>
    /// This enumeration lists known accessible event types.
    /// </summary>
    public enum AccessibleEventType
    {
        /// <summary>
        ///  Sent when a sound is played.  Currently nothing is generating this, we
        ///  are going to be cleaning up the SOUNDSENTRY feature in the control panel
        ///  and will use this at that time.  Applications implementing WinEvents
        ///  are perfectly welcome to use it.  Clients of IAccessible* will simply
        ///  turn around and get back a non-visual object that describes the sound.
        /// </summary>
        EVENT_SYSTEM_SOUND = 0x0001,

        /// <summary>
        /// Sent when an alert needs to be given to the user.  MessageBoxes generate
        /// alerts for example.
        /// </summary>
        EVENT_SYSTEM_ALERT = 0x0002,

        /// <summary>
        /// Sent when the foreground (active) window changes, even if it is changing
        /// to another window in the same thread as the previous one.
        /// </summary>
        EVENT_SYSTEM_FOREGROUND = 0x0003,

        /// <summary>
        /// Sent when entering into and leaving from menu mode (system, app bar, and
        /// track popups).
        /// </summary>
        EVENT_SYSTEM_MENUSTART = 0x0004,

        /// <summary>
        /// Sent when entering into and leaving from menu mode (system, app bar, and
        /// track popups).
        /// </summary>
        EVENT_SYSTEM_MENUEND = 0x0005,

        /// <summary>
        /// Sent when a menu popup comes up and just before it is taken down.  Note
        /// that for a call to TrackPopupMenu(), a client will see EVENT_SYSTEM_MENUSTART
        /// followed almost immediately by EVENT_SYSTEM_MENUPOPUPSTART for the popup
        /// being shown.
        /// </summary>
        EVENT_SYSTEM_MENUPOPUPSTART = 0x0006,

        /// <summary>
        /// Sent when a menu popup comes up and just before it is taken down.  Note
        /// that for a call to TrackPopupMenu(), a client will see EVENT_SYSTEM_MENUSTART
        /// followed almost immediately by EVENT_SYSTEM_MENUPOPUPSTART for the popup
        /// being shown.
        /// </summary>
        EVENT_SYSTEM_MENUPOPUPEND = 0x0007,


        /// <summary>
        /// Sent when a window takes the capture and releases the capture.
        /// </summary>
        EVENT_SYSTEM_CAPTURESTART = 0x0008,

        /// <summary>
        /// Sent when a window takes the capture and releases the capture.
        /// </summary>
        EVENT_SYSTEM_CAPTUREEND = 0x0009,

        /// <summary>
        /// Sent when a window enters and leaves move-size dragging mode.
        /// </summary>
        EVENT_SYSTEM_MOVESIZESTART = 0x000A,

        /// <summary>
        /// Sent when a window enters and leaves move-size dragging mode.
        /// </summary>
        EVENT_SYSTEM_MOVESIZEEND = 0x000B,

        /// <summary>
        /// Sent when a window enters and leaves context sensitive help mode.
        /// </summary>
        EVENT_SYSTEM_CONTEXTHELPSTART = 0x000C,

        /// <summary>
        /// Sent when a window enters and leaves context sensitive help mode.
        /// </summary>
        EVENT_SYSTEM_CONTEXTHELPEND = 0x000D,

        /// <summary>
        /// Sent when a window enters and leaves drag drop mode.  Note that it is up
        /// to apps and OLE to generate this, since the system doesn't know.  Like
        /// EVENT_SYSTEM_SOUND, it will be a while before this is prevalent.
        /// </summary>
        EVENT_SYSTEM_DRAGDROPSTART = 0x000E,

        /// <summary>
        /// Sent when a window enters and leaves drag drop mode.  Note that it is up
        /// to apps and OLE to generate this, since the system doesn't know.  Like
        /// EVENT_SYSTEM_SOUND, it will be a while before this is prevalent.
        /// </summary>
        EVENT_SYSTEM_DRAGDROPEND = 0x000F,

        /// <summary>
        /// Sent when a dialog comes up and just before it goes away.
        /// </summary>
        EVENT_SYSTEM_DIALOGSTART = 0x0010,

        /// <summary>
        /// Sent when a dialog comes up and just before it goes away.
        /// </summary>
        EVENT_SYSTEM_DIALOGEND = 0x0011,

        /// <summary>
        /// Sent when beginning and ending the tracking of a scrollbar in a window,
        /// and also for scrollbar controls.
        /// </summary>
        EVENT_SYSTEM_SCROLLINGSTART = 0x0012,

        /// <summary>
        /// Sent when beginning and ending the tracking of a scrollbar in a window,
        /// and also for scrollbar controls.
        /// </summary>
        EVENT_SYSTEM_SCROLLINGEND = 0x0013,

        /// <summary>
        /// Sent when beginning and ending alt-tab mode with the switch window.
        /// </summary>
        EVENT_SYSTEM_SWITCHSTART = 0x0014,

        /// <summary>
        /// Sent when beginning and ending alt-tab mode with the switch window.
        /// </summary>
        EVENT_SYSTEM_SWITCHEND = 0x0015,

        /// <summary>
        /// Sent when a window minimizes.
        /// </summary>
        EVENT_SYSTEM_MINIMIZESTART = 0x0016,

        /// <summary>
        /// Sent just before a window restores.
        /// </summary>
        EVENT_SYSTEM_MINIMIZEEND = 0x0017,

        /// <summary>
        /// hwnd + ID + idChild is created item
        /// </summary>
        EVENT_OBJECT_CREATE = 0x8000,

        /// <summary>
        /// hwnd + ID + idChild is destroyed item
        /// </summary>
        EVENT_OBJECT_DESTROY = 0x8001,

        /// <summary>
        /// hwnd + ID + idChild is shown item
        /// </summary>
        EVENT_OBJECT_SHOW = 0x8002,

        /// <summary>
        /// hwnd + ID + idChild is hidden item
        /// </summary>
        EVENT_OBJECT_HIDE = 0x8003,

        /// <summary>
        /// hwnd + ID + idChild is parent of zordering children
        /// </summary>
        EVENT_OBJECT_REORDER = 0x8004,

        /// <summary>
        /// hwnd + ID + idChild is focused item
        /// </summary>
        EVENT_OBJECT_FOCUS = 0x8005,

        /// <summary>
        /// hwnd + ID + idChild is selected item (if only one), or idChild is OBJID_WINDOW if complex
        /// </summary>
        EVENT_OBJECT_SELECTION = 0x8006,

        /// <summary>
        /// hwnd + ID + idChild is item added
        /// </summary>
        EVENT_OBJECT_SELECTIONADD = 0x8007,

        /// <summary>
        /// hwnd + ID + idChild is item removed
        /// </summary>
        EVENT_OBJECT_SELECTIONREMOVE = 0x8008,

        /// <summary>
        /// hwnd + ID + idChild is parent of changed selected items
        /// </summary>
        EVENT_OBJECT_SELECTIONWITHIN = 0x8009,

        /// <summary>
        /// hwnd + ID + idChild is item w/ state change
        /// </summary>
        EVENT_OBJECT_STATECHANGE = 0x800A,

        /// <summary>
        /// hwnd + ID + idChild is moved/sized item
        /// </summary>
        EVENT_OBJECT_LOCATIONCHANGE = 0x800B,

        /// <summary>
        /// hwnd + ID + idChild is item w/ name change
        /// </summary>
        EVENT_OBJECT_NAMECHANGE = 0x800C,

        /// <summary>
        /// hwnd + ID + idChild is item w/ desc change
        /// </summary>
        EVENT_OBJECT_DESCRIPTIONCHANGE = 0x800D,

        /// <summary>
        /// hwnd + ID + idChild is item w/ value change
        /// </summary>
        EVENT_OBJECT_VALUECHANGE = 0x800E,

        /// <summary>
        /// hwnd + ID + idChild is item w/ new parent
        /// </summary>
        EVENT_OBJECT_PARENTCHANGE = 0x800F,

        /// <summary>
        /// hwnd + ID + idChild is item w/ help change
        /// </summary>
        EVENT_OBJECT_HELPCHANGE = 0x8010,

        /// <summary>
        /// hwnd + ID + idChild is item w/ def action change
        /// </summary>
        EVENT_OBJECT_DEFACTIONCHANGE = 0x8011,

        /// <summary>
        /// hwnd + ID + idChild is item w/ keybd accel change
        /// </summary>
        EVENT_OBJECT_ACCELERATORCHANGE = 0x8012,

        /// <summary>
        /// The lowest possible event value
        /// </summary>
        EVENT_MIN = 0x00000001,

        /// <summary>
        /// The highest possible event value
        /// </summary>
        EVENT_MAX = 0x7FFFFFFF,
    }
}
