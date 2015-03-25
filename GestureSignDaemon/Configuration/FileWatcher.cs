using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
namespace GestureSignDaemon.Configuration
{
    public class FileWatcher
    {
        private FileSystemWatcher fsw;
        public event EventHandler ConfigChanged;
        public event EventHandler GestureChanged;
        public event EventHandler ActionChanged;

        FileWatcher()
        {
            fsw = new System.IO.FileSystemWatcher(Directory.GetCurrentDirectory());
            fsw.Filter = "*.*";
            fsw.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime;
            fsw.IncludeSubdirectories = true;
            // Add event handlers.
            fsw.Created += fsw_Changed;
            fsw.Changed += fsw_Changed;


            ConfigChanged += FileWatcher_ConfigChanged;
            GestureChanged += FileWatcher_GestureChanged;
            ActionChanged += FileWatcher_ActionChanged;
            // Begin watching.
            fsw.EnableRaisingEvents = true;
        }

        void FileWatcher_ActionChanged(object sender, EventArgs e)
        {
            GestureSign.Common.Applications.ApplicationManager.Instance.LoadApplications();
        }

        void FileWatcher_GestureChanged(object sender, EventArgs e)
        {
            GestureSign.Common.Gestures.GestureManager.Instance.LoadGestures();
        }

        void FileWatcher_ConfigChanged(object sender, EventArgs e)
        {
            GestureSign.Common.Configuration.AppConfig.Reload();
        }

        void fsw_Changed(object sender, FileSystemEventArgs e)
        {
            switch (e.Name.ToLower())
            {
                case @"data\applications.json":
                    if (ActionChanged != null) ActionChanged(this, EventArgs.Empty);
                    break;
                case @"data\gestures.json":
                    if (GestureChanged != null) GestureChanged(this, EventArgs.Empty);
                    break;
                case "gesturesign.exe.config":
                    if (ConfigChanged != null) ConfigChanged(this, EventArgs.Empty);
                    break;
                default: break;
            }
        }

        private static readonly FileWatcher instance = new FileWatcher();

        public static FileWatcher Instance
        { get { return instance; } }

        public bool EnableWatcher { set { fsw.EnableRaisingEvents = value; } }

        public void Load()
        {

        }
    }
}
