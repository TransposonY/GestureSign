using GestureSign.Common.Configuration;
using GestureSign.Common.InterProcessCommunication;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;

namespace GestureSign.TouchInputProvider
{
    class MessageProcessor : IMessageProcessor
    {
        public bool ProcessMessages(NamedPipeServerStream server)
        {
            BinaryFormatter binForm = new BinaryFormatter();

            using (MemoryStream memoryStream = new MemoryStream())
            {
                server.CopyTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                object data = binForm.Deserialize(memoryStream);
                string message = data as string;
                if (message != null)
                {
                    switch (message)
                    {
                        case "LoadConfiguration":
                            AppConfig.Reload();
                            break;
                    }
                }
            }
            return true;
        }

    }
}
