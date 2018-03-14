using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using GestureSign.Common.Configuration;

namespace GestureSign.Common.Localization
{
    public class LocalizationProvider
    {
        private const string DefaultLanguageName = "en";

        protected static Dictionary<string, string> Texts = new Dictionary<string, string>(10);
        private static LocalizationProvider _instance;
        private List<string> _assemblyNameList = new List<string>();
        private CultureInfo _cultureInfo;

        protected LocalizationProvider()
        {
            try
            {
                _cultureInfo = String.IsNullOrEmpty(AppConfig.CultureName) ? CultureInfo.CurrentUICulture : CultureInfo.CreateSpecificCulture(AppConfig.CultureName);
            }
            catch
            {
                _cultureInfo = CultureInfo.CurrentUICulture;
            }
        }

        public bool HasData
        {
            get { return Texts.Count != 0; }
        }

        public static LocalizationProvider Instance
        {
            get { return _instance ?? (_instance = new LocalizationProvider()); }
        }


        public Dictionary<string, string> GetLanguageList(string languageFolderName)
        {
            var languageList = new Dictionary<string, string>(2);
            var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages", languageFolderName);
            if (!Directory.Exists(folderPath)) return null;
            foreach (string file in Directory.GetFiles(folderPath, "*.xml"))
            {
                using (XmlTextReader xtr = new XmlTextReader(file) { WhitespaceHandling = WhitespaceHandling.None })
                {
                    while (xtr.Read())
                    {
                        if ("language".Equals(xtr.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            string key = xtr.GetAttribute("Culture");
                            if (key != null && !languageList.ContainsKey(key))
                                languageList.Add(key, xtr.GetAttribute("DisplayName"));
                            break;
                        }
                    }
                }
            }
            return languageList;
        }

        public string GetTextValue(string key)
        {
            string text;
            if (Texts.TryGetValue(key, out text))
                return text;

            LoadFromAssemblyResource(_cultureInfo.TwoLetterISOLanguageName);
            if (Texts.TryGetValue(key, out text))
                return text;

            LoadFromAssemblyResource(DefaultLanguageName);
            return Texts.TryGetValue(key, out text) ? text : "";
        }

        public void AddAssembly(string assemblyName)
        {
            if (!_assemblyNameList.Contains(assemblyName))
                _assemblyNameList.Add(assemblyName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool LoadFromFile(string languageFolderName)
        {
            Assembly callerAssembly = Assembly.GetCallingAssembly();
            _assemblyNameList.Add(callerAssembly.FullName);

            string languageFile = GetLanguageFilePath(languageFolderName);
            if (languageFile == null) return false;
            using (XmlTextReader xtr = new XmlTextReader(languageFile) { WhitespaceHandling = WhitespaceHandling.None })
            {
                LoadLanguageData(xtr);
            }
            return true;
        }

        public void LoadFromResource(string languageResource)
        {
            using (XmlTextReader xtr = new XmlTextReader(languageResource, XmlNodeType.Document, null)
            {
                WhitespaceHandling = WhitespaceHandling.None
            })
            {
                LoadLanguageData(xtr);
            }
        }

        private string GetLanguageFilePath(string languageFolderName)
        {
            var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages", languageFolderName);
            if (!Directory.Exists(folderPath)) return null;
            foreach (string file in Directory.GetFiles(folderPath, "*.xml"))
            {
                using (XmlTextReader xtr = new XmlTextReader(file) { WhitespaceHandling = WhitespaceHandling.None })
                {
                    while (xtr.Read())
                    {
                        if ("language".Equals(xtr.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            if (_cultureInfo.Name.Equals(xtr.GetAttribute("Culture"), StringComparison.OrdinalIgnoreCase))
                            {
                                return file;
                            }
                            break;
                        }
                    }
                }
            }
            return null;
        }

        private void LoadLanguageData(XmlTextReader xmlTextReader)
        {
            List<string> nodes = new List<string>(4);
            while (xmlTextReader.Read())
            {
                if (!"language".Equals(xmlTextReader.Name, StringComparison.OrdinalIgnoreCase)) continue;
                while (xmlTextReader.Read())
                {
                    if (xmlTextReader.NodeType == XmlNodeType.Element)
                    {
                        nodes.Add(xmlTextReader.Name);
                    }
                    else if (xmlTextReader.NodeType == XmlNodeType.EndElement)
                    {
                        if (nodes.Count != 0)
                            nodes.RemoveAt(nodes.Count - 1);
                    }
                    else if (xmlTextReader.NodeType == XmlNodeType.Text)
                    {
                        var key = String.Join(".", nodes);
                        if (!Texts.ContainsKey(key))
                            Texts.Add(key, xmlTextReader.Value);
                    }
                }
                break;
            }
        }

        private void LoadFromAssemblyResource(string languageName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!_assemblyNameList.Contains(assembly.FullName)) continue;

                try
                {
                    string[] resNames = assembly.GetManifestResourceNames();

                    foreach (var n in resNames)
                    {
                        int index = n.IndexOf(".resource");
                        if (index < 1) continue;
                        var typeName = n.Substring(0, index);
                        var type = assembly.GetType(typeName, false);

                        if (null != type)
                        {
                            var res = type.GetProperty(languageName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                            if (res == null)
                                continue;
                            var text = res.GetValue(null, null) as string;
                            LoadFromResource(text);
                        }
                    }
                }
                catch { }
            }
        }
    }
}
