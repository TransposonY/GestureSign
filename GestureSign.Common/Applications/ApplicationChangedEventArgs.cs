using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GestureSign.Common.Applications
{
    public class ApplicationChangedEventArgs : EventArgs
    {
        #region Constructors

        public ApplicationChangedEventArgs()
        {

        }

        public ApplicationChangedEventArgs(IEnumerable<IApplication> applications)
        {
            this.Applications = applications;
        }

        #endregion

        #region Public Properties

        public IEnumerable<IApplication> Applications { get; set; }

        #endregion
    }
}
