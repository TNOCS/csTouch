using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using System.Xml.Serialization;
using Caliburn.Micro;
using csCommon.Types.DataServer.Interfaces;
using PoiServer.PoI;
using ProtoBuf;
using csShared.Utils;

namespace DataServer
{
    using csCommon.Types.DataServer.PoI;

    // TODO REVIEW: There is a file called PoiStyle that contains various enums, and a file called NPoIStyle that contains a class called PoIStyle.

    [ProtoContract]
    [DebuggerDisplay("Name: {Name}; IconUri: {IconUri}; Icon: {Icon}; Picture: {Picture}")]
    public class PoIStyle : PropertyChangedBase, IConvertibleXmlWithSettings, IReadonlyPoIStyle
    {
        public const string DefaultNameLabel = "Name";

        private DrawingModes? drawingMode;
        private Color?        fillColor;
        private double?       iconHeight;
        //private Uri         iconUri;
        private double?       iconWidth;
        private BitmapSource  picture;
        private byte[]        pictureByteArray;
        private Color?        strokeColor;
        private double?       strokeWidth;
        public event          EventHandler StyleChanged;
        private Color?        callOutFillColor;
        private bool?         cluster;

        private bool? scalePoi;
        private double? scaleStartResolution;
        private double? scaleUnits;
        private double? maxScale;

        public string Folder { get; set; }

        [ProtoMember(1), XmlAttribute]
        public double? IconWidth
        {
            get { return iconWidth; }
            set
            {
                iconWidth = value; 
                NotifyOfPropertyChange(() => IconWidth);
            }
        }

        [ProtoMember(2), XmlAttribute]
        public double? IconHeight
        {
            get { return iconHeight; }
            set
            {
                if (iconHeight == value) return;
                iconHeight = value;
                NotifyOfPropertyChange(() => IconHeight);
            }
        }

        public Uri IconUri {
            get {
                if (icon == null) return null;
                return (icon.StartsWith("http", StringComparison.InvariantCultureIgnoreCase) || icon.StartsWith("file", StringComparison.InvariantCultureIgnoreCase))
                           ? new Uri(icon)
                           : new Uri(Folder + @"//_Media//" + icon, UriKind.RelativeOrAbsolute); //@"file://" + 
            }
        }

        private string icon;

        [ProtoMember(3), XmlIgnore]
        public string Icon
        {
            get { return icon; }
            set
            {
                if (icon == value) return;
                icon = value; NotifyOfPropertyChange(()=>Icon); NotifyOfPropertyChange(()=>IconUri);
            }
        }

        private AnalysisMetaInfo analysis = new AnalysisMetaInfo();

        [ProtoMember(36)]
        public AnalysisMetaInfo Analysis
        {
            get { return analysis; }
            set { analysis = value; NotifyOfPropertyChange(()=>Analysis); }
        }
        
        public void TriggerStyleChanged()
        {
            if (StyleChanged != null) StyleChanged(this, null);
        }

        //public static byte[] ImageToByteArray(BitmapSource bitmapSource)
        //{
        //    byte[] imgAsByteArray = null;

        //    if (bitmapSource != null)
        //    {
        //        (new WriteableBitmap(bitmapSource)).CopyPixels(imgAsByteArray,bit);
        //        imgAsByteArray = (new WriteableBitmap(bitmapSource)).Pixels.SelectMany(p => new byte[] 
        //    { 

        //        ( byte )  p        , 
        //        ( byte )( p >> 8  ), 
        //        ( byte )( p >> 16 ), 
        //        ( byte )( p >> 24 ) 

        //    }).ToArray();
        //    }

        //    return imgAsByteArray;
        //}

        [XmlIgnore]
        public BitmapSource Picture
        {
            get { return picture; }
            set
            {
                if (Equals(picture, value)) return;
                picture = value;
                NotifyOfPropertyChange(() => Picture);
            }
        }

        //[ProtoMember(4), XmlIgnore]
        public byte[] PictureByteArray {
            get {
                //if (picture == null) return null;
                return pictureByteArray;
            }
            set {
                if (value == null || value.Length == 0) return;
                pictureByteArray = value;
            }
        }

        [ProtoMember(5, DataFormat = DataFormat.FixedSize)]
        private string Fill
        {
            get { return FillColor.ToString(); }
            set { 
                FillColor = string.IsNullOrEmpty(value) ? new Color?() : ColorReflector.ToColorFromHex(value); 
            }
        }

