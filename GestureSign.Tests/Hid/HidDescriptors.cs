using System.Collections.Generic;

namespace GestureSign.Tests.Hid
{
    /// <summary>
    /// Realistic digitizer report descriptors (based on Microsoft's published samples) for synthesizing
    /// preparsed data and input reports with <see cref="HidPreparsedDataBuilder"/> and <see cref="HidReport"/>.
    /// </summary>
    public static class HidDescriptors
    {
        #region Precision touchpad

        /// <summary>Input report ID of the touchpad TLC (5 fingers per report).</summary>
        public const byte TouchpadReportId = 0x01;
        /// <summary>Feature report ID of Contact Count Maximum / Pad Type (touchpad TLC).</summary>
        public const byte TouchpadMaxCountReportId = 0x02;
        /// <summary>Feature report ID of Input Mode (Configuration TLC).</summary>
        public const byte TouchpadInputModeReportId = 0x03;
        /// <summary>Input report ID of the mouse TLC.</summary>
        public const byte TouchpadMouseReportId = 0x04;
        /// <summary>Feature report ID of Surface/Button switch (Configuration TLC).</summary>
        public const byte TouchpadFunctionSwitchReportId = 0x06;
        /// <summary>Feature report ID of the 256-byte certification blob (touchpad TLC).</summary>
        public const byte TouchpadPtphqaReportId = 0x08;

        public const int TouchpadFingers = 5;
        public const int TouchpadLogicalMaxX = 4095;
        public const int TouchpadLogicalMaxY = 4095;
        /// <summary>X physical range 0..400 in 10^-2 inch (4.00"), as in the Microsoft sample.</summary>
        public const int TouchpadPhysicalMaxX = 400;
        /// <summary>Y physical range 0..275 in 10^-2 inch (2.75"), as in the Microsoft sample.</summary>
        public const int TouchpadPhysicalMaxY = 275;
        /// <summary>Logical max of the 3-bit Contact Identifier field.</summary>
        public const int TouchpadContactIdMax = 4;

