using csImb;
using csShared;
using csShared.Documents;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;


namespace csRemoteScreenPlugin
{
    /// <summary>
    /// Interaction logic for ucSensicon.xaml
    /// </summary>
    public partial class ucClient : TagVisualization
    {
        //private int _tagId;



        public ImbClientStatus Status
        {
            get { return (ImbClientStatus)GetValue(StatusProperty); }
            set { SetValue(StatusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Status.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(ImbClientStatus), typeof(ucClient), new UIPropertyMetadata(null));


        public ucClient()
        {
            InitializeComponent();
        }

        private AppStateSettings State
        {
            get { return AppStateSettings.GetInstance(); }
        }


        private void TagVisualizationLoaded(object sender, RoutedEventArgs e)
        {

            {
                var c = State.Imb.Clients.Where((k => k.Value.TagID != "" && Convert.ToByte(int.Parse(k.Value.TagID, NumberStyles.HexNumber)) == VisualizedTag.Value)).FirstOrDefault();

                if (c.Value != null)
                {
                    Status = c.Value;
                    var fe = new FloatingElement
                    {
                        AllowDrop = true,
                        AllowedDropsTags = new List<string>() { "document" }
                    };
                    fe.Drop += fe_Drop;
                    fe.DragEnter += (s, es) => { Opacity = 0.5; };
                    fe.DragLeave += (s, es) => { Opacity = 1; };
                    border.DataContext = fe;
                    SurfaceDragDrop.AddDragEnterHandler(this, (s, ea) => this.Opacity = 0.5); // VisualStateManager.GoToState(this,"DragOver",true));
                    SurfaceDragDrop.AddDragLeaveHandler(this, (s, ea) => this.Opacity = 1.0); // VisualStateManager.GoToState(this, "Normal", true));
                    SurfaceDragDrop.AddDropHandler(this, (s, ea) => {
                        if (!(ea.Cursor.Data is Document)) return;
                        // send document
                        var d = (Document)ea.Cursor.Data;
                        this.Opacity = 1.0;
                        AppStateSettings.Instance.SendDocument(Status, d);
                    });
                }
                LostTag += UcSensiconLostTag;
            }

            Moved += UcSensiconMoved;
        }

        void fe_Drop(object sender, FloatingDragDropEventArgs e)
        {
            if (e.Element.Document != null)
            {
                AppStateSettings.Instance.SendDocument(this.Status, e.Element.Document);
                AppStateSettings.Instance.FloatingItems.RemoveFloatingElement(e.Element,
                                                                              FloatingCollection.ClosingStyle.Shrink,
                                                                              target:
                                                                                  this.TranslatePoint(
                                                                                      new Point(-300, -150),
                                                                                      Application.Current.MainWindow),
                                                                              duration: 200);
            }
            this.Opacity = 1;
        }

        // The hit testing results
        private List<FrameworkElement> hitResultsList = new List<FrameworkElement>();

        void UcSensiconMoved(object sender, TagVisualizerEventArgs e)
        {
        }

        private void UcSensiconLostTag(object sender, RoutedEventArgs e)
        {
        }
    }
}