        public Color? FillColor
        {
            get { return fillColor; }
            set
            {
                if (fillColor == value) return;
                fillColor = value;
                NotifyOfPropertyChange(() => FillColor);
            }
        }

        [ProtoMember(6, DataFormat = DataFormat.FixedSize)]
        private string Stroke
        {
            get { return StrokeColor.ToString(); }
            set { StrokeColor = ColorReflector.ToColorFromHex(value); }
        }
        
        public Color? StrokeColor
        {
            get { return strokeColor; }
            set
            {
                if (strokeColor == value) return;
                strokeColor = value;
                NotifyOfPropertyChange(() => StrokeColor);
            }
        }

        /// <summary>
        ///     How should the points be used for drawing
        /// </summary>
        [ProtoMember(7, IsRequired = true), XmlAttribute]
        public DrawingModes? DrawingMode
        {
            get { return drawingMode; }
            set
            {
                if (drawingMode == value) return;
                drawingMode = value;
                NotifyOfPropertyChange(() => DrawingMode);
            }
        }

        [ProtoMember(8), XmlAttribute]
        public double? StrokeWidth
        {
            get { return strokeWidth; }
            set
            {
                if (strokeWidth == value) return;
                strokeWidth = value;                
                NotifyOfPropertyChange(() => StrokeWidth);
                
            }
        }

        [ProtoMember(9, DataFormat = DataFormat.FixedSize)]
        public string CallOutFillColorString
        {
            get { return CallOutFillColor.ToString(); }
            set
            {
                if (!string.IsNullOrEmpty(value)) CallOutFillColor = ColorReflector.ToColorFromHex(value);
            }
        }
        
        public Color? CallOutFillColor
        {
            get { return callOutFillColor; }
            set { callOutFillColor = value; NotifyOfPropertyChange(() => CallOutFillColor); }
        }

        [ProtoMember(10, DataFormat = DataFormat.FixedSize)]
        public string CallOutForegroundString
        {
            get { return CallOutForeground.ToString(); }
            set
            {
                if (!string.IsNullOrEmpty(value)) CallOutForeground = ColorReflector.ToColorFromHex(value);
            }
        }

        private Color? callOutForeground;

        
        public Color? CallOutForeground
        {
            get { return callOutForeground; }
            set { callOutForeground = value; NotifyOfPropertyChange(()=>CallOutForeground); }
        }

        private TapMode? tapMode;

        [ProtoMember(11)]
        public TapMode? TapMode
        {
            get { return tapMode; }
            set { tapMode = value; NotifyOfPropertyChange(()=>TapMode); }
        }

        private TitleModes? titleMode;

        [ProtoMember(12)]
        public TitleModes? TitleMode
        {
            get { return titleMode; }
            set { titleMode = value; NotifyOfPropertyChange(()=>TitleMode); }
        }

        private string nameLabel = DefaultNameLabel;

        [ProtoMember(13), DefaultValue(DefaultNameLabel)]
        public string NameLabel
        {
            get { return nameLabel; }
            set
            {
                if (value == nameLabel) return;
                nameLabel = value; NotifyOfPropertyChange(()=>NameLabel);
            }
        }

        private string innerTextLabel = string.Empty;

        [ProtoMember(4), DefaultValue("")]
        public string InnerTextLabel
        {
            get { return innerTextLabel; }
            set
            {
                if (value == innerTextLabel) return;
                innerTextLabel = value; NotifyOfPropertyChange(() => InnerTextLabel);
            }
        }

        private double? minResolution;

        [ProtoMember(14)]
        public double? MinResolution
        {
            get { return minResolution; }
            set { minResolution = value; NotifyOfPropertyChange(()=>MinResolution); }
        }

        private double? maxResolution;

        [ProtoMember(15)]
        public double? MaxResolution
        {
            get { return maxResolution; }
            set { maxResolution = value; NotifyOfPropertyChange(()=>MaxResolution); }
        }

        private bool? autoCallOutLabels;

        [ProtoMember(16), DefaultValue(false)]
        public bool? AutoCallOutLabels
        {
            get { return autoCallOutLabels; }
            set { autoCallOutLabels = value; NotifyOfPropertyChange(()=>AutoCallOutLabels); }
        }

        private double? strokeOpacity;

        [ProtoMember(17)]
        public double? StrokeOpacity
        {
            get { return strokeOpacity; }
            set
            {
                if (strokeOpacity == value) return;
                strokeOpacity = value; NotifyOfPropertyChange(()=>StrokeOpacity);
                
            }
        }

