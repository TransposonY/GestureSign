using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Runtime.Serialization.Formatters.Binary;

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

                // string[] pointString= message.Split(',');
                //  new System.Drawing.Point(int.Parse(pointString[0]), int.Parse(pointString[1])

                object data = binForm.Deserialize(server);
                Application.Current.Dispatcher.Invoke(new Action(() =>
                       {
                           if (data is string)
                           {
                               string message = (string)data;
                               if (message.Equals("MainWindow"))
                               {

                                   foreach (Window win in Application.Current.Windows)
                                   {
                                       if (win.GetType().Equals(typeof(GestureSign.MainWindow)))
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
                                       mw.availableAction.BindActions();
                                   }
                               }
                               else if (message.Equals("EndGuide"))
                               {
                                   if (OnInitialized != null)
                                       OnInitialized(this, EventArgs.Empty);
                               }
                               else if (message.Equals("Exit"))
                               {
                                   Application.Current.Shutdown();
                               }
                           }
                           else if (data is List<List<System.Drawing.Point>>)
                           {
                               List<List<System.Drawing.Point>> newGesture = (List<List<System.Drawing.Point>>)data;
                               UI.GestureDefinition gu = new UI.GestureDefinition(newGesture);
                               gu.Show();
                               gu.Activate();
                           }
                       }));
            }
            catch (Exception e) { System.Windows.MessageBox.Show(e.Message); }

        }
    }
}
