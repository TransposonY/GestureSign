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
using Accessibility;
using ManagedWinapi.Windows;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Runtime.InteropServices.ComTypes;

namespace ManagedWinapi.Accessibility
{
    /// <summary>
    /// Provides access to the Active Accessibility API. Every <see cref="SystemWindow"/>
    /// has one ore more AccessibleObjects attached that provide information about the
    /// window to visually impaired people. This information is mainly used by screen 
    /// readers and other accessibility software..
    /// </summary>
    public class SystemAccessibleObject
    {
        private IAccessible iacc;
        private int childID;

        /// <summary>
        /// The IAccessible instance of this object (if <see cref="ChildID"/> is zero)
        /// or its parent.
        /// </summary>
        public IAccessible IAccessible { get { return iacc; } }

        /// <summary>
        /// The underlying child ID
        /// </summary>
        public int ChildID { get { return childID; } }

        /// <summary>
        /// Create an accessible object from an IAccessible instance and a child ID.
        /// </summary>
        public SystemAccessibleObject(IAccessible iacc, int childID)
        {
            if (iacc == null) throw new ArgumentNullException();
            //if (childID < 0) throw new ArgumentException();
            if (childID != 0)
            {
                try
                {
                    object realChild = iacc.get_accChild(childID);
                    if (realChild != null)
                    {
                        iacc = (IAccessible)realChild;
                        childID = 0;
                    }
                }
                catch (ArgumentException) { }
            }
            this.iacc = iacc;
            this.childID = childID;
        }

        /// <summary>
        /// Gets an accessibility object for given screen coordinates.
        /// </summary>
        public static SystemAccessibleObject FromPoint(int x, int y)
        {
            IAccessible iacc;
            object ci;
            IntPtr result = AccessibleObjectFromPoint(new ManagedWinapi.Windows.POINT(x, y), out iacc, out ci);
            if (result != IntPtr.Zero) throw new Exception("AccessibleObjectFromPoint returned " + result.ToInt32());
            return new SystemAccessibleObject(iacc, (int)(ci ?? 0));
        }

        /// <summary>
        /// Gets an accessibility object for a given window.
        /// </summary>
        /// <param name="window">The window</param>
        /// <param name="objectID">Which accessibility object to get</param>
        /// <returns></returns>
        public static SystemAccessibleObject FromWindow(SystemWindow window, AccessibleObjectID objectID)
        {
            IAccessible iacc = (IAccessible)AccessibleObjectFromWindow(window == null ? IntPtr.Zero : window.HWnd, (uint)objectID, new Guid("{618736E0-3C3D-11CF-810C-00AA00389B71}"));
            return new SystemAccessibleObject(iacc, 0);
        }

        /// <summary>
        /// Gets an accessibility object for the mouse cursor.
        /// </summary>
        public static SystemAccessibleObject MouseCursor
        {
            get
            {
                return FromWindow(null, AccessibleObjectID.OBJID_CURSOR);
            }
        }

