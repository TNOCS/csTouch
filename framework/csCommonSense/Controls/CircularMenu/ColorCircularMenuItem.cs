using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace csCommon.csMapCustomControls.CircularMenu
{
    public class ColorCircularMenuItem : CircularMenuItem
    {

        public new double Opacity { get; set; } // FIXME TODO "new" keyword missing?

        private Color color;

        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        public void UpdateOpacity(double o)
        {
            Opacity = o;
            Color = new Color() {A = (byte) (255*o), R = Color.R, G = Color.G, B = Color.B};
            if (ColorChanged != null) ColorChanged(this, null);
        }

        public void UpdateColor(Color c)
        {
            var op = Opacity;
            if (c.A != 0) op = Opacity;
            else op = 0;
            Color = new Color() { A = (byte)(255 * op), R = c.R, G = c.G, B = c.B };
            if (ColorChanged != null) ColorChanged(this, null);
        }

        public event EventHandler ColorChanged;

        public static ColorCircularMenuItem CreateColorMenu(string title, int pos)
        {

            CircularMenuItem opacity = new CircularMenuItem()
                                           {
                                               Title = "Opacity",
                                               Position = 7,
                                               Items = new List<CircularMenuItem>()
                                                           {
                                                               new CircularMenuItem() {Title = "20%", Position = 1},
                                                               new CircularMenuItem() {Title = "40%", Position = 2},
                                                               new CircularMenuItem() {Title = "60%", Position = 3},
                                                               new CircularMenuItem() {Title = "80%", Position = 4},
                                                               new CircularMenuItem() {Title = "100%", Position = 5},
                                                           }
                                           };
            

            ColorCircularMenuItem result = new ColorCircularMenuItem()
                                               {
                                                   Title = title,
                                                   Position = pos,
                                                   Items = new List<CircularMenuItem>()
                                                               {
                                                                   new CircularMenuItem() {Title = "Purple",Fill = Brushes.Purple,Position = 0},
                                                                   new CircularMenuItem() {Title = "Red",Fill = Brushes.Red,Position = 1},
                                                                   new CircularMenuItem() {Title = "Blue",Fill = Brushes.Blue, Position = 2},
                                                                   new CircularMenuItem() {Title = "Black",Fill = Brushes.Black, Position = 3},
                                                                   new CircularMenuItem() {Title = "Orange",Fill = Brushes.Orange,Position = 4},
                                                                   new CircularMenuItem() {Title = "Green",Fill = Brushes.Green, Position = 5},
                                                                   
                                                                 
                                                                   new CircularMenuItem() {Title = "None",Position = 6},
                                                                   opacity
                                                               }
                                               };

            opacity.ItemSelected += (e, f) =>
            {
                var i =
                    double.Parse(f.SelectedItem.Title.Remove(f.SelectedItem.Title.Length - 1, 1)) / 100.0;
                result.UpdateOpacity(i);
                opacity.Menu.Back();
            };

            result.Opacity = 1;

            result.ItemSelected += (e, s) =>
                                       {
                                           result.UpdateColor(((SolidColorBrush)s.SelectedItem.Fill).Color);
                                           if (result.Menu != null) result.Menu.Back();
                                       };

            return result;
        }

        
    }
}