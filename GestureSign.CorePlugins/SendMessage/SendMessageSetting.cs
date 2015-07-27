using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GestureSign.CorePlugins.HotKey;

namespace GestureSign.CorePlugins.SendMessage
{
    internal class SendMessageSetting
    {
        public string ClassName { get; set; }
        public string Title { get; set; }
        public bool IsRegEx { get; set; }
        public bool IsSendMessage { get; set; }
        public bool IsSpecificWindow { get; set; }
        public uint Message { get; set; }
        public IntPtr WParam { get; set; }
        public IntPtr LParam { get; set; }
        public HotKeySettings HotKey { get; set; }
    }
}