        private double? fillOpacity;

        [ProtoMember(18), DefaultValue(1.0)]
        public double? FillOpacity
        {
            get { return fillOpacity; }
            set
            {
                if (fillOpacity == value) return;
                fillOpacity = value; NotifyOfPropertyChange(()=>FillOpacity);
            }
        }

        
        private bool? visible;

        [ProtoMember(19), DefaultValue(true)]
        public bool? Visible
        {
            get { return visible; }
            set
            {
                if (visible == value) return;
                visible = value; NotifyOfPropertyChange(()=>Visible);
            }
        }

        private string name;

        [ProtoMember(20)]
        public string Name
        {
            get { return name; }
            set { name = value; NotifyOfPropertyChange(()=>Name); }
        }

        private string baseStyle;

        [ProtoMember(21)]
        public string BaseStyle
        {
            get { return baseStyle; }
            set { baseStyle = value; NotifyOfPropertyChange(()=>BaseStyle); }
        }

        private bool? canEdit;

        [ProtoMember(22), DefaultValue(true), XmlAttribute]
        public bool? CanEdit
        {
            get { return canEdit; }
            set { canEdit = value; NotifyOfPropertyChange(()=>CanEdit); }
        }

        private bool? canMove;

        
        public bool? CanMove
        {
            get { return canMove; }
            set { canMove = value; NotifyOfPropertyChange(()=>CanMove); }
        }

        [ProtoMember(23), DefaultValue("true"), XmlAttribute]
        public string CanMoveString
        {
            get
            {
                return (CanMove.HasValue) ? CanMove.Value.ToString() : string.Empty;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    CanMove = null;
                }
                else
                {
                    CanMove = bool.Parse(value);
                    NotifyOfPropertyChange(() => CanMove);
                }
            }
        }

        private bool? canDelete;

        [ProtoMember(24), DefaultValue(true), XmlAttribute]
        public bool? CanDelete
        {
            get { return canDelete; }
            set { canDelete = value; NotifyOfPropertyChange(()=>CanDelete); }
        }

        private bool? canRotate;

        [ProtoMember(25), DefaultValue(true), XmlAttribute]
        public bool? CanRotate
        {
            get { return canRotate; }
            set { canRotate = value; NotifyOfPropertyChange(()=>CanRotate); }
        }

        private string category;
        [ProtoMember(26), DefaultValue("Other"), XmlAttribute]
        public string Category {
            get { return category; }
            set {
                category = value;
                NotifyOfPropertyChange(() => Category);
            }
        }

        private CallOutOrientation? callOutOrientation;

        [ProtoMember(27)]
        public CallOutOrientation? CallOutOrientation
        {
            get { return callOutOrientation; }
            set { callOutOrientation = value; NotifyOfPropertyChange(()=>CallOutOrientation); }
        }

        private int? callOutTimeOut; // seconds 

        [ProtoMember(28)]
        public int? CallOutTimeOut {
            get { return callOutTimeOut; }
            set { callOutTimeOut = value; NotifyOfPropertyChange(() => CallOutTimeOut); }
        }

        private TimelineBehaviours? timelineBehaviour;

        [ProtoMember(29)]
        public TimelineBehaviours? TimelineBehaviour
        {
            get { return timelineBehaviour; }
            set { timelineBehaviour = value; NotifyOfPropertyChange(()=>TimelineBehaviour); }
        }

        private string showOnTimeline;

        [ProtoMember(30)]
        public string ShowOnTimeline
        {
            get { return showOnTimeline; }
            set { showOnTimeline = value; NotifyOfPropertyChange(()=>ShowOnTimeline); }
        }

        private AddModes? addMode;

        [ProtoMember(32)]
        public AddModes? AddMode
        {
            get { return addMode; }
            set { addMode = value; NotifyOfPropertyChange(() => AddMode); }
        }

        private double? maxTitleResolution;

        [ProtoMember(33)]
        public double? MaxTitleResolution
        {
            get { return maxTitleResolution; }
            set { maxTitleResolution = value; }
        }

        private double? callOutMaxWidth;

        [ProtoMember(34)]
        public double? CallOutMaxWidth
        {
            get { return callOutMaxWidth; }
            set { callOutMaxWidth = value; NotifyOfPropertyChange(()=>CallOutMaxWidth); }
        }

        private double? callOutMinHeight;