        /// <summary>
        /// Windows Precision Touchpad, three TLCs: 0 = Touch Pad (0x0D:0x05), 1 = Configuration (0x0D:0x0E),
        /// 2 = Mouse (0x01:0x02).
        /// Source: https://learn.microsoft.com/en-us/windows-hardware/design/component-guidelines/touchpad-sample-report-descriptors
        /// Deviations from the sample: the Finger collection is repeated 5 times (parallel reporting, as
        /// shipped by real touchpads; each copy re-declares USAGE_PAGE (Digitizers)), Contact Identifier is
        /// 3 bits with LOGICAL_MAXIMUM (4) followed by 3 padding bits so 5 IDs fit, and the button block
        /// reports Button 1 only (clickpad) plus 7 padding bits.
        /// Input report (30 bytes): ID, 5 x [Confidence|Tip|ContactId:3|pad:3, X:16, Y:16], Scan Time:16,
        /// Contact Count:8, Button 1|pad:7.
        /// X/Y: logical 0..4095, physical 0..400 / 0..275, unit inch (0x13), exponent -2 (0x0E).
        /// </summary>
        public static readonly byte[] PrecisionTouchpad = Concat(
            new byte[]
            {
                0x05, 0x0D,                   // USAGE_PAGE (Digitizers)
                0x09, 0x05,                   // USAGE (Touch Pad)
                0xA1, 0x01,                   // COLLECTION (Application)
                0x85, TouchpadReportId,       //   REPORT_ID (Touch pad)
            },
            Repeat(TouchpadFinger(), TouchpadFingers),
            new byte[]
            {
                0x55, 0x0C,                   //   UNIT_EXPONENT (-4)
                0x66, 0x01, 0x10,             //   UNIT (Seconds)
                0x47, 0xFF, 0xFF, 0x00, 0x00, //   PHYSICAL_MAXIMUM (65535)
                0x27, 0xFF, 0xFF, 0x00, 0x00, //   LOGICAL_MAXIMUM (65535)
                0x75, 0x10,                   //   REPORT_SIZE (16)
                0x95, 0x01,                   //   REPORT_COUNT (1)
                0x05, 0x0D,                   //   USAGE_PAGE (Digitizers)
                0x09, 0x56,                   //   USAGE (Scan Time)
                0x81, 0x02,                   //   INPUT (Data,Var,Abs)
                0x09, 0x54,                   //   USAGE (Contact count)
                0x25, 0x7F,                   //   LOGICAL_MAXIMUM (127)
                0x95, 0x01,                   //   REPORT_COUNT (1)
                0x75, 0x08,                   //   REPORT_SIZE (8)
                0x81, 0x02,                   //   INPUT (Data,Var,Abs)
                0x05, 0x09,                   //   USAGE_PAGE (Button)
                0x09, 0x01,                   //   USAGE (Button 1)
                0x25, 0x01,                   //   LOGICAL_MAXIMUM (1)
                0x75, 0x01,                   //   REPORT_SIZE (1)
                0x95, 0x01,                   //   REPORT_COUNT (1)
                0x81, 0x02,                   //   INPUT (Data,Var,Abs)
                0x95, 0x07,                   //   REPORT_COUNT (7)
                0x81, 0x03,                   //   INPUT (Cnst,Var,Abs)
                0x05, 0x0D,                   //   USAGE_PAGE (Digitizer)
                0x85, TouchpadMaxCountReportId, // REPORT_ID (Feature)
                0x09, 0x55,                   //   USAGE (Contact Count Maximum)
                0x09, 0x59,                   //   USAGE (Pad Type)
                0x75, 0x04,                   //   REPORT_SIZE (4)
                0x95, 0x02,                   //   REPORT_COUNT (2)
                0x25, 0x0F,                   //   LOGICAL_MAXIMUM (15)
                0xB1, 0x02,                   //   FEATURE (Data,Var,Abs)
                0x06, 0x00, 0xFF,             //   USAGE_PAGE (Vendor Defined)
                0x85, TouchpadPtphqaReportId, //   REPORT_ID (PTPHQA)
                0x09, 0xC5,                   //   USAGE (Vendor Usage 0xC5)
                0x15, 0x00,                   //   LOGICAL_MINIMUM (0)
                0x26, 0xFF, 0x00,             //   LOGICAL_MAXIMUM (0xff)
                0x75, 0x08,                   //   REPORT_SIZE (8)
                0x96, 0x00, 0x01,             //   REPORT_COUNT (0x100 (256))
                0xB1, 0x02,                   //   FEATURE (Data,Var,Abs)
                0xC0,                         // END_COLLECTION

                // CONFIG TLC
                0x05, 0x0D,                   // USAGE_PAGE (Digitizer)
                0x09, 0x0E,                   // USAGE (Configuration)
                0xA1, 0x01,                   // COLLECTION (Application)
                0x85, TouchpadInputModeReportId, // REPORT_ID (Feature)
                0x09, 0x22,                   //   USAGE (Finger)
                0xA1, 0x02,                   //   COLLECTION (logical)
                0x09, 0x52,                   //     USAGE (Input Mode)
                0x15, 0x00,                   //     LOGICAL_MINIMUM (0)
                0x25, 0x0A,                   //     LOGICAL_MAXIMUM (10)
                0x75, 0x08,                   //     REPORT_SIZE (8)
                0x95, 0x01,                   //     REPORT_COUNT (1)
                0xB1, 0x02,                   //     FEATURE (Data,Var,Abs)
                0xC0,                         //   END_COLLECTION
                0x09, 0x22,                   //   USAGE (Finger)
                0xA1, 0x00,                   //   COLLECTION (physical)
                0x85, TouchpadFunctionSwitchReportId, // REPORT_ID (Feature)
                0x09, 0x57,                   //     USAGE (Surface switch)
                0x09, 0x58,                   //     USAGE (Button switch)
                0x75, 0x01,                   //     REPORT_SIZE (1)
                0x95, 0x02,                   //     REPORT_COUNT (2)
                0x25, 0x01,                   //     LOGICAL_MAXIMUM (1)
                0xB1, 0x02,                   //     FEATURE (Data,Var,Abs)
                0x95, 0x06,                   //     REPORT_COUNT (6)
                0xB1, 0x03,                   //     FEATURE (Cnst,Var,Abs)
                0xC0,                         //   END_COLLECTION
                0xC0,                         // END_COLLECTION

                // MOUSE TLC
                0x05, 0x01,                   // USAGE_PAGE (Generic Desktop)
                0x09, 0x02,                   // USAGE (Mouse)
                0xA1, 0x01,                   // COLLECTION (Application)
                0x85, TouchpadMouseReportId,  //   REPORT_ID (Mouse)
                0x09, 0x01,                   //   USAGE (Pointer)
                0xA1, 0x00,                   //   COLLECTION (Physical)
                0x05, 0x09,                   //     USAGE_PAGE (Button)
                0x19, 0x01,                   //     USAGE_MINIMUM (Button 1)
                0x29, 0x02,                   //     USAGE_MAXIMUM (Button 2)
                0x25, 0x01,                   //     LOGICAL_MAXIMUM (1)
                0x75, 0x01,                   //     REPORT_SIZE (1)
                0x95, 0x02,                   //     REPORT_COUNT (2)
                0x81, 0x02,                   //     INPUT (Data,Var,Abs)
                0x95, 0x06,                   //     REPORT_COUNT (6)
                0x81, 0x03,                   //     INPUT (Cnst,Var,Abs)
                0x05, 0x01,                   //     USAGE_PAGE (Generic Desktop)
                0x09, 0x30,                   //     USAGE (X)
                0x09, 0x31,                   //     USAGE (Y)
                0x75, 0x10,                   //     REPORT_SIZE (16)
                0x95, 0x02,                   //     REPORT_COUNT (2)
                0x25, 0x0A,                   //     LOGICAL_MAXIMUM (10)
                0x81, 0x06,                   //     INPUT (Data,Var,Rel)
                0xC0,                         //   END_COLLECTION
                0xC0,                         // END_COLLECTION
            });

