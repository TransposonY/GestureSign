using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

using System.Threading;
using Newtonsoft.Json;

namespace GestureSign.Common.Configuration
{
    public static class FileManager
    {
        #region Constructors

        static FileManager()
        {
        }

        #endregion

        #region Public Methods

        public static bool SaveObject(object serializableObject, string filePath, bool typeName = false)
        {
            try
            {
                if (File.Exists(filePath))
                    WaitFile(filePath);
                // Open json file
                using (StreamWriter sWrite = new StreamWriter(filePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    if (typeName)
                    {
                        serializer.TypeNameHandling = TypeNameHandling.Objects;
                        serializer.TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
                    }
                    serializer.Serialize(sWrite, serializableObject);
                }
                //  File.WriteAllText(filePath, JsonConvert.SerializeObject(SerializableObject));
                return true;
            }
            catch (Exception ex)
            {
                Logging.LogException(ex);
                return false;
            }
        }

        public static T LoadObject<T>(string filePath, bool backup, bool typeName = false)
        {
            try
            {
                if (!File.Exists(filePath)) return default(T);

                WaitFile(filePath);

                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<T>(json, typeName
                        ? new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects }
                        : new JsonSerializerSettings());
            }
            catch (Exception e)
            {
                Logging.LogException(e);
                if (backup)
                    BackupFile(filePath);
                return default(T);
            }
        }

        public static T LoadObject<T>(string filePath, Type[] KnownTypes, bool backup)
        {
            try
            {
                if (!File.Exists(filePath)) return default(T);

                WaitFile(filePath);

                using (StreamReader sRead = new StreamReader(filePath))
                {
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
            }
            catch (System.Runtime.Serialization.SerializationException)
            {
                if (backup)
                    BackupFile(filePath);
                return default(T);
            }
            catch (Exception e)
            {
                Logging.LogException(e);
                return default(T);
            }
        }

        public static void WaitFile(string filePath)
        {
            int count = 0;
            while (IsFileLocked(filePath) && count != 10)
            {
                count++;
                Thread.Sleep(50);
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
            catch (Exception e)
            {
                Logging.LogException(e);
            }
        }

        private static bool IsFileLocked(string file)
        {
            try
            {
                using (File.Open(file, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    return false;
                }
            }
            catch (IOException exception)
            {
                var errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & 65535;
                return errorCode == 32 || errorCode == 33;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion
    }
}
