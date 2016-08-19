using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using System.Globalization;
using ESRI.ArcGIS.Client.Geometry;

namespace csCommon.Plugins.MgrsGrid
{
    /// <summary>
    /// Interaction logic for MgrsCoordinateLabel.xaml
    /// </summary>
    public partial class MgrsCoordinateLabel : UserControl
    {
        public MgrsCoordinateLabel()
        {
            InitializeComponent();
            Loaded += MgrsCoordinateLabel_Loaded;

        }

        void MgrsCoordinateLabel_Loaded(object sender, RoutedEventArgs e)
        {
            AppStateSettings.Instance.ViewDef.MapControl.MouseMove += MapControl_MouseMove;
        }

        void MapControl_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                System.Windows.Point screenPoint = e.GetPosition(AppStateSettings.Instance.ViewDef.MapControl);
                MapPoint mapPoint = AppStateSettings.Instance.ViewDef.MapControl.ScreenToMap(screenPoint);
                var latlon = mapPoint.ToGenericGeometry();
                var mgrs = latlon.ConvertToMgrs();
                // LatLonCoordsTextBlock.Text = string.Format(CultureInfo.InvariantCulture, "WGS84({0:00.000000},{1:000.000000}) ", latlon.Latitude, latlon.Longitude);
                LatLonCoordsTextBlock.Text = latlon.ToTextualDescription(WGS84LatLongPoint.TextualDescription.Decimal);
                MgrsCoordsTextBlock.Text = string.Format(CultureInfo.InvariantCulture, "MGRS({0})", mgrs.ToLongString());

            }
            catch (Exception)
            {
                LatLonCoordsTextBlock.Text = "-";
                MgrsCoordsTextBlock.Text = "-";
            }
        }
    }
}
