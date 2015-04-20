using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestureSign.Common.Applications
{
    public class ActionChangedEventArgs : EventArgs
    {
        #region Constructors

        public ActionChangedEventArgs()
        {

        }

        public ActionChangedEventArgs(IApplication application, IAction action)
        {
            Application = application;
            Action = action;
        }

        #endregion

        #region Public Properties

        public IApplication Application { get; set; }
        public IAction Action { get; set; }
        #endregion
    }
}
