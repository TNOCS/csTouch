using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace csDataServerPlugin.RatingControl
{
    /// <summary>
    ///     Interaction logic for RatingControl.xaml
    /// </summary>
    public partial class RatingControl
    {
        public static readonly DependencyProperty RatingValueProperty =
            DependencyProperty.Register("RatingValue", typeof(int), typeof(RatingControl),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    RatingValueChanged));

        #region CanEdit

        public static readonly DependencyProperty CanEditProperty = DependencyProperty.Register("CanEdit", typeof(bool),
            typeof(RatingControl), new FrameworkPropertyMetadata(false));

        public bool CanEdit
        {
            get { return (bool)GetValue(CanEditProperty); }
            set { SetValue(CanEditProperty, value); }
        }

        #endregion CanEdit

        #region MaxValue

        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register("MaxValue", typeof(double),
            typeof(RatingControl), new FrameworkPropertyMetadata(5d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, MaxValueChanged));

        private static void MaxValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var parent = sender as RatingControl;
            if (parent == null) return;
            if ((double) e.NewValue == 0) parent.MaxValue = (double)e.OldValue;
            SetStars(parent);
        }

        public double MaxValue
        {
            get { return (double)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        #endregion MaxValue

        public RatingControl()
        {
            InitializeComponent();
        }

        public int RatingValue
        {
            get { return (int)GetValue(RatingValueProperty); }
            set
            {
                if (value < 0)
                {
                    SetValue(RatingValueProperty, 0);
                }
                else if (value > MaxValue)
                {
                    SetValue(RatingValueProperty, MaxValue);
                }
                else
                {
                    SetValue(RatingValueProperty, value);
                }
            }
        }

        private const int NumberOfStars = 5;

        private static void RatingValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var parent = sender as RatingControl;
            if (parent == null) return;
            SetStars(parent);
        }

        private static void SetStars(RatingControl parent)
        {
            var children = ((UniformGrid)(parent.Content)).Children;

            var maxValue = Math.Abs(parent.MaxValue) < 0.00001 ? NumberOfStars : parent.MaxValue;
            var numberOfStars = (int)Math.Min(NumberOfStars, Math.Ceiling((parent.RatingValue * NumberOfStars) / maxValue));
            for (var i = 0; i < numberOfStars; i++)
            {
                var button = children[i] as ToggleButton;
                if (button != null)
                    button.IsChecked = true;
            }

            for (var i = numberOfStars; i < children.Count; i++)
            {
                var button = children[i] as ToggleButton;
                if (button != null)
                    button.IsChecked = false;
            }
        }

        private void RatingButtonClickEventHandler(Object sender, RoutedEventArgs e)
        {
            if (!CanEdit)
            {
                RatingValueChanged(this, new DependencyPropertyChangedEventArgs(RatingValueProperty, 0, RatingValue));
                e.Handled = true;
                return;
            }

            var button = sender as ToggleButton;
            if (button == null) return;
            var newRating = int.Parse((String)button.Tag);

            if (button.IsChecked == true || newRating < RatingValue)
            {
                RatingValue = newRating;
            }
            else
            {
                RatingValue = newRating - 1;
            }

            e.Handled = true;
        }
    }
}