using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Threading;
using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using Point = System.Drawing.Point;

namespace GestureSign.ControlPanel
{
    class MessageProcessor : IMessageProcessor
    {
        public static event EventHandler<PointPattern[]> GotNewPattern;

        public bool ProcessMessages(NamedPipeServerStream server)
        {
            try
            {
                BinaryFormatter binForm = new BinaryFormatter();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    server.CopyTo(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    object data = binForm.Deserialize(memoryStream);
                    Application.Current?.Dispatcher.InvokeAsync(() =>
                    {
                        string message = data as string;
                        if (message != null)
                        {
                            switch (message)
                            {
                                case "Exit":
                                    {
                                        Application.Current.Shutdown();
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            var newGesture = data as List<List<List<Point>>>;
                            if (newGesture == null) return;

                            GotNewPattern?.Invoke(this, newGesture.Select(list => new PointPattern(list)).ToArray());
                        }
                    }, DispatcherPriority.Input);
                }
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
