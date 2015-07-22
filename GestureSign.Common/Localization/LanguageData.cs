using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using GestureSign.Common.Configuration;

namespace GestureSign.Common.Localization
{
    public class LanguageDataManager
    {
        private Dictionary<string, string> _texts = new Dictionary<string, string>(10);
        private FlowDirection _flowDirection;
        private FontFamily _font = new FontFamily("Segoe UI, Lucida Sans Unicode, Verdana");
        private static LanguageDataManager _instance;
        internal LanguageDataManager()
        {
        }

        public bool HasData
        {
            get { return _texts.Count != 0; }
        }

        public FlowDirection FlowDirection
        {
            get { return _flowDirection; }
        }

        public static LanguageDataManager Instance
        {
            get { return _instance ?? (_instance = new LanguageDataManager()); }
        }

        public FontFamily Font
        {
            get { return _font; }
        }

        public Dictionary<string, string> GetLanguageList(string languageFolderName)
        {
            var languageList = new Dictionary<string, string>(2) { { "Built-in", "English (Built-in)" } };
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages", languageFolderName);
            foreach (string file in Directory.GetFiles(folderPath, "*.xml"))
            {
                using (XmlTextReader xtr = new XmlTextReader(file) { WhitespaceHandling = WhitespaceHandling.None })
                {
                    xtr.Read();
                    xtr.Read();
                    if ("language".Equals(xtr.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        string key = xtr.GetAttribute("Culture");
                        if (key != null && !languageList.ContainsKey(key))
                            languageList.Add(key, xtr.GetAttribute("DisplayName"));
                    }
                }
            }
            return languageList;
        }

        public string GetTextValue(string key)
        {
            return _texts.ContainsKey(key) ? _texts[key] : "";
        }

        public bool LoadFromFile(string languageFolderName)
        {
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
            var culture = String.IsNullOrEmpty(AppConfig.CultureName) ? CultureInfo.InstalledUICulture : CultureInfo.CreateSpecificCulture(AppConfig.CultureName);

            var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages", languageFolderName);
            if (!Directory.Exists(folderPath)) return null;
            foreach (string file in Directory.GetFiles(folderPath, "*.xml"))
            {
                using (XmlTextReader xtr = new XmlTextReader(file) { WhitespaceHandling = WhitespaceHandling.None })
                {
                    xtr.Read();
                    xtr.Read();
                    if ("language".Equals(xtr.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (culture.Name.Equals(xtr.GetAttribute("Culture"), StringComparison.OrdinalIgnoreCase))
                        {
                            return file;
                        }
                    }
                }
            }
            return null;
        }

        private void LoadLanguageData(XmlTextReader xmlTextReader)
        {
            List<string> nodes = new List<string>(4);
            xmlTextReader.Read();
            xmlTextReader.Read();
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
                    if (!_texts.ContainsKey(key))
                        _texts.Add(key, xmlTextReader.Value);
                }
            }

            if (_texts.ContainsKey("Font"))
                _font = new FontFamily(_texts["Font"]);
            if (_texts.ContainsKey("IsRightToLeft"))
                _flowDirection = Boolean.Parse(_texts["IsRightToLeft"]) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }


    }
}
