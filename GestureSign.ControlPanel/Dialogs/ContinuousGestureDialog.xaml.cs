using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestureSign.Common.Applications;
using GestureSign.Common.Extensions;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.ViewModel;
using MahApps.Metro.Controls;

namespace GestureSign.ControlPanel.Dialogs
{
    /// <summary>
    /// Creates or edits a catalog entry and the actions bound to it. Nothing is applied until OK.
    /// </summary>
    public partial class ContinuousGestureDialog : MetroWindow
    {
        private static readonly ContinuousDirection[] Directions =
            { ContinuousDirection.Left, ContinuousDirection.Right, ContinuousDirection.Up, ContinuousDirection.Down };

        /// <summary>Entry being edited; null when creating a new one.</summary>
        private readonly ContinuousGesture _source;
        private readonly List<IAction> _initiallyBound;
        private readonly ObservableCollection<ActionBinding> _bindings = new ObservableCollection<ActionBinding>();
        private readonly bool _ready;
        /// <summary>The name still follows the motion (the user has not typed a name of their own).</summary>
        private bool _nameIsGenerated;
        private bool _updatingName;

        public ContinuousGestureDialog(ContinuousGesture source)
        {
            InitializeComponent();
            _source = source;

            Title = LocalizationProvider.Instance.GetTextValue(source == null ? "ContinuousGesture.Dialog.NewTitle" : "ContinuousGesture.Dialog.EditTitle");
            FingersSlider.Minimum = ContinuousGesture.MinContactCount;
            FingersSlider.Maximum = ContinuousGesture.MaxContactCount;
            DirectionListBox.ItemsSource = Directions.Select(d => new DirectionChoice(d)).ToList();
            BindingListBox.ItemsSource = _bindings;

            int contactCount;
            ContinuousDirection direction;
            if (source != null)
            {
                contactCount = source.ContactCount;
                direction = source.Direction;
            }
            else
                FindFreeMotion(out contactCount, out direction);

            FingersSlider.Value = contactCount;
            DirectionListBox.SelectedValue = direction;

            if (source != null)
            {
                NameTextBox.Text = source.Name;
                foreach (var app in ContinuousGestureItemProvider.GetBindableApplications().Where(a => a.Actions != null))
                {
                    foreach (var action in app.Actions.Where(a => a != null && a.ContinuousGestureName == source.Name))
                        _bindings.Add(new ActionBinding(app, action, GetBindingText(app, action)));
                }
            }
            _initiallyBound = _bindings.Select(b => b.Action).ToList();
            _nameIsGenerated = source == null || source.Name == GenerateName(contactCount, direction);

            _ready = true;
            UpdateMotion();

            AppComboBox.ItemsSource = ContinuousGestureItemProvider.GetBindableApplications();
            if (AppComboBox.Items.Count > 0)
                AppComboBox.SelectedIndex = 0;
        }

        /// <summary>The created or edited catalog entry once the dialog was confirmed.</summary>
        public ContinuousGesture Result { get; private set; }

        private int CurrentContactCount => (int)FingersSlider.Value;

        private ContinuousDirection CurrentDirection => (ContinuousDirection?)DirectionListBox.SelectedValue ?? ContinuousDirection.Left;

