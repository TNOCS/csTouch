using System;
using System.Windows;
using csShared.Controls.Popups.MenuPopup;
using Microsoft.Surface.Presentation.Controls;

namespace csShared.Controls
{
    public class PopupButton : SurfaceButton
    {

        // Create a custom routed event by first registering a RoutedEventID 
        // This event uses the bubbling routing strategy 
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
            "ValueChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PopupButton));

        // Provide CLR accessors for the event 
        public event RoutedEventHandler ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        // This method raises the Tap event 
        void RaiseValueChangedEvent()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(PopupButton.ValueChangedEvent);
            RaiseEvent(newEventArgs);
        }

        public string Options
        {
            get { return (string)GetValue(OptionsProperty); }
            set { SetValue(OptionsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Options.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OptionsProperty =
            DependencyProperty.Register("Options", typeof(string), typeof(PopupButton), new PropertyMetadata(""));




        public bool ShowValue
        {
            get { return (bool)GetValue(ShowValueProperty); }
            set { SetValue(ShowValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowValueProperty =
            DependencyProperty.Register("ShowValue", typeof(bool), typeof(PopupButton), new PropertyMetadata(true));

        

        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(PopupButton), new PropertyMetadata("", OptionValueChanged));

        //Content

        public static void OptionValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs value)
        {
            ((SurfaceButton) obj).Content = value.NewValue;
        }

        private MenuPopupViewModel GetMenu(FrameworkElement fe)
        {
            var menu = new MenuPopupViewModel
            {
                RelativeElement = fe,
                RelativePosition = new Point(35, -5),
                TimeOut = new TimeSpan(0, 0, 0, 15),
                VerticalAlignment = VerticalAlignment.Top,
                DisplayProperty = string.Empty,
                AutoClose = true
            };

            

            return menu;
        }
        
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.Click += PopupButton_Click;
            if (ShowValue) Content = Value;
        }

        void PopupButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var menu = GetMenu(this);
            menu.AddMenuItems(Options.Split(','));
            menu.Selected += menu_Selected;
            AppStateSettings.Instance.Popups.Add(menu);
        }

        void menu_Selected(object sender, MenuSelectedEventArgs e)
        {
            Value = e.Object.ToString();
            if (ShowValue) Content = Value;
            RaiseValueChangedEvent();
        }
    }
}