using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml.Serialization;

namespace GestureSign.Configuration.IO
{
    public static class FileManager
    {
        #region Constructors

        static FileManager()
        {
            try
            {
                if (!Directory.Exists("Data"))
                    Directory.CreateDirectory("Data");
            }
            catch { }
        }

        #endregion

        #region Public Methods

        public static bool SaveObject<T>(object SerializableObject, string Filename)
        {
            return SaveObject<T>(SerializableObject, Filename, null);
        }

        public static bool SaveObject<T>(object SerializableObject, string Filename, Type[] KnownTypes)
        {
            try
            {
                // Create json serializer to serialize json file
                DataContractJsonSerializer jSerial = KnownTypes != null ? new DataContractJsonSerializer(typeof(T), KnownTypes) : new DataContractJsonSerializer(typeof(T));

                // Open json file
                StreamWriter sWrite = new StreamWriter(Path.Combine("Data", Filename));

                // Serialize actions into json file
                jSerial.WriteObject(sWrite.BaseStream, SerializableObject);
                // Close file
                sWrite.Close();
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "保存失败");
                return false;
            }
        }

        public static T LoadObject<T>(string Filename)
        {
            return LoadObject<T>(Filename, null);
        }

        public static T LoadObject<T>(string Filename, Type[] KnownTypes)
        {
            try
            {
                string path = Path.Combine("Data", Filename);
                if (!File.Exists(path)) return default(T);
                StreamReader sRead = new StreamReader(path);
                int BOM = sRead.BaseStream.ReadByte();
                if (BOM == 0xEF)
                    sRead.BaseStream.Seek(3, SeekOrigin.Begin);
                else sRead.BaseStream.Seek(0, SeekOrigin.Begin);
                // Create json serializer to deserialize json file
                DataContractJsonSerializer jSerial = KnownTypes != null ? new DataContractJsonSerializer(typeof(T), KnownTypes) : new DataContractJsonSerializer(typeof(T));

                // Deserialize json file into actions list
                T objBuffer = (T)jSerial.ReadObject(sRead.BaseStream);

                sRead.Close();
                // Return results of serialization
                return objBuffer;
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        #endregion
    }
}
