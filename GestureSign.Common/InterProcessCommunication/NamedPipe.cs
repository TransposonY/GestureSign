﻿using System;
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
using GestureSign.Common.Log;

namespace GestureSign.Common.InterProcessCommunication
{
    public class NamedPipe : IDisposable
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool WaitNamedPipe(string name, int timeout);

        private static NamedPipeServerStream _pipeServer;
        private static readonly NamedPipe instance = new NamedPipe();

        private bool disposed = false; // To detect redundant calls

        public static NamedPipe Instance
        {
            get
            {
                return instance;
            }
        }

        private static void ReadMessages(NamedPipeServerStream server, out CommandEnum command, out object data)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter binForm = new BinaryFormatter();

                server.CopyTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                command = (CommandEnum)memoryStream.ReadByte();
                data = memoryStream.Length == memoryStream.Position ? null : binForm.Deserialize(memoryStream);
            }
        }

        private static bool WaitNamedPipe(string pipeName)
        {
            for (int i = 0; i != 20; i++)
            {
                if (!NamedPipeDoesNotExist(pipeName))
                    return true;
                Thread.Sleep(50);
            }
            return false;
        }

        public void RunNamedPipeServer(string pipeName, IMessageProcessor messageProcessor)
        {
            _pipeServer = new NamedPipeServerStream(GetUserPipeName(pipeName), PipeDirection.In, 1, PipeTransmissionMode.Message,
                PipeOptions.Asynchronous);

            AsyncCallback ac = null;
            ac = o =>
            {
                if (disposed) return;
                NamedPipeServerStream server = (NamedPipeServerStream)o.AsyncState;
                try
                {
                    server.EndWaitForConnection(o);

                    ReadMessages(server, out CommandEnum command, out object data);
                    messageProcessor.ProcessMessages(command, data);
                    server.Disconnect();

                    server.BeginWaitForConnection(ac, server);
                }
                catch (Exception e)
                {
                    Logging.LogException(e);
                }
            };
            _pipeServer.BeginWaitForConnection(ac, _pipeServer);
        }

        public static Task<bool> SendMessageAsync(CommandEnum command, string pipeName, object message = null, bool wait = true)
        {
            string userPipeName = GetUserPipeName(pipeName);
            return Task.Run<bool>(new Func<bool>(() =>
               {
                   try
                   {
                       using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", userPipeName, PipeDirection.Out, PipeOptions.None, TokenImpersonationLevel.None))
                       {
                           using (MemoryStream ms = new MemoryStream())
                           {
                               if (wait)
                               {
                                   if (!WaitNamedPipe(userPipeName))
                                       return false;
                               }
                               else if (NamedPipeDoesNotExist(userPipeName))
                               {
                                   return false;
                               }

                               pipeClient.Connect(10);

                               ms.WriteByte((byte)command);
                               if (message != null)
                               {
                                   BinaryFormatter bf = new BinaryFormatter();
                                   bf.Serialize(ms, message);
                               }
                               ms.Seek(0, SeekOrigin.Begin);

                               ms.CopyTo(pipeClient);
                               pipeClient.Flush();
                               pipeClient.WaitForPipeDrain();
                           }
                       }
                       return true;
                   }
                   catch (IOException)
                   {
                       return false;
                   }
                   catch (TimeoutException)
                   {
                       return false;
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

        public static string GetUserPipeName(string pipeName)
        {
            var currentUser = WindowsIdentity.GetCurrent();
            return pipeName + "-" + currentUser.User.ToString();
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _pipeServer?.Dispose();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

    }
}
