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
using System.Windows.Shapes;
using csShared;
using SessionsEvents;

namespace csUSDomainPlugin
{
    /// <summary>
    /// Interaction logic for USLegendaView.xaml
    /// </summary>
    public partial class USLegendaView : UserControl
    {
        public USLegendaView()
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
                if (fPlugin != null && fPlugin.CurrentDomain != null)
                {
                    Title = fPlugin.CurrentDomain.domain;
                    Palette = fPlugin.CurrentDomain.palette;
                }
            }
        }

        public string Title
        {
            get { return textboxTitle.Text; }
            set { textboxTitle.Text = value; }
        }
        
        private SessionPalette fPalette;
        public SessionPalette Palette
        {
            get { return fPalette; }
            set
            {
                if (fPalette != value)
                {
                    fPalette = value;
                    // remove current entries
                    for (int c = grid.Children.Count - 1; c >= 0; c--)
                    {
                        if ((grid.Children[c] as FrameworkElement).Name.StartsWith("entry"))
                            grid.Children.RemoveAt(c);
                    }
                    // add new entries
                    if (fPalette != null && fPalette.Count > 0)
                    {
                        int row = 0;
                        for (int e=0; e<fPalette.Count; e++)
                        {
                            if (fPalette[e].description != "")
                            {
                                row++;
                                // row def
                                // rectangle
                                Rectangle r = new Rectangle();
                                r.Name = "entryRect" + row;
                                r.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                                r.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                                r.Fill = new SolidColorBrush(Color.FromRgb(
                                    (byte)((fPalette[e].color >> 16) & 0xFF),
                                    (byte)((fPalette[e].color >> 8) & 0xFF),
                                    (byte)((fPalette[e].color >> 0) & 0xFF)));
                                r.Margin = new Thickness(2);
                                grid.Children.Add(r);
                                Grid.SetRow(r, row);
                                // text
                                TextBlock t = new TextBlock();
                                t.Name = "entryText" + row;
                                t.Text = fPalette[e].description;
                                t.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                                grid.Children.Add(t);
                                Grid.SetRow(t, row);
                                Grid.SetColumn(t, 1);
                            }
                        }
                        // update row definitions
                        if (grid.RowDefinitions.Count > row + 1)
                        {
                            grid.RowDefinitions.RemoveRange(row + 1, grid.RowDefinitions.Count - row - 1);
                        }
                        else
                        {
                            while (grid.RowDefinitions.Count < row + 1)
                            {
                                grid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(28) });
                            }
                        }
                    }
                }
            }
        }
    }
}
