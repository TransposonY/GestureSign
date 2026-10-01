using System;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GestureSign.Common.Input;
using GestureSign.Common.Log;
using Newtonsoft.Json;

namespace GestureSign.Common.InterProcessCommunication
{
    public class NamedPipe : IDisposable
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool WaitNamedPipe(string name, int timeout);

        private static CustomNamedPipeServer _pipeServer;
        private static readonly NamedPipe instance = new NamedPipe();

        private bool disposed = false; // To detect redundant calls

        public static NamedPipe Instance
        {
            get
            {
                return instance;
            }
        }

        public static object ReadMessages(PipeStream pipe, out IpcCommands command)
        {
            int commandByte = pipe.ReadByte();
            if (commandByte < 0)
                throw new EndOfStreamException("The pipe message has no command.");

            command = (IpcCommands)commandByte;
            Type payloadType = GetPayloadType(command);
            if (payloadType == null)
            {
                if (pipe.ReadByte() != -1)
                    throw new InvalidDataException("The pipe command does not accept a payload.");
                return null;
            }

            using (var streamReader = new StreamReader(pipe, Encoding.UTF8, false, 1024, true))
            using (var reader = new JsonTextReader(streamReader) { CloseInput = false })
            {
                object payload = JsonSerializer.Create().Deserialize(reader, payloadType);
                if (payload == null || reader.Read())
                    throw new InvalidDataException("Invalid pipe message payload.");
                return payload;
            }
        }

        private static Type GetPayloadType(IpcCommands command)
        {
            switch (command)
            {
                case IpcCommands.GotGesture:
                    return typeof(Point[][][]);
                case IpcCommands.SynDeviceState:
                    return typeof(Devices);
                case IpcCommands.StartControlPanel:
                case IpcCommands.StartTeaching:
                case IpcCommands.StopTraining:
                case IpcCommands.LoadApplications:
                case IpcCommands.LoadGestures:
                case IpcCommands.LoadConfiguration:
                case IpcCommands.ConfigReload:
                case IpcCommands.Exit:
                case IpcCommands.LoadContinuousGestures:
                    return null;
                default:
                    throw new InvalidDataException("Unknown pipe command.");
            }
        }

        internal static void WriteMessage(Stream stream, IpcCommands command, object message)
        {
            Type payloadType = GetPayloadType(command);
            if (message == null ? payloadType != null : payloadType == null || !payloadType.IsInstanceOfType(message))
                throw new ArgumentException("Unexpected payload for pipe command.", nameof(message));

            stream.WriteByte((byte)command);
            if (message == null)
                return;

            using (var streamWriter = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
            using (var writer = new JsonTextWriter(streamWriter) { CloseOutput = false })
            {
                JsonSerializer.Create().Serialize(writer, message);
            }
        }

        private static bool WaitForNamedPipeConnection(string pipeName, int interval = 1000)
        {
            const int unit = 50;
            for (int i = 0; i < interval / unit; i++)
            {
                if (!NamedPipeDoesNotExist(pipeName))
                    return true;
                Thread.Sleep(unit);
            }
            return false;
        }

        public void RunNamedPipeServer(string pipeName, IMessageProcessor messageProcessor)
        {
            _pipeServer = new CustomNamedPipeServer(pipeName, messageProcessor);
        }

        public static Task<bool> SendMessageAsync(IpcCommands command, string pipeName, object message = null, bool wait = true)
        {
            string userPipeName = GetUserPipeName(pipeName);
            return Task.Run<bool>(new Func<bool>(() =>
               {
                   try
                   {
                       using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", userPipeName, PipeDirection.Out, PipeOptions.None, TokenImpersonationLevel.None))
                       {
                           if (wait)
                           {
                               if (!WaitForNamedPipeConnection(userPipeName))
                                   return false;
                           }
                           else if (NamedPipeDoesNotExist(userPipeName))
                           {
                               return false;
                           }

                           pipeClient.Connect(10);
                           WriteMessage(pipeClient, command, message);
                           pipeClient.Flush();
                           pipeClient.WaitForPipeDrain();
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

        public static Task<object> GetMessageAsync(string pipeName, int wait = 1000)
        {
            string userPipeName = GetUserPipeName(pipeName);
            return Task.Run(new Func<object>(() =>
            {
                try
                {
                    using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", userPipeName, PipeDirection.In, PipeOptions.None, TokenImpersonationLevel.None))
                    {
                        if (wait > 0)
                        {
                            if (!WaitForNamedPipeConnection(userPipeName, wait))
                                return null;
                        }
                        else if (NamedPipeDoesNotExist(userPipeName))
                        {
                            return null;
                        }

                        pipeClient.Connect(10);
                        return ReadMessages(pipeClient, out IpcCommands command);
                    }
                }
                catch (IOException)
                {
                    return null;
                }
                catch (TimeoutException)
                {
                    return null;
                }
                catch (Exception e)
                {
                    Logging.LogException(e);
                    return null;
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
