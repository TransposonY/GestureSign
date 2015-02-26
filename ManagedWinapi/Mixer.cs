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
using System.ComponentModel;
using System.Runtime.InteropServices;
using ManagedWinapi.Windows;

namespace ManagedWinapi.Audio.Mixer
{
    /// <summary>
    /// Represents a mixer provided by a sound card. Each mixer has
    /// multiple destination lines (e. g. Record and Playback) of which
    /// each has multiple source lines (Wave, MIDI, Mic, etc.).
    /// </summary>
    public class Mixer : IDisposable
    {
        /// <summary>
        /// Gets the number of available mixers in this system.
        /// </summary>
        public static uint MixerCount
        {
            get
            {
                return mixerGetNumDevs();
            }
        }

        /// <summary>
        /// Opens a mixer.
        /// </summary>
        /// <param name="index">The zero-based index of this mixer.</param>
        /// <returns>A reference to this mixer.</returns>
        public static Mixer OpenMixer(uint index)
        {
            if (index < 0 || index > MixerCount)
                throw new ArgumentException();
            IntPtr hMixer = IntPtr.Zero;
            EventDispatchingNativeWindow ednw = EventDispatchingNativeWindow.Instance;
            int error = mixerOpen(ref hMixer, index, ednw.Handle, IntPtr.Zero, CALLBACK_WINDOW);
            if (error != 0)
            {
                throw new Win32Exception("Could not load mixer: " + error);
            }
            return new Mixer(hMixer);
        }

        private IntPtr hMixer;
        private MIXERCAPS mc;
        private IList<DestinationLine> destLines = null;
        private bool createEvents;

        /// <summary>
        /// Occurs when a control of this mixer changes value.
        /// </summary>
        public MixerEventHandler ControlChanged;

        /// <summary>
        /// Occurs when a line of this mixer changes.
        /// </summary>
        public MixerEventHandler LineChanged;

        private Mixer(IntPtr hMixer)
        {
            this.hMixer = hMixer;
            EventDispatchingNativeWindow.Instance.EventHandler += ednw_EventHandler;
            mixerGetDevCapsA(hMixer, ref mc, Marshal.SizeOf(mc));
        }

        private void ednw_EventHandler(ref System.Windows.Forms.Message m, ref bool handled)
        {
            if (!createEvents) return;
            if (m.Msg == MM_MIXM_CONTROL_CHANGE && m.WParam == hMixer)
            {
                int ctrlID = m.LParam.ToInt32();
                MixerControl c = FindControl(ctrlID);
                if (c != null)
                {
                    if (ControlChanged != null)
                    {
                        ControlChanged(this, new MixerEventArgs(this, c.Line, c));
                    }
                    c.OnChanged();
                }
            }
            else if (m.Msg == MM_MIXM_LINE_CHANGE && m.WParam == hMixer)
            {
                int lineID = m.LParam.ToInt32();
                MixerLine l = FindLine(lineID);
                if (l != null)
                {
                    if (ControlChanged != null)
                    {
                        LineChanged(this, new MixerEventArgs(this, l, null));
                    }
                    l.OnChanged();
                }
            }
        }

        /// <summary>
        /// Whether to create change events.
        /// Enabling this may create a slight performance impact, so only
        /// enable it if you handle these events.
        /// </summary>
        public bool CreateEvents
        {
            get { return createEvents; }
            set { createEvents = value; }
        }

        internal IntPtr Handle { get { return hMixer; } }

        /// <summary>
        /// Gets the name of this mixer's sound card.
        /// </summary>
        public string Name
        {
            get
            {
                return mc.szPname;
            }
        }

        /// <summary>
        /// Gets the number of destination lines of this mixer.
        /// </summary>
        public int DestinationLineCount
        {
            get
            {
                return mc.cDestinations;
            }
        }

