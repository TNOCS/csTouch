using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Serialization;
using Caliburn.Micro;
using csCommon.Types.DataServer.Interfaces;
using csCommon.Types.DataServer.PoI.IO;
using csCommon.Types.Geometries;
using csCommon.Types.TextAnalysis;
using csCommon.Types.TextAnalysis.TextFinder;
using csCommon.Utils.Collections;
using csDataServerPlugin;
using csEvents.Sensors;
using csShared.Controls.Popups.MapCallOut;
using csShared.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OxyPlot;
using PoiServer.PoI;
using ProtoBuf;
using SharpMap.Geometries;
using Formatting = Newtonsoft.Json.Formatting;
using Point = System.Windows.Point;
#if !WINDOWS_PHONE
using csShared.Documents;
#endif

namespace DataServer
{
    using System.Windows.Data;

    using csCommon.Types.DataServer.PoI;

    [ProtoContract]
    public class BaseContent : PropertyChangedBase, IContent, IComparable, IConvertibleGeoJson, ITextSearchable
    {
        public bool UpdateSensorData = false;
        private BindableCollection<Media> allMedia = new BindableCollection<Media>();
        public List<CallOutAction> CalloutActions = new List<CallOutAction>();

        private string contentId;
        private Dictionary<string, object> data = new Dictionary<string, object>();
        private long dateLong;
        private bool expanded;
        private BaseGeometry geometry;
        private string wktText;

        private Guid id;
        private bool isVisibleInMenu = true;

        private Dictionary<string, string> labels = new Dictionary<string, string>();
        private string layer = string.Empty;
        private int? maxItems;
        private Dictionary<string, IModelPoiInstance> modelInstances = new Dictionary<string, IModelPoiInstance>();
        private List<Model> models;
        private PoIStyle nEffectiveStyle;
        private PoIStyle nstyle;
        private double orientation;
        private string poiTypeId;

        private ObservableCollection<Point> points = new ObservableCollection<Point>();
        private Position position;
        private int priority = 2;
        private SensorSet sensors = new SensorSet();
        private Service service;
        private string timelineString;
        private long updatedLong;
        private WordHistogram keywords;

        public string FormattedTimeStamp
        {
            get { return Date.ToString(CultureInfo.InvariantCulture); }
        }

        [XmlAttribute("name")]
        public string Name
        {
            get
            {
                return Labels.ContainsKey(NEffectiveStyle.NameLabel) ? Labels[NEffectiveStyle.NameLabel] : ContentId;
            }
            set
            {
                Labels[NEffectiveStyle.NameLabel] = value;
                NotifyOfPropertyChange(() => Name);
            }
        }

        public string SubTitlesText
        {
            get
            {
                if (string.IsNullOrEmpty(NEffectiveStyle.SubTitles)) return string.Empty;
                var res = string.Empty;
                var st = NEffectiveStyle.SubTitles.Split(new[] { ',', ';' });
                foreach (var s in st)
                {
                    if (!Labels.ContainsKey(s) || string.IsNullOrEmpty(Labels[s])) continue;
                    var mi = EffectiveMetaInfo.FirstOrDefault(k => string.Equals(k.Label, s));
                    var title = mi == null ? s : mi.Title;
                    res += title + ": " + Labels[s] + "\n";
                }
                return res.Trim('\n');
            }
        }

        [XmlAttribute("innerText")]
        public string InnerText
        {
            get
            {
                var innerTextLabel = NEffectiveStyle.InnerTextLabel;
                return Labels.ContainsKey(innerTextLabel)
                    ? Labels[innerTextLabel]
                    : Sensors.ContainsKey(innerTextLabel)
                        ? Sensors[innerTextLabel].FocusValue.ToString("{0:0}")
                        : String.Empty;
            }
            set
            {
                // Do not set the label in case we are dealing with a sensor.
                var innerTextLabel = NEffectiveStyle.InnerTextLabel;
                if (Sensors.ContainsKey(innerTextLabel)) return;
                Labels[innerTextLabel] = value;
                NotifyOfPropertyChange(() => InnerText);
                HasChanged();
            }
        }

        [XmlIgnore]
        public Dictionary<string, IModelPoiInstance> ModelInstances
        {
            get { return modelInstances; }
            set { modelInstances = value; }
        }

        [XmlIgnore]
        public List<MetaInfo> EffectiveMetaInfo
        {
            get
            {
                if (MetaInfo != null) return MetaInfo;
                if (PoiType != null && PoiType != this && PoiType.EffectiveMetaInfo != null)
                    return PoiType.EffectiveMetaInfo;
                return new List<MetaInfo>();
            }
        }

        [ProtoMember(3, OverwriteList = true), XmlIgnore]
        public BindableCollection<Media> AllMedia
        {
            get { return allMedia; }
            set
            {
                allMedia = value;
                NotifyOfPropertyChange(() => AllMedia);
                allMedia.CollectionChanged += allMedia_CollectionChanged;
            }
        }

        public bool HasMedia
        {
            get { return AllMedia.Any(); }
        }

        [ProtoMember(6)]
        [XmlAttribute, DefaultValue(0)]
        public long DateLong
        {
            get { return dateLong; }
            set
            {
                if (dateLong == value) return;
                dateLong = value;
                NotifyOfPropertyChange(() => DateLong);
                NotifyOfPropertyChange(() => Date);
            }
        }

        [ProtoMember(7)]
        public PoIStyle Style
        {
            get { return nstyle; }
            set
            {
                if (value == nstyle) return;
                nstyle = value;
                NotifyOfPropertyChange(() => Style);
            }
        }

        [ProtoMember(8)]
        public string ContentId // SdJ Note: this sets different attributes in the XML, e.g. PoiId for the Poi tag.
        {
            get { return contentId; }
            set
            {
                contentId = value;
                NotifyOfPropertyChange(() => ContentId);
            }
        }

        public string PoiId // SdJ: Added, because this corresponds to the name in the XML.
        {
            get { return ContentId; }
            set { ContentId = value; }
        }

        [ProtoMember(9, OverwriteList = true)]
        public List<MetaInfo> MetaInfo { get; set; }

        public BaseContent PoiType { get; set; } // See method LookupPropertyPoiTypeInPoITypes()

        [XmlIgnore]
        public PoIStyle NAnalysisStyle { get; set; }

