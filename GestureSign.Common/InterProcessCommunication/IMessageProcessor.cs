using System.IO.Pipes;

namespace GestureSign.Common.InterProcessCommunication
{
    public interface IMessageProcessor
    {
        bool ProcessMessages(NamedPipeServerStream server);
    }
}
