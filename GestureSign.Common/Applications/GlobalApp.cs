using GestureSign.Common.Localization;

namespace GestureSign.Common.Applications
{
    public class GlobalApp : ApplicationBase
    {
        #region IApplication Properties

        public override string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("Common.GlobalActions"); ; }
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
