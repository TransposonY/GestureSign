using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GestureSign.Common.Applications;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace GestureSign.UI
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
            UserApplication userApplication = new CustomApplication();

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