        [ProtoMember(37)]
        public double? CallOutMinHeight
        {
            get { return callOutMinHeight; }
            set { callOutMinHeight = value; NotifyOfPropertyChange(() => CallOutMinHeight); }
        }

        [ProtoMember(35, DataFormat = DataFormat.FixedSize)]
        private string TextColor
        {
            get { return InnerTextColor.ToString(); }
            set { InnerTextColor = ColorReflector.ToColorFromHex(value); }
        }

        private string subTitles;

        [ProtoMember(38)]
        public string SubTitles
        {
            get { return subTitles; }
            set { subTitles = value; }
        }
        

        private Color? innerTextColor;

        
        public Color? InnerTextColor
        {
            get { return innerTextColor; }
            set
            {
                if (innerTextColor == value) return;
                innerTextColor = value;
                NotifyOfPropertyChange(() => InnerTextColor);
                
            }
        }

        public bool? Cluster
        {
            get { return cluster; }   
            set
            {
                if (cluster == value) return;
                if (cluster.HasValue && !cluster.Value)
                {
                    
                }
                cluster = value;
                NotifyOfPropertyChange(()=>Cluster);
            }
        }

        /// <summary>
        /// When true, scales the PoI so at higher resolutions, the PoI doesn't take so much space.
        /// </summary>
        public bool? ScalePoi
        {
            get { return scalePoi; }
            set
            {
                if (scalePoi == value) return;
                scalePoi = value;
                NotifyOfPropertyChange(()=>ScalePoi);
            }
        }

        /// <summary>
        /// When ScalePoi is true, determines the resolution at which to start scaling the PoI.
        /// </summary>
        public double? ScaleStartResolution
        {
            get { return scaleStartResolution; }
            set
            {
                if (scaleStartResolution == value) return;
                scaleStartResolution = value;
                NotifyOfPropertyChange(()=>ScaleStartResolution);
            }
        }

        /// <summary>
        /// When ScalePoi is true, determines the scale at which the PoI is scaled.
        /// </summary>
        public double? ScaleUnits
        {
            get { return scaleUnits; }
            set
            {
                if (scaleUnits == value) return;
                scaleUnits = value;
                NotifyOfPropertyChange(()=>ScaleUnits);
            }
        }

        /// <summary>
        /// When ScalePoi is true, specifies the maximum scale factor, i.e. 
        /// when the original size is 32 and maxScale is 4, the minimum size will be 32/4 = 8.
        /// </summary>
        public double? MaxScale
        {
            get { return maxScale; }
            set
            {
                if (maxScale == value) return;
                maxScale = value;
                NotifyOfPropertyChange(()=>MaxScale);
            }
        }

        public static PoIStyle defaultStyle;

        public static PoIStyle GetBasicStyle(string name = "Name", DrawingModes mode = DrawingModes.Point)
        {
            if (defaultStyle != null) return defaultStyle;
            defaultStyle = new PoIStyle
            {
                MinResolution      = 0,
                MaxResolution      = -1,
                DrawingMode        = mode,
                CallOutForeground  = Colors.Black,
                CallOutFillColor   = Colors.White,
                StrokeOpacity      = 1.0,
                StrokeWidth        = 1.0,
                StrokeColor        = Colors.Black,
                FillColor          = Colors.Black,
                FillOpacity        = .25,
                NameLabel          = name,
                Visible            = true,
                Category           = "Other",
                Icon               = "",
                CanDelete          = true,
                CanRotate          = true,
                CanEdit            = true,
                CanMove            = true,
                CallOutOrientation = DataServer.CallOutOrientation.Right,
                CallOutTimeOut     = 5,
                IconHeight         = 30,
                IconWidth          = 30,
                ShowOnTimeline     = null,
                TapMode            = DataServer.TapMode.CallOutPopup,
                TitleMode          = TitleModes.None,
                AddMode            = AddModes.Silent,
                TimelineBehaviour  = TimelineBehaviours.None,
                AutoCallOutLabels  = false,
                MaxTitleResolution = -1,
                InnerTextColor     = Colors.White,
                Cluster            = false,
                ScalePoi           = null,
                ScaleStartResolution = null,
                MaxScale           = null,
                ScaleUnits         = null,
                SubTitles =        ""
            };
            return defaultStyle;
        }

        public XElement ToXml(ServiceSettings service)
        {
            return ToXml();
        }

