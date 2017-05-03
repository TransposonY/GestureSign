using System.Windows.Media.Imaging;
using GestureSign.Common.Applications;

namespace GestureSign.ControlPanel.Common
{

    public class ApplicationListViewItem
    {
        #region Public Properties

        // Add additional properties to the store window title, class, and filename
        public BitmapSource ApplicationIcon { get; set; }
        public string WindowTitle { get; set; }
        public string WindowClass { get; set; }
        public string WindowFilename { get; set; }
        public string ApplicationName { get; set; }

        #endregion

        #region Public Instance Methods

        public IApplication ToUserApplication(MatchUsing MatchUsing)
        {
            var userApplication = new UserApp();

            userApplication.Name = ApplicationName;
            userApplication.MatchUsing = MatchUsing;

            switch (MatchUsing)
            {
                case MatchUsing.WindowClass:
                    userApplication.MatchString = WindowClass;
                    break;
                case MatchUsing.WindowTitle:
                    userApplication.MatchString = WindowTitle;
                    break;
                case MatchUsing.ExecutableFilename:
                    userApplication.MatchString = WindowFilename;
                    break;
            }

            userApplication.IsRegEx = false;

            return userApplication;
        }

        #endregion
    }

}