        private static byte[] TouchpadFinger() => new byte[]
        {
            0x05, 0x0D,                       //   USAGE_PAGE (Digitizers)
            0x09, 0x22,                       //   USAGE (Finger)
            0xA1, 0x02,                       //   COLLECTION (Logical)
            0x15, 0x00,                       //     LOGICAL_MINIMUM (0)
            0x25, 0x01,                       //     LOGICAL_MAXIMUM (1)
            0x09, 0x47,                       //     USAGE (Confidence)
            0x09, 0x42,                       //     USAGE (Tip switch)
            0x95, 0x02,                       //     REPORT_COUNT (2)
            0x75, 0x01,                       //     REPORT_SIZE (1)
            0x81, 0x02,                       //     INPUT (Data,Var,Abs)
            0x95, 0x01,                       //     REPORT_COUNT (1)
            0x75, 0x03,                       //     REPORT_SIZE (3)
            0x25, TouchpadContactIdMax,       //     LOGICAL_MAXIMUM (4)
            0x09, 0x51,                       //     USAGE (Contact Identifier)
            0x81, 0x02,                       //     INPUT (Data,Var,Abs)
            0x75, 0x01,                       //     REPORT_SIZE (1)
            0x95, 0x03,                       //     REPORT_COUNT (3)
            0x81, 0x03,                       //     INPUT (Cnst,Var,Abs)
            0x05, 0x01,                       //     USAGE_PAGE (Generic Desktop)
            0x15, 0x00,                       //     LOGICAL_MINIMUM (0)
            0x26, 0xFF, 0x0F,                 //     LOGICAL_MAXIMUM (4095)
            0x75, 0x10,                       //     REPORT_SIZE (16)
            0x55, 0x0E,                       //     UNIT_EXPONENT (-2)
            0x65, 0x13,                       //     UNIT (Inch,EngLinear)
            0x09, 0x30,                       //     USAGE (X)
            0x35, 0x00,                       //     PHYSICAL_MINIMUM (0)
            0x46, 0x90, 0x01,                 //     PHYSICAL_MAXIMUM (400)
            0x95, 0x01,                       //     REPORT_COUNT (1)
            0x81, 0x02,                       //     INPUT (Data,Var,Abs)
            0x46, 0x13, 0x01,                 //     PHYSICAL_MAXIMUM (275)
            0x09, 0x31,                       //     USAGE (Y)
            0x81, 0x02,                       //     INPUT (Data,Var,Abs)
            0xC0,                             //   END_COLLECTION
        };

        #endregion

        #region Multi-touch touch screen

        /// <summary>Input report ID of the touch screen (2 contacts per report).</summary>
        public const byte TouchScreenReportId = 0x01;
        /// <summary>Feature report ID of Contact Count Maximum.</summary>
        public const byte TouchScreenMaxCountReportId = 0x02;
        /// <summary>Feature report ID of the 256-byte certification blob.</summary>
        public const byte TouchScreenPtphqaReportId = 0x44;

