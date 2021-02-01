/*
   HidLibrary.Net   http://sourceforge.net/projects/hidlibrary/
 
   Copyright (C)    Roman Reichel 2006
					Bauhaus University of Weimar

   This library is free software; you can redistribute it and/or
   modify it under the terms of the GNU Lesser General Public
   License as published by the Free Software Foundation; either
   version 2.1 of the License, or (at your option) any later version.

   This library is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
   Lesser General Public License for more details.

   You should have received a copy of the GNU Lesser General Public
   License along with this library; if not, see http://www.gnu.org/licenses .

*/

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Text;

namespace GestureSign.Daemon.Native
{
    // from hidpi.h
    // Typedef enum defines a set of integer constants for HidP_Report_Typ

    public sealed partial class HidNativeApi
    {

        // API Declarations for communicating with HID-class devices.

        // ******************************************************************************
        // API error codes
        // ******************************************************************************

        // from hidpi.h
        public const int HIDP_STATUS_SUCCESS = (0x0 << 28) | (0x11 << 16) | 0;
        public const int HIDP_STATUS_NULL = (0x8 << 28) | (0x11 << 16) | 1;
        public const int HIDP_STATUS_INVALID_PREPARSED_DATA = (0xC << 28) | (0x11 << 16) | 1;
        public const int HIDP_STATUS_INVALID_REPORT_TYPE = (0xC << 28) | (0x11 << 16) | 2;
        public const int HIDP_STATUS_INVALID_REPORT_LENGTH = (0xC << 28) | (0x11 << 16) | 3;
        public const int HIDP_STATUS_USAGE_NOT_FOUND = (0xC << 28) | (0x11 << 16) | 4;
        public const int HIDP_STATUS_VALUE_OUT_OF_RANGE = (0xC << 28) | (0x11 << 16) | 5;
        public const int HIDP_STATUS_BAD_LOG_PHY_VALUES = (0xC << 28) | (0x11 << 16) | 6;
        public const int HIDP_STATUS_BUFFER_TOO_SMALL = (0xC << 28) | (0x11 << 16) | 7;
        public const int HIDP_STATUS_INTERNAL_ERROR = (0xC << 28) | (0x11 << 16) | 8;
        public const int HIDP_STATUS_I8042_TRANS_UNKNOWN = (0xC << 28) | (0x11 << 16) | 9;
        public const int HIDP_STATUS_INCOMPATIBLE_REPORT_ID = (0xC << 28) | (0x11 << 16) | 0xA;
        public const int HIDP_STATUS_NOT_VALUE_ARRAY = (0xC << 28) | (0x11 << 16) | 0xB;
        public const int HIDP_STATUS_IS_VALUE_ARRAY = (0xC << 28) | (0x11 << 16) | 0xC;
        public const int HIDP_STATUS_DATA_INDEX_NOT_FOUND = (0xC << 28) | (0x11 << 16) | 0xD;
        public const int HIDP_STATUS_DATA_INDEX_OUT_OF_RANGE = (0xC << 28) | (0x11 << 16) | 0xE;
        public const int HIDP_STATUS_BUTTON_NOT_PRESSED = (0xC << 28) | (0x11 << 16) | 0xF;
        public const int HIDP_STATUS_REPORT_DOES_NOT_EXIST = (0xC << 28) | (0x11 << 16) | 0x10;
        public const int HIDP_STATUS_NOT_IMPLEMENTED = (0xC << 28) | (0x11 << 16) | 0x20;

