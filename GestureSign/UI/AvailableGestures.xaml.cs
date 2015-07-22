using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.Gestures;
using GestureSign.UI.Common;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;

namespace GestureSign.UI
{
    /// <summary>
    /// AvailableGestures.xaml 的交互逻辑
    /// </summary>
    public partial class AvailableGestures : UserControl
    {

        public static event EventHandler GestureChanged;


        public AvailableGestures()
        {
            InitializeComponent();
            GestureDefinition.GesturesChanged += GestureDefinition_GesturesChanged;

            if (GestureManager.Instance.FinishedLoading) BindGestures();
            GestureManager.Instance.OnLoadGesturesCompleted += (o, e) => { this.Dispatcher.Invoke(BindGestures); };
        }


        void GestureDefinition_GesturesChanged(object sender, EventArgs e)
        {
            BindGestures();
        }

        private void lstAvailableGestures_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.btnEditGesture.IsEnabled = this.btnDelGesture.IsEnabled = lstAvailableGestures.SelectedItems.Count > 0;
        }

        private async void btnDelGesture_Click(object sender, RoutedEventArgs e)
        {
            // Make sure at least one item is selected
            if (lstAvailableGestures.SelectedItems.Count == 0) return;
            if (await UIHelper.GetParentWindow(this)
                    .ShowMessageAsync(
                        LocalizationProvider.Instance.GetTextValue("Gesture.Messages.DeleteConfirmTitle"),
                        LocalizationProvider.Instance.GetTextValue("Gesture.Messages.DeleteGestureConfirm"),
                        MessageDialogStyle.AffirmativeAndNegative,
                        new MetroDialogSettings()
                        {
                            AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK"),
                            NegativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.Cancel"),
                            AnimateHide = false
                        }) == MessageDialogResult.Affirmative)
            {
                foreach (GestureItem listItem in lstAvailableGestures.SelectedItems)
                    GestureManager.Instance.DeleteGesture(listItem.Name);
                if (GestureChanged != null)
                    GestureChanged(this, new EventArgs());

                BindGestures();
                GestureManager.Instance.SaveGestures();
            }
        }
        private void btnEditGesture_Click(object sender, RoutedEventArgs e)
        {
            // Make sure at least one item is selected
            if (lstAvailableGestures.SelectedItems.Count == 0) return;

            GestureDefinition gd = new GestureDefinition(
                GestureManager.Instance.GetNewestGestureSample(((GestureItem)lstAvailableGestures.SelectedItems[0]).Name).Points,
                ((GestureItem)lstAvailableGestures.SelectedItems[0]).Name, true);
            gd.ShowDialog();
        }

        private async void btnAddGesture_Click(object sender, RoutedEventArgs e)
        {
            if (await UIHelper.GetParentWindow(this).ShowMessageAsync(
                LocalizationProvider.Instance.GetTextValue("Gesture.Messages.AddGestureTitle"),
                LocalizationProvider.Instance.GetTextValue("Gesture.Messages.AddGesture"),
                MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings()
                {
                    AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK"),
                    NegativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.Cancel")
                }) == MessageDialogResult.Affirmative)
            {
                NamedPipe.SendMessageAsync("StartTeaching", "GestureSignDaemon");
            }
        }
        private void ImportGestureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofdGestures = new OpenFileDialog()
            {
                Filter = LocalizationProvider.Instance.GetTextValue("Gesture.GestureFile") + "|*.json;*.gest",
                Title = LocalizationProvider.Instance.GetTextValue("Gesture.ImportGesture"),
                CheckFileExists = true
            };
            if (ofdGestures.ShowDialog().Value)
            {
                int addcount = 0;
                List<IGesture> newGestures;
                if (Path.GetExtension(ofdGestures.FileName)
                    .Equals(".gest", StringComparison.OrdinalIgnoreCase))
                {
                    var gestures =
                        FileManager.LoadObject<List<Gesture>>(ofdGestures.FileName, false);
                    newGestures = gestures == null ? null : gestures.Cast<IGesture>().ToList();
                }
                else newGestures = FileManager.LoadObject<List<IGesture>>(ofdGestures.FileName, new Type[] { typeof(Gesture) }, false);

                if (newGestures != null)
                    foreach (IGesture newGesture in newGestures)
                    {
                        if (GestureManager.Instance.GestureExists(newGesture.Name))
                        {
                            var result =
                                MessageBox.Show(
                                    String.Format(
                                        LocalizationProvider.Instance.GetTextValue("Gesture.Messages.ReplaceConfirm"),
                                        newGesture.Name),
                                    LocalizationProvider.Instance.GetTextValue("Gesture.Messages.ReplaceConfirmTitle"),
                                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                            if (result == MessageBoxResult.Yes)
                            {
                                GestureManager.Instance.DeleteGesture(newGesture.Name);
                                GestureManager.Instance.AddGesture(newGesture);
                                addcount++;
                            }
                            else if (result == MessageBoxResult.Cancel) goto End;
                        }
                        else
                        {
                            GestureManager.Instance.AddGesture(newGesture);
                            addcount++;
                        }
                    }
            End:
                if (addcount != 0)
                {
                    GestureManager.Instance.SaveGestures();
                    BindGestures();
                }
                MessageBox.Show(
                    String.Format(LocalizationProvider.Instance.GetTextValue("Gesture.Messages.ImportComplete"),
                        addcount), LocalizationProvider.Instance.GetTextValue("Gesture.Messages.ImportCompleteTitle"));
            }
        }

        private void ExportGestureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfdGestures = new SaveFileDialog()
            {
                Filter = LocalizationProvider.Instance.GetTextValue("Gesture.GestureFile") + "|*.gest",
                Title = LocalizationProvider.Instance.GetTextValue("Gesture.ExportGestures"),
                AddExtension = true,
                DefaultExt = "gest",
                ValidateNames = true
            };
            if (sfdGestures.ShowDialog().Value)
            {
                FileManager.SaveObject(GestureManager.Instance.Gestures, sfdGestures.FileName);
            }
        }

        #region Private Methods

        private void BindGestures()
        {
            // Clear existing gestures in list
            lstAvailableGestures.Items.Clear();
            Task task = new Task(AddAvailableGesturesItems);
            task.Start();
        }

        private void AddAvailableGesturesItems()
        {
            // Get all available gestures from gesture manager
            IEnumerable<IGesture> results = GestureManager.Instance.Gestures.OrderBy(g => g.Name);//.GroupBy(g => g.Name).Select(g => g.First().Name);
            Thread.Sleep(300);
            var brush = Application.Current.Resources["HighlightBrush"] as Brush ?? Brushes.RoyalBlue;

            foreach (IGesture gesture in results)
            {
                lstAvailableGestures.Dispatcher.BeginInvoke(new Action(() =>
                 {  // Create new listviewitem to represent gestures, create a thumbnail of the latest version of each gesture
                     // and add it to image list, then to the output list      gestureName
                     GestureItem newItem = new GestureItem()
                     {
                         Image = GestureImage.CreateImage(gesture.Points, new Size(65, 65), brush),
                         Name = gesture.Name
                     };
                     lstAvailableGestures.Items.Add(newItem);
                 }));
            }
        }

        #endregion






    }
}