        public const int TouchScreenContactsPerReport = 2;
        public const int TouchScreenLogicalMaxX = 4095;
        public const int TouchScreenLogicalMaxY = 4095;
        /// <summary>LOGICAL_MAXIMUM of the Contact Count Maximum feature (allows hybrid mode up to 10 contacts).</summary>
        public const int TouchScreenContactCountMaximumLimit = 10;

        /// <summary>
        /// Two-finger parallel/hybrid-mode touch screen, one TLC (0x0D:0x04). Contacts beyond two are sent in
        /// follow-up reports whose Contact Count is 0 (hybrid mode).
        /// Source: https://learn.microsoft.com/en-us/windows-hardware/design/component-guidelines/touchscreen-sample-report-descriptors
        /// Deviations from the sample: X and Y each use REPORT_COUNT (1) (the sample leaves REPORT_COUNT (2)
        /// in effect, which turns X and Y into 2-element value arrays) and Width/Height get an explicit
        /// REPORT_COUNT (2); X/Y PHYSICAL_MAXIMUM equals LOGICAL_MAXIMUM (4095, unit inch, exponent -2) so
        /// scaled and raw values coincide; Contact Count Maximum has LOGICAL_MAXIMUM (10) instead of (2).
        /// Input report (28 bytes): ID, 2 x [Tip|pad:7, ContactId:8, X:16, Y:16, Width:16, Height:16,
        /// Azimuth:16], Scan Time:16, Contact Count:8.
        /// </summary>
        public static readonly byte[] MultiTouchScreen = Concat(
            new byte[]
            {
                0x05, 0x0D,                   // USAGE_PAGE (Digitizers)
                0x09, 0x04,                   // USAGE (Touch Screen)
                0xA1, 0x01,                   // COLLECTION (Application)
                0x85, TouchScreenReportId,    //   REPORT_ID (Touch)
            },
            Repeat(TouchScreenFinger(), TouchScreenContactsPerReport),
            new byte[]
            {
                0x05, 0x0D,                   //   USAGE_PAGE (Digitizers)
                0x55, 0x0C,                   //   UNIT_EXPONENT (-4)
                0x66, 0x01, 0x10,             //   UNIT (Seconds)
                0x47, 0xFF, 0xFF, 0x00, 0x00, //   PHYSICAL_MAXIMUM (65535)
                0x27, 0xFF, 0xFF, 0x00, 0x00, //   LOGICAL_MAXIMUM (65535)
                0x75, 0x10,                   //   REPORT_SIZE (16)
                0x95, 0x01,                   //   REPORT_COUNT (1)
                0x09, 0x56,                   //   USAGE (Scan Time)
                0x81, 0x02,                   //   INPUT (Data,Var,Abs)
                0x09, 0x54,                   //   USAGE (Contact count)
                0x25, 0x7F,                   //   LOGICAL_MAXIMUM (127)
                0x95, 0x01,                   //   REPORT_COUNT (1)
                0x75, 0x08,                   //   REPORT_SIZE (8)
                0x81, 0x02,                   //   INPUT (Data,Var,Abs)
                0x85, TouchScreenMaxCountReportId, // REPORT_ID (Feature)
                0x09, 0x55,                   //   USAGE (Contact Count Maximum)
                0x95, 0x01,                   //   REPORT_COUNT (1)
                0x25, TouchScreenContactCountMaximumLimit, // LOGICAL_MAXIMUM (10)
                0xB1, 0x02,                   //   FEATURE (Data,Var,Abs)
                0x85, TouchScreenPtphqaReportId, // REPORT_ID (Feature)
                0x06, 0x00, 0xFF,             //   USAGE_PAGE (Vendor Defined)
                0x09, 0xC5,                   //   USAGE (Vendor Usage 0xC5)
                0x15, 0x00,                   //   LOGICAL_MINIMUM (0)
                0x26, 0xFF, 0x00,             //   LOGICAL_MAXIMUM (0xff)
                0x75, 0x08,                   //   REPORT_SIZE (8)
                0x96, 0x00, 0x01,             //   REPORT_COUNT (0x100 (256))
                0xB1, 0x02,                   //   FEATURE (Data,Var,Abs)
                0xC0,                         // END_COLLECTION
            });