        // ******************************************************************************
        // Structures and classes for API calls, listed alphabetically
        // ******************************************************************************

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDD_ATTRIBUTES
        {
            public int Size;
            public ushort VendorID;
            public ushort ProductID;
            public ushort VersionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDP_CAPS
        {
            public short Usage;
            public short UsagePage;
            public short InputReportByteLength;
            public short OutputReportByteLength;
            public short FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public short[] Reserved;
            public short NumberLinkCollectionNodes;
            public short NumberInputButtonCaps;
            public short NumberInputValueCaps;
            public short NumberInputDataIndices;
            public short NumberOutputButtonCaps;
            public short NumberOutputValueCaps;
            public short NumberOutputDataIndices;
            public short NumberFeatureButtonCaps;
            public short NumberFeatureValueCaps;
            public short NumberFeatureDataIndices;

        }

        [StructLayout(LayoutKind.Explicit)]
        public struct HIDP_LINK_COLLECTION_NODE
        {
            [FieldOffset(0)]
            public short LinkUsage;
            [FieldOffset(2)]
            public short LinkUsagePage;
            [FieldOffset(4)]
            public short Parent;
            [FieldOffset(6)]
            public short NumberOfChildren;
            [FieldOffset(8)]
            public short NextSibling;
            [FieldOffset(10)]
            public short FirstChild;

            [FieldOffset(12)]
            public byte CollectionType;

            // the next single bit normally would marshal to boolean, however...no clue
            // [FieldOffset(13), MarshalAs()]
            // public boolean IsAlias;

            [FieldOffset(12)]
            public int Reserved;

            [FieldOffset(16)]
            public IntPtr UserContext;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct USAGE_AND_PAGE
        {
            short Usage;
            short UsagePage;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct HIDP_DATA
        {
            [FieldOffset(0)]
            public short DataIndex;
            [FieldOffset(2)]
            public short Reserved;

            [FieldOffset(4)]
            public int RawValue;
            [FieldOffset(4), MarshalAs(UnmanagedType.U1)]
            public bool On;
        }

        // ******************************************************************************
        // Value and Button Caps structures
        // ******************************************************************************

        [StructLayout(LayoutKind.Sequential)]
        public struct HidP_Range
        {
            public short UsageMin;
            public short UsageMax;
            public short StringMin;
            public short StringMax;
            public short DesignatorMin;
            public short DesignatorMax;
            public short DataIndexMin;
            public short DataIndexMax;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HidP_NotRange
        {
            public short Usage;
            public short Reserved1;
            public short StringIndex;
            public short Reserved2;
            public short DesignatorIndex;
            public short Reserved3;
            public short DataIndex;
            public short Reserved4;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct HidP_Value_Caps
        {
            [FieldOffset(0)]
            public ushort UsagePage;
            [FieldOffset(2)]
            public byte ReportID;
            [FieldOffset(3), MarshalAs(UnmanagedType.U1)]
            public bool IsAlias;
            [FieldOffset(4)]
            public ushort BitField;
            [FieldOffset(6)]
            public ushort LinkCollection;
            [FieldOffset(8)]
            public ushort LinkUsage;
            [FieldOffset(10)]
            public ushort LinkUsagePage;
            [FieldOffset(12), MarshalAs(UnmanagedType.U1)]
            public bool IsRange;
            [FieldOffset(13), MarshalAs(UnmanagedType.U1)]
            public bool IsStringRange;
            [FieldOffset(14), MarshalAs(UnmanagedType.U1)]
            public bool IsDesignatorRange;
            [FieldOffset(15), MarshalAs(UnmanagedType.U1)]
            public bool IsAbsolute;
            [FieldOffset(16), MarshalAs(UnmanagedType.U1)]
            public bool HasNull;
            [FieldOffset(17)]
            public byte Reserved;
            [FieldOffset(18)]
            public short BitSize;
            [FieldOffset(20)]
            public short ReportCount;
            [FieldOffset(22)]
            public ushort Reserved2a;
            [FieldOffset(24)]
            public ushort Reserved2b;
            [FieldOffset(26)]
            public ushort Reserved2c;
            [FieldOffset(28)]
            public ushort Reserved2d;
            [FieldOffset(30)]
            public ushort Reserved2e;
            [FieldOffset(32)]
            public int UnitsExp;
            [FieldOffset(36)]
            public int Units;
            [FieldOffset(40)]
            public int LogicalMin;
            [FieldOffset(44)]
            public int LogicalMax;
            [FieldOffset(48)]
            public int PhysicalMin;
            [FieldOffset(52)]
            public int PhysicalMax;

            [FieldOffset(56)]
            public HidP_Range Range;
            [FieldOffset(56)]
            public HidP_NotRange NotRange;

        }

        [StructLayout(LayoutKind.Explicit)]
        public struct HidP_Button_Caps
        {
            [FieldOffset(0)]
            public short UsagePage;
            [FieldOffset(2)]
            public byte ReportID;
            [FieldOffset(3), MarshalAs(UnmanagedType.U1)]
            public bool IsAlias;
            [FieldOffset(4)]
            public short BitField;
            [FieldOffset(6)]
            public short LinkCollection;
            [FieldOffset(8)]
            public short LinkUsage;
            [FieldOffset(10)]
            public short LinkUsagePage;
            [FieldOffset(12), MarshalAs(UnmanagedType.U1)]
            public bool IsRange;
            [FieldOffset(13), MarshalAs(UnmanagedType.U1)]
            public bool IsStringRange;
            [FieldOffset(14), MarshalAs(UnmanagedType.U1)]
            public bool IsDesignatorRange;
            [FieldOffset(15), MarshalAs(UnmanagedType.U1)]
            public bool IsAbsolute;
            [FieldOffset(16), MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public int[] Reserved;

            [FieldOffset(56)]
            public HidP_Range Range;
            [FieldOffset(56)]
            public HidP_NotRange NotRange;

        }

        // ******************************************************************************
        // API functions
        // ******************************************************************************

        #region HIDD Functions - hidsdi.h

        /// <summary>
        /// Flush the input queue for the given HID device.
        /// </summary>
        /// <param name="HidDeviceObject">A handle to a Hid Device that the client obtains using 
        ///           a call to CreateFile on a valid Hid device string name.
        ///           The string name can be obtained using standard PnP calls.</param>
        /// <returns>
        /// TRUE if successful
        /// FALSE otherwise  -- Use GetLastError() to get extended error information
        /// </returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern bool HidD_FlushQueue(SafeFileHandle HidDeviceObject);

        [DllImport("hid.dll", SetLastError = true)]
        static public extern bool HidD_FreePreparsedData(ref IntPtr PreparsedData);

        /// <summary>
        /// Fill in the given HIDD_ATTRIBUTES structure with the attributes of the
        /// given hid device.
        /// </summary>
        /// <param name="HidDeviceObject"></param>
        /// <param name="Attributes"></param>
        /// <returns></returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidD_GetAttributes(SafeFileHandle HidDeviceObject, ref HIDD_ATTRIBUTES Attributes);

        /// <summary>
        /// Retrieve a feature report from a HID device.
        /// </summary>
        /// <param name="HidDeviceObject">A handle to a Hid Device Object.</param>
        /// <param name="ReportBuffer">The buffer that the feature report should be placed 
        ///                 into.  The first byte of the buffer should be set to
        ///                 the report ID of the desired report</param>
        /// <param name="ReportBufferLength">The size (in bytes) of ReportBuffer.  This value 
        ///                 should be greater than or equal to the 
        ///                 FeatureReportByteLength field as specified in the 
        ///                 HIDP_CAPS structure for the device</param>
        /// <returns>
        /// TRUE if successful
        /// FALSE otherwise  -- Use GetLastError() to get extended error information
        /// </returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern bool HidD_GetFeature(SafeFileHandle HidDeviceObject, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] ReportBuffer, int ReportBufferLength);

        /// <summary>
        /// Retrieve an input report from a HID device.
        /// </summary>
        /// <param name="HidDeviceObject">A handle to a Hid Device Object.</param>
        /// <param name="ReportBuffer">The buffer that the input report should be placed 
        ///                 into.  The first byte of the buffer should be set to
        ///                 the report ID of the desired report</param>
        /// <param name="ReportBufferLength">The size (in bytes) of ReportBuffer.  This value 
        ///                 should be greater than or equal to the 
        ///                 InputReportByteLength field as specified in the 
        ///                 HIDP_CAPS structure for the device</param>
        /// <returns>
        /// TRUE if successful
        /// FALSE otherwise  -- Use GetLastError() to get extended error information
        /// </returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern bool HidD_GetInputReport(SafeFileHandle HidDeviceObject, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] ReportBuffer, int ReportBufferLength);

        /// <summary>
        /// Get GUID for Hid Devices
        /// </summary>
        /// <param name="HidGuid"></param>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern void HidD_GetHidGuid(ref System.Guid HidGuid);

        /// <summary>
        /// This function returns the number of input buffers used by the specified
        /// file handle to the Hid device.  Each file object has a number of buffers
        /// associated with it to queue reports read from the device but which have
        /// not yet been read by the user-mode app with a handle to that device.
        /// </summary>
        /// <param name="HidDeviceObject">A handle to a Hid Device Object.</param>
        /// <param name="NumberBuffers">Number of buffers currently being used for this file
        ///                 handle to the Hid device</param>
        /// <returns>
        /// TRUE if successful
        /// FALSE otherwise  -- Use GetLastError() to get extended error information
        /// </returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern bool HidD_GetNumInputBuffers(SafeFileHandle HidDeviceObject, ref int NumberBuffers);

        /// <summary>
        /// Given a handle to a valid Hid Class Device Object, retrieve the preparsed
        /// data for the device.  This routine will allocate the appropriately 
        /// sized buffer to hold this preparsed data.  It is up to client to call
        /// HidP_FreePreparsedData to free the memory allocated to this structure when
        /// it is no longer needed.
        /// </summary>
        /// <param name="HidDeviceObject">A handle to a Hid Device that the client obtains using 
        ///           a call to CreateFile on a valid Hid device string name.
        ///           The string name can be obtained using standard PnP calls.</param>
        /// <param name="PreparsedData">An opaque data structure used by other functions in this 
        ///           library to retrieve information about a given device.</param>
        /// <returns>TRUE if successful.
        /// FALSE otherwise  -- Use GetLastError() to get extended error information</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern bool HidD_GetPreparsedData(SafeFileHandle HidDeviceObject, ref IntPtr PreparsedData);

        /// <summary>
        /// Send a feature report to a HID device.
        /// </summary>
        /// <param name="HidDeviceObject">A handle to a Hid Device Object.</param>
        /// <param name="ReportBuffer">The buffer of the feature report to send to the device</param>
        /// <param name="ReportBufferLength">The size (in bytes) of ReportBuffer.  This value 
        ///                 should be greater than or equal to the 
        ///                 FeatureReportByteLength field as specified in the 
        ///                 HIDP_CAPS structure for the device</param>
        /// <returns>
        /// TRUE if successful
        /// FALSE otherwise  -- Use GetLastError() to get extended error information
        /// </returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern bool HidD_SetFeature(SafeFileHandle HidDeviceObject, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] ReportBuffer, int ReportBufferLength);

        /// <summary>
        /// This function sets the number of input buffers used by the specified
        /// file handle to the Hid device.  Each file object has a number of buffers
        /// associated with it to queue reports read from the device but which have
        /// not yet been read by the user-mode app with a handle to that device.
        /// </summary>
        /// <param name="HidDeviceObject">A handle to a Hid Device Object.</param>
        /// <param name="NumberBuffers">New number of buffers to use for this file handle to
        ///                 the Hid device</param>
        /// <returns>
        /// TRUE if successful
        /// FALSE otherwise  -- Use GetLastError() to get extended error information
        /// </returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern bool HidD_SetNumInputBuffers(SafeFileHandle HidDeviceObject, int NumberBuffers);

        /// <summary>
        /// Send an output report to a HID device.
        /// </summary>
        /// <param name="HidDeviceObject">A handle to a Hid Device Object.</param>
        /// <param name="ReportBuffer">The buffer of the output report to send to the device</param>
        /// <param name="ReportBufferLength">The size (in bytes) of ReportBuffer.  This value 
        ///                 should be greater than or equal to the 
        ///                 OutputReportByteLength field as specified in the 
        ///                 HIDP_CAPS structure for the device</param>
        /// <returns>
        /// TRUE if successful
        /// FALSE otherwise  -- Use GetLastError() to get extended error information
        /// </returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern bool HidD_SetOutputReport(SafeFileHandle HidDeviceObject, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] ReportBuffer, int ReportBufferLength);

        /// <summary>
        /// This function retrieves the raw physical descriptor for the specified Hid device.
        /// </summary>
        /// <param name="HidDeviceObject">A handle to a Hid Device Object.</param>
        /// <param name="Buffer">Buffer which on return will contain the physical
        ///                 descriptor if one exists for the specified device
        ///                 handle</param>
        /// <param name="BufferLength">Length of buffer (in bytes)</param>
        /// <returns>
        /// TRUE if successful
        /// FALSE otherwise  -- Use GetLastError() to get extended error information
        /// </returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern bool HidD_GetPhysicalDescriptor(SafeFileHandle HidDeviceObject, [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 2)] StringBuilder Buffer, int BufferLength);

        /// <summary>
        /// This function retrieves a string from the specified Hid device that is specified with a certain string index.
        /// </summary>
        /// <param name="HidDeviceObject">A handle to a Hid Device Object.</param>
        /// <param name="StringIndex">Index of the string to retrieve</param>
        /// <param name="Buffer">Buffer which on return will contain the product
        ///                 string returned from the device.  This string is a 
        ///                 wide-character string</param>
        /// <param name="BufferLength">Length of Buffer (in bytes)</param>
        /// <returns>
        /// TRUE if successful
        /// FALSE otherwise  -- Use GetLastError() to get extended error information
        /// </returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern bool HidD_GetIndexedString(SafeFileHandle HidDeviceObject, int StringIndex, [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 3)] StringBuilder Buffer, int BufferLength);

        /// <summary>
        /// This function retrieves the product string from the specified Hid device.
        /// </summary>
        /// <param name="HidDeviceObject">A handle to a Hid Device Object.</param>
        /// <param name="Buffer">Buffer which on return will contain the product
        ///                 string returned from the device.  This string is a 
        ///                 wide-character string</param>
        /// <param name="BufferLength">Length of Buffer (in bytes)</param>
        /// <returns>
        /// TRUE if successful
        /// FALSE otherwise  -- Use GetLastError() to get extended error information
        /// </returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern bool HidD_GetProductString(SafeFileHandle HidDeviceObject, [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 2)] StringBuilder Buffer, int BufferLength);

        /// <summary>
        /// This function retrieves the serial number string from the specified Hid device.
        /// </summary>
        /// <param name="HidDeviceObject">A handle to a Hid Device Object.</param>
        /// <param name="Buffer">Buffer which on return will contain the serial number
        ///                 string returned from the device.  This string is a 
        ///                 wide-character string</param>
        /// <param name="BufferLength">Length of Buffer (in bytes)</param>
        /// <returns>
        /// TRUE if successful
        /// FALSE otherwise  -- Use GetLastError() to get extended error information
        /// </returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern bool HidD_GetSerialNumberString(SafeFileHandle HidDeviceObject, [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 2)] StringBuilder Buffer, int BufferLength);

        /// <summary>
        /// This function retrieves the manufacturer string from the specified 
        /// Hid device.
        /// </summary>
        /// <param name="HidDeviceObject">A handle to a Hid Device Object.</param>
        /// <param name="Buffer">Buffer which on return will contain the manufacturer
        ///                 string returned from the device.  This string is a 
        ///                 wide-character string</param>
        /// <param name="BufferLength">Length of Buffer (in bytes)</param>
        /// <returns>
        /// TRUE if successful
        /// FALSE otherwise  -- Use GetLastError() to get extended error information
        /// </returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern bool HidD_GetManufacturerString(SafeFileHandle HidDeviceObject, [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 2)] StringBuilder Buffer, int BufferLength);

        #endregion

        #region HIDP Functions - hidpi.h

        /// <summary>
        /// Returns a list of capabilities of a given hid device as described by its preparsed data. 
        /// </summary>
        /// <param name="PreparsedData">The preparsed data returned from HIDCLASS.</param>
        /// <param name="Capabilities">a HIDP_CAPS structure</param>
        /// <returns>HIDP_STATUS_SUCCESS, HIDP_STATUS_INVALID_PREPARSED_DATA</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_GetCaps(IntPtr PreparsedData, ref HIDP_CAPS Capabilities);

        /// <summary>
        /// Return a list of HIDP_LINK_COLLECTION_NODEs used to describe the link collection tree of this hid device.
        /// </summary>
        /// <param name="LinkCollectionNodes">a caller allocated array into which HidP_GetLinkCollectionNodes will store the information</param>
        /// <param name="LinkCollectionNodesLength">the caller sets this value to the length of the
        ///         the array in terms of number of elements.
        ///         HidP_GetLinkCollectionNodes sets this value to the actual
        ///         number of elements set. The total number of nodes required to
        ///         describe this HID device can be found in the
        ///         NumberLinkCollectionNodes field in the HIDP_CAPS structure.</param>
        /// <param name="PreparsedData"></param>
        /// <returns></returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_GetLinkCollectionNodes([Out] HIDP_LINK_COLLECTION_NODE[] LinkCollectionNodes, ref int LinkCollectionNodesLength, IntPtr PreparsedData);

        /// <summary>
        /// HidP_GetValueCaps returns all the values (non-binary) that are a part of the given report type for the Hid device represented by the given preparsed data.
        /// </summary>
        /// <param name="ReportType">One of HidP_Input, HidP_Output, or HidP_Feature.</param>
        /// <param name="ValueCaps">A _HIDP_VALUE_CAPS array containing information about all the non-binary values in the given report.  This buffer is provided by the caller.</param>
        /// <param name="ValueCapsLength">As input, this parameter specifies the length of the ValueCaps
        ///          parameter (array) in number of array elements.  As output,
        ///          this value is set to indicate how many of those array elements
        ///          were filled in by the function.  The maximum number of
        ///          value caps that can be returned is found in the HIDP_CAPS
        ///          structure.  If HIDP_STATUS_BUFFER_TOO_SMALL is returned,
        ///          this value contains the number of array elements needed to
        ///          successfully complete the request.</param>
        /// <param name="PreparsedData">The preparsed data returned from HIDCLASS.</param>
        /// <returns>HIDP_STATUS_SUCCESS, HIDP_STATUS_INVALID_REPORT_TYPE, HIDP_STATUS_INVALID_PREPARSED_DATA, HIDP_STATUS_BUFFER_TOO_SMALL (all given entries however have been filled in), HIDP_STATUS_USAGE_NOT_FOUND</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_GetValueCaps([MarshalAs(UnmanagedType.U2)]HidReportType ReportType, [MarshalAs(UnmanagedType.LPArray)] HidP_Value_Caps[] ValueCaps, ref short ValueCapsLength, IntPtr PreparsedData);

        /// <summary>
        /// HidP_GetValueCaps returns all the values (non-binary) that are a part of the given report type for the Hid device represented by the given preparsed data.
        /// </summary>
        /// <param name="ReportType">One of HidP_Input, HidP_Output, or HidP_Feature.</param>
        /// <param name="UsagePage">A usage page value used to limit the value caps returned to
        ///        those on a given usage page.  If set to 0, this parameter is
        ///        ignored.  Can be used with LinkCollection and Usage parameters
        ///        to further limit the number of value caps structures returned.</param>
        /// <param name="LinkCollection">HIDP_LINK_COLLECTION node array index used to limit the
        ///          value caps returned to those buttons in a given link
        ///          collection.  If set to 0, this parameter is
        ///          ignored.  Can be used with UsagePage and Usage parameters
        ///          to further limit the number of value caps structures
        ///          returned.</param>
        /// <param name="Usage">A usage value used to limit the value caps returned to those
        ///       with the specified usage value.  If set to 0, this parameter
        ///       is ignored.  Can be used with LinkCollection and UsagePage
        ///       parameters to further limit the number of value caps
        ///       structures returned.</param>
        /// <param name="ValueCaps">A _HIDP_VALUE_CAPS array containing information about all the
        ///       non-binary values in the given report.  This buffer is provided
        ///       by the caller.</param>
        /// <param name="ValueCapsLength">As input, this parameter specifies the length of the ValueCaps
        ///          parameter (array) in number of array elements.  As output,
        ///          this value is set to indicate how many of those array elements
        ///          were filled in by the function.  The maximum number of
        ///          value caps that can be returned is found in the HIDP_CAPS
        ///          structure.  If HIDP_STATUS_BUFFER_TOO_SMALL is returned,
        ///          this value contains the number of array elements needed to
        ///          successfully complete the request.</param>
        /// <param name="PreparsedData">The preparsed data returned from HIDCLASS.</param>
        /// <returns>HIDP_STATUS_SUCCESS, HIDP_STATUS_INVALID_REPORT_TYPE, HIDP_STATUS_INVALID_PREPARSED_DATA, HIDP_STATUS_BUFFER_TOO_SMALL (all given entries however have been filled in), HIDP_STATUS_USAGE_NOT_FOUND</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_GetSpecificValueCaps(HidReportType ReportType, ushort UsagePage, ushort LinkCollection, ushort Usage, [Out] HidP_Value_Caps[] ValueCaps, ref short ValueCapsLength, IntPtr PreparsedData);

        /// <summary>
        /// HidP_GetButtonCaps returns all the buttons (binary values) that are a part of the given report type for the Hid device represented by the given preparsed data.
        /// </summary>
        /// <param name="ReportType">One of HidP_Input, HidP_Output, or HidP_Feature.</param>
        /// <param name="ButtonCaps">A _HIDP_BUTTON_CAPS array containing information about all the
        ///       binary values in the given report.  This buffer is provided by
        ///       the caller.</param>
        /// <param name="ButtonCapsLength">As input, this parameter specifies the length of the
        ///          ButtonCaps parameter (array) in number of array elements.
        ///          As output, this value is set to indicate how many of those
        ///          array elements were filled in by the function.  The maximum number of
        ///          button caps that can be returned is found in the HIDP_CAPS
        ///          structure.  If HIDP_STATUS_BUFFER_TOO_SMALL is returned,
        ///          this value contains the number of array elements needed to
        ///          successfully complete the request.</param>
        /// <param name="PreparsedData">The preparsed data returned from HIDCLASS.</param>
        /// <returns>HIDP_STATUS_SUCCESS, HIDP_STATUS_INVALID_REPORT_TYPE, HIDP_STATUS_INVALID_PREPARSED_DATA, HIDP_STATUS_BUFFER_TOO_SMALL (all given entries however have been filled in), HIDP_STATUS_USAGE_NOT_FOUND</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_GetButtonCaps([MarshalAs(UnmanagedType.U2)]HidReportType ReportType, [In, Out] HidP_Button_Caps[] ButtonCaps, ref short ButtonCapsLength, IntPtr PreparsedData);

        /// <summary>
        /// HidP_GetButtonCaps returns all the buttons (binary values) that are a part of the given report type for the Hid device represented by the given preparsed data.
        /// </summary>
        /// <param name="ReportType">One of HidP_Input, HidP_Output, or HidP_Feature.</param>
        /// <param name="UsagePage">A usage page value used to limit the button caps returned to
        ///        those on a given usage page.  If set to 0, this parameter is
        ///        ignored.  Can be used with LinkCollection and Usage parameters
        ///        to further limit the number of button caps structures returned.</param>
        /// <param name="LinkCollection">HIDP_LINK_COLLECTION node array index used to limit the
        ///          button caps returned to those buttons in a given link
        ///          collection.  If set to 0, this parameter is
        ///          ignored.  Can be used with UsagePage and Usage parameters
        ///          to further limit the number of button caps structures
        ///          returned.</param>
        /// <param name="Usage">A usage value used to limit the button caps returned to those
        ///       with the specified usage value.  If set to 0, this parameter
        ///       is ignored.  Can be used with LinkCollection and UsagePage
        ///       parameters to further limit the number of button caps
        ///       structures returned.</param>
        /// <param name="ButtonCaps">A _HIDP_BUTTON_CAPS array containing information about all the
        ///       binary values in the given report.  This buffer is provided by
        ///       the caller.</param>
        /// <param name="ButtonCapsLength">As input, this parameter specifies the length of the
        ///          ButtonCaps parameter (array) in number of array elements.
        ///          As output, this value is set to indicate how many of those
        ///          array elements were filled in by the function.  The maximum number of
        ///          button caps that can be returned is found in the HIDP_CAPS
        ///          structure.  If HIDP_STATUS_BUFFER_TOO_SMALL is returned,
        ///          this value contains the number of array elements needed to
        ///          successfully complete the request.</param>
        /// <param name="PreparsedData"></param>
        /// <returns>HIDP_STATUS_SUCCESS, HIDP_STATUS_INVALID_REPORT_TYPE, HIDP_STATUS_INVALID_PREPARSED_DATA, HIDP_STATUS_BUFFER_TOO_SMALL (all given entries however have been filled in), HIDP_STATUS_USAGE_NOT_FOUND</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_GetSpecificButtonCaps([MarshalAs(UnmanagedType.U2)]HidReportType ReportType, short UsagePage, short LinkCollection, short Usage, [In, Out] HidP_Button_Caps[] ButtonCaps, ref short ButtonCapsLength, IntPtr PreparsedData);

        /// <summary>
        /// Initialize a report based on the given report ID.
        /// </summary>
        /// <param name="ReportType">One of HidP_Input, HidP_Output, or HidP_Feature.</param>
        /// <param name="ReportID"></param>
        /// <param name="PreparsedData">Preparsed data structure returned by HIDCLASS</param>
        /// <param name="Report">Buffer which to set the data into.</param>
        /// <param name="ReportLength">Length of Report...Report should be at least as long as the
        ///        value indicated in the HIDP_CAPS structure for the device and
        ///        the corresponding ReportType</param>
        /// <returns>HIDP_STATUS_INVALID_REPORT_TYPE, HIDP_STATUS_INVALID_PREPARSED_DATA, HIDP_STATUS_INVALID_REPORT_LENGTH, HIDP_STATUS_REPORT_DOES_NOT_EXIST</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_InitializeReportForID([MarshalAs(UnmanagedType.U2)]HidReportType ReportType, byte ReportID, IntPtr PreparsedData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] Report, int ReportLength);

        /// <summary>
        /// Note: Since usage value arrays deal with multiple fields for
        ///         for one usage value, they cannot be used with HidP_SetData
        ///         and HidP_GetData.  In this case,
        ///         HIDP_STATUS_IS_USAGE_VALUE_ARRAY will be returned.
        /// </summary>
        /// <param name="ReportType">One of HidP_Input, HidP_Output, or HidP_Feature.</param>
        /// <param name="DataList">Array of HIDP_DATA structures that contains the data values
        ///        that are to be set into the given report</param>
        /// <param name="DataLength">As input, length in array elements of DataList.  As output,
        ///        contains the number of data elements set on successful
        ///        completion or an index into the DataList array to identify
        ///        the faulting HIDP_DATA value if an error code is returned.</param>
        /// <param name="PreparsedData">Preparsed data structure returned by HIDCLASS</param>
        /// <param name="Report">Buffer which to set the data into.</param>
        /// <param name="ReportLength">Length of Report...Report should be at least as long as the
        ///        value indicated in the HIDP_CAPS structure for the device and
        ///        the corresponding ReportType</param>
        /// <returns>HIDP_STATUS_SUCCESS, HIDP_STATUS_INVALID_REPORT_TYPE, HIDP_STATUS_INVALID_PREPARSED_DATA, 
        /// HIDP_STATUS_DATA_INDEX_NOT_FOUND, HIDP_STATUS_INVALID_REPORT_LENGTH, HIDP_STATUS_REPORT_DOES_NOT_EXIST,
        /// HIDP_STATUS_IS_USAGE_VALUE_ARRAY, HIDP_STATUS_BUTTON_NOT_PRESSED, HIDP_STATUS_INCOMPATIBLE_REPORT_ID, HIDP_STATUS_BUFFER_TOO_SMALL</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_SetData([MarshalAs(UnmanagedType.U2)]HidReportType ReportType, [In, Out] HIDP_DATA[] DataList, ref int DataLength, IntPtr PreparsedData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] Report, int ReportLength);

        /// <summary>
        /// Note: For obvious reasons HidP_SetData and HidP_GetData will not access UsageValueArrays.
        /// </summary>
        /// <param name="ReportType">One of HidP_Input, HidP_Output, or HidP_Feature.</param>
        /// <param name="DataList">Array of HIDP_DATA structures that will receive the data values that are set in the given report</param>
        /// <param name="DataLength">As input, length in array elements of DataList.  As output,
        ///        contains the number of data elements that were successfully
        ///        set by HidP_GetData.  The maximum size necessary for DataList
        ///        can be determined by calling HidP_MaxDataListLength</param>
        /// <param name="PreparsedData">Preparsed data structure returned by HIDCLASS</param>
        /// <param name="Report">Buffer which to set the data into.</param>
        /// <param name="ReportLength">Length of Report...Report should be at least as long as the
        ///        value indicated in the HIDP_CAPS structure for the device and
        ///        the corresponding ReportType</param>
        /// <returns>HIDP_STATUS_SUCCESS, HIDP_STATUS_INVALID_REPORT_TYPE, HIDP_STATUS_INVALID_PREPARSED_DATA, 
        /// HIDP_STATUS_INVALID_REPORT_LENGTH, HIDP_STATUS_REPORT_DOES_NOT_EXIST, HIDP_STATUS_BUFFER_TOO_SMALL</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_GetData([MarshalAs(UnmanagedType.U2)]HidReportType ReportType, [In, Out] HIDP_DATA[] DataList, ref int DataLength, IntPtr PreparsedData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] Report, int ReportLength);

        /// <summary>
        /// This function returns the maximum length of HIDP_DATA elements that HidP_GetData could return for the given report type.
        /// </summary>
        /// <param name="ReportType">One of HidP_Input, HidP_Output or HidP_Feature.</param>
        /// <param name="PreparsedData">Preparsed data structure returned by HIDCLASS</param>
        /// <returns>The length of the data list array required for the HidP_GetData function
        /// call.  If an error occurs (either HIDP_STATUS_INVALID_REPORT_TYPE or
        /// HIDP_STATUS_INVALID_PREPARSED_DATA), this function returns 0.</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_MaxDataListLength([MarshalAs(UnmanagedType.U2)]HidReportType ReportType, IntPtr PreparsedData);

        /// <summary>
        /// This function sets binary values (buttons) in a report.  Given an
        /// initialized packet of correct length, it modifies the report packet so that
        /// each element in the given list of usages has been set in the report packet.
        /// For example, in an output report with 5 LED�s, each with a given usage,
        /// an application could turn on any subset of these lights by placing their
        /// usages in any order into the usage array (UsageList).  HidP_SetUsages would,
        /// in turn, set the appropriate bit or add the corresponding byte into the
        /// HID Main Array Item.
        ///
        /// A properly initialized Report packet is one of the correct byte length,
        /// and all zeros.
        ///
        /// NOTE: A packet that has already been set with a call to a HidP_Set routine
        ///  can also be passed in.  This routine then sets processes the UsageList
        ///  in the same fashion but verifies that the ReportID already set in
        ///  Report matches the report ID for the given usages.
        /// </summary>
        /// <param name="ReportType">One of HidP_Input, HidP_Output or HidP_Feature.</param>
        /// <param name="UsagePage">All of the usages in the usage array, which HidP_SetUsages will
        ///        set in the report, refer to this same usage page.
        ///        If a client wishes to set usages in a report for multiple
        ///        usage pages then that client needs to make multiple calls to
        ///        HidP_SetUsages for each of the usage pages.</param>
        /// <param name="LinkCollection"></param>
        /// <param name="UsageList">A usage array containing the usages that HidP_SetUsages will set in
        ///        the report packet.</param>
        /// <param name="UsageLength">The length of the given usage array in array elements.
        ///        The parser will set this value to the position in the usage
        ///        array where it stopped processing.  If successful, UsageLength
        ///        will be unchanged.  In any error condition, this parameter
        ///        reflects how many of the usages in the usage list have
        ///        actually been set by the parser.  This is useful for finding
        ///        the usage in the list which caused the error.</param>
        /// <param name="PreparsedData">The preparsed data recevied from HIDCLASS</param>
        /// <param name="Report">The report packet.</param>
        /// <param name="ReportLength">Length of the given report packet...Must be equal to the
        ///           value reported in the HIDP_CAPS structure for the device
        ///           and corresponding report type.</param>
        /// <returns>HIDP_STATUS_SUCCESS, HIDP_STATUS_INVALID_REPORT_TYPE, HIDP_STATUS_INVALID_PREPARSED_DATA,
        /// HIDP_STATUS_INVALID_REPORT_LENGTH, HIDP_STATUS_REPORT_DOES_NOT_EXIST,HIDP_STATUS_INCOMPATIBLE_REPORT_ID,
        /// HIDP_STATUS_USAGE_NOT_FOUND, HIDP_STATUS_BUFFER_TOO_SMALL</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_SetUsages([MarshalAs(UnmanagedType.U2)]HidReportType ReportType, short UsagePage, short LinkCollection, [In, Out] HIDP_DATA[] UsageList, ref int UsageLength, IntPtr PreparsedData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 7)] byte[] Report, int ReportLength);

        /// <summary>
        /// This function unsets (turns off) binary values (buttons) in the report.  Given
        /// an initialized packet of correct length, it modifies the report packet so
        /// that each element in the given list of usages has been unset in the
        /// report packet.
        /// 
        /// This function is the "undo" operation for SetUsages.  If the given usage
        /// is not already set in the Report, it will return an error code of
        /// HIDP_STATUS_BUTTON_NOT_PRESSED.  If the button is pressed, HidP_UnsetUsages
        /// will unset the appropriate bit or remove the corresponding index value from
        /// the HID Main Array Item.
        ///
        /// A properly initialized Report packet is one of the correct byte length,
        /// and all zeros..
        /// 
        /// NOTE: A packet that has already been set with a call to a HidP_Set routine
        ///  can also be passed in.  This routine then processes the UsageList
        ///  in the same fashion but verifies that the ReportID already set in
        ///  Report matches the report ID for the given usages.
        /// </summary>
        /// <param name="ReportType">One of HidP_Input, HidP_Output or HidP_Feature.</param>
        /// <param name="UsagePage">All of the usages in the usage array, which HidP_UnsetUsages will
        ///        unset in the report, refer to this same usage page.
        ///        If a client wishes to unset usages in a report for multiple
        ///        usage pages then that client needs to make multiple calls to
        ///        HidP_UnsetUsages for each of the usage pages.</param>
        /// <param name="LinkCollection"></param>
        /// <param name="UsageList">A usage array containing the usages that HidP_UnsetUsages will
        ///        unset in the report packet.</param>
        /// <param name="UsageLength">The length of the given usage array in array elements.
        ///        The parser will set this value to the position in the usage
        ///        array where it stopped processing.  If successful, UsageLength
        ///        will be unchanged.  In any error condition, this parameter
        ///        reflects how many of the usages in the usage list have
        ///        actually been unset by the parser.  This is useful for finding
        ///        the usage in the list which caused the error.</param>
        /// <param name="PreparsedData">The preparsed data recevied from HIDCLASS</param>
        /// <param name="Report">The report packet.</param>
        /// <param name="ReportLength">Length of the given report packet...Must be equal to the
        ///           value reported in the HIDP_CAPS structure for the device
        ///           and corresponding report type.</param>
        /// <returns>HIDP_STATUS_SUCCESS, HIDP_STATUS_INVALID_REPORT_TYPE, HIDP_STATUS_INVALID_PREPARSED_DATA,
        /// HIDP_STATUS_INVALID_REPORT_LENGTH, HIDP_STATUS_REPORT_DOES_NOT_EXIST, HIDP_STATUS_INCOMPATIBLE_REPORT_ID,
        /// HIDP_STATUS_USAGE_NOT_FOUND, HIDP_STATUS_BUTTON_NOT_PRESSED</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_UnsetUsages([MarshalAs(UnmanagedType.U2)]HidReportType ReportType, short UsagePage, short LinkCollection, [In, Out] HIDP_DATA[] UsageList, ref int UsageLength, IntPtr PreparsedData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 7)] byte[] Report, int ReportLength);

        /// <summary>
        /// This function returns the binary values (buttons) that are set in a HID
        /// report.  Given a report packet of correct length, it searches the report
        /// packet for each usage for the given usage page and returns them in the
        /// usage list.
        /// </summary>
        /// <param name="ReportType">One of HidP_Input, HidP_Output or HidP_Feature.</param>
        /// <param name="UsagePage">All of the usages in the usage list, which HidP_GetUsages will
        ///       retrieve in the report, refer to this same usage page.
        ///       If the client wishes to get usages in a packet for multiple
        ///       usage pages then that client needs to make multiple calls
        ///       to HidP_GetUsages.</param>
        /// <param name="LinkCollection">An optional value which can limit which usages are returned
        ///            in the UsageList to those usages that exist in a specific
        ///            LinkCollection.  A non-zero value indicates the index into
        ///            the HIDP_LINK_COLLECITON_NODE list returned by
        ///            HidP_GetLinkCollectionNodes of the link collection the
        ///            usage should belong to.  A value of 0 indicates this
        ///            should value be ignored.</param>
        /// <param name="UsageList">The usage array that will contain all the usages found in
        ///       the report packet.</param>
        /// <param name="UsageLength">The length of the given usage array in array elements.
        ///        On input, this value describes the length of the usage list.
        ///        On output, HidP_GetUsages sets this value to the number of
        ///        usages that was found.  Use HidP_MaxUsageListLength to
        ///        determine the maximum length needed to return all the usages
        ///        that a given report packet may contain.</param>
        /// <param name="PreparsedData">Preparsed data structure returned by HIDCLASS</param>
        /// <param name="Report">The report packet.</param>
        /// <param name="ReportLength">Length (in bytes) of the given report packet</param>
        /// <returns>HIDP_STATUS_SUCCESS, HIDP_STATUS_INVALID_REPORT_TYPE,
        /// HIDP_STATUS_INVALID_PREPARSED_DATA, HIDP_STATUS_INVALID_REPORT_LENGTH,
        /// IDP_STATUS_REPORT_DOES_NOT_EXIST, HIDP_STATUS_BUFFER_TOO_SMALL,
        /// HIDP_STATUS_INCOMPATIBLE_REPORT_ID, HIDP_STATUS_USAGE_NOT_FOUND</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_GetUsages(HidReportType ReportType, ushort UsagePage, short LinkCollection, [Out] ushort[] UsageList, ref int UsageLength, IntPtr PreparsedData, IntPtr Report, int ReportLength);

        /// <summary>
        /// This function returns the binary values (buttons) in a HID report.
        /// Given a report packet of correct length, it searches the report packet
        /// for all buttons and returns the UsagePage and Usage for each of the buttons
        /// it finds.
        /// </summary>
        /// <param name="ReportType">One of HidP_Input, HidP_Output or HidP_Feature.</param>
        /// <param name="LinkCollection">An optional value which can limit which usages are returned
        ///            in the ButtonList to those usages that exist in a specific
        ///            LinkCollection.  A non-zero value indicates the index into
        ///            the HIDP_LINK_COLLECITON_NODE list returned by
        ///            HidP_GetLinkCollectionNodes of the link collection the
        ///            usage should belong to.  A value of 0 indicates this
        ///            should value be ignored.</param>
        /// <param name="ButtonList">An array of USAGE_AND_PAGE structures describing all the
        ///        buttons currently ``down'' in the device.</param>
        /// <param name="UsageLength">The length of the given array in terms of elements.
        ///        On input, this value describes the length of the list.  On
        ///        output, HidP_GetUsagesEx sets this value to the number of
        ///        usages that were found.  Use HidP_MaxUsageListLength to
        ///        determine the maximum length needed to return all the usages
        ///        that a given report packet may contain.</param>
        /// <param name="PreparsedData">Preparsed data returned by HIDCLASS</param>
        /// <param name="Report">The report packet.</param>
        /// <param name="ReportLength">Length (in bytes) of the given report packet.</param>
        /// <returns>HIDP_STATUS_SUCCESS, HIDP_STATUS_INVALID_REPORT_TYPE,
        /// HIDP_STATUS_INVALID_PREPARSED_DATA, HIDP_STATUS_INVALID_REPORT_LENGTH,
        /// HIDP_STATUS_REPORT_DOES_NOT_EXIST, HIDP_STATUS_BUFFER_TOO_SMALL,
        /// HIDP_STATUS_INCOMPATIBLE_REPORT_ID, HIDP_STATUS_USAGE_NOT_FOUND</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_GetUsagesEx([MarshalAs(UnmanagedType.U2)]HidReportType ReportType, short LinkCollection, [In, Out] USAGE_AND_PAGE[] ButtonList, ref int UsageLength, IntPtr PreparsedData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 6)] byte[] Report, int ReportLength);

        /// <summary>
        /// This function returns the maximum number of usages that a call to
        /// HidP_GetUsages or HidP_GetUsagesEx could return for a given HID report.
        /// If calling for number of usages returned by HidP_GetUsagesEx, use 0 as
        /// the UsagePage value.
        /// </summary>
        /// <param name="ReportType">One of HidP_Input, HidP_Output or HidP_Feature.</param>
        /// <param name="UsagePage">Specifies the optional UsagePage to query for.  If 0, will
        ///        return all the maximum number of usage values that could be
        ///        returned for a given ReportType.   If non-zero, will return
        ///        the maximum number of usages that would be returned for the
        ///        ReportType with the given UsagePage.</param>
        /// <param name="PreparsedData">Preparsed data returned from HIDCLASS</param>
        /// <returns>The length of the usage list array required for the HidP_GetUsages or
        /// HidP_GetUsagesEx function call.  If an error occurs (such as
        /// HIDP_STATUS_INVALID_REPORT_TYPE or HIDP_INVALID_PREPARSED_DATA, this
        /// returns 0.</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_MaxUsageListLength([MarshalAs(UnmanagedType.U2)]HidReportType ReportType, ushort UsagePage, IntPtr PreparsedData);

        /// <summary>
        /// HidP_SetUsageValue inserts a value into the HID Report Packet in the field
        /// corresponding to the given usage page and usage.  HidP_SetUsageValue
        /// casts this value to the appropriate bit length.  If a report packet
        /// contains two different fields with the same Usage and UsagePage,
        /// they can be distinguished with the optional LinkCollection field value.
        /// Using this function sets the raw value into the report packet with
        /// no checking done as to whether it actually falls within the logical
        /// minimum/logical maximum range.  Use HidP_SetScaledUsageValue for this...
        /// NOTE: Although the UsageValue parameter is a ULONG, any casting that is
        ///  done will preserve or sign-extend the value.  The value being set
        ///  should be considered a LONG value and will be treated as such by
        ///  this function.
        /// </summary>
        /// <param name="ReportType">One of HidP_Output or HidP_Feature.</param>
        /// <param name="UsagePage">The usage page to which the given usage refers.</param>
        /// <param name="LinkCollection">(Optional)  This value can be used to differentiate
        ///                        between two fields that may have the same
        ///                        UsagePage and Usage but exist in different
        ///                        collections.  If the link collection value
        ///                        is zero, this function will set the first field
        ///                        it finds that matches the usage page and
        ///                        usage.</param>
        /// <param name="Usage">The usage whose value HidP_SetUsageValue will set.</param>
        /// <param name="UsageValue">The raw value to set in the report buffer.  This value must be within
        ///        the logical range or if a NULL value this value should be the
        ///        most negative value that can be represented by the number of bits
        ///        for this field.</param>
        /// <param name="PreparsedData">The preparsed data returned for HIDCLASS</param>
        /// <param name="Report">The report packet.</param>
        /// <param name="ReportLength">Length (in bytes) of the given report packet.</param>
        /// <returns>HIDP_STATUS_SUCCESS, HIDP_STATUS_INVALID_REPORT_TYPE,
        /// HIDP_STATUS_INVALID_PREPARSED_DATA, HIDP_STATUS_INVALID_REPORT_LENGTH,
        /// HIDP_STATUS_REPORT_DOES_NOT_EXIST, HIDP_STATUS_INCOMPATIBLE_REPORT_ID,
        /// HIDP_STATUS_USAGE_NOT_FOUND</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_SetUsageValue([MarshalAs(UnmanagedType.U2)]HidReportType ReportType, short UsagePage, short LinkCollection, short Usage, int UsageValue, IntPtr PreparsedData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 7)] byte[] Report, int ReportLength);

        /// <summary>
        /// HidP_SetScaledUsageValue inserts the UsageValue into the HID report packet
        /// in the field corresponding to the given usage page and usage.  If a report
        /// packet contains two different fields with the same Usage and UsagePage,
        /// they can be distinguished with the optional LinkCollection field value.
        ///
        /// If the specified field has a defined physical range, this function converts
        /// the physical value specified to the corresponding logical value for the
        /// report.  If a physical value does not exist, the function will verify that
        /// the value specified falls within the logical range and set according.
        ///
        /// If the range checking fails but the field has NULL values, the function will
        /// set the field to the defined NULL value (most negative number possible) and
        /// return HIDP_STATUS_NULL.  In other words, use this function to set NULL
        /// values for a given field by passing in a value that falls outside the
        /// physical range if it is defined or the logical range otherwise.
        ///
        /// If the field does not support NULL values, an out of range error will be
        /// returned instead.
        /// </summary>
        /// <param name="ReportType">One of HidP_Output or HidP_Feature.</param>
        /// <param name="UsagePage">The usage page to which the given usage refers.</param>
        /// <param name="LinkCollection">(Optional)This value can be used to differentiate
        ///                        between two fields that may have the same
        ///                        UsagePage and Usage but exist in different
        ///                        collections.  If the link collection value
        ///                        is zero, this function will set the first field
        ///                        it finds that matches the usage page and
        ///                        usage.</param>
        /// <param name="Usage">The usage whose value HidP_SetScaledUsageValue will set.</param>
        /// <param name="UsageValue">The value to set in the report buffer.  See the routine
        ///        description above for the different interpretations of this
        ///        value</param>
        /// <param name="PreparsedData">The preparsed data returned from HIDCLASS</param>
        /// <param name="Report">The report packet.</param>
        /// <param name="ReportLength">Length (in bytes) of the given report packet.</param>
        /// <returns>HIDP_STATUS_SUCCESS, HIDP_STATUS_NULL,
        /// HIDP_STATUS_INVALID_REPORT_TYPE, HIDP_STATUS_INVALID_PREPARSED_DATA,
        /// HIDP_STATUS_INVALID_REPORT_LENGTH, HIDP_STATUS_VALUE_OUT_OF_RANGE,
        /// HIDP_STATUS_BAD_LOG_PHY_VALUES, HIDP_STATUS_INCOMPATIBLE_REPORT_ID,
        /// HIDP_STATUS_USAGE_NOT_FOUND</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_SetScaledUsageValue([MarshalAs(UnmanagedType.U2)]HidReportType ReportType, short UsagePage, short LinkCollection, short Usage, int UsageValue, IntPtr PreparsedData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 7)] byte[] Report, int ReportLength);

        /// <summary>
        /// A usage value array occurs when the last usage in the list of usages
        /// describing a main item must be repeated because there are less usages defined
        /// than there are report counts declared for the given main item.  In this case
        /// a single value cap is allocated for that usage and the report count of that
        /// value cap is set to reflect the number of fields to which that usage refers.
        /// 
        /// HidP_SetUsageValueArray sets the raw bits for that usage which spans
        /// more than one field in a report.
        /// 
        /// NOTE: This function currently does not support value arrays where the
        ///  ReportSize for each of the fields in the array is not a multiple
        ///  of 8 bits.
        ///
        ///  The UsageValue buffer should have the values set as they would appear
        ///  in the report buffer.  If this function supported non 8-bit multiples
        ///  for the ReportSize then caller should format the input buffer so that
        ///  each new value begins at the bit immediately following the last bit
        ///  of the previous value
        /// </summary>
        /// <param name="ReportType">One of HidP_Output or HidP_Feature.</param>
        /// <param name="UsagePage">The usage page to which the given usage refers.</param>
        /// <param name="LinkCollection">(Optional)This value can be used to differentiate
        ///                        between two fields that may have the same
        ///                        UsagePage and Usage but exist in different
        ///                        collections.  If the link collection value
        ///                        is zero, this function will set the first field
        ///                        it finds that matches the usage page and
        ///                        usage.</param>
        /// <param name="Usage">The usage whose value array HidP_SetUsageValueArray will set.</param>
        /// <param name="UsageValue">The buffer with the values to set into the value array.
        ///        The number of BITS required is found by multiplying the
        ///        BitSize and ReportCount fields of the Value Cap for this
        ///        control.  The least significant bit of this control found in the
        ///        given report will be placed in the least significan bit location
        ///        of the array given (little-endian format), regardless of whether
        ///        or not the field is byte alligned or if the BitSize is a multiple
        ///        of sizeof (CHAR).</param>
        /// <param name="UsageValueByteLength">Length of the UsageValue buffer (in bytes)</param>
        /// <param name="PreparsedData">The preparsed data returned from HIDCLASS</param>
        /// <param name="Report">The report packet.</param>
        /// <param name="ReportLength">Length (in bytes) of the given report packet.</param>
        /// <returns>HIDP_STATUS_SUCCESS, HIDP_STATUS_INVALID_REPORT_TYPE,
        /// HIDP_STATUS_INVALID_PREPARSED_DATA, HIDP_STATUS_INVALID_REPORT_LENGTH,
        /// HIDP_STATUS_REPORT_DOES_NOT_EXIST, HIDP_STATUS_NOT_VALUE_ARRAY, HIDP_STATUS_BUFFER_TOO_SMALL,
        /// HIDP_STATUS_NOT_IMPLEMENTED, HIDP_STATUS_INCOMPATIBLE_REPORT_ID, HIDP_STATUS_USAGE_NOT_FOUND</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_SetUsageValueArray([MarshalAs(UnmanagedType.U2)]HidReportType ReportType, short UsagePage, short LinkCollection, short Usage, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] UsageValue, short UsageValueByteLength, IntPtr PreparsedData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 8)] byte[] Report, int ReportLength);

        /// <summary>
        /// HidP_GetUsageValue retrieves the value from the HID Report for the usage
        /// specified by the combination of usage page, usage and link collection.
        /// If a report packet contains two different fields with the same
        /// Usage and UsagePage, they can be distinguished with the optional
        /// LinkCollection field value.
        /// </summary>
        /// <param name="ReportType">One of HidP_Input or HidP_Feature.</param>
        /// <param name="UsagePage">The usage page to which the given usage refers.</param>
        /// <param name="LinkCollection">(Optional)This value can be used to differentiate
        ///                        between two fields that may have the same
        ///                        UsagePage and Usage but exist in different
        ///                        collections.  If the link collection value
        ///                        is zero, this function will set the first field
        ///                        it finds that matches the usage page and
        ///                        usage.</param>
        /// <param name="Usage">The usage whose value HidP_GetUsageValue will retrieve</param>
        /// <param name="UsageValue">The raw value that is set for the specified field in the report
        ///        buffer. This value will either fall within the logical range
        ///        or if NULL values are allowed, a number outside the range to
        ///        indicate a NULL</param>
        /// <param name="PreparsedData">The preparsed data returned for HIDCLASS</param>
        /// <param name="Report">The report packet.</param>
        /// <param name="ReportLength">Length (in bytes) of the given report packet.</param>
        /// <returns>HIDP_STATUS_SUCCESS, HIDP_STATUS_INVALID_REPORT_TYPE,
        /// HIDP_STATUS_INVALID_PREPARSED_DATA, HIDP_STATUS_INVALID_REPORT_LENGTH,
        /// HIDP_STATUS_REPORT_DOES_NOT_EXIST, HIDP_STATUS_INCOMPATIBLE_REPORT_ID, HIDP_STATUS_USAGE_NOT_FOUND</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_GetUsageValue(HidReportType ReportType, ushort UsagePage, short LinkCollection, ushort Usage, ref int UsageValue, IntPtr PreparsedData, IntPtr Report, int ReportLength);

        /// <summary>
        /// HidP_GetScaledUsageValue retrieves a UsageValue from the HID report packet
        /// in the field corresponding to the given usage page and usage.  If a report
        /// packet contains two different fields with the same Usage and UsagePage,
        /// they can be distinguished with the optional LinkCollection field value.
        ///
        /// If the specified field has a defined physical range, this function converts
        /// the logical value that exists in the report packet to the corresponding
        /// physical value.  If a physical range does not exist, the function will
        /// return the logical value.  This function will check to verify that the
        /// logical value in the report falls within the declared logical range.
        ///
        /// When doing the conversion between logical and physical values, this
        /// function assumes a linear extrapolation between the physical max/min and
        /// the logical max/min. (Where logical is the values reported by the device
        /// and physical is the value returned by this function).  If the data field
        /// size is less than 32 bits, then HidP_GetScaledUsageValue will sign extend
        /// the value to 32 bits.
        ///
        /// If the range checking fails but the field has NULL values, the function
        /// will set UsageValue to 0 and return HIDP_STATUS_NULL.  Otherwise, it
        /// returns a HIDP_STATUS_OUT_OF_RANGE error.
        /// </summary>
        /// <param name="ReportType">One of HidP_Output or HidP_Feature.</param>
        /// <param name="UsagePage">The usage page to which the given usage refers.</param>
        /// <param name="LinkCollection">(Optional)This value can be used to differentiate
        ///                        between two fields that may have the same
        ///                        UsagePage and Usage but exist in different
        ///                        collections.  If the link collection value
        ///                        is zero, this function will retrieve the first
        ///                        field it finds that matches the usage page
        ///                        and usage.</param>
        /// <param name="Usage">The usage whose value HidP_GetScaledUsageValue will retrieve</param>
        /// <param name="UsageValue">The value retrieved from the report buffer.  See the routine
        ///        description above for the different interpretations of this
        ///        value</param>
        /// <param name="PreparsedData">The preparsed data returned from HIDCLASS</param>
        /// <param name="Report">The report packet.</param>
        /// <param name="ReportLength">Length (in bytes) of the given report packet.</param>
        /// <returns>HIDP_STATUS_SUCCESS, HIDP_STATUS_NULL,
        /// HIDP_STATUS_INVALID_REPORT_TYPE, HIDP_STATUS_INVALID_PREPARSED_DATA,
        /// HIDP_STATUS_INVALID_REPORT_LENGTH, HIDP_STATUS_VALUE_OUT_OF_RANGE,
        /// HIDP_STATUS_BAD_LOG_PHY_VALUES, HIDP_STATUS_INCOMPATIBLE_REPORT_ID,
        /// HIDP_STATUS_USAGE_NOT_FOUND</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_GetScaledUsageValue(HidReportType ReportType, ushort UsagePage, short LinkCollection, ushort Usage, ref int UsageValue, IntPtr PreparsedData, IntPtr Report, int ReportLength);

        /// <summary>
        /// A usage value array occurs when the last usage in the list of usages
        /// describing a main item must be repeated because there are less usages defined
        /// than there are report counts declared for the given main item.  In this case
        /// a single value cap is allocated for that usage and the report count of that
        /// value cap is set to reflect the number of fields to which that usage refers.
        /// 
        /// HidP_GetUsageValueArray returns the raw bits for that usage which spans
        /// more than one field in a report.
        /// 
        /// NOTE: This function currently does not support value arrays where the
        ///  ReportSize for each of the fields in the array is not a multiple
        ///  of 8 bits.
        ///
        ///  The UsageValue buffer will have the raw values as they are set
        ///  in the report packet.
        /// </summary>
        /// <param name="ReportType">One of HidP_Input, HidP_Output or HidP_Feature.</param>
        /// <param name="UsagePage">The usage page to which the given usage refers.</param>
        /// <param name="LinkCollection">(Optional)This value can be used to differentiate
        ///                        between two fields that may have the same
        ///                        UsagePage and Usage but exist in different
        ///                        collections.  If the link collection value
        ///                        is zero, this function will set the first field
        ///                        it finds that matches the usage page and
        ///                        usage.</param>
        /// <param name="Usage">The usage whose value HidP_GetUsageValueArray will retreive.</param>
        /// <param name="UsageValue">A pointer to an array of characters where the value will be
        ///       placed.  The number of BITS required is found by multiplying the
        ///       BitSize and ReportCount fields of the Value Cap for this
        ///       control.  The least significant bit of this control found in the
        ///       given report will be placed in the least significant bit location
        ///       of the buffer (little-endian format), regardless of whether
        ///       or not the field is byte aligned or if the BitSize is a multiple
        ///       of sizeof (CHAR).</param>
        /// <param name="UsageValueByteLength">the length of the given UsageValue buffer.</param>
        /// <param name="PreparsedData">The preparsed data returned by the HIDCLASS</param>
        /// <param name="Report">The report packet.</param>
        /// <param name="ReportLength">Length of the given report packet.</param>
        /// <returns>HIDP_STATUS_SUCCESS, HIDP_STATUS_INVALID_REPORT_TYPE,
        /// HIDP_STATUS_INVALID_PREPARSED_DATA, HIDP_STATUS_INVALID_REPORT_LENGTH,
        /// HIDP_STATUS_NOT_VALUE_ARRAY, HIDP_STATUS_BUFFER_TOO_SMALL,
        /// HIDP_STATUS_NOT_IMPLEMENTED, HIDP_STATUS_INCOMPATIBLE_REPORT_ID,
        /// HIDP_STATUS_USAGE_NOT_FOUND</returns>
        [DllImport("hid.dll", SetLastError = true)]
        static public extern int HidP_GetUsageValueArray([MarshalAs(UnmanagedType.U2)]HidReportType ReportType, short UsagePage, short LinkCollection, short Usage, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] UsageValue, short UsageValueByteLength, IntPtr PreparsedData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 8)] byte[] Report, int ReportLength);

        #endregion
    }
}
