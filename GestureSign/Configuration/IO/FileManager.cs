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

        public static bool SaveObject<T>(object SerializableObject, string filePath)
        {
            return SaveObject<T>(SerializableObject, filePath, null);
        }

        public static bool SaveObject<T>(object SerializableObject, string filePath, Type[] KnownTypes)
        {
            try
            {
                // Create json serializer to serialize json file
                DataContractJsonSerializer jSerial = KnownTypes != null ? new DataContractJsonSerializer(typeof(T), KnownTypes) : new DataContractJsonSerializer(typeof(T));

                // Open json file
                StreamWriter sWrite = new StreamWriter(filePath);

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


        public static T LoadObject<T>(string filePath, Type[] KnownTypes, bool backup)
        {
            try
            {
                if (!File.Exists(filePath)) return default(T);
                StreamReader sRead = new StreamReader(filePath);
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
            catch (System.Runtime.Serialization.SerializationException)
            {
                if (backup)
                    BackupFile(filePath);
                return default(T);
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        private static void BackupFile(string filePath)
        {
            try
            {
                string backupFileName = Path.Combine(Path.GetDirectoryName(filePath),
                    Path.GetFileNameWithoutExtension(filePath) +
            DateTime.Now.ToString("yyMMddHHmmssffff") +
            Path.GetExtension(filePath));
                File.Copy(filePath, backupFileName, true);
            }
            catch { }
        }
        #endregion
    }
}
