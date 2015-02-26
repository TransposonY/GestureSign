using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ManagedWinapi
{
    /// <summary>
    /// A <see cref="TextBox" /> that can be used to select a keyboard shortcut.
    /// A context menu allows selecting keys that are not available directly
    /// by typing them.
    /// </summary>
    public partial class ShortcutBox : TextBox
    {
        private Keys key;
        private bool shift;
        private bool alt;
        private bool ctrl;
        private bool windowsKey;

        /// <summary>
        /// Creates a new shortcut box.
        /// </summary>
        public ShortcutBox()
        {
            InitializeComponent();

            tabMenuItem.Text = GetKeyName(Keys.Tab);
            returnMenuItem.Text = GetKeyName(Keys.Return);
            escMenuItem.Text = GetKeyName(Keys.Escape);
            prtscMenuItem.Text = GetKeyName(Keys.PrintScreen);
            ctrlMenuItem.Text = GetKeyName(Keys.ControlKey);
            altMenuItem.Text = GetKeyName(Keys.Menu);
            shiftMenuItem.Text = GetKeyName(Keys.ShiftKey);
            winMenuItem.Text = GetKeyName(Keys.LWin);

            RefreshText();
        }

        /// <summary>
        /// The "non-modifier" key code of the currently selected shortcut, or
        /// <see cref="Keys.None"/> if no key is selected.
        /// </summary>
        public Keys KeyCode
        {
            get { return key; }
            set { key = value; RefreshText(); }
        }

        /// <summary>
        /// Whether the currently selected shortcut includes the Shift key.
        /// </summary>
        public bool Shift
        {
            get { return shift; }
            set { shift = value; shiftMenuItem.Checked = value; RefreshText(); }
        }

        /// <summary>
        /// Whether the currently selected shortcut includes the Alt key.
        /// </summary>
        public bool Alt
        {
            get { return alt; }
            set { alt = value; altMenuItem.Checked = value; RefreshText(); }
        }

        /// <summary>
        /// Whether the currently selected shortcut includes the Control key.
        /// </summary>
        public bool Ctrl
        {
            get { return ctrl; }
            set { ctrl = value; ctrlMenuItem.Checked = value; RefreshText(); }
        }

        /// <summary>
        /// Whether the currently selected shortcut includes the Windows key.
        /// </summary>
        public bool WindowsKey
        {
            get { return windowsKey; }
            set { windowsKey = value; winMenuItem.Checked = value; RefreshText(); }
        }

        /// <summary>
        /// The textual representation of the currently selected key.
        /// This property cannot be set.
        /// </summary>
        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                // ignore
            }
        }

        private void RefreshText()
        {
            string s;
            if (key == Keys.None)
            {
                if (ctrl || alt || shift || windowsKey)
                {
                    s = "?";
                }
                else
                {
                    s = "None";
                }
            }
            else
            {
                s = GetKeyName(key);
            }
            if (shift) s = GetKeyName(Keys.ShiftKey) + " + " + s;
            if (windowsKey) s = GetKeyName(Keys.LWin) + " + " + s;
            if (alt) s = GetKeyName(Keys.Menu) + " + " + s;
            if (ctrl) s = GetKeyName(Keys.ControlKey) + " + " + s;
            base.Text = s;
            base.SelectionStart = s.Length;
        }

        private static string GetKeyName(Keys key)
        {
            return new KeyboardKey(key).KeyName;
        }

        private bool currWindowsKey = false;
        private void ShortcutBox_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (!ReadOnly)
            {
                Shift = e.Shift;
                Ctrl = e.Control;
                Alt = e.Alt;
                WindowsKey = currWindowsKey;
            }
            if (e.KeyCode == Keys.RWin || e.KeyCode == Keys.LWin)
            {
                currWindowsKey = true;
            }
            if (ReadOnly) return;
            switch (e.KeyCode)
            {
                case Keys.ShiftKey:
                case Keys.ControlKey:
                case Keys.Menu:
                case Keys.LWin:
                case Keys.RWin:
                    key = Keys.None;
                    break;
                default:
                    key = e.KeyCode;
                    break;
            }
            RefreshText();
        }

        private void ShortcutBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.RWin || e.KeyCode == Keys.LWin)
            {
                currWindowsKey = false;
            }
            if (key == Keys.None && !ReadOnly)
            {
                Shift = Ctrl = Alt = WindowsKey = false;
                RefreshText();
            }
            e.Handled = true;
        }

        private void ShortcutBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void altMenuItem_Click(object sender, EventArgs e)
        {
            if (!ReadOnly && key != Keys.None) { Alt = !altMenuItem.Checked; RefreshText(); }
        }

        private void ctrlMenuItem_Click(object sender, EventArgs e)
        {
            if (!ReadOnly && key != Keys.None) { Ctrl = !ctrlMenuItem.Checked; RefreshText(); }
        }

        private void shiftMenuItem_Click(object sender, EventArgs e)
        {
            if (!ReadOnly && key != Keys.None) { Shift = !shiftMenuItem.Checked; RefreshText(); }
        }

        private void winMenuItem_Click(object sender, EventArgs e)
        {
            if (!ReadOnly && key != Keys.None) { WindowsKey = !winMenuItem.Checked; RefreshText(); }
        }

        private void escMenuItem_Click(object sender, EventArgs e)
        {
            if (!ReadOnly) { key = Keys.Escape; RefreshText(); }
        }

        private void noneMenuItem_Click(object sender, EventArgs e)
        {
            if (!ReadOnly) { key = Keys.None; RefreshText(); }
        }

        private void prtscMenuItem_Click(object sender, EventArgs e)
        {
            if (!ReadOnly) { key = Keys.PrintScreen; RefreshText(); }
        }

        private void returnMenuItem_Click(object sender, EventArgs e)
        {
            if (!ReadOnly) { key = Keys.Return; RefreshText(); }
        }

        private void tabMenuItem_Click(object sender, EventArgs e)
        {
            if (!ReadOnly) { key = Keys.Tab; RefreshText(); }
        }
    }
}
