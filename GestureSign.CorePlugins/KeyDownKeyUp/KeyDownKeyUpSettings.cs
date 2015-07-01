using System.Collections.Generic;
using System.Windows.Forms;

namespace GestureSign.CorePlugins.KeyDownKeyUp
{
    public class KeyDownKeyUpSettings
    {
        public bool IsKeyDown { get; set; }
        public List<Keys> KeyCode { get; set; }
    }
}
