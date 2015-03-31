using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Pipes;
using System.IO;

using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;

namespace GestureSign.Common.InterProcessCommunication
{
    public class NamedPipe
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool WaitNamedPipe(string name, int timeout);
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
                        for (int i = 0; i != 10; i++)
                        {
                            if (!NamedPipeDoesNotExist(pipeName)) break;
                            Thread.Sleep(100);
                        } pipeClient.Connect(10);
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
                //System.Windows.Forms.MessageBox.Show("可能缺失另一程序文件，或无法启动另一程序。", "错误", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        static private bool NamedPipeDoesNotExist(string pipeName)
        {
            try
            {
                int timeout = 0;
                string normalizedPath = System.IO.Path.GetFullPath(
                 string.Format(@"\\.\pipe\{0}", pipeName));
                bool exists = WaitNamedPipe(normalizedPath, timeout);
                if (!exists)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == 0) // pipe does not exist
                        return true;
                    else if (error == 2) // win32 error code for file not found
                        return true;
                    // all other errors indicate other issues
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception("Failure in WaitNamedPipe()", ex);
                return true; // assume it exists
            }
        }

    }
}
