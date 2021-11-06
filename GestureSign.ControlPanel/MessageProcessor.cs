using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Point = System.Drawing.Point;

namespace GestureSign.ControlPanel
{
    class MessageProcessor : IMessageProcessor
    {
        public static event EventHandler<PointPattern[]> GotNewPattern;

        public bool ProcessMessages(IpcCommands command, object data)
        {
            try
            {
                Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    switch (command)
                    {
                        case IpcCommands.Exit:
                            {
                                Application.Current.Shutdown();
                                break;
                            }
                        case IpcCommands.GotGesture:
                            {
                                var newGesture = data as Point[][][];
                                if (newGesture == null) return;

                                GotNewPattern?.Invoke(this, newGesture.Select(list => new PointPattern(list)).ToArray());
                                break;
                            }
                    }
                }, DispatcherPriority.Input);

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }

        }
    }
}
