using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using csShared;
using csShared.Interfaces;

namespace csCommon
{
    using System.ComponentModel.Composition;
    using System.Windows.Threading;

    [Export(typeof(IPluginScreen))]
    public class NotificationViewModel : Screen, IPluginScreen
    {
        private NotificationView _view;

        private NotificationEventArgs notification;

        public NotificationEventArgs Notification
        {
            get { return notification; }
            set { notification = value; NotifyOfPropertyChange(()=>Notification); }
        }

        private BindableCollection<NotificationEventArgs> notifications = new BindableCollection<NotificationEventArgs>();

        public BindableCollection<NotificationEventArgs> Notifications
        {
            get { return notifications; }
            set { notifications = value; NotifyOfPropertyChange(()=>Notifications); }
        }

        private void PlaySound(Uri u)
        {
             var me = new MediaPlayer();                
             me.Open(u);
             me.Play();
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            _view = (NotificationView)view;
            AppStateSettings.Instance.NewNotification += AppStateNewNotification;
            AppStateSettings.Instance.DeleteNotification += InstanceDeleteNotification;
            AppStateSettings.Instance.DeleteAllTextNotifications += Instance_DeleteAllTextNotifications;
        }

        void Instance_DeleteAllTextNotifications(object sender, EventArgs e)
        {
            _view.bFreeText.Children.Clear();
        }

        void InstanceDeleteNotification(NotificationEventArgs args)
        {
            if (args == null) return;
            var tbd = new List<FrameworkElement>();
            foreach (FrameworkElement a in _view.bFreeText.Children)
            {
                if (a.Tag is Guid && (Guid)a.Tag == args.Id) tbd.Add(a);
            }
            foreach (var fe in tbd)
            {
                _view.bFreeText.Children.Remove(fe);
            }
            Remove(args);
        }

        public void EndNotifications()
        {
            Execute.OnUIThread(() => _view.bFreeText.Children.Clear());
        }

        public void NotificationClick(NotificationEventArgs s)
        {
            Remove(s);
        }

        private void Remove(NotificationEventArgs s)
        {
            if (s == null) return; // REVIEW TODO fix
            if (s.Timer != null) s.Timer.Stop();
            if (!Notifications.Contains(s)) return;
            s.OnClosing();
            Notifications.Remove(s);
        }

        public void AutoClick(NotificationEventArgs o, object eventArgs)
        {
            if (o == null) return;
            var usesTouch = false;
            var touchEvent = eventArgs as TouchEventArgs;
            if (touchEvent != null)
            {
                usesTouch = true;
                touchEvent.Handled = true;
            }
            else
            {
                var routedEvent = eventArgs as RoutedEventArgs;
                if (routedEvent != null) routedEvent.Handled = true;
            }
            o.TriggerOptionClicked(o.AutoClickText, usesTouch);
            Remove(o);
        }

        public void OptionClick(NotificationOption o, object eventArgs)
        {
            if (o == null) return;
            var usesTouch = false;
            var touchEvent = eventArgs as TouchEventArgs;
            if (touchEvent != null)
            {
                usesTouch = true;
                touchEvent.Handled = true;
            }
            else
            {
                var routedEvent = eventArgs as RoutedEventArgs;
                if (routedEvent != null) routedEvent.Handled = true;
            }
            o.Notification.TriggerOptionClicked(o.Option, usesTouch);
            Remove(o.Notification);
        }

        void AppStateNewNotification(NotificationEventArgs e)
        {
            Notification = e;
            e.OnStarting();          
            if (e.AutoClickInSeconds > 0)
            {
                e.Duration = TimeSpan.FromSeconds(e.AutoClickInSeconds + 1);

                var _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, (sender, args) =>
                {
                    e.AutoClickInSeconds--;
                    e.AutoClickUpdate();
                    if (e.AutoClickInSeconds > 0) return;
                    var timer = sender as DispatcherTimer;
                    timer.Stop();
                    if (e.AutoClickInSeconds > int.MinValue) e.TriggerOptionClicked(e.AutoClickText, false);
                }, Application.Current.Dispatcher);

                _timer.Start();
            }
            switch (e.Style)
            {
                case NotificationStyle.Popup:
                    AddPopup(e);
                    break;
                case NotificationStyle.FreeText:
                    Execute.OnUIThread(() =>
                    {
                        var tb = new TextBlock
                        {
                            Tag = e.Id,
                            Text = e.Text,
                            HorizontalAlignment = e.HorizontalAlignment,
                            VerticalAlignment = e.VerticalAlignment,
                            Background = e.Background,
                            Foreground = e.Foreground,
                            FontSize = e.FontSize,
                            Padding = e.Padding,
                            Margin = e.Margin,
                            FontFamily = e.FontFamily,
                            TextAlignment = e.TextAlignment
                        };
                        tb.MouseDown += (es, s) => e.TriggerClick();
                        tb.TextWrapping = TextWrapping.Wrap;

                        if (e.Size != null)
                        {
                            tb.Width = e.Size.Width;
                            tb.Height = e.Size.Height;
                        }
                        _view.bFreeText.Children.Add(tb);
                        var dt = new DispatcherTimer {Interval = e.Duration};
                        dt.Tick += (es, s) =>
                        {
                            if (_view.bFreeText.Children.Contains(tb))
                            {
                                _view.bFreeText.Children.Remove(tb);
                            }
                            dt.Stop();
                            dt = null;
                        };
                        dt.Start();
                    });
                    break;
            }
        }

        /// <summary>
        /// Add a notification popup (title, text and options)
        /// </summary>
        /// <param name="e"></param>
        private void AddPopup(NotificationEventArgs e)
        {
            Notifications.Add(e);
            //if (e.SoundUri!=null) PlaySound(e.SoundUri);
            if (e.Duration.Ticks <= 0) return;
            e.Timer = new DispatcherTimer();
            if (e.Image == null)
            {
                e.Image = new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/Message.png"));
            }
            e.Timer.Interval = e.Duration;
            e.Timer.Tick += (s, ea) => Remove(e);
            e.Timer.Start();
            if (!e.Options.Any()) return;
            e.WorkingOptions = new BindableCollection<NotificationOption>();
            foreach (var a in e.Options)
                e.WorkingOptions.Add(new NotificationOption { Notification = e, Option = a });
        }

        public string Name
        {
            get { return "Notification"; }
        }
    }
}
