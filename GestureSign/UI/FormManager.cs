using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GestureSign.Common;
using GestureSign.Common.UI;

using System.Windows;

namespace GestureSign.UI
{
    public class FormManager : ILoadable, IFormManager
    {
        #region Private Variables

        static Dictionary<Type, Window> _AvailableForms = new Dictionary<Type, Window>();

        // Create variable to hold the only allowed instance of this class
        static readonly FormManager _Instance = new FormManager();

        #endregion

        #region Constructors

        protected FormManager()
        {

        }

        #endregion

        #region Public Properties

        public static FormManager Instance
        {
            get { return _Instance; }
        }

        #endregion

        #region Custom Events

        // Create event to notify subscribers that an instance was requested
        public event InstanceEventHandler InstanceRequested;

        protected virtual void OnInstanceRequested(InstanceEventArgs e)
        {
            if (InstanceRequested != null) InstanceRequested(this, e);
        }

        #endregion

        #region Strongly Typed Form Variables
        
        //public ApplicationDialog ApplicationDialog
        //{
        //    get { return GetInstance<ApplicationDialog>(); }
        //}

        public MainWindow MainWindow
        {
            get { return GetInstance<MainWindow>(); }
        }


        #endregion

        #region Private Methods

        private T GetInstance<T>() where T : Window, new()
        {
            if (!_AvailableForms.ContainsKey(typeof(T)))
            {
                T newInstance = new T();
                newInstance.Closed += (sender, e) =>
                    {
                        _AvailableForms.Remove(sender.GetType());
                    };

                _AvailableForms.Add(typeof(T), newInstance);

                // Fire instance requested event
                OnInstanceRequested(new InstanceEventArgs(newInstance));
            }

            return (T)_AvailableForms[typeof(T)];
        }

        private void SetInstance<T>(T formInstance) where T : Window, new()
        {
            if (!_AvailableForms.Contains(new KeyValuePair<Type, Window>(typeof(T), formInstance)))
            {
                formInstance.Closed += (sender, e) =>
                    {
                        _AvailableForms.Remove(sender.GetType());
                    };

                _AvailableForms.Add(typeof(T), formInstance);
            }
        }

        #endregion

        #region Public Methods

        public void Load()
        {
            // Preinstantiate surface and gesture definition form
            //	SetInstance<Surface>(new Surface());
           // SetInstance<GestureDefinition>(new GestureDefinition());
        }

        #endregion

        #region IFormManager Form Instances

        // We have to use the Form base class to represent our
        // form instances because of inteface limitations


        #endregion
    }
}