        /// <summary>
        /// Gets an accessibility object for the input caret, or
        /// <b>null</b> if there is none.
        /// </summary>
        public static SystemAccessibleObject Caret
        {
            get
            {
                try
                {
                    return FromWindow(null, AccessibleObjectID.OBJID_CARET);
                }
                catch (COMException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Convert a role number to a localized string.
        /// </summary>
        public static string RoleToString(int roleNumber)
        {
            StringBuilder sb = new StringBuilder(1024);
            uint result = GetRoleText((uint)roleNumber, sb, 1024);
            if (result == 0) throw new Exception("Invalid role number");
            return sb.ToString();
        }

        /// <summary>
        /// Convert a state number (which may include more than one state bit)
        /// to a localized string.
        /// </summary>
        public static String StateToString(int stateNumber)
        {
            if (stateNumber == 0) return "None";
            int lowBit = stateNumber & -stateNumber;
            int restBits = stateNumber - lowBit;
            String s1 = StateBitToString(lowBit);
            if (restBits == 0) return s1;
            return StateToString(restBits) + ", " + s1;
        }

        /// <summary>
        /// Convert a single state bit to a localized string.
        /// </summary>
        public static string StateBitToString(int stateBit)
        {
            StringBuilder sb = new StringBuilder(1024);
            uint result = GetStateText((uint)stateBit, sb, 1024);
            if (result == 0) throw new Exception("Invalid role number");
            return sb.ToString();
        }

        /// <summary>
        /// The description of this accessible object.
        /// </summary>
        public string Description
        {
            get
            {
                return iacc.get_accDescription(childID);
            }
        }

        /// <summary>
        /// The name of this accessible object.
        /// </summary>
        public string Name
        {
            get
            {
                return iacc.get_accName(childID);
            }
        }

        /// <summary>
        /// The role of this accessible object. This can either be an int
        /// (for a predefined role) or a string.
        /// </summary>
        public object Role
        {
            get
            {
                return iacc.get_accRole(childID);
            }
        }

        /// <summary>
        /// The role of this accessible object, as an integer. If this role
        /// is not predefined, -1 is returned.
        /// </summary>
        public int RoleIndex
        {
            get
            {
                object role = Role;
                if (role is int)
                {
                    return (int)role;
                }
                else
                {
                    return -1;
                }
            }
        }

        /// <summary>
        /// The role of this accessible object, as a localized string.
        /// </summary>
        public string RoleString
        {
            get
            {
                object role = Role;
                if (role is int)
                {
                    return RoleToString((int)role);
                }
                else if (role is string)
                {
                    return (string)role;
                }
                else
                {
                    return role.ToString();
                }
            }
        }

        /// <summary>
        /// The location of this accessible object on screen. This rectangle
        /// is the smallest rectangle that includes the whole object, but not
        /// every point in the rectangle must be part of the object.
        /// </summary>
        public Rectangle Location
        {
            get
            {
                int x, y, w, h;
                iacc.accLocation(out x, out y, out w, out h, childID);
                return new Rectangle(x, y, w, h);

            }
        }

        /// <summary>
        /// The value of this accessible object.
        /// </summary>
        public string Value
        {
            get
            {
                return iacc.get_accValue(childID);
            }
        }

        /// <summary>
        /// The state of this accessible object.
        /// </summary>
        public int State
        {
            get
            {
                return (int)iacc.get_accState(childID);
            }
        }

        /// <summary>
        /// A string representation of the state of this accessible object.
        /// </summary>
        public string StateString
        {
            get
            {
                return StateToString(State);
            }
        }

        /// <summary>
        /// Whether this accessibile object is visible.
        /// </summary>
        public bool Visible
        {
            get
            {
                return (State & 0x8000) == 0;
            }
        }

        /// <summary>
        /// The parent of this accessible object, or <b>null</b> if none exists.
        /// </summary>
        public SystemAccessibleObject Parent
        {
            get
            {
                if (childID != 0) return new SystemAccessibleObject(iacc, 0);
                IAccessible p = (IAccessible)iacc.accParent;
                if (p == null) return null;
                return new SystemAccessibleObject(p, 0);
            }
        }

        /// <summary>
        /// The keyboard shortcut of this accessible object.
        /// </summary>
        public string KeyboardShortcut
        {
            get
            {
                try
                {
                    return iacc.get_accKeyboardShortcut(childID);
                }
                catch (ArgumentException) { return ""; }
                catch (NotImplementedException) { return ""; }
                catch (COMException) { return null; }
            }
        }

        /// <summary>
        /// A string describing the default action of this accessible object.
        /// For a button, this might be "Press".
        /// </summary>
        public string DefaultAction
        {
            get
            {
                try
                {
                    return iacc.get_accDefaultAction(childID);
                }
                catch (COMException) { return null; }
            }
        }

        /// <summary>
        /// Perform the default action of this accessible object.
        /// </summary>
        public void DoDefaultAction()
        {
            iacc.accDoDefaultAction(ChildID);
        }

        /// <summary>
        /// Get all objects of this accessible object that are selected.
        /// </summary>
        public SystemAccessibleObject[] SelectedObjects
        {
            get
            {
                if (childID != 0) return new SystemAccessibleObject[0];
                object sel;
                try
                {
                    sel = iacc.accSelection;
                }
                catch (NotImplementedException)
                {
                    return new SystemAccessibleObject[0];
                }
                catch (COMException)
                {
                    return new SystemAccessibleObject[0];
                }
                if (sel == null) return new SystemAccessibleObject[0];
                if (sel is IEnumVARIANT)
                {
                    IEnumVARIANT e = (IEnumVARIANT)sel;
                    e.Reset();
                    List<SystemAccessibleObject> retval = new List<SystemAccessibleObject>();
                    object[] tmp = new object[1];
                    while (e.Next(1, tmp, IntPtr.Zero) == 0)
                    {
                        if (tmp[0] is int && (int)tmp[0] < 0) break;
                        retval.Add(ObjectToSAO(tmp[0]));
                    }
                    return retval.ToArray();
                }
                else
                {
                    if (sel is int && (int)sel < 0)
                    {
                        return new SystemAccessibleObject[0];
                    }
                    return new SystemAccessibleObject[] { ObjectToSAO(sel) };
                }
            }
        }

        private SystemAccessibleObject ObjectToSAO(object obj)
        {
            if (obj is int)
            {
                return new SystemAccessibleObject(iacc, (int)obj);
            }
            else
            {
                return new SystemAccessibleObject((IAccessible)obj, 0);
            }
        }

        /// <summary>
        /// Get the SystemWindow that owns this accessible object.
        /// </summary>
        public SystemWindow Window
        {
            get
            {
                IntPtr hwnd;
                WindowFromAccessibleObject(iacc, out hwnd);
                return new SystemWindow(hwnd);
            }
        }

        /// <summary>
        /// Get all child accessible objects.
        /// </summary>
        public SystemAccessibleObject[] Children
        {
            get
            {
                // ID-referenced objects cannot have children
                if (childID != 0) return new SystemAccessibleObject[0];

                int cs = iacc.accChildCount, csReal;
                object[] children = new object[cs * 2];

                uint result = AccessibleChildren(iacc, 0, cs * 2, children, out csReal);
                if (result != 0 && result != 1)
                    return new SystemAccessibleObject[0];
                if (csReal == 1 && children[0] is int && (int)children[0] < 0)
                    return new SystemAccessibleObject[0];
                List<SystemAccessibleObject> values = new List<SystemAccessibleObject>();
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i] != null)
                    {
                        try
                        {
                            values.Add(ObjectToSAO(children[i]));
                        }
                        catch (InvalidCastException) { }
                    }
                }
                return values.ToArray();
            }
        }

