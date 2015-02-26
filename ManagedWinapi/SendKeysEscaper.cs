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

namespace ManagedWinapi
{

    /// <summary>
    /// Utility class to escape literal strings so that they can be used 
    /// for the <see cref="System.Windows.Forms.SendKeys"/> class.
    /// </summary>
    public class SendKeysEscaper
    {

        /// <summary>
        /// Specifies if a character needs to be escaped.
        /// </summary>
        private enum EscapableState { 
            
            /// <summary>
            /// The character cannot be used at all with SendKeys.
            /// </summary>
            NOT_AT_ALL, 
            
            /// <summary>
            /// The character must be escaped by putting it into braces
            /// </summary>
            BRACED_ONLY, 
            
            /// <summary>
            /// The character may not be escaped by putting it into braces
            /// </summary>
            UNBRACED_ONLY, 
            
            /// <summary>
            /// Both ways are okay.
            /// </summary>
            ALWAYS 
        }

        private static SendKeysEscaper _instance;

        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static SendKeysEscaper Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SendKeysEscaper();
                return _instance;
            }
        }

        private EscapableState[] lookupTable = new EscapableState[256];

        private SendKeysEscaper() {
            for (int i = 0; i < lookupTable.Length; i++)
            {
                lookupTable[i] = EscapableState.ALWAYS;
            }
            foreach (char c in "%()+^`{}~?")
            {
                lookupTable[c] = EscapableState.BRACED_ONLY;
            }
            lookupTable[180] = EscapableState.BRACED_ONLY;
            for (int i = 9; i <= 13; i++)
            {
                lookupTable[i] = EscapableState.UNBRACED_ONLY;
            }
            lookupTable[32] = EscapableState.UNBRACED_ONLY;
            lookupTable[133] = EscapableState.UNBRACED_ONLY;
            lookupTable[160] = EscapableState.UNBRACED_ONLY;
            for (int i = 0; i < 9; i++)
            {  
               lookupTable[i] = EscapableState.NOT_AT_ALL; 
            }
            for (int i = 14; i < 30; i++)
			{
               lookupTable[i] = EscapableState.NOT_AT_ALL; 
			}
            lookupTable[127] = EscapableState.NOT_AT_ALL;
        }

        private EscapableState getEscapableState(char c) {
            if (c < 256) return lookupTable[c]; else return EscapableState.ALWAYS;
        }

        /// <summary>
        /// Escapes a literal string.
        /// </summary>
        /// <param name="literal">The literal string to be sent.</param>
        /// <param name="preferBraced">Whether you prefer to put characters into braces.
        /// </param>
        /// <returns>The escaped string.</returns>
        public string escape(string literal, bool preferBraced)
        {
            StringBuilder sb = new StringBuilder(literal.Length);
            foreach (char c in literal)
            {
                switch (getEscapableState(c))
                {
                    case EscapableState.NOT_AT_ALL:
                        // ignore
                        break;
                    case EscapableState.BRACED_ONLY:
                        sb.Append("{").Append(c).Append("}");
                        break;
                    case EscapableState.UNBRACED_ONLY:
                        sb.Append(c);
                        break;
                    case EscapableState.ALWAYS:
                        if (preferBraced)
                        {
                            sb.Append("{").Append(c).Append("}");
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
