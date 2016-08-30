using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DataServer;
using PoiServer.PoI;

namespace csCommon.Types.DataServer.PoI
{
    public class DynamicEffectiveStyle : IReadonlyPoIStyle
    {
        private PoIStyle FallbackStyle = PoIStyle.GetBasicStyle(); 

        public DynamicEffectiveStyle(BaseContent pOwner)
        {
            Owner = pOwner;
        }

        public BaseContent Owner { get; private set; }

        public AddModes? AddMode
        {
            get
            {
                if (Owner.PoiType == null) Owner.LookupPropertyPoiTypeInPoITypes();
                return 
                    ((Owner.NAnalysisStyle != null) && (Owner.NAnalysisStyle.AddMode.HasValue)) ? Owner.NAnalysisStyle.AddMode : 
                    ((Owner.Style != null) && (Owner.Style.AddMode.HasValue)) ? Owner.Style.AddMode :
                    ((Owner.PoiType?.Style != null) && (Owner.PoiType.NEffectiveStyle.AddMode.HasValue)) ? Owner.PoiType.NEffectiveStyle.AddMode : 
                    FallbackStyle.AddMode;
            }
        }

        public AnalysisMetaInfo Analysis
        {
            get
            {
                // Deze wordt nooit gezet?!?!
                throw new NotImplementedException();
            }
        }

        public bool? AutoCallOutLabels
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string BaseStyle
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Color? CallOutFillColor
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string CallOutFillColorString
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Color? CallOutForeground
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string CallOutForegroundString
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double? CallOutMaxWidth
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double? CallOutMinHeight
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public CallOutOrientation? CallOutOrientation
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int? CallOutTimeOut
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool? CanDelete
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool? CanEdit
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool? CanMove
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string CanMoveString
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool? CanRotate
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string Category
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool? Cluster
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public DrawingModes? DrawingMode
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Color? FillColor
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double? FillOpacity
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string Icon
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double? IconHeight
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Uri IconUri
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double? IconWidth
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Color? InnerTextColor
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string InnerTextLabel
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double? MaxResolution
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double? MaxScale
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double? MaxTitleResolution
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double? MinResolution
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string Name
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string NameLabel
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public BitmapSource Picture
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public byte[] PictureByteArray
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool? ScalePoi
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double? ScaleStartResolution
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double? ScaleUnits
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string ShowOnTimeline
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Color? StrokeColor
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double? StrokeOpacity
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double? StrokeWidth
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string SubTitles
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public TapMode? TapMode
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public TimelineBehaviours? TimelineBehaviour
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public TitleModes? TitleMode
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool? Visible
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public PoIStyle CloneStyle()
        {
            throw new NotImplementedException();
        }
    }
}