        /// <summary>
        /// NEffectiveStyle is a calculated style, don't change any values in the NEffectiveStyle style
        /// Any manually changes will be overwritten in next merge!
        /// Merge order of style
        /// * Default style (hardcoded values)
        /// * NEffectiveStyle of POI type (if found)
        /// * POI style (if defined)
        /// * NAnalysisStyle
        /// 
        /// Casting IReadonlyPoIStyle to PoIStyle will make properties writeable (not recommended)
        /// </summary>
        [XmlIgnore]
        public IReadonlyPoIStyle NEffectiveStyle
        {
            get
            {
                if (nEffectiveStyle != null) return nEffectiveStyle; // used cached calculated style
                var resultStyle = PoIStyle.GetBasicStyle(); //As default (fallback) use hardcoded default style
                LookupPropertyPoiTypeInPoITypes(); // Set property PoiType if not set

                // If there is a PoI Type and has PoI type a style? Merge it!
                if (PoiType != null && PoiType.Style != null)
                {
                    resultStyle = PoIStyle.MergeStyle(resultStyle, PoiType.NEffectiveStyle);
                }

                // Has the PoI a style? Merge the it!
                if (Style != null)
                {
                    resultStyle = PoIStyle.MergeStyle(resultStyle, Style);
                }

                // Has the PoI a NAnalysisStyle style? Merge the it!
                if (NAnalysisStyle != null)
                {
                    resultStyle = PoIStyle.MergeStyle(resultStyle, NAnalysisStyle);
                }

                nEffectiveStyle = resultStyle; // Cache final merged style!

                NotifyOfPropertyChange(() => InnerText);
                NotifyOfPropertyChange(() => Name);
                return resultStyle;
            }
        }

        /// <summary>
        /// Look up PoiTypeId in the Service.PoITypes (if found, property PoiType is set)
        /// </summary>
        private void LookupPropertyPoiTypeInPoITypes()
        {
            if ((Service is PoiService /* Need service to lookup PoIType */) && 
                (!String.IsNullOrEmpty(PoiTypeId) /* Need PoIType id to lookup*/ ))
            {
                var lookup = ((PoiService)Service).PoITypes.FirstOrDefault(k => k.ContentId == PoiTypeId);
                if ((PoiType == null) || (!PoiType.Equals(lookup)))
                {
                    PoiType = lookup; // Only call when value is changed
                }
            }
        }

        /// <summary>
        ///     A collection of, potentially empty, labels, where each dictionary item is a different representation of this PoI,
        ///     e.g. a JSON, XML, RSS or HTML representation. Note that a PoI may have multipe representations.
        /// 
        /// TODO This is not how this is currently used. Instead, it stores the key-value pairs
        /// </summary>
        [ProtoMember(10, OverwriteList = true), XmlIgnore]
        public Dictionary<string, string> Labels
        {
            get { return labels; }
            set
            {
                if (labels == value) return;
                labels = value;
                NotifyOfPropertyChange(() => Labels);
                HasChanged();
            }
        }

        [ProtoIgnore]
        public bool HasKeywords
        {
            get { return keywords != null && keywords.DistinctWords.Any(); }
        }

        [ProtoIgnore]
        public WordHistogram Keywords // Generates keywords if none are present!
        {
            get
            {
                // If the object has distinct keywords (or we've already derived them), use these.
                if (HasKeywords)
                {
                    return keywords;
                }

                // Otherwise, gather all text and automatically create keywords.
                // TODO Note that this does not take into account that label values may change. Attach LabelChanged event!!!
                var fullText = FullText; // This does not contain keywords, by definition.
                var language = fullText.Language(); // Slow and approximate language extension method.
                keywords = new WordHistogram(language, fullText);
                return keywords;
            }

            set { keywords = value; }
        }

        public string FullText // No keywords!!!
        {
            get
            {
                // TODO This can be cached. Attach to LabelChanged event to remove the cache once something changes.
                var sb = new StringBuilder();
                // sb.Append(Keywords != null ? string.Join(" ", Keywords.DistinctWords) : "");

                // Add everything that is explicitly searchable.
                foreach (var metaInfo in EffectiveMetaInfo.Where(metaInfo => metaInfo.IsSearchable))
                {
                    string labelValue;
                    if (labels.TryGetValue(metaInfo.Label, out labelValue))
                    {
                        sb.Append(labelValue).Append(" ");
                    }
                }

                // Add everything that is not explicitly not searchable.
                var unsearchableLabels = EffectiveMetaInfo.Where(metaInfo => !metaInfo.IsSearchable).Select(metaInfo => metaInfo.Label);
                foreach (var kv in labels.Where(kv => !(unsearchableLabels.Contains(kv.Key))))
                {
                    sb.Append(kv.Value).Append(" ");
                }
                return sb.ToString();
            }
        }

        [ProtoIgnore]
        public long IndexId
        {
            get { return FullText.GetHashCode(); } // TODO Cache this, this is slow.
        }

        [ProtoMember(13), XmlAttribute]
        public string PoiTypeId
        {
            get { return poiTypeId; }
            set
            {
                if (value == PoiTypeId) return;
                poiTypeId = value;
                NotifyOfPropertyChange(() => PoiTypeId);
            }
        }

        [XmlIgnore]
        public DateTime Updated
        {
            get { return UpdatedLong.FromEpoch(); }
            set { UpdatedLong = value.ToEpoch(); }
        }

        [ProtoMember(14), XmlAttribute, DefaultValue(0)]
        public long UpdatedLong
        {
            get { return updatedLong; }
            set
            {
                updatedLong = value;
                NotifyOfPropertyChange(() => UpdatedLong);
                NotifyOfPropertyChange(() => Updated);
                HasChanged();
            }
        }

        [ProtoMember(15), XmlElement]
        public Position Position
        {
            get { return position; }
            set
            {
                if (value != null && value.Equals(position)) return;

                position = value;

                TriggerPositionChanged();
                NotifyOfPropertyChange(() => Position);
                NotifyOfPropertyChange(() => HasPosition);

                TriggerChanged();
                HasChanged();
            }
        }

        [ProtoMember(16, OverwriteList = true), XmlElement]
        //[Obsolete("Using Points attribute instead of Geometry is deprecated. Points are not in sync with the geometry, and do not support complex geometries such as polygons with holes.")]
        public ObservableCollection<Point> Points
        {
            get { return points; }
            set
            {
                // TODO This is shaky business. The drawing code in e.g. dsPoiLayer asks for these points and draws them, but 
                // 1. points and geometry/wkt are not in sync.
                // 2. complex shapes such as polygons with holes in them are flattened if no geometry is defined.
                points = value;
                NotifyOfPropertyChange(() => Points);
                HasChanged();
            }
        }

        [XmlIgnore]
        public BaseGeometry Geometry
        {
            get { return geometry; }
            set
            {
                if (Equals(geometry, value)) return;
                geometry = value;
                wktText = geometry.ConvertToWkt(); // Default projection; keep geometry and WKT in sync.
                NotifyOfPropertyChange(() => Geometry);
                NotifyOfPropertyChange(() => WktText);
                HasChanged();
            }
        }

        [ProtoMember(23)]
        public string WktText
        {
            get { return wktText; }
            set
            {
                if (string.Equals(wktText, value)) return;
                wktText = value;
                geometry = wktText.ConvertFromWkt(); // Keep these in sync.
                NotifyOfPropertyChange(() => Geometry);
                NotifyOfPropertyChange(() => WktText);
            }
        }

