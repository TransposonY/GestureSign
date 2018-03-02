using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using GestureSign.Common.Applications;
using GestureSign.Common.Log;
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

        public static bool SaveObject(object serializableObject, string filePath, bool typeName = false, bool throwException = false)
        {
            try
            {
                string backup = null;
                if (File.Exists(filePath))
                {
                    backup = BackupFile(filePath);
                    WaitFile(filePath);
                }

                // Open json file
                using (StreamWriter sWrite = new StreamWriter(filePath))
                {
                    JsonSerializer serializer = new JsonSerializer
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Ignore
                    };
                    if (typeName)
                    {
                        serializer.TypeNameHandling = TypeNameHandling.Objects;
                        serializer.TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
                    }
                    serializer.Serialize(sWrite, serializableObject);
                }
                //  File.WriteAllText(filePath, JsonConvert.SerializeObject(SerializableObject));

                if (File.Exists(backup))
                    File.Delete(backup);
                return true;
            }
            catch (Exception ex)
            {
                Logging.LogException(ex);
                if (throwException)
                    throw;
                return false;
            }
        }

        public static T LoadObject<T>(string filePath, bool backup, bool typeName = false, bool throwException = false)
        {
            try
            {
                if (!File.Exists(filePath)) return default(T);

                WaitFile(filePath);

                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<T>(json, typeName
                    ? new JsonSerializerSettings()
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        Converters = new List<JsonConverter>() { new ActionConverter(), new CommandConverter() }
                    }
                    : new JsonSerializerSettings());
            }
            catch (Exception e)
            {
                Logging.LogException(e);
                if (backup)
                    BackupFile(filePath);
                if (throwException)
                    throw;
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

        private static string BackupFile(string filePath)
        {
            try
            {
                var backupDirectory = new DirectoryInfo(AppConfig.BackupPath);
                if (!backupDirectory.Exists)
                    backupDirectory.Create();
                string backupFileName = Path.Combine(backupDirectory.FullName, DateTime.Now.ToString("yyMMddHHmmss") + Path.GetExtension(filePath));
                File.Copy(filePath, backupFileName, true);
                return backupFileName;
            }
            catch (Exception e)
            {
                Logging.LogException(e);
                return null;
            }
        }

        private static bool IsFileLocked(string file)
        {
            try
            {
                if (!File.Exists(file)) return false;
                using (File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
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
