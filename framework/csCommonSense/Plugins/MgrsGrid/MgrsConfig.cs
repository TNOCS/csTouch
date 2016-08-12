using csShared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csCommon.Plugins.MgrsGrid
{
    public class MgrsConfig
    {

        public const double DefaultMaxMetersPerPixelGrid100m = 1;
        public const double DefaultMaxMetersPerPixelGrid1km = 11;
        public const double DefaultMaxMetersPerPixelGrid10km = 43;

        public const double LimitMetersPerPixelGrid100m = 1;
        public const double LimitMetersPerPixelGrid1km = 15;
        public const double LimitMetersPerPixelGrid10km = 43;

        public MgrsConfig()
        {
           
            DrawCenterMgrsGridLabel = true;
            
        }

        public bool IsEnabled
        {
            get { return AppStateSettings.Instance.Config.GetBool("MGRS.IsEnabled", true); }
            set { AppStateSettings.Instance.Config.SetLocalConfig("MGRS.IsEnabled", Convert.ToString(value)); }
        }

        
        public bool DrawCenterMgrsGridLabel
        {
            get { return AppStateSettings.Instance.Config.GetBool("MGRS.DrawCenterMgrsGridLabel", true); }
            set { AppStateSettings.Instance.Config.SetLocalConfig("MGRS.DrawCenterMgrsGridLabel", Convert.ToString(value)); }
        }


        public double MaxMetersPerPixelGrid100m
        {
            get { return AppStateSettings.Instance.Config.GetDouble("MGRS.MaxMetersPerPixelGrid100m", DefaultMaxMetersPerPixelGrid100m); }
            set { AppStateSettings.Instance.Config.SetLocalConfig("MGRS.MaxMetersPerPixelGrid100m", Convert.ToString(value, CultureInfo.InvariantCulture)); }
           
        }

        public double MaxMetersPerPixelGrid1km
        {
            get { return AppStateSettings.Instance.Config.GetDouble("MGRS.MaxMetersPerPixelGrid1km", DefaultMaxMetersPerPixelGrid1km); }
            set { AppStateSettings.Instance.Config.SetLocalConfig("MGRS.MaxMetersPerPixelGrid1km", Convert.ToString(value, CultureInfo.InvariantCulture)); }
        }

        public double MaxMetersPerPixelGrid10km
        {
            get { return AppStateSettings.Instance.Config.GetDouble("MGRS.MaxMetersPerPixelGrid10km", DefaultMaxMetersPerPixelGrid10km); }
            set { AppStateSettings.Instance.Config.SetLocalConfig("MGRS.MaxMetersPerPixelGrid10km", Convert.ToString(value, CultureInfo.InvariantCulture)); }
        }

        public void SetDefaultLevels()
        {
            MaxMetersPerPixelGrid100m = DefaultMaxMetersPerPixelGrid100m;
            MaxMetersPerPixelGrid1km = DefaultMaxMetersPerPixelGrid1km;
            MaxMetersPerPixelGrid10km = DefaultMaxMetersPerPixelGrid10km;
        }

        public void SetLevel(DrawMgrsRaster.EGridPrecision pPrecision, double pMeterPerPixel)
        {
            pMeterPerPixel = ((int)(pMeterPerPixel * 10)) / 10.0;
            switch (pPrecision)
            {
                case DrawMgrsRaster.EGridPrecision.Grid100m:
                    MaxMetersPerPixelGrid100m = Math.Min(LimitMetersPerPixelGrid100m, pMeterPerPixel);
                    if (MaxMetersPerPixelGrid1km <= MaxMetersPerPixelGrid100m) MaxMetersPerPixelGrid1km = Math.Min(MaxMetersPerPixelGrid100m + 0.1, LimitMetersPerPixelGrid1km);
                    if (MaxMetersPerPixelGrid10km <= MaxMetersPerPixelGrid1km) MaxMetersPerPixelGrid10km = Math.Min(MaxMetersPerPixelGrid10km + 0.1, LimitMetersPerPixelGrid10km);
                    break;
                case DrawMgrsRaster.EGridPrecision.Grid1km:
                    MaxMetersPerPixelGrid1km = Math.Min(LimitMetersPerPixelGrid1km, pMeterPerPixel);
                    if (MaxMetersPerPixelGrid100m >= MaxMetersPerPixelGrid1km) MaxMetersPerPixelGrid100m = Math.Min(MaxMetersPerPixelGrid1km - 0.1, LimitMetersPerPixelGrid100m);
                    if (MaxMetersPerPixelGrid10km <= MaxMetersPerPixelGrid1km) MaxMetersPerPixelGrid10km = Math.Min(MaxMetersPerPixelGrid1km + 0.1, LimitMetersPerPixelGrid10km);
                    break;
                case DrawMgrsRaster.EGridPrecision.Grid10km:
                    MaxMetersPerPixelGrid10km = Math.Min(LimitMetersPerPixelGrid10km, pMeterPerPixel);
                    if (MaxMetersPerPixelGrid1km >= MaxMetersPerPixelGrid10km) MaxMetersPerPixelGrid1km = Math.Min(MaxMetersPerPixelGrid10km-0.1, LimitMetersPerPixelGrid1km);
                    if (MaxMetersPerPixelGrid100m >= MaxMetersPerPixelGrid1km) MaxMetersPerPixelGrid100m = Math.Min(MaxMetersPerPixelGrid1km-0.1, LimitMetersPerPixelGrid100m);
                    
                    break;
                case DrawMgrsRaster.EGridPrecision.Grid100km:
                    SetLevel(DrawMgrsRaster.EGridPrecision.Grid10km, pMeterPerPixel - 0.1);
                    break;
            }
           
           
           
            
        }
    }
}