        [ProtoMember(17), XmlAttribute]
        public string Layer
        {
            get { return layer; }
            set
            {
                if (layer == value) return;
                layer = value;
                NotifyOfPropertyChange(() => Layer);
            }
        }

        [ProtoMember(18, OverwriteList = true), XmlIgnore]
        public List<Model> Models
        {
            get { return models; }
            set
            {
                models = value;
                NotifyOfPropertyChange(() => Models);
            }
        }

        [ProtoMember(19), XmlIgnore]
        public int? MaxItems
        {
            get { return maxItems; }
            set
            {
                maxItems = value;
                NotifyOfPropertyChange(() => MaxItems);
            }
        }

        public int? ItemsLeft
        {
            get
            {
                if (!MaxItems.HasValue) return new int?();
                var poiService = Service as PoiService;
                if (poiService != null)
                {
                    return MaxItems.Value - poiService.PoIs.Count(k => k.PoiTypeId == ContentId);
                }
                return new int?();
            }
        }

        /// <summary>
        ///     Direction of PoI in degrees, where 0 represents North. In case of icons, 0 is the normal reading mode.
        /// </summary>
        [ProtoMember(20), XmlAttribute, DefaultValue(0)]
        public double Orientation
        {
            get { return orientation; }
            set
            {
                if (Math.Abs(orientation - value) < Double.Epsilon) return;
                orientation = value;
                TriggerPositionChanged();
                NotifyOfPropertyChange(() => Orientation);
                HasChanged();
            }
        }

        [ProtoMember(21), DefaultValue(true)]
        public bool IsVisibleInMenu
        {
            get { return isVisibleInMenu; }
            set
            {
                isVisibleInMenu = value;
                NotifyOfPropertyChange(() => IsVisibleInMenu);
            }
        }

        [XmlIgnore]
        public SensorSet Sensors
        {
            get { return sensors; }
            set
            {
                sensors = value;
                NotifyOfPropertyChange(() => Sensors);
            }
        }

        [XmlAttribute, DefaultValue(false)]
        public bool Expanded
        {
            get { return expanded; }
            set
            {
                expanded = value;
                NotifyOfPropertyChange(() => Expanded);
            }
        }

        /// <summary>
        ///     Needed to maintain the PoI's state in the View.
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, object> Data
        {
            get { return data; }
            set { data = value; }
        }

        [XmlIgnore]
        public Service Service
        {
            get { return service; }
            set
            {
                service = value;
                if (service as PoiService != null)
                {
                    var s = service as PoiService;
                    var pl = s.Layer as dsStaticLayer;
                    if (pl != null)
                    {
                        var subl = pl.GetSubLayer(Layer);
                        if (subl != null)
                            subl.PropertyChanged += (o, e) =>
                            {
                                if (e.PropertyName == "Visible")
                                    IsVisible = subl.Visible;
                            };
                    }
                }
                NotifyOfPropertyChange(() => Service);
            }
        }


        [XmlAttribute, DefaultValue(false)]
        public bool IsVisible { get; set; }

        [XmlAttribute, DefaultValue(false)]
        public bool FilterReset { get; set; }

        [XmlAttribute, DefaultValue(false)]
        public bool HasPosition
        {
            get { return (Position != null || Points.Any()); }
        }

        /// <summary>
        ///     What is the relevance of this Poi (not used for syncing)
        /// </summary>
        [XmlIgnore]
        public int PoiPriority
        {
            get
            {
                var result = 0;
                if (Labels.ContainsKey("Priority") && Int32.TryParse(Labels["Priority"], out result))
                {
                }
                return result;
            }
            set
            {
                Labels["Priority"] = value.ToString(CultureInfo.InvariantCulture);
                NotifyOfPropertyChange(() => PoiPriority);
                TriggerLabelChanged("Priority", "", Labels["Priority"]);
            }
        }

        public int CompareTo(object obj)
        {
            var o = obj as BaseContent;
            if (o != null)
            {
                return -Date.CompareTo(o.Date);
            }

            return 0;
        }

        public event EventHandler<ChangedEventArgs> Changed;

        #region Deleting PoIs

        public event EventHandler<EventArgs> Deleted;