        #region Events

        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_ready || _updatingName) return;
            // Clearing the box hands the name back to the motion.
            _nameIsGenerated = string.IsNullOrWhiteSpace(NameTextBox.Text);
        }

        private void FingersSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_ready)
                UpdateMotion();
        }

        private void DirectionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // A direction is always required; undo Ctrl+click deselection.
            if (DirectionListBox.SelectedItem == null && e.RemovedItems.Count > 0)
            {
                DirectionListBox.SelectedItem = e.RemovedItems[0];
                return;
            }
            if (_ready)
                UpdateMotion();
        }

        private void AppComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshActionChoices();
        }

        private void ActionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddBindingButton.IsEnabled = ActionComboBox.SelectedItem != null;
        }

        private void AddBindingButton_Click(object sender, RoutedEventArgs e)
        {
            var choice = ActionComboBox.SelectedItem as ActionBinding;
            if (choice == null) return;

            var binding = new ActionBinding(choice.Application, choice.Action, GetBindingText(choice.Application, choice.Action));
            _bindings.Add(binding);
            BindingListBox.ScrollIntoView(binding);
            RefreshActionChoices();
        }

        private void RemoveBindingButton_Click(object sender, RoutedEventArgs e)
        {
            var binding = (sender as FrameworkElement)?.DataContext as ActionBinding;
            if (binding == null) return;

            _bindings.Remove(binding);
            RefreshActionChoices();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();
            int contactCount = CurrentContactCount;
            ContinuousDirection direction = CurrentDirection;
            var catalog = ContinuousGestureManager.Instance.ContinuousGestures;

            if (name.Length == 0)
            {
                ShowErrorMessage(LocalizationProvider.Instance.GetTextValue("ContinuousGesture.Messages.EmptyName"));
                return;
            }
            if (catalog.Any(g => !ReferenceEquals(g, _source) && g.Name == name))
            {
                ShowErrorMessage(string.Format(LocalizationProvider.Instance.GetTextValue("ContinuousGesture.Messages.DuplicateName"), name));
                return;
            }
            var conflict = FindMotionConflict(contactCount, direction);
            if (conflict != null)
            {
                ShowErrorMessage(string.Format(LocalizationProvider.Instance.GetTextValue("ContinuousGesture.Messages.DuplicateMotion"), conflict.Name));
                return;
            }

            var applications = ApplicationManager.Instance.Applications;
            ContinuousGesture entry = _source;
            if (entry == null)
            {
                entry = new ContinuousGesture(name, contactCount, direction);
                ContinuousGestureManager.Instance.Add(entry);
            }
            else
            {
                if (entry.Name != name)
                    applications.RenameContinuousGestures(new Dictionary<string, string> { { entry.Name, name } });
                entry.Name = name;
                entry.ContactCount = contactCount;
                entry.Direction = direction;
            }

            var bound = new HashSet<IAction>(_bindings.Select(b => b.Action));
            foreach (var action in _initiallyBound.Where(a => !bound.Contains(a)))
                action.ContinuousGestureName = null;
            foreach (var action in bound)
                action.ContinuousGestureName = entry.Name;

            ContinuousGestureManager.Instance.Save();
            ApplicationManager.Instance.SaveApplications();

            Result = entry;
            DialogResult = true;
        }

        #endregion

        #region Private Methods

        private void ShowErrorMessage(string message)
        {
            MessageFlyoutText.Text = message;
            MessageFlyout.Header = LocalizationProvider.Instance.GetTextValue("ContinuousGesture.Messages.InvalidTitle");
            MessageFlyout.IsOpen = true;
        }

        private void UpdateMotion()
        {
            int contactCount = CurrentContactCount;
            ContinuousDirection direction = CurrentDirection;

            MotionTextBlock.Text = ContinuousGestureItemProvider.GetMotionText(contactCount, direction);

            var conflict = FindMotionConflict(contactCount, direction);
            MotionConflictTextBlock.Text = conflict == null
                ? null
                : string.Format(LocalizationProvider.Instance.GetTextValue("ContinuousGesture.Messages.DuplicateMotion"), conflict.Name);
            MotionConflictTextBlock.Visibility = conflict == null ? Visibility.Collapsed : Visibility.Visible;

            if (_nameIsGenerated)
            {
                _updatingName = true;
                NameTextBox.Text = GenerateName(contactCount, direction);
                _updatingName = false;
            }
        }

        private IEnumerable<ContinuousGesture> OtherEntries()
        {
            return ContinuousGestureManager.Instance.ContinuousGestures.Where(g => !ReferenceEquals(g, _source));
        }

        private ContinuousGesture FindMotionConflict(int contactCount, ContinuousDirection direction)
        {
            return OtherEntries().FirstOrDefault(g => g.ContactCount == contactCount && g.Direction == direction);
        }

        private string GenerateName(int contactCount, ContinuousDirection direction)
        {
            return ContinuousGestureCatalog.CreateName(OtherEntries(), contactCount, direction);
        }

        /// <summary>First motion not yet in the catalog, starting at three fingers (two-finger swipes usually scroll).</summary>
        private void FindFreeMotion(out int contactCount, out ContinuousDirection direction)
        {
            var counts = Enumerable.Range(3, ContinuousGesture.MaxContactCount - 2).Concat(new[] { ContinuousGesture.MinContactCount });
            foreach (int count in counts)
            {
                foreach (var d in Directions)
                {
                    if (FindMotionConflict(count, d) == null)
                    {
                        contactCount = count;
                        direction = d;
                        return;
                    }
                }
            }
            contactCount = 3;
            direction = ContinuousDirection.Left;
        }

        private void RefreshActionChoices()
        {
            var app = AppComboBox.SelectedItem as IApplication;
            var pending = new HashSet<IAction>(_bindings.Select(b => b.Action));
            var choices = app?.Actions == null
                ? new List<ActionBinding>()
                : app.Actions.Where(a => a != null && !pending.Contains(a))
                    .Select(a => new ActionBinding(app, a, GetChoiceText(a)))
                    .ToList();

            ActionComboBox.ItemsSource = choices;
            ActionComboBox.SelectedIndex = choices.Count > 0 ? 0 : -1;
            AddBindingButton.IsEnabled = ActionComboBox.SelectedItem != null;
        }

        /// <summary>Name of the continuous gesture this action is bound to that OK would replace, or null.</summary>
        private string GetReplacedName(IAction action)
        {
            string current = action.ContinuousGestureName;
            if (string.IsNullOrEmpty(current) || (_source != null && current == _source.Name))
                return null;
            return current;
        }

        private string GetChoiceText(IAction action)
        {
            string text = ContinuousGestureItemProvider.GetActionName(action);
            string replaced = GetReplacedName(action);
            return replaced == null
                ? text
                : string.Format(LocalizationProvider.Instance.GetTextValue("ContinuousGesture.Dialog.CurrentlyBound"), text, replaced);
        }

        private string GetBindingText(IApplication app, IAction action)
        {
            return app.Name + " → " + GetChoiceText(action);
        }

        #endregion

        public sealed class ActionBinding
        {
            public ActionBinding(IApplication application, IAction action, string text)
            {
                Application = application;
                Action = action;
                Text = text;
            }

            public IApplication Application { get; }
            public IAction Action { get; }
            public string Text { get; }
        }

        public sealed class DirectionChoice
        {
            public DirectionChoice(ContinuousDirection direction)
            {
                Direction = direction;
                Text = ContinuousGestureItemProvider.GetDirectionText(direction);
                switch (direction)
                {
                    case ContinuousDirection.Left:
                        Angle = 270;
                        break;
                    case ContinuousDirection.Right:
                        Angle = 90;
                        break;
                    case ContinuousDirection.Down:
                        Angle = 180;
                        break;
                    default:
                        Angle = 0;
                        break;
                }
            }

            public ContinuousDirection Direction { get; }
            public string Text { get; }
            public double Angle { get; }
        }
    }
}