        private static byte[] TouchScreenFinger() => new byte[]
        {
            0x05, 0x0D,                       //   USAGE_PAGE (Digitizers)
            0x09, 0x22,                       //   USAGE (Finger)
            0xA1, 0x02,                       //   COLLECTION (Logical)
            0x09, 0x42,                       //     USAGE (Tip Switch)
            0x15, 0x00,                       //     LOGICAL_MINIMUM (0)
            0x25, 0x01,                       //     LOGICAL_MAXIMUM (1)
            0x75, 0x01,                       //     REPORT_SIZE (1)
            0x95, 0x01,                       //     REPORT_COUNT (1)
            0x81, 0x02,                       //     INPUT (Data,Var,Abs)
            0x95, 0x07,                       //     REPORT_COUNT (7)
            0x81, 0x03,                       //     INPUT (Cnst,Var,Abs)
            0x75, 0x08,                       //     REPORT_SIZE (8)
            0x09, 0x51,                       //     USAGE (Contact Identifier)
            0x95, 0x01,                       //     REPORT_COUNT (1)
            0x81, 0x02,                       //     INPUT (Data,Var,Abs)
            0x05, 0x01,                       //     USAGE_PAGE (Generic Desktop)
            0x26, 0xFF, 0x0F,                 //     LOGICAL_MAXIMUM (4095)
            0x75, 0x10,                       //     REPORT_SIZE (16)
            0x55, 0x0E,                       //     UNIT_EXPONENT (-2)
            0x65, 0x13,                       //     UNIT (Inch,EngLinear)
            0x09, 0x30,                       //     USAGE (X)
            0x35, 0x00,                       //     PHYSICAL_MINIMUM (0)
            0x46, 0xFF, 0x0F,                 //     PHYSICAL_MAXIMUM (4095)
            0x95, 0x01,                       //     REPORT_COUNT (1)
            0x81, 0x02,                       //     INPUT (Data,Var,Abs)
            0x09, 0x31,                       //     USAGE (Y)
            0x81, 0x02,                       //     INPUT (Data,Var,Abs)
            0x05, 0x0D,                       //     USAGE_PAGE (Digitizers)
            0x09, 0x48,                       //     USAGE (Width)
            0x09, 0x49,                       //     USAGE (Height)
            0x95, 0x02,                       //     REPORT_COUNT (2)
            0x81, 0x02,                       //     INPUT (Data,Var,Abs)
            0x95, 0x01,                       //     REPORT_COUNT (1)
            0x55, 0x0C,                       //     UNIT_EXPONENT (-4)
            0x65, 0x12,                       //     UNIT (Radians,SIRotation)
            0x35, 0x00,                       //     PHYSICAL_MINIMUM (0)
            0x47, 0x6F, 0xF5, 0x00, 0x00,     //     PHYSICAL_MAXIMUM (62831)
            0x15, 0x00,                       //     LOGICAL_MINIMUM (0)
            0x27, 0x6F, 0xF5, 0x00, 0x00,     //     LOGICAL_MAXIMUM (62831)
            0x09, 0x3F,                       //     USAGE (Azimuth[Orientation])
            0x81, 0x02,                       //     INPUT (Data,Var,Abs)
            0xC0,                             //   END_COLLECTION
        };

        #endregion

        #region Pen

        public const byte PenReportId = 0x02;
        public const int PenLogicalMaxX = 21240;
        public const int PenLogicalMaxY = 15980;
        public const int PenLogicalMaxPressure = 255;

