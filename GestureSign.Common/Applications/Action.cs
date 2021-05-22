using GestureSign.Common.Input;
using ManagedWinapi;
using ManagedWinapi.Hooks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace GestureSign.Common.Applications
{
    public class Action : IAction, ICloneable, INotifyCollectionChanged
    {
        #region Private Variable

        private List<ICommand> _commands;

        #endregion

        #region Public Properties

        public string Name { get; set; }

        public string GestureName { get; set; }

        [DefaultValue("")]
        public string Condition { get; set; }

        public bool? ActivateWindow { get; set; }

        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.None)]
        public IEnumerable<ICommand> Commands
        {
            get
            {
                CheckInitialization();
                return _commands.AsEnumerable();
            }
            set
            {
                _commands = value.ToList();
            }
        }

        public Hotkey Hotkey { get; set; }

        public MouseActions MouseHotkey { get; set; }
        public ContinuousGesture ContinuousGesture { get; set; }
        public Devices IgnoredDevices { get; set; }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region Private Methods

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        private void CheckInitialization()
        {
            if (_commands == null)
                _commands = new List<ICommand>();
        }

        #endregion

        #region Public Methods

        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// make a deep copy of Action except events
        /// </summary>
        public Action DeepCopy()
        {
            Action action = (Action)MemberwiseClone();
            action.CollectionChanged = null;

            action.Name = Name == null ? null : string.Copy(Name);
            action.GestureName = GestureName == null ? null : string.Copy(GestureName);
            action.Condition = Condition == null ? null : string.Copy(Condition);
            action._commands = new List<ICommand>(_commands);
            action.Hotkey = Hotkey == null ? null : new Hotkey() { KeyCode = Hotkey.KeyCode, ModifierKeys = Hotkey.ModifierKeys };
            action.ContinuousGesture = ContinuousGesture == null ? null : new ContinuousGesture(ContinuousGesture.ContactCount, ContinuousGesture.Gesture);

            return action;
        }

        public void AddCommand(ICommand command)
        {
            CheckInitialization();
            _commands.Add(command);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, command));
        }

        public void InsertCommand(int index, ICommand command)
        {
            CheckInitialization();
            _commands.Insert(index, command);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, command, index));
        }

        public void RemoveCommand(ICommand command)
        {
            CheckInitialization();
            _commands.Remove(command);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, command));
        }

        public void MoveCommand(int oldIndex, int newIndex)
        {
            CheckInitialization();
            var command = _commands[oldIndex];
            _commands.RemoveAt(oldIndex);
            _commands.Insert(newIndex, command);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, command, newIndex, oldIndex));
        }

        public bool IsEmpty()
        {
            return _commands?.Count == 0;
        }

        #endregion
    }

    public class ActionConverter : CustomCreationConverter<IAction>
    {
        public override IAction Create(Type objectType)
        {
            return new Action();
        }
    }
}
