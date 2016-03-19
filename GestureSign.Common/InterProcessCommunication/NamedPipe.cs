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

        private static NamedPipeServerStream _pipeServer;
        private static NamedPipeServerStream _persistentPipeServerStream;
        private static readonly NamedPipe instance = new NamedPipe();

        public static NamedPipe Instance
        {
            get
            {
                return instance;
            }
        }

        public void RunNamedPipeServer(string pipeName, IMessageProcessor messageProcessor)
        {
            _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Message,
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
            _pipeServer.BeginWaitForConnection(ac, _pipeServer);
        }

        public void RunPersistentPipeConnection(string pipeName, IMessageProcessor messageProcessor)
        {
            _persistentPipeServerStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            AsyncCallback ac = null;
            ac = o =>
            {
                NamedPipeServerStream server = (NamedPipeServerStream)o.AsyncState;
                server.EndWaitForConnection(o);

                while (true)
                {
                    if (!messageProcessor.ProcessMessages(server)) break;
                }

                server.Dispose();
                server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                server.BeginWaitForConnection(ac, server);
            };
            _persistentPipeServerStream.BeginWaitForConnection(ac, _persistentPipeServerStream);
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
                               for (; i != 20; i++)
                               {
                                   if (!NamedPipeDoesNotExist(pipeName)) break;
                                   Thread.Sleep(50);
                               }
                               if (i == 20) return false;

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
                   catch (Exception e)
                   {
                       Logging.LogException(e);
                       return false;
                   }
               }));
        }

        public static bool NamedPipeDoesNotExist(string pipeName)
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
