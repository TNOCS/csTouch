using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Diagnostics;
using SessionsEvents;
using Microsoft.Surface.Presentation.Controls;
using csCommon;
using System.Windows.Shapes;

namespace csUSDomainPlugin
{
    public partial class USDomainView : UserControl
    {
        public USDomainView()
        {
            InitializeComponent();
        }

        private USDomainPlugin fPlugin;
        public USDomainPlugin Plugin
        {
            get { return fPlugin; }
            set
            {
                fPlugin = value;
                UpdateSessionList();
                Plugin.DomainImageOpacity = OpacitySlider.Value;
                Plugin.Follow = FollowChk.IsChecked.Value;
            }
        }

        private void Sessions_Expanded(object sender, RoutedEventArgs e)
        {
            SessionsVisible = true;
            e.Handled = true;
        }

        private void Sessions_Collapsed(object sender, RoutedEventArgs e)
        {
            SessionsVisible = false;
            e.Handled = true;
        }

        private void Sessions_TouchDown(object sender, TouchEventArgs e)
        {
            SessionsVisible = !SessionsVisible;
            e.Handled = true;
        }

        private void Sessions_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SessionsVisible = !SessionsVisible;
            e.Handled = true;
        }

        public bool SessionsVisible
        {
            get { return SessionsRow.ActualHeight != 0; }
            set
            {
                if (value)
                {
                    if (!SessionsVisible)
                    {
                        SessionsRow.Height = new GridLength(1.0, GridUnitType.Star);
                        DomainsRow.Height = new GridLength(0.0, GridUnitType.Pixel);
                        SessionsExpander.IsExpanded = true;
                    }
                }
                else
                {
                    if (SessionsVisible)
                    {
                        SessionsRow.Height = new GridLength(0.0, GridUnitType.Pixel);
                        DomainsRow.Height = new GridLength(1.0, GridUnitType.Star);
                        SessionsExpander.IsExpanded = false;
                    }
                }
            }
        }

