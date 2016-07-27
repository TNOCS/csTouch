using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using csShared;
using csShared.Controls.Popups.MenuPopup;
using DataServer;
using ESRI.ArcGIS.Client;
using csCommon.Logging;

namespace csCommon.Types.DataServer.PoI
{

    public class IconGroupLayer : GroupLayer
    {
        public ImageSource Icon { get; set; }
    }

    public class SaveService : PoiService
    {
        public string BaseFolder { get; set; }


        public override List<System.Windows.Controls.MenuItem> GetMenuItems()
        {
            var r = base.GetMenuItems();
            if (!Layer.IsStarted) return r;
            var save = MenuHelpers.CreateMenuItem("Save as layer", MenuHelpers.LayerIcon);
            save.Click += (e, f) => SaveLayer();
            r.Add(save);
            return r;
        }

        public PoiService CreateService(string name, string folder, string relativeFolder)
        {
            LogCs.LogMessage(String.Format("SaveService: Create PoiService '{0}'", name));
            var ss = new PoiService()
            {
                IsLocal        = true,
                Name           = name,
                Id             = Guid.NewGuid(),
                IsFileBased    = true,
                StaticService  = StaticService,
                IsVisible      = false,
                Folder         = folder,
                RelativeFolder = RelativeFolder
            };


            ss.Init(Mode.client, AppStateSettings.Instance.DataServer);
            ss.RelativeFolder = relativeFolder;
            ss.InitPoiService();
            ss.SettingsList = new ContentList
            {
                Service      = ss,
                ContentType  = typeof(ServiceSettings),
                Id           = "settings",
                IsRessetable = false
            };
            ss.SettingsList.Add(new ServiceSettings());
            ss.AllContent.Add(ss.SettingsList);
            ss.Settings.OpenTab       = false;
            ss.Settings.TabBarVisible = false;
            ss.Settings.Icon          = "layer.png";
            ss.AutoStart              = false;
            
            return ss;

        }

        public string SaveName { get; set; }


        public void SaveLayer()
        {
            //var g = new Guid();
            var d = DateTime.Now;
            if (string.IsNullOrEmpty(SaveName)) SaveName = Name + " " + d.Hour + "h " + d.Minute + "m";
            var sr = CreateService(SaveName, Folder,RelativeFolder);
            
            foreach (var pt in this.PoITypes) sr.PoITypes.Add(pt);
            foreach (var p in this.PoIs) sr.PoIs.Add(p);
            sr.SaveXml();
            AppStateSettings.Instance.DataServer.AddService(sr,Mode.client);


            //SaveXml();
        }


        
    }
}