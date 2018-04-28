using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Pipes;
using System.Threading;
using GestureSign.Common.Input;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Log;

namespace GestureSign.Daemon.Input
{
    class TouchInputProcessor : IMessageProcessor
    {
        public event RawPointsDataMessageEventHandler PointsIntercepted;

        private readonly SynchronizationContext _synchronizationContext;

        public TouchInputProcessor(SynchronizationContext synchronizationContext)
        {
            _synchronizationContext = synchronizationContext;
        }

        public bool ProcessMessages(NamedPipeServerStream server)
        {
            const int size = 16;
            try
            {
                //BinaryReader would throw EndOfStreamException

                byte[] buffer = new byte[8];
                if (server.Read(buffer, 0, buffer.Length) == 0)
                    return false;

                int count = BitConverter.ToInt32(buffer, 0);
                //Connection test
                if (count < 1)
                    return true;
                Devices source = (Devices)BitConverter.ToInt32(buffer, 4);

                buffer = new byte[size * count];
                var rawTouchDatas = new List<RawData>(count);
                server.Read(buffer, 0, buffer.Length);

                for (int i = 0; i < count; i++)
                {
                    int state = BitConverter.ToInt32(buffer, size * i);
                    int contactIdentifier = BitConverter.ToInt32(buffer, size * i + 4);
                    int x = BitConverter.ToInt32(buffer, size * i + 8);
                    int y = BitConverter.ToInt32(buffer, size * i + 12);

                    rawTouchDatas.Add(new RawData((DeviceStates)state, contactIdentifier, new Point(x, y)));
                }

                _synchronizationContext.Post(o =>
                {
                    PointsIntercepted?.Invoke(this, new RawPointsDataMessageEventArgs(rawTouchDatas, source));
                }, null);
            }
            catch (Exception exception)
            {
                Logging.LogException(exception);
                return false;
            }
            return true;
        }


    }
}
