using GestureSign.Common.Localization;

namespace GestureSign.Common.Applications
{
    public class GlobalApplication : ApplicationBase
    {
        #region IApplication Properties

        public override string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("Common.AllApplications"); ; }
            //set { /* Set only exists for deserialization purposes */ }
        }

        public override MatchUsing MatchUsing
        {
            get { return MatchUsing.All; }
            //	set { /* Set only exists for deserialization purposes */ }
        }

        #endregion
    }
}
