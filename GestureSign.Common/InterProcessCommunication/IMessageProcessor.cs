using System.IO.Pipes;

namespace GestureSign.Common.InterProcessCommunication
{
    public interface IMessageProcessor
    {
        void ProcessMessages(NamedPipeServerStream server);
    }
}