        protected virtual void OnDeleted()
        {
            var handler = Deleted;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        internal void TriggerDeleted() { OnDeleted(); }

        #endregion Deleting PoIs

        [XmlAttribute, DefaultValue("")]
        public string TimelineString
        {
            get { return timelineString; }
            set
            {
                timelineString = value;
                NotifyOfPropertyChange(() => TimelineString);
            }
        }

        public virtual string XmlNodeId
        {
            get { return "Base"; }
        }

        [ProtoMember(2), XmlAttribute, DefaultValue(0)]

        public long RevisionId { get; set; }

        [ProtoMember(1), XmlAttribute("id")]
        public Guid Id
        {
            get { return id; }
            set
            {
                if (id == value) return;
                id = value;
                NotifyOfPropertyChange(() => Id);
            }
        }

        [ProtoMember(4)]
        public string UserId { get; set; }

        [ProtoMember(5)]
        [XmlAttribute, DefaultValue(2)]
        public int Priority
        {
            get { return priority; }
            set
            {
                if (priority == value) return;
                priority = value;
                NotifyOfPropertyChange(() => Priority);
                HasChanged();
            }
        }

        [XmlIgnore]
        public DateTime Date
        {
            get { return DateLong.FromEpoch(); }
            set { DateLong = value.ToEpoch(); }
        }

        [XmlIgnore]
        public bool IsInTransit { get; set; }

        public virtual XElement ToXml()
        {
            return ToXml(null);
        }

        public virtual void FromXml(XElement element)
        {
            FromXml(element, null);
        }

        public virtual XElement ToXml(ServiceSettings settings)
        {
            return null;
        }

        public virtual void FromXml(XElement element, string directoryName)
        {
        }

        public event EventHandler RotationStarted;

        /// <summary>
        /// Update the PoI's style, optionally triggering a label changed and position changed.
        /// </summary>
        /// <param name="poiType"></param>
        /// <param name="triggerLabelChanged"></param>
        /// <param name="triggerUpdatePosition"></param>
        public void UpdateStyle(BaseContent poiType, bool triggerLabelChanged = false, bool triggerUpdatePosition = false)
        {
            if (poiType == null) return;
            PoiTypeId = poiType.ContentId;
            PoiType = poiType;
            foreach (var label in poiType.Labels)
            {
                Labels[label.Key] = label.Value;
            }
            foreach (var data in poiType.Data)
            {
                Data[data.Key] = data.Value;
            }
            ForceUpdate(triggerLabelChanged, triggerUpdatePosition);
        }

        /// <summary>
        /// Update the effective style, optionally triggering a label changed and position changed.
        /// </summary>
        /// <param name="triggerLabelChanged"></param>
        /// <param name="triggerUpdatePosition"></param>
        public void ForceUpdate(bool triggerLabelChanged = false, bool triggerUpdatePosition = false)
        {
            Execute.OnUIThread(() =>
            {
                UpdateEffectiveStyle();
                TriggerUpdated(false);
                if (triggerLabelChanged) TriggerLabelChanged(string.Empty);
                if (triggerUpdatePosition) TriggerPositionChanged();
            });
        }

        public void UpdateEffectiveStyle()
        {
            nEffectiveStyle = null;
        }

        public void TriggerPositionChanged()
        {
            var handler = PositionChanged;
            if (handler != null) handler(this, new PositionEventArgs { Position = position });
            //if (handler != null) Execute.OnUIThread(() => handler(this, new PositionEventArgs { Position = position }));
        }

        public void TriggerItemsLeft()
        {
            NotifyOfPropertyChange(() => ItemsLeft);
        }

        /// <summary>
        ///     triggers changed event, for syncing content
        /// </summary>
        protected void HasChanged()
        {
            var handler = Changed;
            if (handler != null) handler(this, null);
        }

        public event EventHandler<PositionEventArgs> PositionChanged;


        public event EventHandler<LabelChangedEventArgs> LabelChanged;

        public void TriggerLabelChanged(string key, string oldValue = "", string newValue = "")
        {
            UpdateAnalysisStyle();
            TriggerChanged();
            OnLabelChanged(key, oldValue, newValue);
            NotifyOfPropertyChange(() => Labels);
            TriggerUpdated();
        }

        public void OnLabelChanged(string key, string old = "", string newvalue = "")
        {
            var handler = LabelChanged;
            if (handler != null) handler(this, new LabelChangedEventArgs { Label = key, OldValue = old, NewValue = newvalue });
        }

        public event EventHandler<LabelChangedEventArgs> DataChanged;
        public void OnDataChanged(string key, string old = "", string newvalue = "")
        {
            var handler = DataChanged;
            if (handler != null) handler(this, new LabelChangedEventArgs { Label = key, OldValue = old, NewValue = newvalue });
        }

        public ObservableCollection<MetaLabel> GetMetaLabels()
        {
            var metaLabels = new ObservableCollection<MetaLabel>();
            if (EffectiveMetaInfo != null)
            {
                foreach (var m in EffectiveMetaInfo)
                {
                    var ml = new MetaLabel { Meta = m, PoI = this };
                    switch (m.Type)
                    {
                        case MetaTypes.sensor:
                            if (Sensors.ContainsKey(m.Label)) ml.Sensor = Sensors[m.Label];
                            break;
                        case MetaTypes.image:
                            if (Labels.ContainsKey(m.Label))
                            {
                                var im = service.MediaFolder + Labels[m.Label];
#if !WINDOWS_PHONE
                                ml.Document = new Document { OriginalUrl = im, Location = im, FileType = FileTypes.image };
#endif
                            }
                            break;
                        default:

                            if (!Labels.ContainsKey(m.Label))
                            {
                                Labels.Add(m.Label, String.Empty);
                            }
                            ml.Data = Labels[m.Label];
                            break;
                    }
                    metaLabels.Add(ml);
                }
            }

            if (NEffectiveStyle.AutoCallOutLabels.HasValue && !NEffectiveStyle.AutoCallOutLabels.Value)
                return metaLabels;
            foreach (var l in Labels)
            {
                if (metaLabels.Any(k => k.Meta.Label == l.Key)) continue;
                var ml = new MetaLabel
                {
                    Meta = new MetaInfo
                    {
                        Label = l.Key,
                        VisibleInCallOut = true,
                        Title = l.Key,
                        Type = MetaTypes.text
                    },
                    PoI = this,
                    Data = l.Value
                };

                metaLabels.Add(ml);
            }

            return metaLabels;
        }

        public void TriggerChanged()
        {
            var ps = service as PoiService;
            Updated = DateTime.Now;
            if (ps != null)
            {
                ps.TriggerContentChanged(this);
            }
        }

        public void TriggerUpdated(bool visibility = true)
        {
            var ps = service as PoiService;
            if (ps == null) return;
            var staticLayer = ps.Layer as dsStaticLayer;
            if (staticLayer != null)
            {
                staticLayer.UpdatePoiStyle((PoI)this, visibility);
            }
            else
            {
                var ds = (ps.Layer as dsLayer);
                if (ds != null) ds.UpdatePoi((PoI)this); //else Debug.Assert(false, "invalid cast");
            }
        }

        public bool IsVisibleInExtent(BoundingBox t)
        {
            if (t == null) return false;
            if (position != null)
                return t.Contains(new SharpMap.Geometries.Point { Y = position.Latitude, X = position.Longitude });
            return !points.Any() || points.Any(k => t.Contains(new SharpMap.Geometries.Point { Y = k.Y, X = k.X }));
        }

        private void allMedia_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add) return;
            foreach (var mi in e.NewItems.Cast<Media>().Where(mi => mi.Content == null))
            {
                mi.Content = this;
            }
        }

        public void CalculateVisible(double p)
        {
            if (Service.Settings != null && Service.Settings.FilterMap &&
                !((PoiService)Service).SearchContent.Contains(this))
            {
                IsVisible = false;
                return;
            }
            if (NEffectiveStyle.Visible.HasValue && !NEffectiveStyle.Visible.Value)
            {
                IsVisible = false;
            }
            else if (Service.Settings != null)
            {
                var b = p > Service.Settings.MinResolution
                    && (Service.Settings.MaxResolution <= -1
                    || Service.Settings.MaxResolution > p);
                if (!b)
                {
                    IsVisible = false;
                }
                else if (NEffectiveStyle.MinResolution.HasValue && NEffectiveStyle.MaxResolution.HasValue)
                {
                    IsVisible = ((NEffectiveStyle.MinResolution.Value <= -1 || p > NEffectiveStyle.MinResolution.Value) &&
                                 (NEffectiveStyle.MaxResolution.Value <= -1 || NEffectiveStyle.MaxResolution.Value > p));
                }
            }
        }

        #region icon management