        public void UpdateSessionList()
        {
            // check sessions for new or changed items
            lock (Plugin.sessions)
            {
                foreach (SessionNewEvent session in Plugin.sessions.Values)
                {
                    bool found = false;
                    for (int i = 0; i < SessionsListBox.Items.Count; i++)
                    {
                        if ((int)(SessionsListBox.Items[i] as SurfaceListBoxItem).Tag == session.sessionID)
                        {
                            (SessionsListBox.Items[i] as SurfaceListBoxItem).Content = session.GenerateUI();
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        SurfaceListBoxItem slbi = new SurfaceListBoxItem();
                        slbi.Content = session.GenerateUI();
                        slbi.Tag = session.sessionID;
                        slbi.Selected += new RoutedEventHandler(Session_Selected);
                        SessionsListBox.Items.Add(slbi);
                    }
                }
                // check list box for deleted items
                for (int i = SessionsListBox.Items.Count - 1; i >= 0; i--)
                {
                    if (!Plugin.sessions.ContainsKey((int)(SessionsListBox.Items[i] as SurfaceListBoxItem).Tag))
                        SessionsListBox.Items.RemoveAt(i);
                }
                if (Plugin.sessions.Count == 0)
                    CurrentSession.Text = "NO sessions";
                else
                {
                    if ("NO sessions".CompareTo(CurrentSession.Text) == 0)
                        CurrentSession.Text = "<select session>";
                }
            }
        }

        public void UnselectDomain()
        {
            //LayersListBox.SelectedItem = null;
            LayersListBox.UnselectAll();
        }

        public void UpdateDomainList()
        {
            // check domains for new or changed items
            lock (Plugin.domains)
            {
                foreach (DomainNewEvent domain in Plugin.domains.Values)
                {
                    bool found = false;
                    foreach (SurfaceListBoxItem lbi in LayersListBox.Items)
                    {
                        if ((string)lbi.Tag == domain.domain)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        // create here because of image thread ownership problems
                        domain.dynamicImage = new DynamicImage(this,
                            Plugin.Connection.Subscribe(domain.eventName, false),
                            Plugin.Connection.Subscribe(Plugin.privateEvent.EventName + "." + domain.domain, false),
                            domain.cols, domain.rows);
                                    
                        Image im = new Image();
                        im.Source = domain.dynamicImage.ImageSrc;
                        im.Stretch = Stretch.Uniform;
                        im.Height = 100;
                        im.Tag = domain.domain;
                        im.IsHitTestVisible = false;
                        
                        Label lbl = new Label();
                        lbl.Content = domain.domain;
                        lbl.FontSize = 10;
                        lbl.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;

                        Path pathZoom = new Path();
                        pathZoom.Data = ZoomPath.Data;
                        pathZoom.Stretch = ZoomPath.Stretch;
                        pathZoom.Fill = ZoomPath.Fill;
                        pathZoom.Width = ZoomPath.Width;
                        pathZoom.Height = ZoomPath.Height;
                        pathZoom.Margin = ZoomPath.Margin;
                        pathZoom.RenderTransformOrigin = ZoomPath.RenderTransformOrigin;
                        pathZoom.IsHitTestVisible = false;

                        SurfaceButton sbZoom = new SurfaceButton();
                        sbZoom.Height = 15;
                        sbZoom.Width = 15;
                        sbZoom.Tag = domain.domain;
                        sbZoom.Background = Brushes.Transparent;
                        sbZoom.Content = pathZoom;
                        sbZoom.Click += sbZoom_Click;

                        Path pathLegend = new Path();
                        pathLegend.Data = LegendPath.Data;
                        pathLegend.Stretch = LegendPath.Stretch;
                        pathLegend.Fill = LegendPath.Fill;
                        pathLegend.Width = LegendPath.Width;
                        pathLegend.Height = LegendPath.Height;
                        pathLegend.Margin = LegendPath.Margin;
                        pathLegend.RenderTransformOrigin = LegendPath.RenderTransformOrigin;
                        pathLegend.IsHitTestVisible = false;

                        SurfaceButton sbLegend = new SurfaceButton();
                        sbLegend.Height = 15;
                        sbLegend.Width = 15;
                        sbLegend.Tag = domain.domain;
                        sbLegend.Background = Brushes.Transparent;
                        sbLegend.Content = pathLegend;
                        sbLegend.Click += sbLegend_Click;

                        SurfaceButton sbPlus = new SurfaceButton();
                        sbPlus.Height = 15;
                        sbPlus.Width = 15;
                        sbPlus.Tag = domain.domain;
                        sbPlus.Background = Brushes.Transparent;
                        //sbPlus.Content = pathPlus;
                        sbPlus.Click += sbPlus_Click;

                        StackPanel spButtons = new StackPanel();
                        spButtons.Orientation = Orientation.Vertical;
                        spButtons.Children.Add(sbZoom);
                        spButtons.Children.Add(sbLegend);
                        spButtons.Children.Add(sbPlus);

                        StackPanel spImage = new StackPanel();
                        spImage.Orientation = Orientation.Vertical;
                        spImage.Tag = domain.domain;
                        spImage.Children.Add(lbl);
                        spImage.Children.Add(im);

                        StackPanel sp = new StackPanel();
                        sp.Orientation = Orientation.Horizontal;
                        sp.Tag = domain.domain;
                        //sp.Background = Brushes.LightBlue;
                        sp.Children.Add(spButtons);
                        sp.Children.Add(spImage);
                        
                        SurfaceListBoxItem lbi = new SurfaceListBoxItem();
                        lbi.Tag = domain.domain;
                        lbi.Content = sp;
                        lbi.Selected += new RoutedEventHandler(Domain_Selected);
                        
                        lbi.Background = Brushes.LightBlue;
                        lbi.Width = LayersListBox.Width;
                        
                        LayersListBox.Items.Add(lbi);
                    }
                }

                // remove domains
                for (int i = LayersListBox.Items.Count - 1; i >= 0; i--)
                {
                    string key = (string)(LayersListBox.Items[i] as SurfaceListBoxItem).Tag;
                    if (!Plugin.domains.ContainsKey(key))
                        LayersListBox.Items.RemoveAt(i);
                }

            }
        }

        void sbPlus_Click(object sender, RoutedEventArgs e)
        {
            string selectedDomain;
            if (LayersListBox.SelectedItem != null)
                selectedDomain = (string)(LayersListBox.SelectedItem as SurfaceListBoxItem).Tag;
            else
                selectedDomain = "";
            string imageDomain = (string)((SurfaceButton)e.Source).Tag;
            /*
            if (imageDomain.CompareTo(selectedDomain) == 0)
                Plugin.SetDomainVisibile(imageDomain, OpacitySlider.Value);
            else
                Plugin.ToggleDomainVisibility(imageDomain, OpacitySlider.Value);
             */
            Plugin.ToggleDomainVisibility(imageDomain, OpacitySlider.Value);
            e.Handled = true;
        }

        void sbZoom_Click(object sender, RoutedEventArgs e)
        {
            string domain = (sender as SurfaceButton).Tag as string;
            Plugin.FocusLayer(domain, OpacitySlider.Value);
            // show listbox item as selected
            foreach (SurfaceListBoxItem sli in LayersListBox.Items)
            {
                if (domain.CompareTo((string)sli.Tag) == 0 && LayersListBox.SelectedItem != sli)
                {
                    LayersListBox.SelectedItem = sli;
                    Plugin.SelectDomain(domain, OpacitySlider.Value, true);
                    break;
                }
            }
            e.Handled = true;
        }

        void sbLegend_Click(object sender, RoutedEventArgs e)
        {
            string domain = (sender as SurfaceButton).Tag as string;
            Plugin.ShowLegend(domain);
            e.Handled = true;
        }

        private void Domain_Selected(object sender, RoutedEventArgs e)
        {
            string domain = (sender as SurfaceListBoxItem).Tag as string;
            Plugin.SelectDomain(domain, OpacitySlider.Value, true);
        }

        void Session_Selected(object sender, RoutedEventArgs e)
        {
            SessionsVisible = false;
            CurrentSession.Text = Plugin.SelectSession((int)(e.Source as SurfaceListBoxItem).Tag);
        }

        void im_TouchDown(object sender, TouchEventArgs e)
        {
            if (Plugin != null)
            {
                string selectedDomain;
                if (LayersListBox.SelectedItem != null)
                    selectedDomain = (string)(LayersListBox.SelectedItem as SurfaceListBoxItem).Tag;
                else
                    selectedDomain = "";
                string imageDomain = (string)((Image)e.Source).Tag;
                if (imageDomain.CompareTo(selectedDomain)==0)
                    Plugin.SetDomainVisibile(imageDomain, OpacitySlider.Value);
                else
                    Plugin.ToggleDomainVisibility(imageDomain, OpacitySlider.Value); 
                e.Handled = true;
            }
        }

        void im_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Plugin != null)
            {
                string selectedDomain;
                if (LayersListBox.SelectedItem != null)
                    selectedDomain = (string)(LayersListBox.SelectedItem as SurfaceListBoxItem).Tag;
                else
                    selectedDomain = "";
                string imageDomain = (string)((Image)e.Source).Tag;
                if (imageDomain.CompareTo(selectedDomain) == 0)
                    Plugin.SetDomainVisibile(imageDomain, OpacitySlider.Value);
                else
                    Plugin.ToggleDomainVisibility(imageDomain, OpacitySlider.Value);
                e.Handled = true;
            }
        }

        private void SurfaceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Plugin != null)
                Plugin.DomainImageOpacity = e.NewValue;
            if (e.NewValue <= 0.1)
                (sender as SurfaceSlider).BorderBrush = new SolidColorBrush(Colors.Red);
            else
                (sender as SurfaceSlider).BorderBrush = new SolidColorBrush(Color.FromArgb(0x66,0,0,0));
        }

        private void FollowChk_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin != null)
                Plugin.Follow = FollowChk.IsChecked.Value;
        }

        private void FocusButton_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin != null)
                Plugin.FocusLayer();
        }

        private void legendButton_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin != null)
                Plugin.ShowLegend();
        }
    }
}