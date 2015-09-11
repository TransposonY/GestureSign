using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using GestureSign.Common.Configuration;

namespace GestureSign.Common.Localization
{
    public class LocalizationProvider
    {
        protected static string Resource;
        protected static Dictionary<string, string> Texts = new Dictionary<string, string>(10);
        private static LocalizationProvider _instance;

        protected LocalizationProvider()
        {
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
            var languageList = new Dictionary<string, string>(2) { { "Built-in", "English (Built-in)" } };
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
            if (Texts.ContainsKey(key)) return Texts[key];
            if (Resource != null)
                LoadFromResource(Resource);
            return Texts.ContainsKey(key) ? Texts[key] : "";
        }

        public bool LoadFromFile(string languageFolderName, string resource)
        {
            Resource = resource;
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
            var culture = String.IsNullOrEmpty(AppConfig.CultureName) ? CultureInfo.CurrentUICulture : CultureInfo.CreateSpecificCulture(AppConfig.CultureName);

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
                            if (culture.Name.Equals(xtr.GetAttribute("Culture"), StringComparison.OrdinalIgnoreCase))
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


    }
}