        public XElement ToXml()
        {
            //var bs = settings.NBaseStyles.FirstOrDefault(k => k.Name == BaseStyle) ??
            //         new PoIStyle { MaxResolution = -1, StrokeOpacity = 1.0, CallOutOrientation = CallOutOrientation, 
            //             CallOutTimeOut = 5, FillOpacity = 1.0, NameLabel = "Name", Visible = true, Icon = "" };

            var res = new XElement("Style");
            if (!string.IsNullOrEmpty(Name))           res.SetAttributeValue("Name", Name);
            if (DrawingMode.HasValue)                  res.SetAttributeValue("DrawingMode", DrawingMode.Value);
            if (!string.IsNullOrEmpty(Category))       res.SetAttributeValue("Category", Category);
            if (!string.IsNullOrEmpty(BaseStyle))      res.SetAttributeValue("BaseStyle", BaseStyle);
            if (MaxTitleResolution.HasValue)           res.SetAttributeValue("MaxTitleResolution", MaxTitleResolution.Value);
            if (IconWidth.HasValue)                    res.SetAttributeValue("IconWidth", IconWidth.Value);
            if (IconHeight.HasValue)                   res.SetAttributeValue("IconHeight", IconHeight.Value);
            if (!string.IsNullOrEmpty(Icon))           res.SetAttributeValue("IconUri", Icon);
            if (CanDelete.HasValue)                    res.SetAttributeValue("CanDelete", CanDelete.Value);
            if (CanMove.HasValue)                      res.SetAttributeValue("CanMove", CanMove.Value);
            if (CanEdit.HasValue)                      res.SetAttributeValue("CanEdit", CanEdit.Value);
            if (CanRotate.HasValue)                    res.SetAttributeValue("CanRotate", CanRotate.Value);
            if (Visible.HasValue)                      res.SetAttributeValue("Visible", Visible.Value);
            if (FillColor.HasValue)                    res.SetAttributeValue("FillColor", FillColor.Value);
            if (FillOpacity.HasValue)                  res.SetAttributeValue("FillOpacity", FillOpacity.Value);
            if (StrokeColor.HasValue)                  res.SetAttributeValue("StrokeColor", StrokeColor.Value);
            if (StrokeOpacity.HasValue)                res.SetAttributeValue("StrokeOpacity", StrokeOpacity.Value);
            if (StrokeWidth.HasValue)                  res.SetAttributeValue("StrokeWidth", StrokeWidth.Value);
            if (CallOutFillColor.HasValue)             res.SetAttributeValue("CallOutFillColor", CallOutFillColor.Value);
            if (CallOutForeground.HasValue)            res.SetAttributeValue("CallOutForeground", CallOutForeground.Value);
            if (CallOutOrientation.HasValue)           res.SetAttributeValue("CallOutOrientation", CallOutOrientation.Value);
            if (TapMode.HasValue)                      res.SetAttributeValue("TapMode", TapMode.Value);
            if (AutoCallOutLabels.HasValue)            res.SetAttributeValue("AutoCallOutLabels", AutoCallOutLabels.Value);
            if (TitleMode.HasValue)                    res.SetAttributeValue("TitleMode", TitleMode.Value);
            if (NameLabel !=                           DefaultNameLabel) res.SetAttributeValue("NameLabel", NameLabel);
            if (MinResolution.HasValue)                res.SetAttributeValue("MinResolution", MinResolution.Value);
            if (MaxResolution.HasValue)                res.SetAttributeValue("MaxResolution", MaxResolution.Value);
            if (CallOutTimeOut.HasValue)               res.SetAttributeValue("CallOutTimeOut", CallOutTimeOut.Value);
            if (TimelineBehaviour.HasValue)            res.SetAttributeValue("TimelineBehaviour", TimelineBehaviour.Value);
            if (!string.IsNullOrEmpty(ShowOnTimeline)) res.SetAttributeValue("ShowOnTimeline", ShowOnTimeline);
            if (AddMode.HasValue)                      res.SetAttributeValue("AddMode", AddMode.Value);
            if (CallOutMaxWidth.HasValue)              res.SetAttributeValue("CallOutMaxWidth", CallOutMaxWidth.Value);
            if (CallOutMinHeight.HasValue)             res.SetAttributeValue("CallOutMinHeight", CallOutMinHeight.Value);
            if (!string.IsNullOrEmpty(InnerTextLabel)) res.SetAttributeValue("InnerTextLabel", InnerTextLabel);
            if (InnerTextColor.HasValue)               res.SetAttributeValue("InnerTextColor", InnerTextColor.Value);
            if (Cluster.HasValue)                      res.SetAttributeValue("Cluster", cluster.Value);
            if (ScalePoi.HasValue)                     res.SetAttributeValue("ScalePoi", ScalePoi.Value);
            if (ScaleStartResolution.HasValue)         res.SetAttributeValue("ScaleStartResolution", ScaleStartResolution.Value);
            if (ScaleUnits.HasValue)                   res.SetAttributeValue("ScaleUnits", ScaleUnits.Value);
            if (MaxScale.HasValue)                     res.SetAttributeValue("MaxScale", MaxScale.Value);
            if (!string.IsNullOrEmpty(SubTitles)) res.SetAttributeValue("SubTitles", SubTitles);
            if (Analysis == null || !Analysis.Highlights.Any()) return res;
            
            
            var xa = new XElement("AnalysisMetaInfo");
            var xh = new XElement("Highlights");
            xa.Add(xh);
            foreach (var h in Analysis.Highlights) xh.Add(h.ToXml());
            res.Add(xa);
            return res;
        }

