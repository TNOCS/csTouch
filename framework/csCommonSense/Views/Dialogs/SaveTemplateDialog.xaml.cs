using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using csCommon.Types.DataServer.PoI.Templates;
using csCommon.Utils.IO;
using csShared;

namespace csCommon.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for SaveTemplateDialog.xaml
    /// </summary>
    public partial class SaveTemplateDialog : Window
    {
        // STATIC PART ************************************************************************************************

        private static SaveTemplateDialog _instance;

        /// <summary>
        /// Show a template selector/save dialog box.
        /// </summary>
        /// <typeparam name="T">The type of template.</typeparam>
        /// <param name="templates">Current templates, ordered by the current object ids they are assigned to.</param>
        /// <returns></returns>
        public static Dictionary<T, string> ShowDialog<T>(Dictionary<T, string> templates) where T : class, ITemplateObject, new() 
        {
            if (_instance == null)
            {
                _instance = new SaveTemplateDialog();
            }

            FileLocation templateFileRoot = new FileLocation(AppStateSettings.TemplateFolder);
            TemplateStore<T> templateStore = new TemplateStore<T>(templateFileRoot);

            TemplateProcessor<T> templateProcessor = new TemplateProcessor<T>(templates, templateStore);
            _instance.TemplateProcessorInUse = templateProcessor;

            IEnumerable<string> templateCollectionNames = templateStore.GetTemplateCollectionNames(); // Which templates do we know?
            _instance.Labels = templates.Keys.Select(templateObject => templateObject.Id).ToList();
            if (!templateCollectionNames.Any())
            {
                _instance.RadioExistingTemplate.IsEnabled = false;
                _instance.RadioNewTemplate.IsChecked = true;
                _instance.ComboExistingTemplate.IsEnabled = false;
                _instance.TextNewTemplate.IsEnabled = true;
                FocusManager.SetFocusedElement(_instance, _instance.TextNewTemplate);
            }
            else
            {
                _instance.ComboExistingTemplate.ItemsSource = templateCollectionNames;
                _instance.RadioExistingTemplate.IsEnabled = true;
                _instance.RadioExistingTemplate.IsChecked = true;
                _instance.ComboExistingTemplate.IsEnabled = true;
                _instance.ComboExistingTemplate.SelectedIndex = 0;
                _instance.TextNewTemplate.IsEnabled = false;
                FocusManager.SetFocusedElement(_instance, _instance.ComboExistingTemplate);
            }

            _instance.ShowDialog(); // Modal.

            if (templateProcessor.SelectedTemplates == null || !templateProcessor.SelectedTemplates.Any())
            {
                return new Dictionary<T, string>();
            }

            return templateProcessor.SelectedTemplates;
        }

        // INSTANCE PART **********************************************************************************************

        private TemplateProcessorBase TemplateProcessorInUse { get; set; }

        private SaveTemplateDialog()
        {
            InitializeComponent();
            this.SourceInitialized += (x, y) => this.HideMinimizeAndMaximizeButtons();
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            string templateName = (RadioCurrentTemplate.IsChecked ?? false) 
                ? null 
                : ((RadioExistingTemplate.IsChecked ?? false)
                    ? ComboExistingTemplate.SelectedItem
                    : TextNewTemplate.Text).ToString();

            if (string.IsNullOrEmpty(templateName) && (!(RadioCurrentTemplate.IsChecked ?? false)))
            {
                MessageBox.Show(this, "Please select an existing template or enter a new template name!", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                TemplateProcessorInUse.UpdateTemplates(LabelsList, templateName);
                MessageBox.Show(this, "Templates were saved!", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception er)
            {
                MessageBox.Show(this, "Could not save templates:\n" + er.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }                

            Hide();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private IEnumerable<string> labels;
        private IEnumerable<string> Labels
        {
            get { return labels; }
            set
            {
                if (value == labels) return;
                labels = value;
                LabelsList.ItemsSource = labels;
            }
        }

        private void SelectNoneButton_OnClick(object sender, RoutedEventArgs e)
        {
            LabelsList.SelectedIndex = -1;
        }

        private void SelectAllButton_OnClick(object sender, RoutedEventArgs e)
        {
            LabelsList.SelectAll();
        }

        private void RadioExistingNewTemplate_OnClick(object sender, RoutedEventArgs e)
        {
            if (RadioCurrentTemplate.IsChecked ?? false)
            {
                ComboExistingTemplate.IsEnabled = false;
                TextNewTemplate.IsEnabled = false;                
            }
            else if (RadioExistingTemplate.IsChecked ?? false)
            {
                ComboExistingTemplate.IsEnabled = true;
                TextNewTemplate.IsEnabled = false;
            }
            else
            {
                ComboExistingTemplate.IsEnabled = false;
                TextNewTemplate.IsEnabled = true;                
            }
        }

        private void OpenTemplateFolderButton_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(AppStateSettings.TemplateFolder);
        }

        // Keep input and output in the same generic scope. 
        // Windows cannot have generic parameters, so a rather ugly solution, involving a non-generic superclass 
        // and a generic implementation that is unknown to the instance, but it is working.
        private abstract class TemplateProcessorBase
        {
            public abstract void UpdateTemplates(ListBox selectedLabelsList, string templateName = null);
        }

        private class TemplateProcessor<T> : TemplateProcessorBase where T : class, ITemplateObject, new() 
        {
            private readonly Dictionary<T, string> _templateObjects;
            private readonly TemplateStore<T> _templateStore;

            public Dictionary<T, string> SelectedTemplates { get; set; }

            public TemplateProcessor(Dictionary<T, string> templateObjects, TemplateStore<T> templateStore)
            {
                _templateObjects = templateObjects;
                _templateStore = templateStore;
            }

            public override void UpdateTemplates(ListBox selectedLabelsList, string templateName = null)
            {
                SelectedTemplates = new Dictionary<T, string>();
                foreach (var selectedItem in selectedLabelsList.SelectedItems)
                {
                    KeyValuePair<T, string> first = _templateObjects.First(keyValuePair => keyValuePair.Key.Id == selectedItem.ToString());
                    SelectedTemplates[first.Key] = string.IsNullOrEmpty(templateName) ? first.Value : templateName;
                }
                _templateStore.UpdateTemplates(SelectedTemplates);
            }
        }
    }

    internal static class WindowExtensions
    {
        // from winuser.h
        private const int GWL_STYLE = -16,
                          WS_MAXIMIZEBOX = 0x10000,
                          WS_MINIMIZEBOX = 0x20000;

        [DllImport("user32.dll")]
        extern private static int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        extern private static int SetWindowLong(IntPtr hwnd, int index, int value);

        internal static void HideMinimizeAndMaximizeButtons(this Window window)
        {
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            var currentStyle = GetWindowLong(hwnd, GWL_STYLE);

            SetWindowLong(hwnd, GWL_STYLE, (currentStyle & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX));
        }
    }
}