        /// <summary>
        /// Integrated Windows pen, one TLC (0x0D:0x02) with a Stylus physical collection.
        /// Source: https://learn.microsoft.com/en-us/windows-hardware/design/component-guidelines/pen-sample-report-descriptors
        /// Deviation from the sample: X/Y PHYSICAL_MAXIMUM equals LOGICAL_MAXIMUM (21240 / 15980; the
        /// sample uses 8250 / 6188 thousandths of an inch) so scaled and raw values coincide.
        /// Exercises PUSH/POP: Tip Pressure inherits REPORT_SIZE (16) and drops the X/Y physical range.
        /// Input report (10 bytes): ID, Tip|Barrel|Invert|Eraser|pad|InRange|pad:2, X:16, Y:16,
        /// Tip Pressure:16, X Tilt:8, Y Tilt:8.
        /// </summary>
        public static readonly byte[] Pen =
        {
            0x05, 0x0D,                       // USAGE_PAGE (Digitizers)
            0x09, 0x02,                       // USAGE (Pen)
            0xA1, 0x01,                       // COLLECTION (Application)
            0x85, PenReportId,                //   REPORT_ID (Pen)
            0x09, 0x20,                       //   USAGE (Stylus)
            0xA1, 0x00,                       //   COLLECTION (Physical)
            0x09, 0x42,                       //     USAGE (Tip Switch)
            0x09, 0x44,                       //     USAGE (Barrel Switch)
            0x09, 0x3C,                       //     USAGE (Invert)
            0x09, 0x45,                       //     USAGE (Eraser Switch)
            0x15, 0x00,                       //     LOGICAL_MINIMUM (0)
            0x25, 0x01,                       //     LOGICAL_MAXIMUM (1)
            0x75, 0x01,                       //     REPORT_SIZE (1)
            0x95, 0x04,                       //     REPORT_COUNT (4)
            0x81, 0x02,                       //     INPUT (Data,Var,Abs)
            0x95, 0x01,                       //     REPORT_COUNT (1)
            0x81, 0x03,                       //     INPUT (Cnst,Var,Abs)
            0x09, 0x32,                       //     USAGE (In Range)
            0x81, 0x02,                       //     INPUT (Data,Var,Abs)
            0x95, 0x02,                       //     REPORT_COUNT (2)
            0x81, 0x03,                       //     INPUT (Cnst,Var,Abs)
            0x05, 0x01,                       //     USAGE_PAGE (Generic Desktop)
            0x09, 0x30,                       //     USAGE (X)
            0x75, 0x10,                       //     REPORT_SIZE (16)
            0x95, 0x01,                       //     REPORT_COUNT (1)
            0xA4,                             //     PUSH
            0x55, 0x0D,                       //     UNIT_EXPONENT (-3)
            0x65, 0x13,                       //     UNIT (Inch,EngLinear)
            0x35, 0x00,                       //     PHYSICAL_MINIMUM (0)
            0x46, 0xF8, 0x52,                 //     PHYSICAL_MAXIMUM (21240)
            0x26, 0xF8, 0x52,                 //     LOGICAL_MAXIMUM (21240)
            0x81, 0x02,                       //     INPUT (Data,Var,Abs)
            0x09, 0x31,                       //     USAGE (Y)
            0x46, 0x6C, 0x3E,                 //     PHYSICAL_MAXIMUM (15980)
            0x26, 0x6C, 0x3E,                 //     LOGICAL_MAXIMUM (15980)
            0x81, 0x02,                       //     INPUT (Data,Var,Abs)
            0xB4,                             //     POP
            0x05, 0x0D,                       //     USAGE_PAGE (Digitizers)
            0x09, 0x30,                       //     USAGE (Tip Pressure)
            0x26, 0xFF, 0x00,                 //     LOGICAL_MAXIMUM (255)
            0x81, 0x02,                       //     INPUT (Data,Var,Abs)
            0x75, 0x08,                       //     REPORT_SIZE (8)
            0x09, 0x3D,                       //     USAGE (X Tilt)
            0x15, 0x81,                       //     LOGICAL_MINIMUM (-127)
            0x25, 0x7F,                       //     LOGICAL_MAXIMUM (127)
            0x81, 0x02,                       //     INPUT (Data,Var,Abs)
            0x09, 0x3E,                       //     USAGE (Y Tilt)
            0x15, 0x81,                       //     LOGICAL_MINIMUM (-127)
            0x25, 0x7F,                       //     LOGICAL_MAXIMUM (127)
            0x81, 0x02,                       //     INPUT (Data,Var,Abs)
            0xC0,                             //   END_COLLECTION
            0xC0,                             // END_COLLECTION
        };

        #endregion

        private static byte[] Repeat(byte[] block, int times)
        {
            var result = new byte[block.Length * times];
            for (int i = 0; i < times; i++) System.Buffer.BlockCopy(block, 0, result, i * block.Length, block.Length);
            return result;
        }

        private static byte[] Concat(params byte[][] parts)
        {
            var result = new List<byte>();
            foreach (var p in parts) result.AddRange(p);
            return result.ToArray();
        }
    }
}