        public string XmlNodeId
        {
            get { return "PoIStyle"; }
        }

        public void FromXml(XElement element)
        {
            FromXml(element, ".", true);  // TODO REVIEW Is "." the correct directory to specify here?
        }

        public void FromXml(XElement res, string directoryName, bool catchException) {
            try {
                Folder                                     = directoryName;
                BaseStyle                                  = res.GetString("BaseStyle");
                FillColor                                  = res.GetNullColor("FillColor");
                StrokeColor                                = res.GetNullColor("StrokeColor");
                var dm                                     = res.GetString("DrawingMode");
                if (!string.IsNullOrEmpty(dm)) DrawingMode = (DrawingModes) Enum.Parse(typeof (DrawingModes), dm);
                strokeWidth                                = res.GetNullDouble("StrokeWidth");
                IconWidth                                  = res.GetNullDouble("IconWidth");
                IconHeight                                 = res.GetNullDouble("IconHeight");
                Icon                                       = res.GetString("IconUri");
                Category                                   = res.GetString("Category");
                CallOutFillColor                           = res.GetNullColor("CallOutFillColor");
                CallOutForeground                          = res.GetNullColor("CallOutForeground");
                var tm                                     = res.GetString("TapMode");
                if (tm!=null) TapMode                      = (TapMode) Enum.Parse(typeof (TapMode), tm);
                var ttm                                    = res.GetString("TitleMode");
                if (ttm != null) TitleMode                 = (TitleModes) Enum.Parse(typeof (TitleModes), ttm);
                MinResolution                              = res.GetNullDouble("MinResolution");
                MaxResolution                              = res.GetNullDouble("MaxResolution");
                NameLabel                                  = res.GetString("NameLabel");
                AutoCallOutLabels                          = res.GetNullBool("AutoCallOutLabels");
                StrokeOpacity                              = res.GetNullDouble("StrokeOpacity");
                FillOpacity                                = res.GetNullDouble("FillOpacity");
                Visible                                    = res.GetNullBool("Visible");
                Name                                       = res.GetString("Name");
                CanRotate                                  = res.GetNullBool("CanRotate");
                CanMove                                    = res.GetNullBool("CanMove");
                CanEdit                                    = res.GetNullBool("CanEdit");
                CanDelete                                  = res.GetNullBool("CanDelete");
                CallOutTimeOut                             = res.GetNullInt("CallOutTimeOut");
                ShowOnTimeline                             = res.GetString("ShowOnTimeline");
                MaxTitleResolution                         = res.GetNullDouble("MaxTitleResolution");
                CallOutMaxWidth                            = res.GetNullDouble("CallOutMaxWidth");
                CallOutMinHeight                           = res.GetNullDouble("CallOutMinHeight");
                InnerTextLabel                             = res.GetString("InnerTextLabel");
                InnerTextColor                             = res.GetNullColor("InnerTextColor");
                Cluster                                    = res.GetBool("Cluster");
                ScalePoi                                   = res.GetNullBool("ScalePoi");
                ScaleStartResolution                       = res.GetNullDouble("ScaleStartResolution");
                ScaleUnits                                 = res.GetNullDouble("ScaleUnits");
                MaxScale                                   = res.GetNullDouble("MaxScale");
                SubTitles = res.GetString("SubTitles");
                var am                                     = res.GetString("AddMode");
                if (am!=null) AddMode                      = (AddModes) Enum.Parse(typeof (AddModes), am);

                var coo = res.GetString("CallOutOrientation");
                if (coo!=null) CallOutOrientation = (CallOutOrientation)Enum.Parse(typeof (CallOutOrientation),coo);

                var tb =  res.GetString("TimelineBehaviour");
                if (tb != null) TimelineBehaviour = (TimelineBehaviours) Enum.Parse(typeof (TimelineBehaviours), tb);
                
                var ami = res.Element("AnalysisMetaInfo");
                if (ami == null) return;
                Analysis = new AnalysisMetaInfo();
                var hlx = ami.Element("Highlights");
                if (hlx == null) return;
                Analysis.Highlights = new List<Highlight>();
                foreach (var hx in hlx.Elements())
                {
                    var h = new Highlight();
                    h.FromXml(hx);
                    h.Style = this;
                    Analysis.Highlights.Add(h);
                }
            }
            catch (Exception e) {
                if (!catchException)
                {
                    throw e;
                }
                Logger.Log("Poi Parser", "Error parsing style", e.Message, Logger.Level.Warning);
            }
        }

