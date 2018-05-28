using GestureSign.Common.Applications;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GestureSign.ControlPanel.ViewModel
{
    public class AppListItem : INotifyPropertyChanged, IComparable, IComparable<AppListItem>
    {
        private bool? _isSelected;

        public IApplication Application { get; set; }
        public List<ActionListItem> ActionItemList { get; set; }
        public bool? IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (value.HasValue && ActionItemList != null)
                {
                    ActionItemList.ForEach(ali => ali.IsSelected = value.Value);
                }
                SetProperty(ref _isSelected, value);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public AppListItem(IApplication application, string info, bool isSeleted = false)
        {
            Application = application;
            if (application?.Actions != null)
                ActionItemList = application.Actions.Select(a => new ActionListItem(a, info)).ToList();
            _isSelected = isSeleted;
        }

        public int CompareTo(object obj)
        {
            var item = obj as AppListItem;
            return CompareTo(item);
        }

        public int CompareTo(AppListItem item)
        {
            if (Application is GlobalApp)
            {
                return -1;
            }
            else
            {
                if (item.Application is GlobalApp)
                    return 1;
                return string.Compare(Application.Name, item.Application.Name, false, System.Globalization.CultureInfo.CurrentCulture);
            }
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
