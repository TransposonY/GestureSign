using System.Collections.Generic;

namespace GestureSign.Common.Applications
{
    public abstract class LegacyApplicationBase
    {
        #region IApplication Instance Properties

        public virtual string Name { get; set; }
        public virtual MatchUsing MatchUsing { get; set; }
        public virtual string MatchString { get; set; }
        public virtual bool IsRegEx { get; set; }
        public virtual string Group { get; set; }
        public virtual List<GestureSign.Applications.Action> Actions { get; set; } = new List<GestureSign.Applications.Action>();

        #endregion
    }
}
