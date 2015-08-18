using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestureSign.ControlPanel.Common
{
    public class ActionInfo : INotifyPropertyChanged
    {

        public ActionInfo(string actionName, string description, string gestureName, bool isEnabled)
        {
            IsEnabled = isEnabled;
            ActionName = actionName;
            Description = description;
            GestureName = gestureName;
        }
        private bool _isEnabled;
        private string _gestureName;

        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }

            set { SetProperty(ref _isEnabled, value); }
        }

        public string GestureName
        {
            get { return _gestureName; }
            set { SetProperty(ref _gestureName, value); }
        }

        public string ActionName { get; set; }

        public string Description { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
        {
            if (Equals(storage, value)) return;

            storage = value;
            this.OnPropertyChanged(propertyName);
        }
    }
}