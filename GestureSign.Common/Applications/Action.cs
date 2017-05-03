using System;
using System.Collections.Generic;
using System.ComponentModel;
using ManagedWinapi;
using ManagedWinapi.Hooks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GestureSign.Common.Applications
{
    public class Action : IAction
    {
        #region Public Properties

        public string Name { get; set; }

        public string GestureName { get; set; }

        [DefaultValue("")]
        public string Condition { get; set; }

        public bool? ActivateWindow { get; set; }

        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.None)]
        public List<ICommand> Commands { get; set; }

        public Hotkey Hotkey { get; set; }

        public MouseActions MouseAction { get; set; }

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