        /// <summary>
        /// Gets all destination lines of this mixer
        /// </summary>
        public IList<DestinationLine> DestinationLines
        {
            get
            {
                if (destLines == null)
                {
                    int dlc = DestinationLineCount;
                    List<DestinationLine> l = new List<DestinationLine>(dlc);
                    for (int i = 0; i < dlc; i++)
                    {
                        l.Add(DestinationLine.GetLine(this, i));
                    }
                    destLines = l.AsReadOnly();

                }
                return destLines;
            }
        }

        /// <summary>
        /// Disposes this mixer.
        /// </summary>
        public void Dispose()
        {
            if (destLines != null)
            {
                foreach (DestinationLine dl in destLines)
                {
                    dl.Dispose();
                }
                destLines = null;
            }
            if (hMixer.ToInt32() != 0)
            {
                mixerClose(hMixer);
                EventDispatchingNativeWindow.Instance.EventHandler -= ednw_EventHandler;
                hMixer = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Find a line of this mixer by ID.
        /// </summary>
        /// <param name="lineId">ID of the line to find</param>
        /// <returns>The line, or <code>null</code> if no line was found.</returns>
        public MixerLine FindLine(int lineId)
        {
            foreach (DestinationLine dl in DestinationLines)
            {
                MixerLine found = dl.findLine(lineId);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        /// Find a control of this mixer by ID.
        /// </summary>
        /// <param name="ctrlId">ID of the control to find.</param>
        /// <returns>The control, or <code>null</code> if no control was found.</returns>
        public MixerControl FindControl(int ctrlId)
        {
            foreach (DestinationLine dl in DestinationLines)
            {
                MixerControl found = dl.findControl(ctrlId);
                if (found != null) return found;
            }
            return null;
        }

        #region PInvoke Declarations

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern uint mixerGetNumDevs();

        [DllImport("winmm.dll")]
        private static extern Int32 mixerOpen(ref IntPtr phmx, uint pMxId,
           IntPtr dwCallback, IntPtr dwInstance, UInt32 fdwOpen);

        [DllImport("winmm.dll")]
        private static extern Int32 mixerClose(IntPtr hmx);

        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        private static extern int mixerGetDevCapsA(IntPtr uMxId, ref MIXERCAPS
        pmxcaps, int cbmxcaps);

        private struct MIXERCAPS
        {
            public short wMid;
            public short wPid;
            public int vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public int fdwSupport;
            public int cDestinations;
        }
        private static readonly uint CALLBACK_WINDOW = 0x00010000;
        private static readonly int MM_MIXM_LINE_CHANGE = 0x3D0;
        private static readonly int MM_MIXM_CONTROL_CHANGE = 0x3D1;
        #endregion
    }

    /// <summary>
    /// Represents the method that will handle the <b>LineChanged</b> or 
    /// <b>ControlChanged</b> event of a <see cref="Mixer">Mixer</see>.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="MixerEventArgs">MixerEventArgs</see> 
    /// that contains the event data.</param>
    public delegate void MixerEventHandler(object sender, MixerEventArgs e);

    /// <summary>
    /// Provides data for the LineChanged and ControlChanged events of a 
    /// <see cref="Mixer">Mixer</see>.
    /// </summary>
    public class MixerEventArgs : EventArgs
    {
        private Mixer mixer;
        private MixerLine line;
        private MixerControl control;

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="MixerEventArgs">MixerEventArgs</see> class.
        /// </summary>
        /// <param name="mixer">The affected mixer</param>
        /// <param name="line">The affected line</param>
        /// <param name="control">The affected control, or <code>null</code>
        /// if this is a LineChanged event.</param>
        public MixerEventArgs(Mixer mixer, MixerLine line, MixerControl control)
        {
            this.mixer = mixer;
            this.line = line;
            this.control = control;
        }

        /// <summary>
        /// The affected mixer.
        /// </summary>
        public Mixer Mixer { get { return mixer; } }

        /// <summary>
        /// The affected line.
        /// </summary>
        public MixerLine Line { get { return line; } }

        /// <summary>
        /// The affected control.
        /// </summary>
        public MixerControl Control { get { return control; } }
    }
}