namespace GestureSign.Common.InterProcessCommunication
{
    public interface IMessageProcessor
    {
        bool ProcessMessages(IpcCommands command, object data);
    }
}
