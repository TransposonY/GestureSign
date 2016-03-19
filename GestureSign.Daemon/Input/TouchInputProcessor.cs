using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Pipes;
using System.Threading;
using GestureSign.Common;
using GestureSign.Common.Input;
using GestureSign.Common.InterProcessCommunication;

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
            try
            {
                //BinaryReader would throw EndOfStreamException

                byte[] buffer = new byte[4];
                if (server.Read(buffer, 0, 4) == 0)
                    return false;

                int count = BitConverter.ToInt32(buffer, 0);
                var rawTouchDatas = new List<RawTouchData>(count);
                for (int i = 0; i < count; i++)
                {
                    buffer = new byte[13];

                    server.Read(buffer, 0, 1);
                    server.Read(buffer, 1, 4);
                    server.Read(buffer, 5, 4);
                    server.Read(buffer, 9, 4);

                    bool tip = BitConverter.ToBoolean(buffer, 0);
                    int contactIdentifier = BitConverter.ToInt32(buffer, 1);
                    int x = BitConverter.ToInt32(buffer, 5);
                    int y = BitConverter.ToInt32(buffer, 9);

                    rawTouchDatas.Add(new RawTouchData(tip, contactIdentifier, new Point(x, y)));
                }

                _synchronizationContext.Post(o =>
                {
                    PointsIntercepted?.Invoke(this, new RawPointsDataMessageEventArgs(rawTouchDatas));
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
