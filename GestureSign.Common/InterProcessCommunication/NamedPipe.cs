using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Pipes;
using System.IO;

using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;

namespace GestureSign.Common.InterProcessCommunication
{
    public class NamedPipe
    {
        static NamedPipeServerStream pipeServer;
        static readonly NamedPipe instance = new NamedPipe();
        public static NamedPipe Instance
        {
            get
            {
                return instance;
            }
        }
        public void RunNamedPipeServer(string pipeName, Action<NamedPipeServerStream> processMessages)
        {
            pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);

            //Thread serverThread = new Thread(new ThreadStart(delegate
            //{
            AsyncCallback ac = null;
            ac = (o) =>
            {
                NamedPipeServerStream server = (NamedPipeServerStream)o.AsyncState;
                server.EndWaitForConnection(o);
                // StreamReader sr = new StreamReader(server);
                // string result = null;
                // string clientName = server.GetImpersonationUserName();

                //while (server.IsConnected)
                //{
                //    do
                //    {
                processMessages(server);
                server.Disconnect();
                //    }
                //    while (server.IsConnected&&!server.IsMessageComplete);
                //    //
                //}

                server.BeginWaitForConnection(ac, server);

            };
            pipeServer.BeginWaitForConnection(ac, pipeServer);
            //}));
            //serverThread.Start();
        }



        //using (MemoryStream ms = new MemoryStream())
        //           {
        //               bf.Serialize(ms, );
        //               return ms.ToArray();
        //           }
        //BinaryWriter bw = new BinaryWriter(pipeClient);
        //bw.Write(true);
        public static bool SendMessage(object message, string pipeName)
        {
            try
            {
                using (NamedPipeClientStream pipeClient =
                             new NamedPipeClientStream(".", pipeName,
                                 PipeDirection.Out, PipeOptions.None,
                                 System.Security.Principal.TokenImpersonationLevel.None))
                {
                    using (StreamWriter sw = new StreamWriter(pipeClient))
                    {
                        pipeClient.Connect(100);
                        //sw.AutoFlush = true;
                        //if (message is string)
                        //{
                        //    sw.WriteLine(message);

                        //}
                        // else
                        {
                            BinaryFormatter bf = new BinaryFormatter();

                            bf.Serialize(pipeClient, message);
                            pipeClient.Flush();
                        }
                        pipeClient.WaitForPipeDrain();
                        //pipeClient.Close();

                    }
                }
                return true;
            }
            catch (System.InvalidOperationException)
            {
                System.Windows.Forms.MessageBox.Show("可能缺失另一程序文件，或无法启动另一程序。", "错误", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
