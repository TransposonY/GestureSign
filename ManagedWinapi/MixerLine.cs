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
using System.Runtime.InteropServices;
using ManagedWinapi.Windows;

namespace ManagedWinapi.Audio.Mixer
{
    /// <summary>
    /// Represents a mixer line, either a source line or a destination line.
    /// </summary>
    public abstract class MixerLine : IDisposable
    {
        /// <summary>
        /// Occurs when this line changes.
        /// </summary>
        public EventHandler Changed;

        internal MIXERLINE line;
        internal Mixer mixer;
        private MixerControl[] controls = null;

        internal MixerLine(Mixer mixer, MIXERLINE line)
        {
            this.mixer = mixer;
            this.line = line;
        }

        ///
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// All controls of this line.
        /// </summary>
        public MixerControl[] Controls
        {
            get
            {
                if (controls == null)
                {
                    controls = MixerControl.GetControls(mixer, this, ControlCount);
                }
                return controls;
            }
        }

        /// <summary>
        /// The volume control of this line, if it has one.
        /// </summary>
        public FaderMixerControl VolumeControl
        {
            get
            {
                foreach (MixerControl c in Controls)
                {
                    if (c.ControlType == MixerControlType.MIXERCONTROL_CONTROLTYPE_VOLUME)
                    {
                        return (FaderMixerControl)c;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// The mute switch of this control, if it has one.
        /// </summary>
        public BooleanMixerControl MuteSwitch
        {
            get
            {
                foreach (MixerControl c in Controls)
                {
                    if (c.ControlType == MixerControlType.MIXERCONTROL_CONTROLTYPE_MUTE)
                    {
                        return (BooleanMixerControl)c;
                    }
                }
                return null;

            }
        }

        /// <summary>
        /// Gets the ID of this line.
        /// </summary>
        public int Id { get { return line.dwLineID; } }

        /// <summary>
        /// Gets the number of channels of this line.
        /// </summary>
        public int ChannelCount { get { return line.cChannels; } }

        /// <summary>
        /// Gets the number of controls of this line.
        /// </summary>
        public int ControlCount { get { return line.cControls; } }

        /// <summary>
        /// Gets the short name of this line;
        /// </summary>
        public string ShortName { get { return line.szShortName; } }

        /// <summary>
        /// Gets the full name of this line.
        /// </summary>
        public string Name { get { return line.szName; } }

        /// <summary>
        /// Gets the component type of this line;
        /// </summary>
        public MixerLineComponentType ComponentType
        {
            get
            {
                return (MixerLineComponentType)line.dwComponentType;
            }
        }

        /// <summary>
        /// The mixer that owns this line.
        /// </summary>
        public Mixer Mixer { get { return mixer; } }

        internal MixerLine findLine(int lineId)
        {
            if (Id == lineId) { return this; }
            foreach (MixerLine ml in ChildLines)
            {
                MixerLine found = ml.findLine(lineId);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static readonly IList<MixerLine> EMPTY_LIST =
            new List<MixerLine>().AsReadOnly();

        internal virtual IList<MixerLine> ChildLines
        {
            get
            {
                return EMPTY_LIST;
            }
        }

        internal MixerControl findControl(int ctrlId)
        {
            foreach (MixerControl c in Controls)
            {
                if (c.Id == ctrlId) return c;
            }
            foreach (MixerLine l in ChildLines)
            {
                MixerControl found = l.findControl(ctrlId);
                if (found != null) return found;
            }
            return null;
        }

        internal void OnChanged()
        {
            if (Changed != null)
                Changed(this, EventArgs.Empty);
        }

        #region PInvoke Declarations

        internal struct MIXERLINE
        {
            public int cbStruct;
            public int dwDestination;
            public int dwSource;
            public int dwLineID;
            public int fdwLine;
            public int dwUser;
            public int dwComponentType;
            public int cChannels;
            public int cConnections;
            public int cControls;
            [MarshalAs(UnmanagedType.ByValTStr,
            SizeConst = 16)]
            public string szShortName;
            [MarshalAs(UnmanagedType.ByValTStr,
            SizeConst = 64)]
            public string szName;
            public int dwType;
            public int dwDeviceID;
            public int wMid;
            public int wPid;
            public int vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
        }

        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        internal static extern int mixerGetLineInfoA(IntPtr hmxobj, ref 
            MIXERLINE pmxl, int fdwInfo);

        internal static int MIXER_GETLINEINFOF_DESTINATION = 0;
        internal static int MIXER_GETLINEINFOF_SOURCE = 1;

        #endregion
    }

    /// <summary>
    /// Represents a destination line. There is one destination line for
    /// each way sound can leave the mixer. Usually there are two destination lines,
    /// one for playback and one for recording.
    /// </summary>
    public class DestinationLine : MixerLine
    {
        private DestinationLine(Mixer mixer, MIXERLINE line) : base(mixer, line) { }

        /// <summary>
        /// Gets the number of source lines of this destination line.
        /// </summary>
        public int SourceLineCount { get { return line.cConnections; } }

        private IList<SourceLine> srcLines = null;

        /// <summary>
        /// Gets all source lines of this destination line.
        /// </summary>
        public IList<SourceLine> SourceLines
        {
            get
            {
                if (srcLines == null)
                {
                    List<SourceLine> sls = new List<SourceLine>(SourceLineCount);
                    for (int i = 0; i < SourceLineCount; i++)
                    {
                        sls.Add(SourceLine.GetLine(mixer, line.dwDestination, i));
                    }
                    srcLines = sls.AsReadOnly();
                }
                return srcLines;
            }
        }

        internal static DestinationLine GetLine(Mixer mixer, int index)
        {
            MIXERLINE m = new MIXERLINE();
            m.cbStruct = Marshal.SizeOf(m);
            m.dwDestination = index;
            mixerGetLineInfoA(mixer.Handle, ref m, MIXER_GETLINEINFOF_DESTINATION);
            return new DestinationLine(mixer, m);
        }

        ///
        public override void Dispose()
        {
        }

        private IList<MixerLine> childLines;

        internal override IList<MixerLine> ChildLines
        {
            get
            {
                if (childLines == null)
                {
                    List<MixerLine> cl = new List<MixerLine>();
                    foreach (MixerLine ml in SourceLines)
                    {
                        cl.Add(ml);
                    }
                    childLines = cl.AsReadOnly();
                }
                return childLines;
            }
        }
    }

    /// <summary>
    /// Represents a source line. Source lines represent way sound for one
    /// destination enters the mixer. So, if you can both record and playback
    /// CD audio, there will be two CD audio source lines, one for the Recording
    /// destination line and one for the Playback destination line.
    /// </summary>
    public class SourceLine : MixerLine
    {
        private SourceLine(Mixer m, MIXERLINE l) : base(m, l) { }

        internal static SourceLine GetLine(Mixer mixer, int destIndex, int srcIndex)
        {
            MIXERLINE m = new MIXERLINE();
            m.cbStruct = Marshal.SizeOf(m);
            m.dwDestination = destIndex;
            m.dwSource = srcIndex;
            mixerGetLineInfoA(mixer.Handle, ref m, MIXER_GETLINEINFOF_SOURCE);
            return new SourceLine(mixer, m);
        }
    }

    /// <summary>
    /// Types of source or destination lines. The descriptions for these
    /// lines have been taken from http://www.borg.com/~jglatt/tech/mixer.htm.
    /// </summary>
    public enum MixerLineComponentType
    {
        /// <summary>
        /// An undefined destination line type.
        /// </summary>
        DST_UNDEFINED = 0,

        /// <summary>
        /// A digital destination, for example, a SPDIF output jack.
        /// </summary>
        DST_DIGITAL = 1,

        /// <summary>
        /// A line output destination. Typically used for a line output 
        /// jack, if there is a separate speaker output (ie, 
        /// MIXERLINE_COMPONENTTYPE_DST_SPEAKERS).
        /// </summary>
        DST_LINE = 2,

        /// <summary>
        /// Typically a "Monitor Out" jack to be used for a speaker system 
        /// separate from the main speaker out. Or, it could be some built-in 
        /// monitor speaker on the sound card itself, such as a speaker for a 
        /// built-in modem.
        /// </summary>
        DST_MONITOR = 3,

        /// <summary>
        /// The audio output to a pair of speakers (ie, the "Speaker Out" jack).
        /// </summary>
        DST_SPEAKERS = 4,

        /// <summary>
        /// Typically, a headphone output jack.
        /// </summary>
        DST_HEADPHONES = 5,

        /// <summary>
        /// Typically used to daisy-chain a telephone to an analog 
        /// modem's "telephone out" jack.
        /// </summary>
        DST_TELEPHONE = 6,

        /// <summary>
        /// The card's ADC (to digitize analog sources, for example, 
        /// in recording WAVE files of such).
        /// </summary>
        DST_WAVEIN = 7,

        /// <summary>
        /// May be some sort of hardware used for voice recognition. 
        /// Typically, a microphone source line would be attached to this.
        /// </summary>
        DST_VOICEIN = 8,

        /// <summary>
        /// An undefined source line type.
        /// </summary>
        SRC_UNDEFINED = 0x1000,

        /// <summary>
        /// A digital source, for example, a SPDIF input jack.
        /// </summary>
        SRC_DIGITAL = 0x1001,

        /// <summary>
        /// A line input source. Typically used for a line input jack, 
        /// if there is a separate microphone input (ie, 
        /// MIXERLINE_COMPONENTTYPE_SRC_MICROPHONE).
        /// </summary>
        SRC_LINE = 0x1002,

        /// <summary>
        /// Microphone input (but also used for a combination of 
        /// Mic/Line input if there isn't a separate line input source).
        /// </summary>
        SRC_MICROPHONE = 0x1003,

        /// <summary>
        /// Musical synth. Typically used for a card that contains a 
        /// synth capable of playing MIDI. This would be the audio out 
        /// of that built-in synth.
        /// </summary>
        SRC_SYNTHESIZER = 0x1004,

        /// <summary>
        /// The audio feed from an internal CDROM drive (connected to 
        /// the sound card).
        /// </summary>
        SRC_COMPACTDISC = 0x1005,

        /// <summary>
        /// Typically used for a telephone line's incoming audio 
        /// to be piped through the computer's speakers, or the 
        /// telephone line in jack for a built-in modem.
        /// </summary>
        SRC_TELEPHONE = 0x1006,

        /// <summary>
        /// Typically, to allow sound, that normally goes to the computer's 
        /// built-in speaker, to instead be routed through the card's speaker 
        /// output. The motherboard's system speaker connector would be internally 
        /// connected to some connector on the sound card for this purpose.
        /// </summary>
        SRC_PCSPEAKER = 0x1007,

        /// <summary>
        /// Wave playback (ie, this is the card's DAC).
        /// </summary>
        SRC_WAVEOUT = 0x1008,

        /// <summary>
        /// 	An aux jack meant to be routed to the Speaker Out, or to the 
        /// ADC (for WAVE recording). Typically, this is used to connect external, 
        /// analog equipment (such as tape decks, the audio outputs of musical 
        /// instruments, etc) for digitalizing or playback through the sound card.
        /// </summary>
        SRC_AUXILIARY = 0x1009,

        /// <summary>
        /// May be used similiarly to MIXERLINE_COMPONENTTYPE_SRC_AUXILIARY (although 
        /// I have seen some mixers use this like MIXERLINE_COMPONENTTYPE_SRC_PCSPEAKER). 
        /// In general, this would be some analog connector on the sound card which is 
        /// only accessible internally, to be used to internally connect some analog component 
        /// inside of the computer case so that it plays through the speaker out.
        /// </summary>
        SRC_ANALOG = 0x100A
    }
}
