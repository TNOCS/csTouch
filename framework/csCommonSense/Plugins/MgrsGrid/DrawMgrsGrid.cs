using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csCommon.Plugins.MgrsGrid
{
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Media;

    using csCommon.Converters;

    using csShared;

    using DocumentFormat.OpenXml.Wordprocessing;

    using ESRI.ArcGIS.Client.Geometry;

    using GeoUtility.GeoSystem;
    using GeoUtility.GeoSystem.Base;

    using Brushes = System.Windows.Media.Brushes;
    using Pen = System.Windows.Media.Pen;
    using Point = System.Windows.Point;
    using System.Windows.Input;
    public class DrawMgrsRaster
    {
        public enum EGridPrecision : int
        {
            
            Grid100km = 100000,
            Grid10km = 10000,
            Grid1km = 1000,
            Grid100m = 100,
            Grid10m = 10,
            Grid1m = 1
        }

        private Pen ZoneLinePen = new Pen(Brushes.RoyalBlue, 4) { StartLineCap = PenLineCap.Triangle, EndLineCap = PenLineCap.Triangle };
        private Pen mMgrsLinePen = new Pen(Brushes.Black, 2) { StartLineCap = PenLineCap.Triangle, EndLineCap = PenLineCap.Triangle };
        
        private Typeface labelType1 = new Typeface(new System.Windows.Media.FontFamily("Century"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        // MgrsGridPrecision
        private Typeface mTypefaceMgrsGridPrecision = new Typeface(new System.Windows.Media.FontFamily("Century"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        private int mFontSizeMgrsGridPrecision = 24;
        private SolidColorBrush mFontColorMgrsGridPrecision = Brushes.Black;

        // MgrsCellCenterLabel
        private Typeface mTypefaceMgrsCellCenterLabel = new Typeface(new System.Windows.Media.FontFamily("Century"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        private SolidColorBrush mFontColorMgrsCellCenterLabel = Brushes.Black;

        // MgrsCellLabel
        private Typeface mTypefaceMgrsCellLabel = new Typeface(new System.Windows.Media.FontFamily("Century"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        private int mFontSizeMgrsCellLabel = 12;
        private SolidColorBrush mFontColorMgrsCellLabel = Brushes.White;
        private SolidColorBrush mFontColorMgrsFill = Brushes.Black;
        private Pen mMgrsLabelLinePen = new Pen(Brushes.Black, 2) { StartLineCap = PenLineCap.Triangle, EndLineCap = PenLineCap.Triangle };

        public DrawMgrsRaster(MgrsConfig pConfig, MgrsViewport pMgrsGrid)
        {
            Viewport = pMgrsGrid;
            Cfg = pConfig;
        }

        public MgrsConfig Cfg { get; private set; }

        /// <summary>
        /// Entry point for rendering MGRS grid
        /// </summary>
        /// <param name="pDC"></param>
        public void Render(DrawingContext pDC)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                RenderUtmZoneLines(pDC);
                //if (Viewport.DiagonalDistanceInKm < 40) RenderMgrsGrid(pDC, EGridPrecision.Grid1km);


                // Smaller has 100m has no use
                //if (Viewport.DiagonalDistanceInKm < 1) precision = EGridPrecision.Grid100m;
                //else if (Viewport.DiagonalDistanceInKm < 8) precision = EGridPrecision.Grid1km;
                //else if (Viewport.DiagonalDistanceInKm < 100) precision = EGridPrecision.Grid10km; ;
                double meterPerPixel = ((int)(Viewport.MetersPerPixel * 10)) / 10.0;
                if (meterPerPixel <= Cfg.MaxMetersPerPixelGrid100m) MgrsGridPrecision = EGridPrecision.Grid100m;
                else if (meterPerPixel <= Cfg.MaxMetersPerPixelGrid1km) MgrsGridPrecision = EGridPrecision.Grid1km;
                else if (meterPerPixel <= Cfg.MaxMetersPerPixelGrid10km) MgrsGridPrecision = EGridPrecision.Grid10km;
                else MgrsGridPrecision = EGridPrecision.Grid100km;

                
                RenderMgrsGrid(pDC);
                RenderPresicion(pDC);
                sw.Stop();
                //Console.WriteLine(sw.precision);
            }
            catch (Exception ex)
            {


            }

        }

        private void RenderPresicion(DrawingContext pDC)
        {
            string description = "";
            switch (MgrsGridPrecision)
            {
                case EGridPrecision.Grid100km:
                    description = "100km grid";
                    break;
                case EGridPrecision.Grid10km:
                    description = "10km grid";
                    break;
                case EGridPrecision.Grid1km:
                    description = "1km grid";
                    break;
                case EGridPrecision.Grid100m:
                    description = "100m grid";
                    break;
                case EGridPrecision.Grid10m:
                    description = "10m grid";
                    break;
            }
            bool renderMeterperPixel = ((Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)));
            var ft = new FormattedText(string.Format("{0} {1}", description, renderMeterperPixel ? Viewport.MetersPerPixel.ToString("0.0") : ""), CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                                       mTypefaceMgrsGridPrecision, mFontSizeMgrsGridPrecision, mFontColorMgrsGridPrecision);
            //pDC.DrawText(ft, new Point(Viewport.MapWidth - ft.Width - 50, 100));

            System.Windows.Media.Geometry textGeometry = ft.BuildGeometry(new Point(Viewport.MapWidth - ft.Width - 50, 100));
            
            pDC.DrawGeometry(Brushes.White, new Pen(Brushes.Black, 1), textGeometry);
        }

        private void RenderMgrsCenterLabel(DrawingContext pDC, MgrsBoundingBox pBoundingBox, double pNorth, double pEast)
        {
            if (!Cfg.DrawCenterMgrsGridLabel) return;
            var cellSize = CellSizeInPixels(pBoundingBox);
            var center = pBoundingBox.CenterOfMass();
            var c = center.ConvertToMgrs();
            var p3 = Viewport.EsriMap.MapToScreen(center.ToEsriGeometry(102100));
            int fontSize;
            string label;
            if (cellSize.Width < 100)
            {
                fontSize = 16;
                label = c.Grid;
            } else if (cellSize.Width < 150)
            {
               fontSize = 18;
               label = c.Grid;
            } else
            {
                fontSize = 20;
                label = c.Zoneband + " " + c.Grid;
            }


           var ft = new FormattedText(label, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                                       mTypefaceMgrsCellCenterLabel, fontSize, mFontColorMgrsCellCenterLabel);
            //pDC.DrawText(ft, new Point(p3.X - (ft.Width / 2), p3.Y - (ft.Height / 2)));

            System.Windows.Media.Geometry textGeometry = ft.BuildGeometry(new Point(p3.X - (ft.Width / 2), p3.Y - (ft.Height / 2)));
            pDC.DrawGeometry(Brushes.White, new Pen(Brushes.Black, 1), textGeometry);
        }

        public enum NorthEast {  North, East };

        private System.Windows.Size CellSizeInPixels(MgrsBoundingBox pBoundingBox)
        {
            var upperRight = Viewport.EsriMap.MapToScreen(pBoundingBox.UpperRight.ToEsriGeometry(102100));
            var lowerLeft = Viewport.EsriMap.MapToScreen(pBoundingBox.LowerLeft.ToEsriGeometry(102100));
            return new System.Windows.Size(Math.Abs(lowerLeft.X - upperRight.X), Math.Abs(lowerLeft.Y - upperRight.Y));
        }

        private void RenderMgrsCellLabels(DrawingContext pDC, MgrsBoundingBox pBoundingBox, double pNorth, double pEast, NorthEast pDirection)
        {
           
            int numberOfDigits;
            switch (MgrsGridPrecision)
            {
                case EGridPrecision.Grid10km:
                    numberOfDigits = 1;
                    break;
                case EGridPrecision.Grid1km:
                    numberOfDigits = 2;
                    break;
                case EGridPrecision.Grid100m:
                    numberOfDigits = 3;
                    break;
                default: return; // Dont render label
            }
            var cellSize = CellSizeInPixels(pBoundingBox);

            if ((cellSize.Width > 30) && (cellSize.Height > 30))
            {
                var eastFont = new FormattedText(pNorth.ToString().Substring(2, numberOfDigits),
                                                 CultureInfo.CurrentCulture, FlowDirection.LeftToRight, mTypefaceMgrsCellLabel, mFontSizeMgrsCellLabel, mFontColorMgrsCellLabel);

                var northFont = new FormattedText(pEast.ToString().Substring(1, numberOfDigits),
                                                 CultureInfo.CurrentCulture, FlowDirection.LeftToRight, mTypefaceMgrsCellLabel, mFontSizeMgrsCellLabel, mFontColorMgrsCellLabel);

                var lowerLeft = Viewport.EsriMap.MapToScreen(pBoundingBox.LowerLeft.ToEsriGeometry(102100));
                int marge = 3;
                if (pDirection == NorthEast.East)
                {
                    // Horizontal labels
                    var rectNorth = GetBoundingRect(northFont);
                    rectNorth.Inflate(marge * 2, marge * 2);
                    var northPoint = new Point(lowerLeft.X + (cellSize.Width / 2) - (rectNorth.Width / 2), lowerLeft.Y - (rectNorth.Height / 2));
                    pDC.DrawRoundedRectangle(mFontColorMgrsFill, mMgrsLabelLinePen, new Rect(northPoint, rectNorth.Size), 2, 2);
                    pDC.DrawText(eastFont, new Point(northPoint.X + marge, northPoint.Y + marge));
                }
                if (pDirection == NorthEast.North)
                {
                    // Vertical
                    var rectEast = GetBoundingRect(eastFont);
                    rectEast.Inflate(marge * 2, marge * 2);
                    var eastPoint = new Point(lowerLeft.X - (rectEast.Width / 2), lowerLeft.Y - (cellSize.Height / 2) - (rectEast.Height / 2));
                    pDC.DrawRoundedRectangle(mFontColorMgrsFill, mMgrsLabelLinePen, new Rect(eastPoint, rectEast.Size), 2, 2);
                    pDC.DrawText(northFont, new Point(eastPoint.X + marge, eastPoint.Y + marge));
                }
                
            }
        }


        private void RenderMgrsGrid(DrawingContext pDC)
        {
            foreach (var utmCell in Viewport.GridZoneUtmCells)
            {
                RenderMgrsGridCell(pDC, utmCell);
            }
        }

        private static WGS84LatLongPoint UTMtoLL(double UTMNorthing, double UTMEasting, int UTMZoneNumber, Geocentric.Hemisphere pHemisphere)
        {
            UTM mgrs = new UTM(UTMZoneNumber, UTMEasting, UTMNorthing, pHemisphere);
            Geographic geo = (Geographic)mgrs;
            return WGS84LatLongPoint.Create(geo.Latitude, geo.Longitude);
        }



    
        private void RenderMgrsGridCell(
            DrawingContext pDC,
            GridZone pCell)
        {
            var view = Viewport.Extend;
            Point[] clipPolygon = new Point[]
                                        {
                                           new Point(pCell.Extend.WestBoundLongitude, pCell.Extend.NorthBoundLatitude),
                                           new Point(pCell.Extend.EastBoundLongitude, pCell.Extend.NorthBoundLatitude),
                                           new Point(pCell.Extend.EastBoundLongitude, pCell.Extend.SouthBoundLatitude),
                                           new Point(pCell.Extend.WestBoundLongitude, pCell.Extend.SouthBoundLatitude)
                                        };

            int interval = (int)MgrsGridPrecision; // Number of meters

            int precision = (int)MgrsGridPrecision;
            

            var sw = pCell.SouthWest();
            var ne = pCell.NorthEast();
            var swUtm = sw.ConvertToUtm();
            var neUtm = ne.ConvertToUtm();
            var swMgrs = sw.ConvertToMgrs();

            Debug.Assert(swUtm.Zone == neUtm.Zone, "UTM cell over multiple zones");
            int utmZone = swUtm.Zone;

            double sw_utm_e = (Math.Floor(swUtm.East / precision) * precision);
            double sw_utm_n = (Math.Floor(swUtm.North / precision) * precision);

            double ne_utm_e = (Math.Floor(neUtm.East / precision) * precision);
            double ne_utm_n = (Math.Floor(neUtm.North / precision) * precision);

            int numberOfCells = (((int)(Math.Abs(sw_utm_n - ne_utm_n) / interval)) * ((int)(Math.Abs(sw_utm_e - ne_utm_e) / interval)));
            if (numberOfCells > 210)
            {
                return; // Safety check; takes to long to render
            }
            int count = 0;
            for (double currentNorth = sw_utm_n; currentNorth <= ne_utm_n; currentNorth += interval)
            {
                for (double currentEast = sw_utm_e; currentEast <= ne_utm_e; currentEast += interval)
                {
                    count++;
                    var lowerLeftLatLon = UTMtoLL(currentNorth, currentEast, utmZone, sw.Latitude >= 0 ? Geocentric.Hemisphere.North : Geocentric.Hemisphere.South); ;
                    var upperRightLatLon = UTMtoLL(currentNorth + interval, currentEast + interval, utmZone, sw.Latitude >= 0 ? Geocentric.Hemisphere.North : Geocentric.Hemisphere.South); ;
                    var lowerRightLatLon = UTMtoLL(currentNorth, currentEast + interval, utmZone, sw.Latitude >= 0 ? Geocentric.Hemisphere.North : Geocentric.Hemisphere.South); ;
                    var upperLeftLatLon = UTMtoLL(currentNorth + interval, currentEast, utmZone, sw.Latitude >= 0 ? Geocentric.Hemisphere.North : Geocentric.Hemisphere.South); ;


                    Point[] utm = new Point[]
                                        {
                                           new Point(upperLeftLatLon.Longitude, upperLeftLatLon.Latitude),
                                           new Point(upperRightLatLon.Longitude, upperRightLatLon.Latitude),
                                           new Point(lowerRightLatLon.Longitude, lowerRightLatLon.Latitude),
                                           new Point(lowerLeftLatLon.Longitude, lowerLeftLatLon.Latitude),
                                        };
                    var intersect = SutherlandHodgman.GetIntersectedPolygon(utm, clipPolygon);

                    if ((intersect != null) && (intersect.Length >= 4))
                    {
                        for (int i = 0; i < intersect.Length - 1; i++)
                        {

                            var p1 = Viewport.EsriMap.MapToScreen(WGS84LatLongPoint.Create(intersect[i].Y, intersect[i].X).ToEsriGeometry(102100));
                            var p2 = Viewport.EsriMap.MapToScreen(WGS84LatLongPoint.Create(intersect[i + 1].Y, intersect[i + 1].X).ToEsriGeometry(102100));
                            pDC.DrawLine(mMgrsLinePen, new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
                        }

                        var bb = new MgrsBoundingBox(intersect);
                          
                        if (currentNorth + interval > ne_utm_n) RenderMgrsCellLabels(pDC, bb, currentNorth, currentEast, NorthEast.North);
                        if (currentEast + interval > ne_utm_e) RenderMgrsCellLabels(pDC, bb, currentNorth, currentEast, NorthEast.East);
                        RenderMgrsCenterLabel(pDC, bb, currentNorth, currentEast);
                    }
                }
            }
        }




        private static Rect GetBoundingRect(FormattedText text)
        {
            double lastY = text.Height + text.OverhangAfter;
            double firstY = lastY - text.Extent;
            double width = text.Width - text.OverhangLeading - text.OverhangTrailing;
            return new Rect(text.OverhangLeading, firstY, width, text.Extent);
        }

        



        private void RenderUtmZoneLines(DrawingContext pDC)
        {
            // first and last line in latitude/longtitude list skipped because this are not zone lines (not in interval 6 or 8 degrees)

            // Render horizontal lines
            for (var i = 1; i < Viewport.LatitudeLines.Count - 1; i++)
            {
                MapPoint previousPoint = null;
                for (var j = 0; j < Viewport.LongitudeLines.Count; j++)
                {
                    // convert back to mercator coordinates, the coord system of the base map
                    var lastPoint = WGS84LatLongPoint.Create(Viewport.LatitudeLines[i], Viewport.LongitudeLines[j]).ToEsriGeometry(102100);
                    if (previousPoint != null)
                    {
                        // TODO Point calculated twice now
                        var p1 = Viewport.EsriMap.MapToScreen(previousPoint);
                        var p2 = Viewport.EsriMap.MapToScreen(lastPoint);
                        pDC.DrawLine(ZoneLinePen, new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
                    }
                    previousPoint = lastPoint;
                }
            }
            // Render vertical lines
            for (var i = 1; i < Viewport.LongitudeLines.Count - 1; i++)
            {
                // TODO insert code for Norway and Svalbard special cases here normal case, not in Norway or Svalbard
                MapPoint previousPoint = null;
                for (var j = 0; j < Viewport.LatitudeLines.Count; j++)
                {
                    var lastPoint = WGS84LatLongPoint.Create(Viewport.LatitudeLines[j], Viewport.LongitudeLines[i]).ToEsriGeometry(102100);
                    // TODO Point calculated twice now
                    if (previousPoint != null)
                    {
                        var p1 = Viewport.EsriMap.MapToScreen(previousPoint);
                        var p2 = Viewport.EsriMap.MapToScreen(lastPoint);
                        pDC.DrawLine(ZoneLinePen, new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
                    }
                    previousPoint = lastPoint;
                }
            }  // for each longitude line
        }

        public MgrsViewport Viewport { get; private set; }
        public EGridPrecision MgrsGridPrecision { get; private set; }

    }
}
