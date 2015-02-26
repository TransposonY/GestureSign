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

namespace ManagedWinapi.Windows.Contents
{
    /// <summary>
    /// An abstract representation of the content of a window or control.
    /// </summary>
    public interface WindowContent
    {
        /// <summary>
        /// A short description of the type of this window.
        /// </summary>
        string ComponentType { get;}

        /// <summary>
        /// A short description of this content.
        /// </summary>
        string ShortDescription { get;}

        /// <summary>
        /// The full description of this content.
        /// </summary>
        string LongDescription { get;}

        /// <summary>
        /// A list of properties of this content.
        /// </summary>
        Dictionary<String, String> PropertyList { get;}
    }
}
