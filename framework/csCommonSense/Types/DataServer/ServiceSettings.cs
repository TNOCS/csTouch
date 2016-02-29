using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;

namespace DataServer
{
    [ProtoContract]
    public class ServiceSettings : BaseContent
    {
        private string description;

        [ProtoMember(1)]
        public string Description
        {
            get { return description; }
            set { description = value; NotifyOfPropertyChange(() => Description); }
        }

        private bool canEdit = true;

        [ProtoMember(2)]
        public bool CanEdit
        {
            get { return canEdit; }
            set { canEdit = value; NotifyOfPropertyChange(() => CanEdit); }
        }

        private bool canCreate = true;

        [ProtoMember(3)]
        public bool CanCreate
        {
            get { return canCreate; }
            set { canCreate = value; NotifyOfPropertyChange(() => CanCreate); }
        }

        private int syncDelay = 500;

        [ProtoMember(4), DefaultValue(500)]
        public int SyncDelay
        {
            get { return syncDelay; }
            set { syncDelay = value; }
        }

        private Dictionary<string, string> labels = new Dictionary<string, string>();

        [ProtoMember(5)]
        public new Dictionary<string, string> Labels
        {
            get { return labels; }
            set { labels = value; }
        }

        private bool tabBarVisible = true;

        [ProtoMember(6)]
        public bool TabBarVisible
        {
            get { return tabBarVisible; }
            set { tabBarVisible = value; NotifyOfPropertyChange(() => TabBarVisible); }
        }

        private double minResolution;

        [ProtoMember(7), DefaultValue(0.0)]
        public double MinResolution
        {
            get { return minResolution; }
            set { minResolution = value; NotifyOfPropertyChange(() => MinResolution); }
        }

        private double maxResolution = -1;

        [ProtoMember(8), DefaultValue(-1)]
        public double MaxResolution
        {
            get { return maxResolution; }
            set { maxResolution = value; NotifyOfPropertyChange(() => MaxResolution); }
        }

        private SelectionMode selectionMode;

        [ProtoMember(10), DefaultValue(SelectionMode.None)]
        public SelectionMode SelectionMode
        {
            get { return selectionMode; }
            set { selectionMode = value; NotifyOfPropertyChange(() => SelectionMode); }
        }

        private bool filterMap;

        /// <summary>
        /// Filter Map Based on Search
        /// </summary>
        public bool FilterMap
        {
            get { return filterMap; }
            set { filterMap = value; NotifyOfPropertyChange(() => FilterMap); }
        }

        private bool filterLocation;

        public bool FilterLocation
        {
            get { return filterLocation; }
            set
            {
                filterLocation = value; NotifyOfPropertyChange(() => FilterLocation);
            }
        }

        private bool showTimeline;

        [ProtoMember(11), DefaultValue(false)]
        public bool ShowTimeline
        {
            get { return showTimeline; }
            set { showTimeline = value; NotifyOfPropertyChange(() => ShowTimeline); }
        }

        public bool ShareOnline { get; set; }

        public override string XmlNodeId
        {
            get { return "ServiceSetting"; }
        }

        private bool showAnalysis;

        [ProtoMember(12), DefaultValue(false)]
        public bool ShowAnalysis
        {
            get { return showAnalysis; }
            set { showAnalysis = value; NotifyOfPropertyChange(() => ShowAnalysis); }
        }

        private bool sublayersVisible = true;

        [ProtoMember(13), DefaultValue(true)]
        public bool SublayersVisible
        {
            get { return sublayersVisible; }
            set { sublayersVisible = value; }
        }

        [ProtoMember(14), DefaultValue(false)]
        public bool OpenTab { get; set; }

        private string icon;

        [ProtoMember(15)]
        public string Icon
        {
            get { return icon; }
            set { icon = value; NotifyOfPropertyChange(() => Icon); }
        }

        /// <summary>
        /// Comma or semi-colon separated list of layer names, which defines the order in which 
        /// layers are displayed. The order starts at the bottom, working up.
        /// </summary>
        [ProtoMember(16)]
        public string LayerOrder { get; set; }

        [ProtoMember(17)]
        public bool AutoStart { get; set; }

        /// <summary>
        /// In case of 0, only manual backups are possible, otherwise it's the time in minutes between backups.
        /// </summary>
        [ProtoMember(18)]
        public int BackupInterval { get; set; }

        public override void FromXml(XElement element, string directoryName)
        {
            Description      = element.GetString("Description");
            CanEdit          = element.GetBool("CanEdit");
            CanCreate        = element.GetBool("CanCreate");
            SyncDelay        = element.GetInt("SyncDelay", 500);
            ShareOnline      = element.GetBool("ShareOnline");
            TabBarVisible    = element.GetBool("TabBarVisible", true);
            MinResolution    = element.GetDouble("MinResolution");
            MaxResolution    = element.GetDouble("MaxResolution", -1.0);
            FilterLocation   = element.GetBool("FilterLocation");
            SelectionMode    = (SelectionMode)Enum.Parse(typeof(SelectionMode), element.GetString("SelectionMode", "None"));
            ShowTimeline     = element.GetBool("ShowTimeline", false);
            ShowAnalysis     = element.GetBool("ShowAnalysis", false);
            SublayersVisible = element.GetBool("SublayersVisible", true);
            OpenTab          = element.GetBool("OpenTab", false);
            Icon             = element.GetString("Icon", "");
            LayerOrder       = element.GetString("LayerOrder", "");
            AutoStart        = element.GetBool("AutoStart", false);
            BackupInterval   = element.GetInt("BackupInterval", 0);
            var xLabels      = element.Element("Labels");
            if (xLabels == null) return;
            foreach (var l in xLabels.Elements())
            {
                Labels.Add(l.Name.LocalName, l.Value);
            }
        }

        public override XElement ToXml(ServiceSettings settings)
        {
            var el = new XElement(XmlNodeId);

            el.SetAttributeValue("Description",    Description);
            el.SetAttributeValue("CanEdit",        CanEdit);
            el.SetAttributeValue("CanCreate",      CanCreate);
            el.SetAttributeValue("SyncDelay",      SyncDelay);
            el.SetAttributeValue("ShareOnline",    ShareOnline);
            el.SetAttributeValue("TabBarVisible",  TabBarVisible);
            el.SetAttributeValue("MinResolution",  MinResolution);
            el.SetAttributeValue("MaxResolution",  MaxResolution);
            el.SetAttributeValue("FilterLocation", FilterLocation);
            el.SetAttributeValue("SelectionMode",  SelectionMode);
            el.SetAttributeValue("AutoStart",      AutoStart);
            el.SetAttributeValue("BackupInterval", BackupInterval);

            if (!string.IsNullOrEmpty(LayerOrder)) el.SetAttributeValue("LayerOrder", LayerOrder);
            if (ShowAnalysis) el.SetAttributeValue("ShowAnalysis", ShowAnalysis);
            if (ShowTimeline) el.SetAttributeValue("ShowTimeline", ShowTimeline);
            if (!SublayersVisible) el.SetAttributeValue("SublayersVisible", SublayersVisible);
            if (OpenTab) el.SetAttributeValue("OpenTab", OpenTab);
            if (!string.IsNullOrEmpty(Icon)) el.SetAttributeValue("Icon", Icon);
            var myLabels = new XElement("Labels");
            foreach (var l in Labels)
            {
                myLabels.Add(new XElement(l.Key) { Value = l.Value });
            }
            el.Add(myLabels);
            return el;
        }
    }
}