        public void LoadIcon()
        {
            // TODO Implement or remove LoadIcon().
//            if (string.IsNullOrEmpty(icon)) return;
//            try
//            {
//                Execute.OnUIThread(() =>
//                {
//                    try
//                    {
//                        //if (FileStore.FileExists(IconUri.ToString())) Picture = new BitmapImage(IconUri);
//                    }
//                    catch (Exception e)
//                    {
//
//                    }
//                });
//            }
//            catch (Exception e)
//            {
//                Logger.Log("PoiStyle", "Icon not found:" + IconUri.AbsolutePath, e.Message, Logger.Level.Error);
//            }
        }

        public static int Count;
        public static long Mergetime;
        public static PoIStyle MergeStyle(IReadonlyPoIStyle s1, IReadonlyPoIStyle s2)
        {
            Count += 1;
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            var r = new PoIStyle
            {
                addMode            = s2.AddMode.HasValue ? s2.AddMode : s1.AddMode,
                category           = !string.IsNullOrEmpty(s2.Category) ? s2.Category : s1.Category, 
                autoCallOutLabels  = s2.AutoCallOutLabels.HasValue ? s2.AutoCallOutLabels : s1.AutoCallOutLabels,
                callOutFillColor   = s2.CallOutFillColor.HasValue ? s2.CallOutFillColor : s1.CallOutFillColor,
                callOutForeground  = s2.CallOutForeground.HasValue ? s2.CallOutForeground : s1.CallOutForeground,
                callOutOrientation = s2.CallOutOrientation.HasValue ? s2.CallOutOrientation : s1.CallOutOrientation,
                callOutTimeOut     = s2.CallOutTimeOut.HasValue ? s2.CallOutTimeOut : s1.CallOutTimeOut,
                canDelete          = s2.CanDelete.HasValue ? s2.CanDelete : s1.CanDelete,
                canEdit            = s2.CanEdit.HasValue ? s2.CanEdit : s1.CanEdit,
                canMove            = s2.CanMove.HasValue ? s2.CanMove : s1.CanMove,
                canRotate          = s2.CanRotate.HasValue ? s2.CanRotate : s1.CanRotate,
                drawingMode        = s2.DrawingMode.HasValue ? s2.DrawingMode : s1.DrawingMode,
                icon               = (string.IsNullOrEmpty(s2.Icon)) ? s1.Icon : s2.Icon,
                iconHeight         = s2.IconHeight.HasValue ? s2.IconHeight : s1.IconHeight,
                iconWidth          = s2.IconWidth.HasValue ? s2.IconHeight : s1.IconHeight,
                maxResolution      = s2.MaxResolution.HasValue ? s2.MaxResolution : s1.MaxResolution,
                maxTitleResolution = s2.MaxTitleResolution.HasValue ? s2.MaxTitleResolution : s1.MaxTitleResolution,
                minResolution      = s2.MinResolution.HasValue ? s2.MinResolution : s1.MinResolution,
                nameLabel          = (string.IsNullOrEmpty(s2.NameLabel)) ? s1.NameLabel : s2.NameLabel,
                innerTextLabel     = (string.IsNullOrEmpty(s2.InnerTextLabel)) ? s1.InnerTextLabel : s2.InnerTextLabel,
                showOnTimeline     = !string.IsNullOrEmpty(s2.ShowOnTimeline) ? s2.ShowOnTimeline : s1.ShowOnTimeline,
                strokeColor        = s2.StrokeColor.HasValue ? s2.StrokeColor : s1.StrokeColor,
                strokeOpacity      = s2.StrokeOpacity.HasValue ? s2.StrokeOpacity : s1.StrokeOpacity,
                strokeWidth        = s2.StrokeWidth.HasValue ? s2.StrokeWidth : s1.StrokeWidth,
                tapMode            = s2.TapMode.HasValue ? s2.TapMode : s1.TapMode,
                timelineBehaviour  = s2.TimelineBehaviour.HasValue ? s2.TimelineBehaviour : s1.TimelineBehaviour,
                titleMode          = s2.TitleMode.HasValue ? s2.TitleMode : s1.TitleMode,
                visible            = s2.Visible.HasValue ? s2.Visible : s1.Visible,
                picture            = s2.Picture ?? s1.Picture,
                fillColor          = s2.FillColor.HasValue ? s2.FillColor : s1.FillColor,
                fillOpacity        = s2.FillOpacity.HasValue ? s2.FillOpacity : s1.FillOpacity,
                callOutMaxWidth    = s2.CallOutMaxWidth.HasValue ? s2.CallOutMaxWidth : s1.CallOutMaxWidth,
                callOutMinHeight   = s2.CallOutMinHeight.HasValue ? s2.CallOutMinHeight : s1.CallOutMinHeight,
                innerTextColor     = s2.InnerTextColor.HasValue ? s2.InnerTextColor : s1.InnerTextColor,
                Cluster            = s2.Cluster.HasValue ? s2.Cluster : s1.Cluster,
                ScalePoi           = s2.ScalePoi.HasValue ? s2.ScalePoi : s1.ScalePoi,
                ScaleStartResolution = s2.ScaleStartResolution.HasValue? s2.ScaleStartResolution : s1.ScaleStartResolution,
                ScaleUnits         = s2.ScaleUnits.HasValue ? s2.ScaleUnits : s1.ScaleUnits,
                MaxScale           = s2.MaxScale.HasValue ? s2.MaxScale : s1.MaxScale,
                SubTitles =         string.IsNullOrEmpty(s2.SubTitles) ? s1.SubTitles : s2.SubTitles
            };
            // If the name label is specified by a style, do not overwrite it.
            if (r.nameLabel == DefaultNameLabel && !string.IsNullOrEmpty(s1.NameLabel)) r.nameLabel = s1.NameLabel;
            //sw.Stop();
            //Mergetime += sw.ElapsedTicks;
            return r;
        }

