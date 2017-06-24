using ManagedWinapi.Windows;
using System;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;

namespace GestureSign.CorePlugins
{
    public class KeyboardHelper
    {
        public static void ResetKeyState(SystemWindow targetWindow, params VirtualKeyCode[] keys)
        {
            if (keys == null || keys.Length == 0)
                return;

            var shieldWindow = new NativeWindow();

            try
            {
                shieldWindow.CreateHandle(new CreateParams() { ExStyle = (int)(WindowExStyleFlags.TOOLWINDOW | WindowExStyleFlags.LAYERED) });

                InputSimulator simulator = new InputSimulator();
                SystemWindow.ForegroundWindow = new SystemWindow(shieldWindow.Handle)
                {
                    WindowState = FormWindowState.Normal
                };

                foreach (var k in keys)
                {
                    if (!Enum.IsDefined(typeof(VirtualKeyCode), k.GetHashCode())) continue;
                    simulator.Keyboard.Sleep(10).KeyUp(k);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                SystemWindow.ForegroundWindow = targetWindow;
                shieldWindow.DestroyHandle();
            }
        }
    }
}