        /// <summary>
        ///     Check if Icon exists, if not request it from service
        /// </summary>
        internal async void CheckIcon()
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                if (Style == null || String.IsNullOrEmpty(Style.Icon)) return;
                if (!Service.store.HasFile(Service.MediaFolder, Style.Icon))
                {
                    Service.RequestData("_Media\\" + Style.Icon, IconImageReceived);
                }
                else
                {
                    Style.PictureByteArray = Service.store.GetBytes(Service.MediaFolder, Style.Icon);
                    if (Style.PictureByteArray != null)
                    {
                        if (Style.Icon.ToLower().EndsWith(".png"))
                        {
                            FileStore.LoadImage(Style.PictureByteArray, this);
                        }
                        else
                        {
                            Execute.OnUIThread(() => { Style.Picture = FileStore.LoadPhoto(Style.PictureByteArray); });
                        }
                    }
                    //TriggerUpdated();
                    //nEffectiveStyle = null;
                    Style.TriggerStyleChanged();
                }
            });
        }

        public void IconImageReceived(string anId, byte[] content, Service s)
        {
            if (Style == null) return;
            Style.PictureByteArray = content;
            FileStore.LoadImage(content, this);
            Style.TriggerStyleChanged();
        }

        #endregion

        #region xml methods

        private const string LabelNumPrefix = "_NUM_";
        private const string LabelPercentSubst = "_P_";
        private const string LabelQuoteSubst = "_Q_";

        public void FromXmlBase(ref XElement res, string directoryName)
        {
            //var xmlSerializer = new XmlSerializer(typeof (PoI));
            //var p = xmlSerializer.Deserialize(res.CreateReader());
            try
            {
                Id = res.GetGuid("Id");
                if (Id == Guid.Empty) Id = Guid.NewGuid();
                var n = res.GetString("Name");
                ContentId = res.GetString("PoiId", "");
                if (String.IsNullOrEmpty(ContentId)) ContentId = n;
                Priority = res.GetInt("Priority", 2);
                UserId = res.GetString("UserId");
                DateLong = res.GetLong("Date", DateTime.Now.ToEpoch());
                UpdatedLong = res.GetLong("Updated", DateTime.Now.ToEpoch());
                Layer = res.GetString("Layer");
                MaxItems = res.GetNullInt("MaxItems");
                var xMid = res.Element("MetaInfoData");
                PoiTypeId = res.GetString("PoiTypeId", "");
                IsVisibleInMenu = res.GetBool("IsVisibleInMenu", true);
                Orientation = res.GetDouble("Orientation", 0.0);
                //if (!string.IsNullOrEmpty(PoiTypeId))
                //{

                //}
                if (xMid != null)
                {
                    var metaInfo = new MetaInfoCollection();
                    foreach (var xMi in xMid.Elements())
                    {
                        var mi = new MetaInfo();
                        mi.FromXml(xMi);
                        metaInfo.Add(mi);
                    }
                    MetaInfo = metaInfo;
                }

                if (res.Element("WKT") != null)
                {
                    var xElement = res.Element("WKT");
                    if (xElement != null) WktText = xElement.Value;
                }

                var xlabels = res.Element("Labels");
                if (xlabels != null)
                {
                    Labels = new Dictionary<string, string>();
                    foreach (var xk in xlabels.Elements())
                    {
                        var k = xk.Name.LocalName;
                        // Restore keys starting with numbers or having % or '.
                        k = k.Replace(LabelPercentSubst, "%");
                        k = k.Replace(LabelQuoteSubst, "'");
                        if (k.StartsWith(LabelNumPrefix))
                        {
                            k = k.Substring(LabelNumPrefix.Length);
                        }

                        var s = xk.InnerXml();
                        Labels[k] = s.RestoreInvalidCharacters();
                        Labels[k] = Labels[k].Replace("&lt;", "<").Replace("&gt;", ">");
                    }
                }

                var xkeywords = res.Element("Keywords");
                if (xkeywords != null)
                {
                    Keywords = new WordHistogram(null);
                    Keywords.FromXml(xkeywords);
                }

                if (res.Element("Style") != null)
                {
                    try
                    {
                        var newStyle = new PoIStyle();
                        newStyle.FromXml(res.Element("Style"), directoryName, false); //, Service.Settings); // TODO REVIEW: Settings were ignored.
                        Style = newStyle;
                    }
                    catch (Exception e)
                    {
                        // OK, keep the old style.
                    }
                }

                var media = res.Element("AllMedia");
                if (media != null)
                {
                    AllMedia = new BindableCollection<Media>();
                    foreach (var m in media.Elements())
                    {
                        var me = new Media { Content = this };
                        me.FromXml(m);
                        AllMedia.Add(me);
                    }
                }
                var xpos = res.Element("Position");
                if (xpos != null)
                    Position = new Position(xpos.GetDouble(Position.LONG_LABEL), xpos.GetDouble(Position.LAT_LABEL), xpos.GetDouble(Position.ALT_LABEL)); // TODO Remember other Position attributes.

                var px = res.Element("Points");

                var mo = res.Element("Models");
                if (mo != null)
                {
                    Models = new List<Model>();
                    foreach (var xm in mo.Elements())
                    {
                        var m = new Model();
                        m.FromXml(xm);
                        Models.Add(m);
                    }
                }

                if (px == null) return;
                var pp = px.Value;
                Points = new ObservableCollection<Point>();
                var ppo = pp.Split(' ');
                foreach (var poss in ppo)
                {
                    var split = poss.Split(',');
                    var pt = new Point(
                        Double.Parse(split[0], CultureInfo.InvariantCulture),
                        Double.Parse(split[1], CultureInfo.InvariantCulture));
                    Points.Add(pt);
                }
            }
            catch (SystemException e)
            {
                Logger.Log("DataServer.BaseContent", "Error reading XML " + res + " from " + directoryName, e.Message, Logger.Level.Error, true);
            }
        }

        public XElement ToXmlBase(ServiceSettings settings)
        {
            var res = new XElement(XmlNodeId);
            res.SetAttributeValue("Id", Id);
            if (Style != null) res.Add(Style.ToXml(settings)); // TODO REVIEW PoiStyle ignores these settings.
            if (!String.IsNullOrEmpty(ContentId)) res.SetAttributeValue("PoiId", ContentId);
            if (!String.IsNullOrEmpty(PoiTypeId)) res.SetAttributeValue("PoiTypeId", PoiTypeId);
            if (!String.IsNullOrEmpty(UserId)) res.SetAttributeValue("UserId", UserId);
            if (Priority != 2) res.SetAttributeValue("Priority", Priority);
            res.SetAttributeValue("Date", DateLong);
            if (UpdatedLong != 0) res.SetAttributeValue("Updated", UpdatedLong);
            if (!String.IsNullOrEmpty(Layer)) res.SetAttributeValue("Layer", Layer);
            if (MaxItems.HasValue) res.SetAttributeValue("MaxItems", MaxItems.Value);
            if (!IsVisibleInMenu) res.SetAttributeValue("IsVisibleInMenu", IsVisibleInMenu);
            if (!String.IsNullOrEmpty(WktText)) res.Add(new XElement("WKT") { Value = WktText });
            if (!Orientation.IsZero()) res.SetAttributeValue("Orientation", Orientation);
            if (HasKeywords)
            {
                res.Add(Keywords.ToXml());
            }
            if (Labels != null && Labels.Any())
            {
                var lab = new XElement("Labels");
                foreach (var l in Labels)
                {
                    var k = l.Key.Replace(':', '-').Replace(' ', '_').Replace('/', '-').Trim();
                    if (String.IsNullOrEmpty(k)) continue;
                    // Make sure keys do not start with numbers or have % signs or quotes.
                    k = k.Replace("%", LabelPercentSubst);
                    k = k.Replace("'", LabelQuoteSubst);
                    if (Regex.IsMatch(k, @"^\d+") || k.StartsWith("%"))
                    {
                        k = LabelNumPrefix + k;
                    }
                    var lc = new XElement(k) { Value = l.Value.RemoveInvalidCharacters() };
                    lab.Add(lc);
                }
                res.Add(lab);
            }
            if (AllMedia.Any())
            {
                var media = new XElement("AllMedia");
                foreach (var m in AllMedia) media.Add(m.ToXml());
                res.Add(media);
            }
            if (MetaInfo != null)
            {
                if (PoiType == null || PoiType.MetaInfo != MetaInfo)
                {
                    var mid = new XElement("MetaInfoData");
                    foreach (var mi in MetaInfo) mid.Add(mi.ToXml());
                    res.Add(mid);
                }
            }
            if (Position != null)
            {
                var pos = new XElement("Position"); // TODO Remember other Position attributes.
                pos.SetAttributeValue(Position.LONG_LABEL, Position.Longitude);
                pos.SetAttributeValue(Position.LAT_LABEL, Position.Latitude);
                pos.SetAttributeValue(Position.ALT_LABEL, Position.Altitude);
                res.Add(pos);
            }


            if (Models != null)
            {
                var xm = new XElement("Models");
                foreach (var m in Models)
                {
                    xm.Add(m.ToXml());
                }
                res.Add(xm);
            }

            if (Points == null || !Points.Any()) return res;
            var sb = new StringBuilder();
            foreach (var point in Points)
            {
                sb.Append(String.Format(CultureInfo.InvariantCulture, "{0:0.0000000},{1:0.0000000} ", point.X, point.Y));
            }

            var pe = new XElement("Points") { Value = sb.ToString().Trim() };
            res.Add(pe);

            return res;
        }

        #endregion

        #region helper methods

        public void StartRotation()
        {
            if (RotationStarted != null) RotationStarted(this, null);
        }

        public void AddSensorData(string sensor, DateTime date, double value, string description = "", bool trigger = true)
        {
            if (!Sensors.ContainsKey(sensor))
            {
                Sensors[sensor] = new DataSet
                {
                    Data = new ConcurrentObservableSortedDictionary<DateTime, double>(),
                    Sensor = new csEvents.Sensors.Sensor { Description = description, Id = sensor }
                };
                //Sensors.Add(sensor, new DataSet
                //{
                //    Data = new ConcurrentObservableSortedDictionary<DateTime, double>(),
                //    Sensor = new csEvents.Sensors.Sensor { Description = description, Id = sensor }
                //});
            }
            Sensors[sensor].Data[date] = value;
            if (trigger) Sensors[sensor].TriggerUpdated();
        }

        public void AddLocalMediaItem(string pId, string img)
        {
            if (AllMedia == null) AllMedia = new BindableCollection<Media>();
            AllMedia.Add(new Media { Id = pId, LocalPath = img });
        }

        public void AddPosition(Position myPosition, DateTime date, bool track = true)
        {
            Position = myPosition;
            if (!track) return;
            AddSensorData("[lat]", date, Position.Latitude);
            AddSensorData("[lon]", date, Position.Longitude);
        }

        /// <summary>
        /// Add meta info to the PoIType.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="type"></param>
        /// <param name="searchable"></param>
        /// <param name="callout"></param>
        /// <param name="section"></param>
        /// <param name="options"></param>
        /// <param name="minvalue"></param>
        /// <param name="maxvalue"></param>
        /// <param name="format"></param>
        /// <param name="editActive"></param>
        /// <param name="canEdit"></param>
        public void AddMetaInfo(string title, MetaTypes type = MetaTypes.text, bool searchable = false,
          bool callout = true, string section = "Info", List<string> options = null, double minvalue = Double.NaN,
          double maxvalue = Double.NaN, string format = null, bool editActive = false, bool canEdit = true)
        {
            var metaInfo = MetaInfo ?? new List<MetaInfo>();
            var mi = new MetaInfo
            {
                Label = title,
                Title = title,
                Type = type,
                VisibleInCallOut = callout,
                IsSearchable = searchable,
                Options = options,
                MinValue = minvalue,
                MaxValue = maxvalue,
                StringFormat = format,
                EditActive = editActive,
                IsEditable = canEdit
            };
            if (!String.IsNullOrEmpty(section))
                mi.Section = section;
            metaInfo.Add(mi);
            MetaInfo = metaInfo;
        }

        /// <summary>
        /// Add meta info to the PoIType.
        /// </summary>
        /// <param name="title">Display title</param>
        /// <param name="source">Source label</param>
        /// <param name="type"></param>
        /// <param name="searchable"></param>
        /// <param name="visibleInCallout"></param>
        /// <param name="section"></param>
        /// <param name="options"></param>
        /// <param name="minvalue"></param>
        /// <param name="maxvalue"></param>
        /// <param name="format"></param>
        /// <param name="editActive"></param>
        /// <param name="canEdit"></param>
        public MetaInfo AddMetaInfo(string title, string source, MetaTypes type = MetaTypes.text, bool searchable = false,
            bool visibleInCallout = true, string section = "Info", List<string> options = null, double minvalue = Double.NaN,
            double maxvalue = Double.NaN, string format = null, bool editActive = false, bool canEdit = true)
        {
            var metaInfo = MetaInfo ?? new List<MetaInfo>();
            var mi = new MetaInfo
            {
                Label = source,
                Title = title,
                Type = type,
                VisibleInCallOut = visibleInCallout,
                IsSearchable = searchable,
                Options = options,
                MinValue = minvalue,
                MaxValue = maxvalue,
                StringFormat = format,
                EditActive = editActive,
                IsEditable = canEdit
            };
            if (!String.IsNullOrEmpty(section))
                mi.Section = section;
            metaInfo.Add(mi);
            MetaInfo = metaInfo;
            return mi;
        }

        /// <summary>
        /// Create a highlighter.
        /// Note that you still need to add it to the Style.Analysis.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="label"></param>
        /// <param name="stringvalue"></param>
        /// <returns></returns>
        public Highlight AddHighlighter(string title, string label, string stringvalue = "")
        {
            var mi = MetaInfo.FirstOrDefault(k => k.Label == label);
            if (mi == null) return null;
            var hl = new Highlight
            {
                Title = title,
                HighlighterType = HighlighterTypes.Highlight,
                PoiType = PoiTypeId,
                SelectionCriteria = label,
                SelectionType = (mi.Type == MetaTypes.sensor) ? SelectionTypes.Sensor : SelectionTypes.Label,
            };

            return hl;
        }


        public void Select()
        {
            if (this.Data.ContainsKey("graphic"))
            {
                var pg = (PoiGraphic)Data["graphic"];
                pg.OpenPopup(null, TapMode.CallOut);
            }
        }
        public Highlight AddFilter(string title, string label, string stringvalue = "", double numbervalue = 0)
        {
            var mi = MetaInfo.FirstOrDefault(k => k.Label == label);
            if (mi == null) return null;
            var hl = new Highlight
            {
                Title = title,
                HighlighterType = HighlighterTypes.FilterThreshold,
                StringValue = stringvalue,
                ThresholdType = ThresholdTypes.Equal,
                PoiType = ContentId,
                SelectionCriteria = label,
                SelectionType = (mi.Type == MetaTypes.sensor) ? SelectionTypes.Sensor : SelectionTypes.Label,
            };
            switch (mi.Type)
            {
                case MetaTypes.text:
                    hl.ValueType = ValueTypes.String;
                    break;
                case MetaTypes.number:
                    hl.ValueType = ValueTypes.Number;
                    hl.ThresHoldValue = numbervalue;
                    hl.ThresholdType = ThresholdTypes.Greater;
                    hl.CalculateMax((PoiService)Service);
                    hl.CalculateMin((PoiService)Service);
                    break;
            }

            //var pt = ((PoiService) service).PoITypes.FirstOrDefault(k => k.ContentId == PoiTypeId);
            if (Style != null) Style.Analysis.Highlights.Add(hl);

            return hl;
        }

        #endregion

        #region analysis

        public void GetEffectiveAnalysis(BaseContent p, ref AnalysisMetaInfo a)
        {
            if (p.PoiType != null) GetEffectiveAnalysis(p.PoiType, ref a);
            if (p.Style != null && p.Style.Analysis != null) a.Highlights.AddRange(p.Style.Analysis.Highlights);
        }

        /// <summary>
        ///     Recalculate analysis style based on filters. Also resets the effective style.
        /// </summary>
        /// <returns></returns>
        public bool UpdateAnalysisStyle()
        {
            // get effective highlights
            var a = new AnalysisMetaInfo { Highlights = new List<Highlight>() };
            GetEffectiveAnalysis(this, ref a);

            //var a = (PoiType != null && PoiType.Style != null && PoiType.Style.Analysis != null)
            //    ? PoiType.Style.Analysis
            //    : (Style!=null && Style.Analysis!=null) ? Style.Analysis : null;

            if (a == null || !a.Highlights.Any()) return false;
            NAnalysisStyle = new PoIStyle();

            foreach (var hl in a.Highlights.Where(k => k.IsActive).OrderBy(k => k.Priority))
                hl.CalculateResult(this);
            nEffectiveStyle = null;
            NotifyOfPropertyChange(() => NEffectiveStyle);
            return true;
        }

        #endregion

        private static readonly DateTime DefaultDate = new DateTime(1970, 1, 1, 0, 0, 0);

        private static XName UppercaseFirst(XName x)
        {
            // Check for empty string.
            return string.IsNullOrEmpty(x.LocalName)
                ? XName.Get(String.Empty)
                : XName.Get(Char.ToUpper(x.LocalName[0]) + x.LocalName.Substring(1));
            // Return char and concat substring.
        }

        /// <summary>
        /// Convert the PoI to a GeoJSON string.
        /// </summary>
        /// <returns>GeoJSON string.</returns>
        public string ToGeoJson()
        {
            var builder = new StringBuilder("{\"type\":\"Feature\",\"geometry\": ");

            if (!string.IsNullOrEmpty(WktText))
            {
                var wellKnownText = new WellKnownTextIO(WktText);
                var geoJson = wellKnownText.ToGeoJson(); // This will include {"type":"...","coordinates":[...]}
                if (!string.IsNullOrEmpty(geoJson))
                {
                    builder.Append(geoJson);
                }
                else
                {
                    builder.Append(Position != null ? Position.ToGeoJson() : new Position(0, 0).ToGeoJson());
                }
            }
            else
            {
                builder.Append(Position != null ? Position.ToGeoJson() : new Position(0, 0).ToGeoJson());
            }

            if (HasKeywords)
            {
                builder.Append(",\"keywords\":");
                builder.Append(Keywords.ToGeoJson());
            }

            builder.Append(",\"properties\":{");
            var first = true;
            foreach (var label in Labels)
            {
                var key = label.Key;
                var mi = EffectiveMetaInfo == null
                    ? null
                    : EffectiveMetaInfo.FirstOrDefault(m => String.Equals(m.Label, key));
                var dealtWith = false;
                if (mi == null) mi = new MetaInfo { Type = MetaTypes.text };
                switch (mi.Type)
                {
                    case MetaTypes.number:
                        double resultDouble;
                        var isDouble = Double.TryParse(label.Value, out resultDouble);
                        if (isDouble)
                        {
                            int resultInt;
                            var isInt = Int32.TryParse(label.Value, out resultInt);
                            if (isInt && Math.Abs(resultInt - resultDouble) < 0.000001)
                            {
                                if (!first) builder.Append(",");
                                builder.AppendFormat("\"{0}\":{1}", key, resultInt);
                            }
                            else
                            {
                                if (!first) builder.Append(",");
                                builder.AppendFormat("\"{0}\":{1}", key, resultDouble);
                            }
                            dealtWith = true;
                        }
                        if (!dealtWith)
                        {
                            var value = string.IsNullOrEmpty(label.Value) ? string.Empty : label.Value.Trim().RemoveInvalidCharacters();
                            if (!first) builder.Append(",");
                            builder.AppendFormat("\"{0}\":\"{1}\"", key, value);
                            // builder.AppendFormat("\"{0}\":\"{1}\"", key, label.Value.Trim().Replace('"', '\'').Replace(Environment.NewLine, string.Empty));                    
                        }
                        break;
                    case MetaTypes.options:
                        if (!first) builder.Append(",");
                        if (mi.Options == null) break;
                        var index = mi.Options.IndexOf(label.Value);
                        if (index < 0)
                            builder.AppendFormat("\"{0}\":\"{1}\"", key, string.IsNullOrEmpty(label.Value) ? string.Empty : label.Value.Trim().RemoveInvalidCharacters());
                        else
                            builder.AppendFormat("\"{0}\":{1}", key, index);
                        break;
                    default:
                        if (!first) builder.Append(",");
                        builder.AppendFormat("\"{0}\":\"{1}\"", key, string.IsNullOrEmpty(label.Value) ? string.Empty : label.Value.Trim().RemoveInvalidCharacters());
                        break;
                }
                first = false;
            }
            // EV Do not save the ID, not needed in the GeoJSON.
            //AppendPropertyToGeoJson(builder, GetType().GetProperty("Id"), Labels.Count > 0);
            // REVIEW I don't like having to hardcode the property names.
            //AppendPropertyToGeoJson(builder, GetType().GetProperty("Name")); // Otherwise, the name will be added twice, since it refers to a label
            if (Math.Abs(Orientation) > 0.0001)
                AppendPropertyToGeoJson(builder, GetType().GetProperty("Orientation"));
            if (!string.IsNullOrEmpty(ContentId))
                AppendPropertyToGeoJson(builder, GetType().GetProperty("ContentId"));
            if (!string.IsNullOrEmpty(PoiTypeId))
                AppendPropertyToGeoJson(builder, GetType().GetProperty("PoiTypeId"));
            if (!string.IsNullOrEmpty(Layer)) AppendPropertyToGeoJson(builder, GetType().GetProperty("Layer"));
            if (!DateTime.Equals(Date, DefaultDate))
                AppendPropertyToGeoJson(builder, GetType().GetProperty("Date"));
            if (MaxItems != null && Math.Abs(MaxItems.Value) > 0.0001)
                AppendPropertyToGeoJson(builder, GetType().GetProperty("MaxItems"));
            builder.Append("}}");
            return builder.ToString();
        }

        private void AppendPropertyToGeoJson(StringBuilder builder, PropertyInfo property, bool startWithComma = true)
        {
            try
            {
                // TODO Use TryGetValue and remove the try/catch clause.
                var name = property.Name;
                var value = property.GetValue(this).ToString();
                if (Equals(name, "PoiTypeId"))
                {
                    // Do not save the default template
                    if (string.Equals(value, "default", StringComparison.InvariantCultureIgnoreCase)) return;
                    name = "FeatureTypeId"; // Ugly consequence of changing the file format, but not the code.
                }
                if (startWithComma) builder.Append(", ");
                builder.AppendFormat("\"{0}\":\"{1}\"", name, value);
            }
            catch
            {
                // Nothing. Property simply is missing.
            }
        }

        public void TypeFromGeoJson(JProperty json)
        {
            MetaInfo = new List<MetaInfo>();
            foreach (var childJ in json.Children().OfType<JObject>())
            {
                JToken tokenS;
                childJ.TryGetValue("style", out tokenS);
                if (tokenS != null)
                {
                    // First, convert the JSON to XML.
                    var styleNode = JsonConvert.DeserializeXmlNode("{style:" + tokenS.ToString() + "}");
                    var styleDoc = styleNode.ToXDocument();
                    var xElement = styleDoc.Element(XName.Get("style"));
                    if (xElement != null)
                    {
                        // Convert child nodes to attributes.
                        foreach (var el in xElement.Elements())
                        {
                            xElement.Add(new XAttribute(PoI.UppercaseFirst(el.Name), (string)el));
                        }
                        xElement.Elements().Remove();
                        // Parse the style.
                        try
                        {
                            var newStyle = new PoIStyle();
                            newStyle.FromXml(xElement, ".", false); // Do not catch exception.
                            Style = newStyle;
                        }
                        catch
                        {
                            // Ok, keep old style.
                        }
                    }
                }

                JToken tokenM;
                childJ.TryGetValue("propertyTypeData", out tokenM);
                if (tokenM != null)
                {
                    var metaInfos = tokenM.Children();
                    foreach (var metaInfo in metaInfos)
                    {
                        var newMetaInfo = new MetaInfo();
                        newMetaInfo.FromGeoJson(metaInfo.ToString(Formatting.None), false);
                        MetaInfo.Add(newMetaInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Convert the type information to JSON format.
        /// </summary>
        /// <returns>The DSD poi type information to JSON string.</returns>
        public string TypeToGeoJson() // TODO Review: not the most beautiful solution, since a PoI is used both as data as well as as a template (PoI type).
        {
            var sb = new StringBuilder(String.Format("\"{0}\":{{", ContentId)); // Open OUTER curly braces: #1 (when using string.format, you need {{ to get {
            var fillColorString = NEffectiveStyle.FillColor != null ? NEffectiveStyle.FillColor.Color2Hex() : "#ffffff";
            sb.AppendFormat("\"style\":{{" +                                    // Open STYLE curly braces: #2
                            "\"fillColor\":\"{0}\"," +
                            "\"strokeColor\":\"{1}\"," +
                            "\"drawingMode\":\"{2}\"," +
                            "\"strokeWidth\":{3}," +
                            "\"iconWidth\":{4}," +
                            "\"iconHeight\":{5}," +
                //"\"CallOutFillColor\": \"#FFFFFFFF\", " + 
                //"\"CallOutForeground\": \"#FF000000\", " + 
                //"\"CallOutOrientation\": \"Right\", " + 
                //"\"TapMode\": \"CallOutPopup\", " + 
                //"\"FillOpacity\": \"0.7\", " + 
                //"\"Name\": \"default\", " + 
                //"\"TitleMode\": \"Bottom\", " +
                            "\"iconUri\":\"{6}\"," +
                            "\"nameLabel\":\"{7}\"," +
                            "\"maxTitleResolution\":{8}}}",                                // Close STYLE curly braces: #2
                fillColorString, NEffectiveStyle.StrokeColor.Color2Hex(), NEffectiveStyle.DrawingMode, NEffectiveStyle.StrokeWidth, NEffectiveStyle.IconWidth, NEffectiveStyle.IconHeight, NEffectiveStyle.Icon, NEffectiveStyle.NameLabel, NEffectiveStyle.MaxTitleResolution);
            if (MetaInfo != null && MetaInfo.Count > 0)
            {
                sb.Append(",\"propertyTypeData\":[");
                foreach (var mi in MetaInfo)
                {
                    sb.Append(mi.ToGeoJson());
                    sb.Append(",");
                }
                sb.Remove(sb.Length - 1, 1); // remove the last ,
                sb.Append("]");
            }

            sb.Append("}"); // Close OUTER curly braces: #1
            return sb.ToString();
        }

        public IConvertibleGeoJson FromGeoJson(JObject geoJsonObject, bool newObject = true)
        {
            var poi = (newObject) ? new PoI() : this;

            //var lockId = f["properties"]["lock"].Value<string>();
            //var pos = f["properties"]["pos"].Value<string>();
            //var angle = f["properties"]["angle"].Value<double>();
            foreach (var prp in geoJsonObject["properties"].OfType<JProperty>())
            {
                // TODO REVIEW There are more properties of PoIs that should not end up in the labels, but should be set to a property. Mentioned under ToGeoJson:
                // Orientation, ContentId, Layer, Date, MaxItems.
                if (Equals(prp.Name, "Id"))
                {
                    poi.PoiId = prp.Value.ToString();
                }
                else if (Equals(prp.Name, "FeatureTypeId"))
                {
                    poi.PoiTypeId = prp.Value.ToString();
                }
                else
                {
                    poi.Labels[prp.Name] = prp.Value.ToString().RestoreInvalidCharacters();
                }
            }

            JToken keywordToken;
            if (geoJsonObject.TryGetValue("keywords", out keywordToken))
            {
                var histogram = new WordHistogram(null);
                histogram.FromGeoJson(keywordToken.ToString(Formatting.None), false); // Not very efficient.
                Keywords = histogram;
            }

            var wkt = (new WellKnownTextIO().FromGeoJson(geoJsonObject["geometry"].ToString(Formatting.None), false)).ToString(); // TODO More efficiency if we allow JObjects as input.                
            if (!String.IsNullOrEmpty(wkt))
            {
                poi.WktText = wkt;
            }

            return poi;
        }

        public IConvertibleGeoJson FromGeoJson(string geoJson, bool newObject)
        {
            var f = JObject.Parse(geoJson);
            return FromGeoJson(f, newObject);
        }
    }
}