        public PoIStyle CloneStyle()
        {
            return Clone() as PoIStyle;
        }

        public object Clone()
        {
            var r = new PoIStyle
            {
                IsNotifying        = false,
                StrokeColor        = StrokeColor,
                StrokeWidth        = StrokeWidth,
                DrawingMode        = DrawingMode,
                FillColor          = FillColor,
                CallOutFillColor   = CallOutFillColor,
                CallOutForeground  = CallOutForeground,
                Picture            = Picture,
                PictureByteArray   = PictureByteArray,
                IconHeight         = IconHeight,
                IconWidth          = IconWidth,
                Icon               = Icon,
                TapMode            = TapMode,
                Category           = Category,
                TitleMode          = TitleMode,
                MinResolution      = MinResolution,
                MaxResolution      = MaxResolution,
                NameLabel          = NameLabel,
                AutoCallOutLabels  = AutoCallOutLabels,
                StrokeOpacity      = StrokeOpacity,
                FillOpacity        = FillOpacity,
                Visible            = Visible,
                Name               = Name,
                BaseStyle          = BaseStyle,
                CanRotate          = CanRotate,
                CanEdit            = CanEdit,
                CanDelete          = CanDelete,
                CanMove            = CanMove,
                CallOutOrientation = CallOutOrientation,
                CallOutTimeOut     = CallOutTimeOut,
                TimelineBehaviour  = TimelineBehaviour,
                ShowOnTimeline     = ShowOnTimeline,
                AddMode            = AddMode,
                MaxTitleResolution = MaxTitleResolution,
                CallOutMaxWidth    = CallOutMaxWidth,
                CallOutMinHeight   = CallOutMinHeight,
                InnerTextLabel     = InnerTextLabel,
                InnerTextColor     = InnerTextColor,
                Cluster            = Cluster,
                ScalePoi           = ScalePoi,
                ScaleStartResolution = ScaleStartResolution,
                ScaleUnits         = ScaleUnits,
                MaxScale           = MaxScale
            };

            r.IsNotifying = true;
            return r;
        }

        
    }
}