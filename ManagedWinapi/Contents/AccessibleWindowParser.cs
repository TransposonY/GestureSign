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
using System.Runtime.InteropServices;
using ManagedWinapi.Accessibility;

namespace ManagedWinapi.Windows.Contents
{
    /// <summary>
    /// The content of an object that supports the Accessibility API 
    /// (used by screen readers and similar programs).
    /// </summary>
    public class AccessibleWindowContent : WindowContent
    {

        bool parsed = false;
        readonly string name;
        string menu, sysmenu, clientarea;
        readonly bool hasMenu, hasSysMenu, hasClientArea;
        SystemWindow sw;

        internal AccessibleWindowContent(string name, bool hasMenu, bool hasSysMenu, bool hasClientArea, SystemWindow sw)
        {
            this.name = name;
            this.hasMenu = hasMenu;
            this.hasSysMenu = hasSysMenu;
            this.hasClientArea = hasClientArea;
            this.sw = sw;
        }

        ///
        public string ComponentType
        {
            get { return "AccessibleWindow"; }
        }

        ///
        public string ShortDescription
        {
            get
            {
                return name + " <AccessibleWindow:" +
                    (hasSysMenu ? " SystemMenu" : "") +
                    (hasMenu ? " Menu" : "") +
                    (hasClientArea ? " ClientArea" : "") + ">";
            }
        }

        ///
        public string LongDescription
        {
            get
            {
                ParseIfNeeded();
                string result = ShortDescription + "\n";
                if (sysmenu != null)
                    result += "System menu:\n" + sysmenu + "\n";
                if (menu != null)
                    result += "Menu:\n" + menu + "\n";
                if (clientarea != null)
                    result += "Client area:\n" + clientarea + "\n";
                return result;
            }
        }

        private void ParseIfNeeded()
        {
            if (parsed) return;
            if (hasSysMenu) sysmenu = ParseMenu(sw, AccessibleObjectID.OBJID_SYSMENU);
            if (hasMenu) menu = ParseMenu(sw, AccessibleObjectID.OBJID_MENU);
            if (hasClientArea) clientarea = ParseClientArea(sw);
            parsed = true;
        }

        private string ParseMenu(SystemWindow sw, AccessibleObjectID accessibleObjectID)
        {
            SystemAccessibleObject sao = SystemAccessibleObject.FromWindow(sw, accessibleObjectID);
            StringBuilder menuitems = new StringBuilder();
            ParseSubMenu(menuitems, sao, 1);
            return menuitems.ToString();
        }

        private void ParseSubMenu(StringBuilder menuitems, SystemAccessibleObject sao, int depth)
        {
            foreach (SystemAccessibleObject c in sao.Children)
            {
                if (c.RoleIndex == 11 || c.RoleIndex == 12)
                {
                    menuitems.Append(ListContent.Repeat('\t', depth) + c.Name + "\n");
                    ParseSubMenu(menuitems, c, depth + 1);
                }
            }
        }

        private string ParseClientArea(SystemWindow sw)
        {
            SystemAccessibleObject sao = SystemAccessibleObject.FromWindow(sw, AccessibleObjectID.OBJID_CLIENT);
            StringBuilder sb = new StringBuilder();
            ParseClientAreaElement(sb, sao, 1);
            return sb.ToString();
        }

        private void ParseClientAreaElement(StringBuilder sb, SystemAccessibleObject sao, int depth)
        {
            sb.Append("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");
            sb.Append(ListContent.Repeat('*', depth) + " " + sao.ToString() + "\n");
            try
            {
                sb.Append("D: " + sao.Description + "\n");
            }
            catch (COMException) { }
            try
            {
                sb.Append("V: " + sao.Value + "\n");
            }
            catch (COMException) { }
            foreach (SystemAccessibleObject c in sao.Children)
            {
                if (c.Window == sao.Window)
                    ParseClientAreaElement(sb, c, depth + 1);
            }
        }

        ///
        public Dictionary<string, string> PropertyList
        {
            get
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                return result;
            }
        }

    }

    class AccessibleWindowParser : WindowContentParser
    {
        internal override bool CanParseContent(SystemWindow sw)
        {
            return TestMenu(sw, AccessibleObjectID.OBJID_MENU) ||
                TestMenu(sw, AccessibleObjectID.OBJID_SYSMENU) ||
                TestClientArea(sw);
        }

        internal override WindowContent ParseContent(SystemWindow sw)
        {
            SystemAccessibleObject sao = SystemAccessibleObject.FromWindow(sw, AccessibleObjectID.OBJID_WINDOW);
            bool sysmenu = TestMenu(sw, AccessibleObjectID.OBJID_SYSMENU);
            bool menu = TestMenu(sw, AccessibleObjectID.OBJID_MENU);
            bool clientarea = TestClientArea(sw);
            return new AccessibleWindowContent(sao.Name, menu, sysmenu, clientarea, sw);
        }

        private bool TestClientArea(SystemWindow sw)
        {
            try
            {
                SystemAccessibleObject sao = SystemAccessibleObject.FromWindow(sw, AccessibleObjectID.OBJID_CLIENT);
                foreach (SystemAccessibleObject c in sao.Children)
                {
                    if (c.Window == sw) return true;
                }
            }
            catch (COMException) { }
            return false;
        }

        private bool TestMenu(SystemWindow sw, AccessibleObjectID accessibleObjectID)
        {
            SystemAccessibleObject sao = SystemAccessibleObject.FromWindow(sw, accessibleObjectID);
            return sao.Children.Length > 0;
        }
    }
}
