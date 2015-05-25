using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace GestureSign.CorePlugins.ActivateWindow
{
    [DataContract]
    public class ActivateWindowSettings
    {
        #region Public Properties

        [DataMember]
        public string ClassName { get; set; }
        [DataMember]
        public string Caption { get; set; }
        [DataMember]
        public bool IsRegEx { get; set; }
        [DataMember]
        public int Timeout { get; set; }
        #endregion
    }
}
