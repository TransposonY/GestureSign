using ManagedWinapi.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace GestureSign.Common.Applications
{
    public abstract class ApplicationBase : IApplication, INotifyCollectionChanged, IComparable, IComparable<ApplicationBase>
    {
        #region Private Instance Fields

        List<IAction> _Actions = new List<IAction>();

        #endregion

        #region IApplication Instance Properties
        public virtual string Name { get; set; }
        public virtual MatchUsing MatchUsing { get; set; }
        public virtual string MatchString { get; set; }
        public virtual bool IsRegEx { get; set; }
        [DefaultValue("")]
        public virtual string Group { get; set; }

        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.None)]
        public virtual IEnumerable<IAction> Actions
        {
            get { return _Actions.AsEnumerable(); }
            set { _Actions = value.ToList(); }
        }

        public virtual event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region Private Methods

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, object changedItem)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, changedItem));
        }

        #endregion

        #region IApplication Instance Methods

        public virtual void AddAction(IAction Action)
        {
            _Actions.Add(Action);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, Action);
        }

        public virtual void Insert(int index, IAction action)
        {
            _Actions.Insert(index, action);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, action);
        }

        public virtual void RemoveAction(IAction Action)
        {
            _Actions.Remove(Action);
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, Action);
        }

        public bool IsSystemWindowMatch(SystemWindow Window)
        {
            string compareMatchString = MatchString ?? String.Empty;
            string windowMatchString = String.Empty;
            try
            {
                switch (MatchUsing)
                {
                    case MatchUsing.WindowClass:
                        windowMatchString = Window.ClassName;

                        break;
                    case MatchUsing.WindowTitle:
                        windowMatchString = Window.Title;

                        break;
                    case MatchUsing.ExecutableFilename:
                        windowMatchString = Window.Process.MainModule.ModuleName;

                        break;
                    case MatchUsing.All:
                        return true;
                }

                return IsRegEx ? Regex.IsMatch(windowMatchString, compareMatchString, RegexOptions.Singleline | RegexOptions.IgnoreCase) : String.Equals(windowMatchString.Trim(), compareMatchString.Trim(), StringComparison.CurrentCultureIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public int CompareTo(object obj)
        {
            var item = obj as ApplicationBase;
            return CompareTo(item);
        }

        public int CompareTo(ApplicationBase item)
        {
            if (this is GlobalApp)
            {
                return -1;
            }
            else
            {
                if (item is GlobalApp)
                    return 1;
                return string.Compare(Name, item.Name, false, System.Globalization.CultureInfo.CurrentCulture);
            }
        }

        #endregion
    }
}
