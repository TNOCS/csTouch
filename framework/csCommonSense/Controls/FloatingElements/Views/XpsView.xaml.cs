using BaseWPFHelpers;
using csShared;
using Microsoft.Surface.Presentation.Controls;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Xps.Packaging;

namespace csCommon
{
    public partial class XpsView
    {
        private readonly Style s;
        private int page = 1;

        public XpsView()
        {
            InitializeComponent();
            Loaded += ImageViewLoaded;
            Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);

            foreach (var a in Application.Current.Resources.MergedDictionaries)
            {
                var b = a["SimpleFloatingStyle"];
                if (b != null) s = (Style)b;
            }

            xpsViewer.SizeChanged += xpsViewer_SizeChanged;
        }

        private void xpsViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void UpdateArrows()
        {
            //baNext.Visibility = (xpsViewer.CanGoToPage(page + 1))
            //            ? Visibility.Visible
            //            : System.Windows.Visibility.Collapsed;

            //baPrevious.Visibility = (xpsViewer.CanGoToPage(page - 1))
            //            ? Visibility.Visible
            //            : System.Windows.Visibility.Collapsed;
        }

        private void ImageViewLoaded(object sender, RoutedEventArgs e)
        {
            var vm = (XpsViewModel)DataContext;
            var loc = vm.Doc.Location;
            vm.Doc.PropertyChanged += Doc_PropertyChanged;

            UpdateDocument();
        }

        private void UpdateDocument()
        {
            XpsDocument doc = null;
            try
            {
                var vm = (XpsViewModel)DataContext;
                var loc = vm.Doc.Location;
                if (File.Exists(loc))
                {
                    doc = new XpsDocument(loc, FileAccess.Read);
                    xpsViewer.Document = doc.GetFixedDocumentSequence();
                    var a = xpsViewer.PageViews;


                    UpdateArrows();
                    xpsViewer.FitToWidth();

                    //BitmapImage bi = new BitmapImage(new Uri(((ImageViewModel) this.DataContext).Doc.Location));
                    var _svi = (ScatterViewItem)Helpers.FindElementOfTypeUp(this, typeof(ScatterViewItem));
                    _svi.SizeChanged += _svi_SizeChanged;
                    var fe = (FloatingElement)_svi.DataContext;

                    if (s != null)
                    {
                        fe.Style = s;
                    }

                    fe.ShowShadow = true;
                    //Size si = xpsViewer.Document.DocumentPaginator.PageSize;
                    if (doc.FixedDocumentSequenceReader != null)
                        if (doc.FixedDocumentSequenceReader.PrintTicket != null)
                        {
                            var orientation = doc.FixedDocumentSequenceReader.PrintTicket.PageOrientation;
                            var si = doc.FixedDocumentSequenceReader.PrintTicket.PageMediaSize;

                            switch (orientation)
                            {
                                case PageOrientation.Portrait:
                                    fe.Width = 300;
                                    if (si.Width != null && si.Height != null)
                                        fe.Height = (300 / si.Width.Value) * si.Height.Value;
                                    // (300 / bi.Width) * bi.Height;
                                    break;
                                case PageOrientation.Landscape:
                                    fe.Width = 300;
                                    if (si.Width != null && si.Height != null)
                                        fe.Height = (300 / si.Height.Value) * si.Width.Value;
                                    break;
                            }
                        }
                        else
                        {
                            fe.Width = 300;
                            fe.Height = 200;
                        }
                }
                else
                {
                    xpsViewer.Document = null;
                }
            }
            catch (Exception es)
            {
                Console.Write(es.Message);
            }
        }

        private void Doc_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateDocument();
        }


        private void _svi_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            xpsViewer.FitToWidth();
        }


        private void MainSourceUpdated(object sender, DataTransferEventArgs e)
        {
        }

        private void ssZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // xpsViewer.Zoom = ssZoom.Value;
            // TODO: Add event handler implementation here.
        }

        private void baNext_TouchDown(object sender, TouchEventArgs e)
        {
            Next();
            e.Handled = true;
        }

        private void Next()
        {
            if (xpsViewer.CanGoToPage(page + 1))
            {
                page += 1;
                xpsViewer.GoToPage(page);

                UpdateArrows();
            }
        }

        private void baPrevious_TouchDown(object sender, TouchEventArgs e)
        {
            Previous();
            e.Handled = true;
        }

        private void Previous()
        {
            if (xpsViewer.CanGoToPage(page - 1))
            {
                page -= 1;
                xpsViewer.GoToPage(page);
                UpdateArrows();
            }
        }

        private void baNext_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Next();
            e.Handled = true;
        }

        private void baPrevious_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Previous();
            e.Handled = true;
        }
    }
}