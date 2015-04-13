using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Runtime.Serialization.Formatters.Binary;
using Point = System.Drawing.Point;

namespace GestureSign
{
    class MessageProcessor
    {
        public static event EventHandler OnInitialized;
        public void ProcessMessages(System.IO.Pipes.NamedPipeServerStream server)
        {
            try
            {
                BinaryFormatter binForm = new BinaryFormatter();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    server.CopyTo(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    object data = binForm.Deserialize(memoryStream);
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        string message = data as string;
                        if (message != null)
                        {
                            switch (message)
                            {
                                case "MainWindow":
                                {

                                    foreach (Window win in Application.Current.Windows)
                                    {
                                        if (win.GetType() == typeof (MainWindow))
                                        {
                                            win.Activate();
                                            return;
                                        }
                                    }
                                    if (GestureSign.Common.Configuration.AppConfig.XRatio != 0)
                                    {
                                        MainWindow mw = new MainWindow();
                                        mw.Show();
                                        mw.Activate();
                                    }
                                    break;
                                }
                                case "EndGuide":
                                {
                                    if (OnInitialized != null)
                                        OnInitialized(this, EventArgs.Empty);
                                    break;
                                }
                                case "Exit":
                                {
                                    Application.Current.Shutdown();
                                    break;
                                }
                                case "Guide":
                                    UI.Guide guide = new UI.Guide();
                                    guide.Show();
                                    guide.Activate();
                                    break;
                            }
                        }
                        else
                        {
                            var newGesture = data as Tuple<string, List<List<Point>>>;
                            if (newGesture == null) return;
                            UI.GestureDefinition gu = new UI.GestureDefinition(newGesture.Item2, newGesture.Item1, false);
                            gu.Show();
                            gu.Activate();
                        }
                    }));
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }

        }
    }
}
