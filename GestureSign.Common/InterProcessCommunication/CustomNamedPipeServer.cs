using GestureSign.Common.Log;
using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;

namespace GestureSign.Common.InterProcessCommunication
{
    public class CustomNamedPipeServer : IDisposable
    {
        private bool disposedValue;
        private NamedPipeServerStream _namedPipeServer;

        public CustomNamedPipeServer(string pipeName, IpcCommands command, Func<object> function)
        {
            RunSendingServer(pipeName, command, function);
        }

        public CustomNamedPipeServer(string pipeName, IMessageProcessor messageProcessor)
        {
            RunReceivingServer(pipeName, messageProcessor);
        }

        private void RunReceivingServer(string pipeName, IMessageProcessor messageProcessor)
        {
            _namedPipeServer = new NamedPipeServerStream(NamedPipe.GetUserPipeName(pipeName), PipeDirection.In, 1, PipeTransmissionMode.Message,
                PipeOptions.Asynchronous);

            AsyncCallback ac = null;
            ac = o =>
            {
                if (disposedValue) return;
                NamedPipeServerStream server = (NamedPipeServerStream)o.AsyncState;
                try
                {
                    server.EndWaitForConnection(o);

                    object data = NamedPipe.ReadMessages(server, out IpcCommands command);
                    messageProcessor.ProcessMessages(command, data);
                    server.Disconnect();

                    server.BeginWaitForConnection(ac, server);
                }
                catch (Exception e)
                {
                    Logging.LogException(e);
                    RunReceivingServer(pipeName, messageProcessor);
                }
            };
            _namedPipeServer.BeginWaitForConnection(ac, _namedPipeServer);
        }

        private void RunSendingServer(string pipeName, IpcCommands command, Func<object> function)
        {
            _namedPipeServer = new NamedPipeServerStream(NamedPipe.GetUserPipeName(pipeName), PipeDirection.Out, 2, PipeTransmissionMode.Message,
                PipeOptions.Asynchronous);

            AsyncCallback ac = null;
            ac = o =>
            {
                NamedPipeServerStream s = (NamedPipeServerStream)o.AsyncState;
                try
                {
                    if (disposedValue)
                        return;
                    s.EndWaitForConnection(o);
                    object data = function.Invoke();
                    using (MemoryStream ms = new MemoryStream())
                    {
                        ms.WriteByte((byte)command);
                        if (data != null)
                        {
                            BinaryFormatter bf = new BinaryFormatter();
                            bf.Serialize(ms, data);
                        }
                        ms.Seek(0, SeekOrigin.Begin);

                        ms.CopyTo(s);
                        s.Flush();
                        s.WaitForPipeDrain();
                    }

                    s.Disconnect();
                    s.BeginWaitForConnection(ac, s);
                }
                catch (Exception)
                {
                    s.Dispose();
                    RunSendingServer(pipeName, command, function);
                }
            };
            _namedPipeServer.BeginWaitForConnection(ac, _namedPipeServer);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
                _namedPipeServer.Dispose();
            }
        }

        ~CustomNamedPipeServer()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
