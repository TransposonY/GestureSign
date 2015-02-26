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
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ManagedWinapi
{
    /// <summary>
    /// Helper class that contains static methods useful for API programming. This
    /// class is not exposed to the user.
    /// </summary>
    internal class ApiHelper
    {
        /// <summary>
        /// Throw a <see cref="Win32Exception"/> if the supplied (return) value is zero.
        /// This exception uses the last Win32 error code as error message.
        /// </summary>
        /// <param name="returnValue">The return value to test.</param>
        internal static int FailIfZero(int returnValue)
        {
            if (returnValue == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            return returnValue;
        }

        /// <summary>
        /// Throw a <see cref="Win32Exception"/> if the supplied (return) value is zero.
        /// This exception uses the last Win32 error code as error message.
        /// </summary>
        /// <param name="returnValue">The return value to test.</param>
        internal static IntPtr FailIfZero(IntPtr returnValue)
        {
            if (returnValue == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            return returnValue;
        }
    }
}
