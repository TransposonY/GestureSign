using System;

namespace GestureSign.Common.Applications
{
    public interface ICommand
    {
        string CommandSettings { get; set; }
        string Name { get; set; }
        string PluginClass { get; set; }
        string PluginFilename { get; set; }
        bool IsEnabled { get; set; }
    }
}
