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
using System.Security.Principal;

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
        public void RunNamedPipeServer(string pipeName, IMessageProcessor messageProcessor)
        {
            try
            {
                pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);

                AsyncCallback ac = null;
                ac = o =>
                {
                    NamedPipeServerStream server = (NamedPipeServerStream)o.AsyncState;
                    server.EndWaitForConnection(o);

                    messageProcessor.ProcessMessages(server);
                    server.Disconnect();

                    server.BeginWaitForConnection(ac, server);

                };
                pipeServer.BeginWaitForConnection(ac, pipeServer);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.ToString(), "错误", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
            }
        }


        public static Task<bool> SendMessageAsync(object message, string pipeName)
        {
            return Task.Run<bool>(new Func<bool>(() =>
               {
                   try
                   {
                       using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.None, TokenImpersonationLevel.None))
                       {
                           using (MemoryStream ms = new MemoryStream())
                           {

                               int i = 0;
                               for (; i != 10; i++)
                               {
                                   if (!NamedPipeDoesNotExist(pipeName)) break;
                                   Thread.Sleep(50);
                               }
                               if (i == 10) return false;

                               pipeClient.Connect(10);

                               {
                                   BinaryFormatter bf = new BinaryFormatter();

                                   bf.Serialize(ms, message);
                                   ms.Seek(0, SeekOrigin.Begin);
                                   ms.CopyTo(pipeClient);
                                   pipeClient.Flush();
                               }
                               pipeClient.WaitForPipeDrain();

                           }
                       }
                       return true;
                   }
                   catch (InvalidOperationException)
                   {
                       //System.Windows.Forms.MessageBox.Show("可能缺失另一程序文件，或无法启动另一程序。", "错误", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
                       return false;
                   }
                   catch (Exception)
                   {
                       return false;
                   }
               }));
        }
        static private bool NamedPipeDoesNotExist(string pipeName)
        {
            try
            {
                const int timeout = 0;
                string normalizedPath = Path.GetFullPath(string.Format(@"\\.\pipe\{0}", pipeName));
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
                //return true; // assume it exists
            }
        }

    }
}
