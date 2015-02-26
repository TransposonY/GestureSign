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
using System.Runtime.InteropServices;
using System.Drawing;

namespace ManagedWinapi.Windows
{
    /// <summary>
    /// Any tree view, including those from other applications.
    /// </summary>
    public class SystemTreeView
    {
        /// <summary>
        /// Get a SystemTreeView reference from a SystemWindow (which is a tree view)
        /// </summary>
        public static SystemTreeView FromSystemWindow(SystemWindow sw)
        {
            if (sw.SendGetMessage(TVM_GETCOUNT) == 0) return null;
            return new SystemTreeView(sw);
        }

        internal readonly SystemWindow sw;

        private SystemTreeView(SystemWindow sw)
        {
            this.sw = sw;
        }

        /// <summary>
        /// The number of items (icons) in this tree view.
        /// </summary>
        public int Count
        {
            get
            {
                return sw.SendGetMessage(TVM_GETCOUNT);
            }
        }

        /// <summary>
        /// The root items of this tree view.
        /// </summary>
        public SystemTreeViewItem[] Roots
        {
            get
            {
                return FindSubItems(sw, IntPtr.Zero);
            }
        }

        internal static SystemTreeViewItem[] FindSubItems(SystemWindow sw, IntPtr hParent)
        {
            List<SystemTreeViewItem> result = new List<SystemTreeViewItem>();
            IntPtr hChild;
            HandleRef hr = new HandleRef(sw, sw.HWnd);
            if (hParent == IntPtr.Zero)
            {
                hChild = SystemWindow.SendMessage(hr, TVM_GETNEXTITEM, new IntPtr(TVGN_ROOT), IntPtr.Zero);
            }
            else
            {
                hChild = SystemWindow.SendMessage(hr, TVM_GETNEXTITEM, new IntPtr(TVGN_CHILD), hParent);
            }
            while (hChild != IntPtr.Zero)
            {
                result.Add(new SystemTreeViewItem(sw, hChild));
                hChild = SystemWindow.SendMessage(hr, TVM_GETNEXTITEM, new IntPtr(TVGN_NEXT), hChild);
            }
            return result.ToArray();
        }


        #region PInvoke Declarations

        private static readonly uint TVM_GETCOUNT = 0x1100 + 5,
            TVM_GETNEXTITEM = 0x1100 + 10, TVGN_ROOT = 0,
            TVGN_NEXT = 1, TVGN_CHILD = 4;

        #endregion
    }

    /// <summary>
    /// An item of a tree view.
    /// </summary>
    public class SystemTreeViewItem
    {
        readonly IntPtr handle;
        readonly SystemWindow sw;

        internal SystemTreeViewItem(SystemWindow sw, IntPtr handle)
        {
            this.sw = sw;
            this.handle = handle;
        }

        /// <summary>
        /// The title of that item.
        /// </summary>
        public string Title
        {
            get
            {
                ProcessMemoryChunk tc = ProcessMemoryChunk.Alloc(sw.Process, 2001);
                TVITEM tvi = new TVITEM();
                tvi.hItem = handle;
                tvi.mask = TVIF_TEXT;
                tvi.cchTextMax = 2000;
                tvi.pszText = tc.Location;
                ProcessMemoryChunk ic = ProcessMemoryChunk.AllocStruct(sw.Process, tvi);
                SystemWindow.SendMessage(new HandleRef(sw, sw.HWnd), TVM_GETITEM, IntPtr.Zero, ic.Location);
                tvi = (TVITEM)ic.ReadToStructure(0, typeof(TVITEM));
                if (tvi.pszText != tc.Location) MessageBox.Show(tvi.pszText + " != " + tc.Location);
                string result = Encoding.Default.GetString(tc.Read());
                if (result.IndexOf('\0') != -1) result = result.Substring(0, result.IndexOf('\0'));
                ic.Dispose();
                tc.Dispose();
                return result;
            }
        }

        /// <summary>
        /// All child items of that item.
        /// </summary>
        public SystemTreeViewItem[] Children
        {
            get { return SystemTreeView.FindSubItems(sw, handle); }
        }

        #region PInvoke Declarations

        private static readonly uint TVM_GETITEM = 0x1100 + 12, TVIF_TEXT = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct TVITEM
        {
            public UInt32 mask;
            public IntPtr hItem;
            public UInt32 state;
            public UInt32 stateMask;
            public IntPtr pszText;
            public Int32 cchTextMax;
            public Int32 iImage;
            public Int32 iSelectedImage;
            public Int32 cChildren;
            public IntPtr lParam;
        }
        #endregion
    }
}
