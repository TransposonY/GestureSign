using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestureSign.CorePlugins.SendMessage
{
    class Messages
    {
        static Messages()
        {
            MessagesDict = new Dictionary<string, uint>(3)
            {
                {"WM_CLOSE", 0x0010},
                {"WM_SHOWWINDOW", 0x0018},
                {"WM_COMMAND", 0x0111}
            };

        }

        public static Dictionary<string, uint> MessagesDict { get; private set; }
    }
}
