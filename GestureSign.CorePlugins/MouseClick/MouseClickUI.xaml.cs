using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GestureSign.CorePlugins.MouseClick
{
    /// <summary>
    /// MouseClickUI.xaml 的交互逻辑
    /// </summary>
    public partial class MouseClickUI : UserControl
    {
        #region Private Variables

        MouseClickSettings _settings;
        //   IHostControl _HostControl = null;

        #endregion
        public MouseClickUI()
        {
            InitializeComponent();
        }

        public MouseClickSettings Settings
        {
            get
            {
                _settings = new MouseClickSettings
                {
                    ClickPosition = (ClickPositions)PositionComboBox.SelectedValue,
                    MouseButtonAction = (MouseButtonActions)ActionComboBox.SelectedValue

                };
                return _settings;
            }
            set
            {
                _settings = value ?? new MouseClickSettings();
                ActionComboBox.SelectedValue = _settings.MouseButtonAction;
                PositionComboBox.SelectedValue = _settings.ClickPosition;
            }
        }
    }

}