        #region Equals and HashCode

        ///
        public override bool Equals(System.Object obj)
        {
            if (obj == null)
            {
                return false;
            }
            SystemAccessibleObject sao = obj as SystemAccessibleObject;
            return Equals(sao);
        }

        ///
        public bool Equals(SystemAccessibleObject sao)
        {
            if ((object)sao == null)
            {
                return false;
            }
            return childID == sao.childID && DeepEquals(iacc, sao.iacc);
        }

        private static bool DeepEquals(IAccessible ia1, IAccessible ia2)
        {
            if (ia1.Equals(ia2)) return true;
            if (Marshal.GetIUnknownForObject(ia1) == Marshal.GetIUnknownForObject(ia2)) return true;
            if (ia1.accChildCount != ia2.accChildCount) return false;
            SystemAccessibleObject sa1 = new SystemAccessibleObject(ia1, 0);
            SystemAccessibleObject sa2 = new SystemAccessibleObject(ia2, 0);
            if (sa1.Window.HWnd != sa2.Window.HWnd) return false;
            if (sa1.Location != sa2.Location) return false;
            if (sa1.DefaultAction != sa2.DefaultAction) return false;
            if (sa1.Description != sa2.Description) return false;
            if (sa1.KeyboardShortcut != sa2.KeyboardShortcut) return false;
            if (sa1.Name != sa2.Name) return false;
            if (!sa1.Role.Equals(sa2.Role)) return false;
            if (sa1.State != sa2.State) return false;
            if (sa1.Value != sa2.Value) return false;
            if (sa1.Visible != sa2.Visible) return false;
            if (ia1.accParent == null && ia2.accParent == null) return true;
            if (ia1.accParent == null || ia2.accParent == null) return false;
            bool de =  DeepEquals((IAccessible)ia1.accParent, (IAccessible)ia2.accParent);
            return de;
        }

