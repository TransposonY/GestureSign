using System;
using GestureSign.Common.Log;
using Newtonsoft.Json;

namespace GestureSign.Common.Plugins
{
    public class PluginHelper
    {
        public static string SerializeSettings(object settings)
        {
            return JsonConvert.SerializeObject(settings);
        }
        public static bool DeserializeSettings<T>(string serializedData, out T settings)
        {
            // Clear existing settings if nothing was passed in
            if (String.IsNullOrEmpty(serializedData))
            {
                settings = Activator.CreateInstance<T>();
                return true;
            }
            try
            {
                settings = JsonConvert.DeserializeObject<T>(serializedData);

                if (settings == null)
                {
                    settings = Activator.CreateInstance<T>();
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e);
                settings = Activator.CreateInstance<T>();
                return false;
            }
            return true;
        }
    }
}
