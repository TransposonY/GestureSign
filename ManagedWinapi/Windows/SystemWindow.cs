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
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ManagedWinapi.Windows
{
    /// <summary>
    /// Window Style Flags. The original constants started with WS_.
    /// </summary>
    /// <seealso cref="SystemWindow.Style"/>
    [Flags]
    public enum WindowStyleFlags
    {
        /// <summary>
        /// WS_OVERLAPPED
        /// </summary>
        OVERLAPPED = 0x00000000,

        /// <summary>
        /// WS_POPUP
        /// </summary>
        POPUP = unchecked((int)0x80000000),

        /// <summary>
        /// WS_CHILD
        /// </summary>
        CHILD = 0x40000000,

        /// <summary>
        /// WS_MINIMIZE
        /// </summary>
        MINIMIZE = 0x20000000,

        /// <summary>
        /// WS_VISIBLE
        /// </summary>
        VISIBLE = 0x10000000,

        /// <summary>
        /// WS_DISABLED
        /// </summary>
        DISABLED = 0x08000000,

        /// <summary>
        /// WS_CLIPSIBLINGS
        /// </summary>
        CLIPSIBLINGS = 0x04000000,

        /// <summary>
        /// WS_CLIPCHILDREN
        /// </summary>
        CLIPCHILDREN = 0x02000000,

        /// <summary>
        /// WS_MAXIMIZE
        /// </summary>
        MAXIMIZE = 0x01000000,

        /// <summary>
        /// WS_BORDER
        /// </summary>
        BORDER = 0x00800000,

        /// <summary>
        /// WS_DLGFRAME
        /// </summary>
        DLGFRAME = 0x00400000,

        /// <summary>
        /// WS_VSCROLL
        /// </summary>
        VSCROLL = 0x00200000,

        /// <summary>
        /// WS_HSCROLL
        /// </summary>
        HSCROLL = 0x00100000,

        /// <summary>
        /// WS_SYSMENU
        /// </summary>
        SYSMENU = 0x00080000,

        /// <summary>
        /// WS_THICKFRAME
        /// </summary>
        THICKFRAME = 0x00040000,

        /// <summary>
        /// WS_GROUP
        /// </summary>
        GROUP = 0x00020000,

        /// <summary>
        /// WS_TABSTOP
        /// </summary>
        TABSTOP = 0x00010000,

        /// <summary>
        /// WS_MINIMIZEBOX
        /// </summary>
        MINIMIZEBOX = 0x00020000,

        /// <summary>
        /// WS_MAXIMIZEBOX
        /// </summary>
        MAXIMIZEBOX = 0x00010000,

        /// <summary>
        /// WS_CAPTION
        /// </summary>
        CAPTION = BORDER | DLGFRAME,

        /// <summary>
        /// WS_TILED
        /// </summary>
        TILED = OVERLAPPED,

        /// <summary>
        /// WS_ICONIC
        /// </summary>
        ICONIC = MINIMIZE,

        /// <summary>
        /// WS_SIZEBOX
        /// </summary>
        SIZEBOX = THICKFRAME,

        /// <summary>
        /// WS_TILEDWINDOW
        /// </summary>
        TILEDWINDOW = OVERLAPPEDWINDOW,

        /// <summary>
        /// WS_OVERLAPPEDWINDOW
        /// </summary>
        OVERLAPPEDWINDOW = OVERLAPPED | CAPTION | SYSMENU | THICKFRAME | MINIMIZEBOX | MAXIMIZEBOX,

        /// <summary>
        /// WS_POPUPWINDOW
        /// </summary>
        POPUPWINDOW = POPUP | BORDER | SYSMENU,

        /// <summary>
        /// WS_CHILDWINDOW
        /// </summary>
        CHILDWINDOW = CHILD,
    }

    /// <summary>
    /// Extended Window Style Flags. The original constants started with WS_EX_.
    /// </summary>
    /// <seealso cref="SystemWindow.ExtendedStyle"/>
    [Flags]
    public enum WindowExStyleFlags : uint
    {
        /// <summary>
        /// Specifies that a window created with this style accepts drag-drop files.
        /// </summary>
        ACCEPTFILES = 0x00000010,
        /// <summary>
        /// Forces a top-level window onto the taskbar when the window is visible.
        /// </summary>
        APPWINDOW = 0x00040000,
        /// <summary>
        /// Specifies that a window has a border with a sunken edge.
        /// </summary>
        CLIENTEDGE = 0x00000200,
        /// <summary>
        /// Windows XP: Paints all descendants of a window in bottom-to-top painting order using double-buffering. For more information, see Remarks. This cannot be used if the window has a class style of either CS_OWNDC or CS_CLASSDC. 
        /// </summary>
        COMPOSITED = 0x02000000,
        /// <summary>
        /// Includes a question mark in the title bar of the window. When the user clicks the question mark, the cursor changes to a question mark with a pointer. If the user then clicks a child window, the child receives a WM_HELP message. The child window should pass the message to the parent window procedure, which should call the WinHelp function using the HELP_WM_HELP command. The Help application displays a pop-up window that typically contains help for the child window.
        /// WS_EX_CONTEXTHELP cannot be used with the WS_MAXIMIZEBOX or WS_MINIMIZEBOX styles.
        /// </summary>
        CONTEXTHELP = 0x00000400,
        /// <summary>
        /// The window itself contains child windows that should take part in dialog box navigation. If this style is specified, the dialog manager recurses into children of this window when performing navigation operations such as handling the TAB key, an arrow key, or a keyboard mnemonic.
        /// </summary>
        CONTROLPARENT = 0x00010000,
        /// <summary>
        /// Creates a window that has a double border; the window can, optionally, be created with a title bar by specifying the WS_CAPTION style in the dwStyle parameter.
        /// </summary>
        DLGMODALFRAME = 0x00000001,
        /// <summary>
        /// Windows 2000/XP: Creates a layered window. Note that this cannot be used for child windows. Also, this cannot be used if the window has a class style of either CS_OWNDC or CS_CLASSDC. 
        /// </summary>
        LAYERED = 0x00080000,
        /// <summary>
        /// Arabic and Hebrew versions of Windows 98/Me, Windows 2000/XP: Creates a window whose horizontal origin is on the right edge. Increasing horizontal values advance to the left. 
        /// </summary>
        LAYOUTRTL = 0x00400000,
        /// <summary>
        /// Creates a window that has generic left-aligned properties. This is the default.
        /// </summary>
        LEFT = 0x00000000,
        /// <summary>
        /// If the shell language is Hebrew, Arabic, or another language that supports reading order alignment, the vertical scroll bar (if present) is to the left of the client area. For other languages, the style is ignored.
        /// </summary>
        LEFTSCROLLBAR = 0x00004000,
        /// <summary>
        /// The window text is displayed using left-to-right reading-order properties. This is the default.
        /// </summary>
        LTRREADING = 0x00000000,
        /// <summary>
        /// Creates a multiple-document interface (MDI) child window.
        /// </summary>
        MDICHILD = 0x00000040,
        /// <summary>
        /// Windows 2000/XP: A top-level window created with this style does not become the foreground window when the user clicks it. The system does not bring this window to the foreground when the user minimizes or closes the foreground window. 
        /// To activate the window, use the SetActiveWindow or SetForegroundWindow function.
        /// The window does not appear on the taskbar by default. To force the window to appear on the taskbar, use the WS_EX_APPWINDOW style.
        /// </summary>
        NOACTIVATE = 0x08000000,
        /// <summary>
        /// Windows 2000/XP: A window created with this style does not pass its window layout to its child windows.
        /// </summary>
        NOINHERITLAYOUT = 0x00100000,
        /// <summary>
        /// Specifies that a child window created with this style does not send the WM_PARENTNOTIFY message to its parent window when it is created or destroyed.
        /// </summary>
        NOPARENTNOTIFY = 0x00000004,
        /// <summary>
        /// Combines the WS_EX_CLIENTEDGE and WS_EX_WINDOWEDGE styles.
        /// </summary>
        OVERLAPPEDWINDOW = WINDOWEDGE | CLIENTEDGE,
        /// <summary>
        /// Combines the WS_EX_WINDOWEDGE, WS_EX_TOOLWINDOW, and WS_EX_TOPMOST styles.
        /// </summary>
        PALETTEWINDOW = WINDOWEDGE | TOOLWINDOW | TOPMOST,
        /// <summary>
        /// The window has generic "right-aligned" properties. This depends on the window class. This style has an effect only if the shell language is Hebrew, Arabic, or another language that supports reading-order alignment; otherwise, the style is ignored.
        /// Using the WS_EX_RIGHT style for static or edit controls has the same effect as using the SS_RIGHT or ES_RIGHT style, respectively. Using this style with button controls has the same effect as using BS_RIGHT and BS_RIGHTBUTTON styles.
        /// </summary>
        RIGHT = 0x00001000,
        /// <summary>
        /// Vertical scroll bar (if present) is to the right of the client area. This is the default.
        /// </summary>
        RIGHTSCROLLBAR = 0x00000000,
        /// <summary>
        /// If the shell language is Hebrew, Arabic, or another language that supports reading-order alignment, the window text is displayed using right-to-left reading-order properties. For other languages, the style is ignored.
        /// </summary>
        RTLREADING = 0x00002000,
        /// <summary>
        /// Creates a window with a three-dimensional border style intended to be used for items that do not accept user input.
        /// </summary>
        STATICEDGE = 0x00020000,
        /// <summary>
        /// Creates a tool window; that is, a window intended to be used as a floating toolbar. A tool window has a title bar that is shorter than a normal title bar, and the window title is drawn using a smaller font. A tool window does not appear in the taskbar or in the dialog that appears when the user presses ALT+TAB. If a tool window has a system menu, its icon is not displayed on the title bar. However, you can display the system menu by right-clicking or by typing ALT+SPACE. 
        /// </summary>
        TOOLWINDOW = 0x00000080,
        /// <summary>
        /// Specifies that a window created with this style should be placed above all non-topmost windows and should stay above them, even when the window is deactivated. To add or remove this style, use the SetWindowPos function.
        /// </summary>
        TOPMOST = 0x00000008,
        /// <summary>
        /// Specifies that a window created with this style should not be painted until siblings beneath the window (that were created by the same thread) have been painted. The window appears transparent because the bits of underlying sibling windows have already been painted.
        /// To achieve transparency without these restrictions, use the SetWindowRgn function.
        /// </summary>
        TRANSPARENT = 0x00000020,
        /// <summary>
        /// Specifies that a window has a border with a raised edge.
        /// </summary>
        WINDOWEDGE = 0x00000100
    }

    /// <summary>
    /// Represents any window used by Windows, including those from other applications.
    /// </summary>
    public class SystemWindow
    {

        private static readonly Predicate<SystemWindow> ALL = delegate { return true; };

        private IntPtr _hwnd;

        /// <summary>
        /// Allows getting the current foreground window and setting it.
        /// </summary>
        public static SystemWindow ForegroundWindow
        {
            get
            {
                return new SystemWindow(GetForegroundWindow());
            }
            set
            {
                SetForegroundWindow(value.HWnd);
            }
        }

        /// <summary>
        /// The Desktop window, i. e. the window that covers the
        /// complete desktop.
        /// </summary>
        public static SystemWindow DesktopWindow
        {
            get
            {
                return new SystemWindow(GetDesktopWindow());
            }
        }

        /// <summary>
        /// Retrieves a handle to the Shell's desktop window.
        /// Go to https://msdn.microsoft.com/en-us/library/windows/desktop/ms633512%28v=vs.85%29.aspx for more
        /// information
        /// </summary>
        public static SystemWindow ShellWindow
        {
            get
            {
                return new SystemWindow(GetShellWindow());
            }
        }

        /// <summary>
        /// Returns all available toplevel windows.
        /// </summary>
        public static SystemWindow[] AllToplevelWindows
        {
            get
            {
                return FilterToplevelWindows(new Predicate<SystemWindow>(ALL));
            }
        }

        /// <summary>
        /// Returns all toplevel windows that match the given predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter.</param>
        /// <returns>The filtered toplevel windows</returns>
        public static SystemWindow[] FilterToplevelWindows(Predicate<SystemWindow> predicate)
        {
            List<SystemWindow> wnds = new List<SystemWindow>();
            EnumWindows(new EnumWindowsProc(delegate (IntPtr hwnd, IntPtr lParam)
            {
                SystemWindow tmp = new SystemWindow(hwnd);
                if (predicate(tmp))
                    wnds.Add(tmp);
                return 1;
            }), new IntPtr(0));
            return wnds.ToArray();
        }

        /// <summary>
        /// Finds the system window below the given point. This need not be a
        /// toplevel window; disabled windows are not returned either.
        /// If you have problems with transparent windows that cover nontransparent
        /// windows, consider using <see cref="FromPointEx"/>, since that method
        /// tries hard to avoid this problem.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns></returns>
        public static SystemWindow FromPoint(int x, int y)
        {
            IntPtr hwnd = WindowFromPoint(new POINT(x, y));
            if (hwnd.ToInt64() == 0)
            {
                return null;
            }
            return new SystemWindow(hwnd);
        }

        /// <summary>
        /// Finds the system window below the given point. This method uses a more
        /// sophisticated algorithm than <see cref="FromPoint"/>, but is slower.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="toplevel">Whether to return the toplevel window.</param>
        /// <param name="enabledOnly">Whether to return enabled windows only.</param>
        /// <returns></returns>
        public static SystemWindow FromPointEx(int x, int y, bool toplevel, bool enabledOnly)
        {
            SystemWindow sw = FromPoint(x, y);
            if (sw == null) return null;
            while (sw.ParentSymmetric != null)
                sw = sw.ParentSymmetric;
            if (toplevel)
                return sw;
            int area;
            area = getArea(sw);
            SystemWindow result = sw;
            foreach (SystemWindow w in sw.AllDescendantWindows)
            {
                if (w.Visible && (w.Enabled || !enabledOnly))
                {
                    if (w.Rectangle.ToRectangle().Contains(x, y))
                    {
                        int ar2 = getArea(w);
                        if (ar2 <= area)
                        {
                            area = ar2;
                            result = w;
                        }
                    }
                }
            }
            return result;
        }

        private static int getArea(SystemWindow sw)
        {
            RECT rr = sw.Rectangle;
            return rr.Height * rr.Width;
        }

        /// <summary>
        /// Create a new SystemWindow instance from a window handle.
        /// </summary>
        /// <param name="HWnd">The window handle.</param>
        public SystemWindow(IntPtr HWnd)
        {
            _hwnd = HWnd;
        }

        /// <summary>
        /// Create a new SystemWindow instance from a Windows Forms Control.
        /// </summary>
        /// <param name="control">The control.</param>
        public SystemWindow(Control control)
        {
            _hwnd = control.Handle;
        }

        /// <summary>
        /// Return all descendant windows (child windows and their descendants).
        /// </summary>
        public SystemWindow[] AllDescendantWindows
        {
            get
            {
                return FilterDescendantWindows(false, ALL);
            }
        }

        /// <summary>
        /// Return all direct child windows.
        /// </summary>
        public SystemWindow[] AllChildWindows
        {
            get
            {
                return FilterDescendantWindows(true, ALL);
            }
        }

        /// <summary>
        /// Returns all child windows that match the given predicate.
        /// </summary>
        /// <param name="directOnly">Whether to include only direct children (no descendants)</param>
        /// <param name="predicate">The predicate to filter.</param>
        /// <returns>The list of child windows.</returns>
        public SystemWindow[] FilterDescendantWindows(bool directOnly, Predicate<SystemWindow> predicate)
        {
            List<SystemWindow> wnds = new List<SystemWindow>();
            EnumChildWindows(_hwnd, delegate (IntPtr hwnd, IntPtr lParam)
            {
                SystemWindow tmp = new SystemWindow(hwnd);
                bool add = true;
                if (directOnly)
                {
                    add = tmp.Parent._hwnd == _hwnd;
                }
                if (add && predicate(tmp))
                    wnds.Add(tmp);
                return 1;
            }, new IntPtr(0));
            return wnds.ToArray();
        }

        /// <summary>
        /// The Window handle of this window.
        /// </summary>
        public IntPtr HWnd { get { return _hwnd; } }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "HWND 0x" + _hwnd.ToString("X");
        }

        /// <summary>
        /// The title of this window (by the <c>GetWindowText</c> API function).
        /// </summary>
        public string Title
        {
            get
            {
                StringBuilder sb = new StringBuilder(GetWindowTextLength(_hwnd) + 1);
                GetWindowText(_hwnd, sb, sb.Capacity);
                return sb.ToString();
            }

            set
            {
                SetWindowText(_hwnd, value);
            }
        }

        /// <summary>
        /// The text inside of this window (by sending a <c>WM_GETTEXT</c> message).
        /// For child windows of other applications, this is more reliable
        /// than the <see cref="Title"/> function.
        /// </summary>
        public string Text
        {
            get
            {
                int length = SendGetMessage(WM_GETTEXTLENGTH);
                StringBuilder sb = new StringBuilder(length + 1);
                SendMessage(new HandleRef(this, HWnd), WM_GETTEXT, new IntPtr(sb.Capacity), sb);
                return sb.ToString();
            }
        }

        /// <summary>
        /// The name of the window class (by the <c>GetClassName</c> API function).
        /// This class has nothing to do with classes in C# or other .NET languages.
        /// </summary>
        public string ClassName
        {
            get
            {
                int length = 64;
                while (true)
                {
                    StringBuilder sb = new StringBuilder(length);
                    ApiHelper.FailIfZero(GetClassName(_hwnd, sb, sb.Capacity));
                    if (sb.Length != length - 1)
                    {
                        return sb.ToString();
                    }
                    length *= 2;
                }
            }
        }

        /// <summary>
        /// Whether this window is currently visible. A window is visible if its 
        /// and all ancestor's visibility flags are true.
        /// </summary>
        public bool Visible
        {
            get
            {
                return IsWindowVisible(_hwnd);
            }
        }

        /// <summary>
        /// Whether this window always appears above all other windows
        /// that do not have this property set to true.
        /// </summary>
        public bool TopMost
        {
            get
            {
                return (ExtendedStyle & WindowExStyleFlags.TOPMOST) != 0;
            }
            set
            {
                if (value)
                {
                    SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
                }
                else
                {
                    SetWindowPos(_hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
                }
            }
        }

        /// <summary>
        /// Whether this window is currently enabled (able to accept keyboard input).
        /// </summary>
        public bool Enabled
        {
            get
            {
                return IsWindowEnabled(_hwnd);
            }
            set
            {
                EnableWindow(_hwnd, value);
            }
        }

        /// <summary>
        /// Returns or sets the visibility flag.
        /// </summary>
        /// <seealso cref="SystemWindow.Visible"/>
        public bool VisibilityFlag
        {
            get
            {
                return (Style & WindowStyleFlags.VISIBLE) != 0;
            }
            set
            {
                if (value)
                {
                    ShowWindow(_hwnd, 5);
                }
                else
                {
                    ShowWindow(_hwnd, 0);
                }
            }
        }

        /// <summary>
        /// This window's style flags.
        /// </summary>
        public WindowStyleFlags Style
        {
            get
            {
                return unchecked((WindowStyleFlags)GetWindowLongPtr(_hwnd, (int)(GWL.GWL_STYLE)).ToInt64());
            }
            set
            {
                SetWindowLong(_hwnd, (int)GWL.GWL_STYLE, (int)value);
            }

        }

        /// <summary>
        /// This window's extended style flags.
        /// </summary>
        public WindowExStyleFlags ExtendedStyle
        {
            get
            {
                return unchecked((WindowExStyleFlags)GetWindowLongPtr(_hwnd, (int)(GWL.GWL_EXSTYLE)).ToInt64());
            }
            set
            {
                SetWindowLong(_hwnd, (int)GWL.GWL_EXSTYLE, (int)value);
            }
        }

        /// <summary>
        /// This window's parent. A dialog's parent is its owner, a component's parent is
        /// the window that contains it.
        /// </summary>
        public SystemWindow Parent
        {
            get
            {
                return new SystemWindow(GetParent(_hwnd));
            }
        }

        /// <summary>
        /// The window's parent, but only if this window is its parent child. Some
        /// parents, like dialog owners, do not have the window as its child. In that case,
        /// <c>null</c> will be returned.
        /// </summary>
        public SystemWindow ParentSymmetric
        {
            get
            {
                SystemWindow result = Parent;
                if (!this.IsDescendantOf(result)) result = null;
                return result;
            }
        }

        /// <summary>
        /// The window's position inside its parent or on the screen.
        /// </summary>
        public RECT Position
        {
            get
            {
                WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                wp.length = Marshal.SizeOf(wp);
                GetWindowPlacement(_hwnd, ref wp);
                return wp.rcNormalPosition;
            }

            set
            {
                WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                wp.length = Marshal.SizeOf(wp);
                GetWindowPlacement(_hwnd, ref wp);
                wp.rcNormalPosition = value;
                SetWindowPlacement(_hwnd, ref wp);
            }
        }

        /// <summary>
        /// The window's location inside its parent or on the screen.
        /// </summary>
        public Point Location
        {
            get
            {
                return Position.Location;
            }

            set
            {
                WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                wp.length = Marshal.SizeOf(wp);
                GetWindowPlacement(_hwnd, ref wp);
                wp.rcNormalPosition.Bottom = value.Y + wp.rcNormalPosition.Height;
                wp.rcNormalPosition.Right = value.X + wp.rcNormalPosition.Width;
                wp.rcNormalPosition.Top = value.Y;
                wp.rcNormalPosition.Left = value.X;
                SetWindowPlacement(_hwnd, ref wp);
            }
        }

        /// <summary>
        /// The window's size.
        /// </summary>
        public Size Size
        {
            get
            {
                return Position.Size;
            }

            set
            {
                WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                wp.length = Marshal.SizeOf(wp);
                GetWindowPlacement(_hwnd, ref wp);
                wp.rcNormalPosition.Right = wp.rcNormalPosition.Left + value.Width;
                wp.rcNormalPosition.Bottom = wp.rcNormalPosition.Top + value.Height;
                SetWindowPlacement(_hwnd, ref wp);
            }
        }

        /// <summary>
        /// The window's position in absolute screen coordinates. Use 
        /// <see cref="Position"/> if you want to use the relative position.
        /// </summary>
        public RECT Rectangle
        {
            get
            {
                RECT r = new RECT();
                GetWindowRect(_hwnd, out r);

                try
                {
                    // GetWindowRect returns wrong values for maximized windows when using Windows Aero Glass desktop
                    // because there is negative padding of "glass" pixels for maximized windows.
                    // see http://social.msdn.microsoft.com/Forums/en-US/windowsuidevelopment/thread/6c1c67a9-5548-4e9b-989f-c7dbac0b1375/
                    // Limit miximized window size to working area of the corresponding desktop (supporting multi screen setups)
                    if ((Style & WindowStyleFlags.MAXIMIZE) > 0)
                    {
                        foreach (var screen in Screen.AllScreens)
                            if (((Rectangle)r).Contains(screen.WorkingArea))
                                return screen.WorkingArea;
                    }
                }
                catch { } // special handling should not cause failure

                return r;
            }
        }

        /// <summary>
        /// The position of the window's contents in absolute screen coordinates. Use 
        /// <see cref="Rectangle"/> if you want to include the title bar etc.
        /// </summary>
        public RECT ClientRectangle
        {
            get
            {
                RECT r = new RECT();
                ApiHelper.FailIfZero(GetClientRect(_hwnd, out r));
                Point p = new Point();
                p.X = p.Y = 0;
                ApiHelper.FailIfZero(ClientToScreen(_hwnd, ref p));
                Rectangle result = r;
                result.Location = p;
                return result;
            }
        }

        /// <summary>
        /// Check whether this window is a descendant of <c>ancestor</c>
        /// </summary>
        /// <param name="ancestor">The suspected ancestor</param>
        /// <returns>If this is really an ancestor</returns>
        public bool IsDescendantOf(SystemWindow ancestor)
        {
            return IsChild(ancestor._hwnd, _hwnd);
        }

        public int ProcessId
        {
            get
            {
                if (_cachedProcess != null)
                {
                    return _cachedProcess.Id;
                }
                int pid;
                GetWindowThreadProcessId(HWnd, out pid);
                return pid;
            }
        }

        private Process _cachedProcess = null;

        /// <summary>
        /// The process which created this window.
        /// </summary>
        public Process Process
        {
            get
            {
                if (_cachedProcess == null)
                {
                    int pid;
                    GetWindowThreadProcessId(HWnd, out pid);
                    _cachedProcess = Process.GetProcessById(pid);
                }
                return _cachedProcess;
            }
        }

        public string GetProcessFilePath()
        {
            var processHandle = OpenProcess(0x0400 | 0x0010, false, ProcessId);

            if (processHandle == IntPtr.Zero)
            {
                return null;
            }

            const int lengthSb = 1024;

            var sb = new StringBuilder(lengthSb);

            string result = null;

            if (GetModuleFileNameEx(processHandle, IntPtr.Zero, sb, lengthSb) > 0)
            {
                result = sb.ToString();
            }

            CloseHandle(processHandle);

            return result;
        }

        /// <summary>
        ///  The Thread which created this window.
        /// </summary>
        public ProcessThread Thread
        {
            get
            {
                int pid;
                int tid = GetWindowThreadProcessId(HWnd, out pid);
                foreach (ProcessThread t in Process.GetProcessById(pid).Threads)
                {
                    if (t.Id == tid) return t;
                }
                throw new Exception("Thread not found");
            }
        }

        /// <summary>
        /// Whether this window is minimized or maximized.
        /// </summary>
        public FormWindowState WindowState
        {
            get
            {
                WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                wp.length = Marshal.SizeOf(wp);
                GetWindowPlacement(HWnd, ref wp);
                switch (wp.showCmd % 4)
                {
                    case 2: return FormWindowState.Minimized;
                    case 3: return FormWindowState.Maximized;
                    default: return FormWindowState.Normal;
                }
            }
            set
            {
                int showCommand;
                switch (value)
                {
                    case FormWindowState.Normal:
                        showCommand = 1;
                        break;
                    case FormWindowState.Minimized:
                        showCommand = 2;
                        break;
                    case FormWindowState.Maximized:
                        showCommand = 3;
                        break;
                    default: return;
                }
                ShowWindow(HWnd, showCommand);
            }
        }

        public bool RestoreWindow()
        {
            return ShowWindowAsync(HWnd, SW_RESTORE);
        }

        /// <summary>
        /// Whether this window can be moved on the screen by the user.
        /// </summary>
        public bool Movable
        {
            get
            {
                return (Style & WindowStyleFlags.SYSMENU) != 0;
            }
        }

        /// <summary>
        /// Whether this window can be resized by the user. Resizing a window that
        /// cannot be resized by the user works, but may be irritating to the user.
        /// </summary>
        public bool Resizable
        {
            get
            {
                return (Style & WindowStyleFlags.THICKFRAME) != 0;
            }
        }

        /// <summary>
        /// An image of this window. Unlike a screen shot, this will not
        /// contain parts of other windows (partially) cover this window.
        /// If you want to create a screen shot, use the 
        /// <see cref="System.Drawing.Graphics.CopyFromScreen(System.Drawing.Point,System.Drawing.Point,System.Drawing.Size)"/> 
        /// function and use the <see cref="SystemWindow.Rectangle"/> property for
        /// the range.
        /// </summary>
        public Image Image
        {
            get
            {
                Bitmap bmp = new Bitmap(Position.Width, Position.Height);
                Graphics g = Graphics.FromImage(bmp);
                IntPtr pTarget = g.GetHdc();
                IntPtr pSource = CreateCompatibleDC(pTarget);
                IntPtr pOrig = SelectObject(pSource, bmp.GetHbitmap());
                PrintWindow(HWnd, pTarget, 0);
                IntPtr pNew = SelectObject(pSource, pOrig);
                DeleteObject(pOrig);
                DeleteObject(pNew);
                DeleteObject(pSource);
                g.ReleaseHdc(pTarget);
                g.Dispose();
                return bmp;
            }
        }

        public Icon Icon
        {
            get
            {
                return GetAppIcon(this.HWnd);
            }
        }

        /// <summary>
        /// The window's visible region.
        /// </summary>
        public Region Region
        {
            get
            {
                IntPtr rgn = CreateRectRgn(0, 0, 0, 0);
                int r = GetWindowRgn(HWnd, rgn);
                if (r == (int)GetWindowRegnReturnValues.ERROR)
                {
                    return null;
                }
                return Region.FromHrgn(rgn);
            }
            set
            {
                Bitmap bmp = new Bitmap(1, 1);
                Graphics g = Graphics.FromImage(bmp);
                SetWindowRgn(HWnd, value.GetHrgn(g), true);
                g.Dispose();
            }
        }

        /// <summary>
        /// The character used to mask passwords, if this control is
        /// a text field. May be used for different purpose by other
        /// controls.
        /// </summary>
        public char PasswordCharacter
        {
            get
            {
                return (char)SendGetMessage(EM_GETPASSWORDCHAR);
            }
            set
            {
                SendSetMessage(EM_SETPASSWORDCHAR, value);
            }
        }

        /// <summary>
        /// The ID of a control within a dialog. This is used in
        /// WM_COMMAND messages to distinguish which control sent the command.
        /// </summary>
        public int DialogID
        {
            get
            {
                return GetWindowLong32(_hwnd, (int)GWL.GWL_ID);
            }
        }

        /// <summary>
        /// Get the window that is below this window in the Z order,
        /// or null if this is the lowest window.
        /// </summary>
        public SystemWindow WindowBelow
        {
            get
            {
                IntPtr res = GetWindow(HWnd, (uint)GetWindow_Cmd.GW_HWNDNEXT);
                if (res == IntPtr.Zero) return null;
                return new SystemWindow(res);
            }
        }

        /// <summary>
        /// Get the window that is above this window in the Z order,
        /// or null, if this is the foreground window.
        /// </summary>
        public SystemWindow WindowAbove
        {
            get
            {
                IntPtr res = GetWindow(HWnd, (uint)GetWindow_Cmd.GW_HWNDPREV);
                if (res == IntPtr.Zero) return null;
                return new SystemWindow(res);
            }
        }

        /// <summary>
        /// Gets a device context for this window.
        /// </summary>
        /// <param name="clientAreaOnly">Whether to get the context for
        /// the client area or for the full window.</param>
        public WindowDeviceContext GetDeviceContext(bool clientAreaOnly)
        {
            if (clientAreaOnly)
            {
                return new WindowDeviceContext(this, GetDC(_hwnd));
            }
            else
            {
                return new WindowDeviceContext(this, GetWindowDC(_hwnd));
            }
        }

        /// <summary>
        /// Whether this control, which is a check box or radio button, is checked.
        /// </summary>
        public CheckState CheckState
        {
            get
            {
                return (CheckState)SendGetMessage(BM_GETCHECK);
            }
            set
            {
                SendSetMessage(BM_SETCHECK, (uint)value);
            }
        }

        /// <summary>
        /// Whether this SystemWindow represents a valid window that existed
        /// when this SystemWindow instance was created. To check if a window
        /// still exists, better check its <see cref="ClassName"/> property.
        /// </summary>
        public bool IsValid()
        {
            return _hwnd != IntPtr.Zero;
        }

        /// <summary>
        /// Send a message to this window that it should close. This is equivalent
        /// to clicking the "X" in the upper right corner or pressing Alt+F4.
        /// </summary>
        public void SendClose()
        {
            SendSetMessage(WM_CLOSE, 0);
        }

        /// <summary>
        /// Post a message to this window that it should close. This is equivalent
        /// to clicking the "X" in the upper right corner or pressing Alt+F4.
        /// It sometimes works in instances where the <see cref="SendClose"/> function does
        /// not (for example, Windows Explorer windows.)
        /// </summary>
        public void PostClose()
        {
            PostMessage(HWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Highlights the window with a red border.
        /// </summary>
        public void Highlight()
        {
            RECT rect = Rectangle;
            using (WindowDeviceContext windowDC = GetDeviceContext(false))
            {
                using (Graphics g = windowDC.CreateGraphics())
                {
                    g.DrawRectangle(new Pen(Color.Red, 4), 0, 0, rect.Right - rect.Left, rect.Bottom - rect.Top);
                }
            }
        }

        /// <summary>
        /// Forces the window to invalidate its client area and immediately redraw itself and any child controls. 
        /// </summary>
        public void Refresh()
        {
            // By using parent, we get better results in refreshing old drawing window area.
            IntPtr hwndToRefresh = this._hwnd;
            SystemWindow parentWindow = this.ParentSymmetric;
            if (parentWindow != null)
            {
                hwndToRefresh = parentWindow._hwnd;
            }

            InvalidateRect(hwndToRefresh, IntPtr.Zero, true);
            RedrawWindow(hwndToRefresh, IntPtr.Zero, IntPtr.Zero, RDW.RDW_FRAME | RDW.RDW_INVALIDATE | RDW.RDW_UPDATENOW | RDW.RDW_ALLCHILDREN | RDW.RDW_ERASENOW);
        }

        public bool WaitForIdle(uint timeout)
        {
            const uint WM_NULL = 0x0000;
            IntPtr lRes;
            return SendMessageTimeout(_hwnd, WM_NULL, IntPtr.Zero, IntPtr.Zero, SendMessageTimeoutFlags.SMTO_ABORTIFHUNG, timeout, out lRes) != IntPtr.Zero;
        }

        internal int SendGetMessage(uint message)
        {
            return SendGetMessage(message, 0);
        }

        internal int SendGetMessage(uint message, uint param)
        {
            return SendMessage(new HandleRef(this, HWnd), message, new IntPtr(param), new IntPtr(0)).ToInt32();
        }

        internal void SendSetMessage(uint message, uint value)
        {
            SendMessage(new HandleRef(this, HWnd), message, new IntPtr(value), new IntPtr(0));
        }

        private Icon GetAppIcon(IntPtr hwnd)
        {
            IntPtr iconHandle = SendMessage(new HandleRef(this, hwnd), WM_GETICON, new IntPtr(ICON_BIG), new IntPtr(0));
            if (iconHandle == IntPtr.Zero)
                iconHandle = SendMessage(new HandleRef(this, hwnd), WM_GETICON, new IntPtr(ICON_SMALL), new IntPtr(0));
            if (iconHandle == IntPtr.Zero)
                iconHandle = SendMessage(new HandleRef(this, hwnd), WM_GETICON, new IntPtr(ICON_SMALL2), new IntPtr(0));
            if (iconHandle == IntPtr.Zero)
                iconHandle = GetClassLongPtr(hwnd, GCL_HICON);
            if (iconHandle == IntPtr.Zero)
                iconHandle = GetClassLongPtr(hwnd, GCL_HICONSM);
            if (iconHandle == IntPtr.Zero)
            {
                var extractAssociatedIcon = Icon.ExtractAssociatedIcon(this.Process.MainModule.FileName);
                if (extractAssociatedIcon != null)
                    iconHandle = extractAssociatedIcon.Handle;
            }

            if (iconHandle == IntPtr.Zero)
                return null;

            Icon icn = Icon.FromHandle(iconHandle);

            return icn;
        }

        #region Equals and HashCode

        ///
        public override bool Equals(System.Object obj)
        {
            if (obj == null)
            {
                return false;
            }
            SystemWindow sw = obj as SystemWindow;
            return Equals(sw);
        }

        ///
        public bool Equals(SystemWindow sw)
        {
            if ((object)sw == null)
            {
                return false;
            }
            return _hwnd == sw._hwnd;
        }

        ///
        public override int GetHashCode()
        {
            // avoid exceptions
            return unchecked((int)_hwnd.ToInt64());
        }

        /// <summary>
        /// Compare two instances of this class for equality.
        /// </summary>
        public static bool operator ==(SystemWindow a, SystemWindow b)
        {
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }
            return a._hwnd == b._hwnd;
        }

        /// <summary>
        /// Compare two instances of this class for inequality.
        /// </summary>
        public static bool operator !=(SystemWindow a, SystemWindow b)
        {
            return !(a == b);
        }

        #endregion

        #region PInvoke Declarations

        internal const int GCL_HICONSM = -34;
        internal const int GCL_HICON = -14;

        internal const int ICON_SMALL = 0;
        internal const int ICON_BIG = 1;
        internal const int ICON_SMALL2 = 2;

        internal const uint WM_GETICON = 0x7F;

        private static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
        {
            try
            {
                if (IntPtr.Size > 4)
                    return GetClassLongPtr64(hWnd, nIndex);
                else
                    return new IntPtr(GetClassLongPtr32(hWnd, nIndex));
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        [DllImport("user32.dll", EntryPoint = "GetClassLong")]
        private static extern uint GetClassLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
        private static extern IntPtr GetClassLongPtr64(IntPtr hWnd, int nIndex);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, [Out] StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private delegate int EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowEnabled(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        private enum GWL : int
        {
            GWL_WNDPROC = (-4),
            GWL_HINSTANCE = (-6),
            GWL_HWNDPARENT = (-8),
            GWL_STYLE = (-16),
            GWL_EXSTYLE = (-20),
            GWL_USERDATA = (-21),
            GWL_ID = (-12)
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        [DllImport("user32.dll")]
        static extern bool GetWindowPlacement(IntPtr hWnd,
           ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        static extern bool SetWindowPlacement(IntPtr hWnd,
           [In] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern int GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern int ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect,
           int nBottomRect);

        [DllImport("user32.dll")]
        static extern int GetWindowRgn(IntPtr hWnd, IntPtr hRgn);

        [DllImport("user32.dll")]
        static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport("gdi32.dll")]
        static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth,
           int nHeight, IntPtr hObjSource, int nXSrc, int nYSrc, int dwRop);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        static extern bool DeleteObject(IntPtr hObject);

        enum TernaryRasterOperations : uint
        {
            SRCCOPY = 0x00CC0020,
            SRCPAINT = 0x00EE0086,
            SRCAND = 0x008800C6,
            SRCINVERT = 0x00660046,
            SRCERASE = 0x00440328,
            NOTSRCCOPY = 0x00330008,
            NOTSRCERASE = 0x001100A6,
            MERGECOPY = 0x00C000CA,
            MERGEPAINT = 0x00BB0226,
            PATCOPY = 0x00F00021,
            PATPAINT = 0x00FB0A09,
            PATINVERT = 0x005A0049,
            DSTINVERT = 0x00550009,
            BLACKNESS = 0x00000042,
            WHITENESS = 0x00FF0062
        }

        enum GetWindowRegnReturnValues : int
        {
            ERROR = 0,
            NULLREGION = 1,
            SIMPLEREGION = 2,
            COMPLEXREGION = 3
        }

        static readonly uint EM_GETPASSWORDCHAR = 0xD2, EM_SETPASSWORDCHAR = 0xCC;
        static readonly uint BM_GETCHECK = 0xF0, BM_SETCHECK = 0xF1;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        internal static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        internal static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, IntPtr wParam, [Out] StringBuilder lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X,
           int Y, int cx, int cy, uint uFlags);

        // special values for hWndInsertAfter
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        static readonly IntPtr HWND_TOP = new IntPtr(0);
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        // window sizing and positioning flags
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint SWP_DRAWFRAME = 0x0020;
        const uint SWP_SHOWWINDOW = 0x0040;
        const uint SWP_NOOWNERZORDER = 0x0200;
        const uint SWP_ASYNCWINDOWPOS = 0x4000;

        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        private const int WM_CLOSE = 16, WM_GETTEXT = 13, WM_GETTEXTLENGTH = 14;

        private enum GetWindow_Cmd
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }

        [DllImport("user32.dll")]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        private enum RDW : uint
        {
            RDW_INVALIDATE = 0x0001,
            RDW_INTERNALPAINT = 0x0002,
            RDW_ERASE = 0x0004,

            RDW_VALIDATE = 0x0008,
            RDW_NOINTERNALPAINT = 0x0010,
            RDW_NOERASE = 0x0020,

            RDW_NOCHILDREN = 0x0040,
            RDW_ALLCHILDREN = 0x0080,

            RDW_UPDATENOW = 0x0100,
            RDW_ERASENOW = 0x0200,

            RDW_FRAME = 0x0400,
            RDW_NOFRAME = 0x0800,
        }

        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, RDW flags);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("psapi.dll")]
        static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In] [MarshalAs(UnmanagedType.U4)] int nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [Flags]
        enum SendMessageTimeoutFlags : uint
        {
            SMTO_NORMAL = 0x0,
            SMTO_BLOCK = 0x1,
            SMTO_ABORTIFHUNG = 0x2,
            SMTO_NOTIMEOUTIFNOTHUNG = 0x8,
            SMTO_ERRORONEXIT = 0x20
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr windowHandle,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam,
            SendMessageTimeoutFlags flags,
            uint timeout,
            out IntPtr result);

        #endregion
    }

    /// <summary>
    /// A device context of a window that allows you to draw onto that window.
    /// </summary>
    public class WindowDeviceContext : IDisposable
    {
        IntPtr hDC;
        SystemWindow sw;

        internal WindowDeviceContext(SystemWindow sw, IntPtr hDC)
        {
            this.sw = sw;
            this.hDC = hDC;
        }

        /// <summary>
        /// The device context handle.
        /// </summary>
        public IntPtr HDC { get { return hDC; } }

        /// <summary>
        /// Creates a Graphics object for this device context.
        /// </summary>
        public Graphics CreateGraphics()
        {
            return Graphics.FromHdc(hDC);
        }

        /// <summary>
        /// Frees this device context.
        /// </summary>
        public void Dispose()
        {
            if (hDC != IntPtr.Zero)
            {
                ReleaseDC(sw.HWnd, hDC);
                hDC = IntPtr.Zero;
            }
        }

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    }

}