        ///
        public override int GetHashCode()
        {
            return childID ^ iacc.GetHashCode();
        }

        /// <summary>
        /// Compare two instances of this class for equality.
        /// </summary>
        public static bool operator ==(SystemAccessibleObject a, SystemAccessibleObject b)
        {
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }
            return a.iacc == b.iacc && a.childID == b.childID;
        }

        /// <summary>
        /// Compare two instances of this class for inequality.
        /// </summary>
        public static bool operator !=(SystemAccessibleObject a, SystemAccessibleObject b)
        {
            return !(a == b);
        }

        ///
        public override string ToString()
        {
            try
            {
                return Name + " [" + RoleString + "]";
            }
            catch
            {
                return "??";
            }
        }

        #endregion

        #region PInvoke Declarations

        [DllImport("oleacc.dll")]
        private static extern IntPtr AccessibleObjectFromPoint(POINT pt, [Out, MarshalAs(UnmanagedType.Interface)] out IAccessible accObj, [Out] out object ChildID);
        [DllImport("oleacc.dll")]
        private static extern uint GetRoleText(uint dwRole, [Out] StringBuilder lpszRole, uint cchRoleMax);

        [DllImport("oleacc.dll", ExactSpelling = true, PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.Interface)]
        private static extern object AccessibleObjectFromWindow(
            IntPtr hwnd,
            uint dwObjectID,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid);

        [DllImport("oleacc.dll")]
        private static extern uint GetStateText(uint dwStateBit, [Out] StringBuilder lpszStateBit, uint cchStateBitMax);

        [DllImport("oleacc.dll")]
        private static extern uint WindowFromAccessibleObject(IAccessible pacc, out IntPtr phwnd);

        [DllImport("oleacc.dll")]
        private static extern uint AccessibleChildren(IAccessible paccContainer, int iChildStart, int cChildren, [Out] object[] rgvarChildren, out int pcObtained);

        #endregion
    }

    /// <summary>
    /// This enumeration lists all kinds of accessible objects that can
    /// be directly assigned to a window.
    /// </summary>
    public enum AccessibleObjectID : uint
    {
        /// <summary>
        /// The window itself.
        /// </summary>
        OBJID_WINDOW = 0x00000000,

        /// <summary>
        /// The system menu.
        /// </summary>
        OBJID_SYSMENU = 0xFFFFFFFF,

        /// <summary>
        /// The title bar.
        /// </summary>
        OBJID_TITLEBAR = 0xFFFFFFFE,

        /// <summary>
        /// The menu.
        /// </summary>
        OBJID_MENU = 0xFFFFFFFD,

        /// <summary>
        /// The client area.
        /// </summary>
        OBJID_CLIENT = 0xFFFFFFFC,

        /// <summary>
        /// The vertical scroll bar.
        /// </summary>
        OBJID_VSCROLL = 0xFFFFFFFB,

        /// <summary>
        /// The horizontal scroll bar.
        /// </summary>
        OBJID_HSCROLL = 0xFFFFFFFA,

        /// <summary>
        /// The size grip (part in the lower right corner that
        /// makes resizing the window easier).
        /// </summary>
        OBJID_SIZEGRIP = 0xFFFFFFF9,

        /// <summary>
        /// The caret (text cursor).
        /// </summary>
        OBJID_CARET = 0xFFFFFFF8,

        /// <summary>
        /// The mouse cursor. There is only one mouse
        /// cursor and it is not assigned to any window.
        /// </summary>
        OBJID_CURSOR = 0xFFFFFFF7,

        /// <summary>
        /// An alert window.
        /// </summary>
        OBJID_ALERT = 0xFFFFFFF6,

        /// <summary>
        /// A sound this window is playing.
        /// </summary>
        OBJID_SOUND = 0xFFFFFFF5
    }
}
