using System.Threading;
using GestureSign.Common.InterProcessCommunication;

namespace GestureSign.Daemon.Input
{
    internal class InputProvider
    {
        public TouchInputProcessor TouchInputProcessor;
        private const string TouchInputProvider = "GestureSign_TouchInputProvider";

        public InputProvider()
        {
            RunPipeServer();
            //StartTouchInputProvider();
        }

        private void RunPipeServer()
        {
            SynchronizationContext uiContext = SynchronizationContext.Current;
            TouchInputProcessor = new TouchInputProcessor(uiContext);

            NamedPipe.Instance.RunPersistentPipeConnection(TouchInputProvider, TouchInputProcessor);
        }
    }
}
