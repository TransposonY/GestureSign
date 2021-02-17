namespace GestureSign.Common.InterProcessCommunication
{
    public interface IMessageProcessor
    {
        bool ProcessMessages(CommandEnum command, object data);
    }
}
