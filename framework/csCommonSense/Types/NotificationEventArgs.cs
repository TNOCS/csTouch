using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace csShared
{
    public enum NotificationStyle
    {
        Popup,
        FreeText
    }

    public class NotificationOptionSelectedEventArgs : EventArgs
    {
        /// <summary>
        /// Touch was used to trigger the option.
        /// </summary>
        public bool UsesTouch { get; set; }

        /// <summary>
        /// The selected option.
        /// </summary>
        public string Option { get; set; }
    }

    public class NotificationEventArgs : EventArgs
    {

        private List<string> options = new List<string>();

        public List<string> Options
        {
            get { return options; }
            set { options = value; }
        }

        public Uri SoundUri { get; set; }

        public bool ShowImage { get { return string.IsNullOrEmpty(PathData); } }

        public event EventHandler Closing;

        public void OnClosing()
        {
            var handler = Closing;
            if (handler != null) handler(this, Empty);
        }

        /// <summary>
        /// Event that is triggered when someone clicks on the window (so we can remove it), 
        /// but also when the time is up.
        /// </summary>
        public event EventHandler Click;

        public void TriggerClick()
        {
            var handler = Click;
            if (handler != null) handler(this, null);
        }

        private Guid id = Guid.NewGuid();

        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Header { get; set; }


        /// <summary>
        /// Text to be displayed
        /// </summary>    
        public string Text { get; set; }

        private NotificationStyle style = NotificationStyle.Popup;

        public NotificationStyle Style
        {
            get { return style; }
            set { style = value; }
        }

        /// <summary>
        /// How long notification will apprear (in ms)
        /// </summary>
        private TimeSpan duration = new TimeSpan(0, 0, 3);

        public TimeSpan Duration
        {
            get { return duration; }
            set { duration = value; }
        }

        /// <summary>
        /// What font the notification will use 
        /// </summary>
        private FontFamily fontfamily = new FontFamily("Segoe UI");

        public FontFamily FontFamily
        {
            get { return fontfamily; }
            set { fontfamily = value; }
        }

        /// <summary>
        /// How to align the text inside the textbox
        /// </summary>
        private TextAlignment textAlignment = TextAlignment.Left;

        public TextAlignment TextAlignment
        {
            get { return textAlignment; }
            set { textAlignment = value; }
        }

        private Thickness padding = new Thickness(10);

        public Thickness Padding
        {
            get { return padding; }
            set { padding = value; }
        }

        private HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left;

        public HorizontalAlignment HorizontalAlignment
        {
            get { return horizontalAlignment; }
            set { horizontalAlignment = value; }
        }

        private VerticalAlignment verticalAlignment = VerticalAlignment.Top;

        public VerticalAlignment VerticalAlignment
        {
            get { return verticalAlignment; }
            set { verticalAlignment = value; }
        }

        public DispatcherTimer Timer { get; set; }


        private Brush background = Brushes.Black;

        public Brush Background
        {
            get { return background; }
            set { background = value; }
        }

        private Thickness margin;

        public Thickness Margin
        {
            get { return margin; }
            set { margin = value; }
        }

        private Size size = new Size(Double.NaN, double.NaN);

        public Size Size
        {
            get { return size; }
            set { size = value; }
        }

        public ImageSource Image { get; set; }
        
        public Brush Foreground { get; set; }

        private double fontSize = 20;

        public double FontSize
        {
            get { return fontSize; }
            set { fontSize = value; }
        }

        public BindableCollection<NotificationOption> WorkingOptions { get; set; }

        public event EventHandler<NotificationOptionSelectedEventArgs> OptionClicked;

        /// <summary>
        /// Raise the option clicked event.
        /// </summary>
        /// <param name="option">The option that was selected.</param>
        /// <param name="usesTouch">Touch is used to trigger the event.</param>
        public void TriggerOptionClicked(string option, bool usesTouch)
        {
            var handler = OptionClicked;
            if (handler != null) handler(this, new NotificationOptionSelectedEventArgs { Option = option, UsesTouch = usesTouch });
        }

        public string PathData { get; set; }
    }

    public class NotificationOption : PropertyChangedBase
    {
        public string Option { get; set; }

        public NotificationEventArgs Notification { get; set; }
    }
}