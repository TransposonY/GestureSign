/*
 * ManagedWinapi - A collection of .NET components that wrap PInvoke calls to 
 * access native API by managed code. http://mwinapi.sourceforge.net/
 * Copyright (C) 2006, 2007 Michael Schierl
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
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;
using System.ComponentModel;

namespace ManagedWinapi
{
    /// <summary>
    /// Provides methods for getting additional information about
    /// files, like icons or compressed file size.
    /// </summary>
    public sealed class ExtendedFileInfo
    {
        /// <summary>
        /// Get the icon used for folders.
        /// </summary>
        /// <param name="small">Whether to get the small icon instead of the large one</param>
        public static Icon GetFolderIcon(bool small)
        {
            return GetIconForFilename(Environment.GetFolderPath(Environment.SpecialFolder.System), small);
        }

        /// <summary>
        /// Get the icon used for files that do not have their own icon
        /// </summary>
        /// <param name="small">Whether to get the small icon instead of the large one</param>
        public static Icon GetFileIcon(bool small)
        {
            return GetExtensionIcon("", small);
        }

        /// <summary>
        /// Get the icon used by files of a given extension.
        /// </summary>
        /// <param name="extension">The extension without leading dot</param>
        /// <param name="small">Whether to get the small icon instead of the large one</param>
        public static Icon GetExtensionIcon(string extension, bool small)
        {
            string tmp = Path.GetTempFileName();
            File.Delete(tmp);
            string fn = tmp + "." + extension;
            try
            {
                File.Create(fn).Close();
                return GetIconForFilename(fn, small);
            }
            finally
            {
                File.Delete(fn);
            }
        }

        /// <summary>
        /// Get the icon used for a given, existing file.
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <param name="small">Whether to get the small icon instead of the large one</param>
        public static Icon GetIconForFilename(string fileName, bool small)
        {
            SHFILEINFO shinfo = new SHFILEINFO();

            if (small)
            {
                IntPtr hImgSmall = SHGetFileInfo(fileName, 0, ref shinfo,
                                   (uint)Marshal.SizeOf(shinfo),
                                    SHGFI_ICON |
                                    SHGFI_SMALLICON);
            }
            else
            {
                IntPtr hImgLarge = SHGetFileInfo(fileName, 0,
                ref shinfo, (uint)Marshal.SizeOf(shinfo),
                SHGFI_ICON | SHGFI_LARGEICON);
            }

            if (shinfo.hIcon == IntPtr.Zero) return null;

            System.Drawing.Icon myIcon =
                   System.Drawing.Icon.FromHandle(shinfo.hIcon);
            return myIcon;
        }

        /// <summary>
        /// Get the size a file requires on disk. This takes NTFS
        /// compression into account.
        /// </summary>
        public static ulong GetPhysicalFileSize(string filename)
        {
            uint high;
            uint low;
            low = GetCompressedFileSize(filename, out high);
            int error = Marshal.GetLastWin32Error();
            if (error == 32)
            {
                return (ulong)new FileInfo(filename).Length;
            }
            if (high == 0 && low == 0xFFFFFFFF && error != 0)
                throw new Win32Exception(error);
            else
                return ((ulong)high << 32) + low;
        }

        /// <summary>
        /// Get the cluster size for the filesystem that contains the given file.
        /// </summary>
        public static uint GetClusterSize(string filename)
        {
            uint sectors, bytes, dummy;
            string drive = Path.GetPathRoot(filename);
            if (!GetDiskFreeSpace(drive, out sectors, out bytes,
                    out dummy, out dummy))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            return sectors * bytes;
        }

        #region PInvoke Declarations

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;    // 'Large icon
        private const uint SHGFI_SMALLICON = 0x1;    // 'Small icon

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool GetDiskFreeSpace(string lpRootPathName,
           out uint lpSectorsPerCluster,
           out uint lpBytesPerSector,
           out uint lpNumberOfFreeClusters,
           out uint lpTotalNumberOfClusters);

        [DllImport("kernel32.dll")]
        private static extern uint GetCompressedFileSize(string lpFileName,
           out uint lpFileSizeHigh);

        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath,
                                    uint dwFileAttributes,
                                    ref SHFILEINFO psfi,
                                    uint cbSizeFileInfo,
                                    uint uFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };
        #endregion
    }
}