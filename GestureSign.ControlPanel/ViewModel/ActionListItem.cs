using GestureSign.Common.Applications;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestureSign.ControlPanel.ViewModel
{
    public class ActionListItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        public IAction Action { get; set; }
        public string Info { get; set; }
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set { SetProperty(ref _isSelected, value); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ActionListItem(IAction action, string info)
        {
            Action = action;
            Info = info;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return;

            storage = value;
            this.OnPropertyChanged(propertyName);
        }
    }
}
