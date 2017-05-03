using System;
using Newtonsoft.Json.Converters;

namespace GestureSign.Common.Applications
{
    public class Command : ICommand
    {
        #region Public Properties

        public string CommandSettings { get; set; }
        public string Name { get; set; }
        public string PluginClass { get; set; }
        public string PluginFilename { get; set; }
        public bool IsEnabled { get; set; }

        #endregion
    }
}
