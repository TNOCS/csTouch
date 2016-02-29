using Catfood.Shapefile;
using csCommon.Types.DataServer.Interfaces;
using csCommon.Types.DataServer.PoI.IO;
using csDataServerPlugin.Extensions;
using csShared.Utils;
using DataServer;
using System;
using System.Linq;
using System.Windows;
using Newtonsoft.Json.Linq;

namespace csCommon.Types.DataServer.PoI
{
    public class GeoJsonService : ExtendedPoiService
    {
        public static GeoJsonService CreateGeoJsonService(string name, Guid id, string folder = "", string relativeFolder = "", bool autostart = true, bool sensordata = false)
        {
            var res = new GeoJsonService
            {
                IsLocal = true,
                Name = name,
                Id = id,
                IsFileBased = false,
                StaticService = true,
                AutoStart = autostart,
                HasSensorData = sensordata,
                Mode = Mode.client,
                RelativeFolder = relativeFolder,
            };

            res.Init(Mode.client, AppState.DataServer);
            res.Folder = folder;
            res.InitPoiService();

            res.Settings.OpenTab = false;
            res.Settings.Icon = "layer.png";
            AppState.DataServer.Services.Add(res);
            return res;
        }

        protected override Exception ProcessFile()
        {
           try
           {
               var json = System.IO.File.ReadAllText(File);
               int count = 1;
               JObject areas = JObject.Parse(json);
               try
               {
                   foreach (var p in areas["featureTypes"]) {
                       var property = p as JProperty;
                       if (property == null) continue;
                       var poiType = new global::DataServer.PoI {Service = this}; // EV Set the service in order to be able to set and save the style.
                       poiType.TypeFromGeoJson(property);
                       PoITypes.Add(poiType);
                   }
               }
               catch (Exception e)  // no type def?
               {
                   int i = 0;
               } 

               foreach (var f in areas["features"].Values<JObject>())
               {
                   //var lockId = f["properties"]["lock"].Value<string>();
                   //var pos = f["properties"]["pos"].Value<string>();
                   //var angle = f["properties"]["angle"].Value<double>();
                   var p = new global::DataServer.PoI { Service = this, Id = Guid.NewGuid(), PoiTypeId = "Default" };
                   p.FromGeoJson(f, false); // Into this object!
//
//                   foreach (var pr in f["properties"])
//                   {
//                       if (pr is JProperty)
//                       {
//                           var prp = (JProperty)pr;
//                           if (prp.Name == "Id")
//                           {
//                               p.Id = Guid.Parse(prp.Value.ToString());
//                           }
//                           else if (prp.Name == "FeatureTypeId")
//                           {
//                               p.PoiTypeId = prp.Value.ToString();
//                           }
//                           else
//                           {
//                               p.Labels[prp.Name] = prp.Value.ToString().RestoreInvalidCharacters();                               
//                           }
//                       }
//                   }
//
//                   try
//                   {
//                       WellKnownTextIO wkt = new WellKnownTextIO();
//                       string geometry = f["geometry"].ToString(); // TODO Probably this is very inefficiently coded.
//                       wkt.FromGeoJson(geometry, false);  // Into this WKT.
//                       p.PoiTypeId = "WKT";
//                       p.WktText = wkt.WktText;
//                   }
//                   catch (Exception e)
//                   {
//                       p.PoiTypeId = "Point";
//
//                       // TODO The code below perfectly illustrates what is wrong with the shape/position related attributes of the BaseContent class.
//                       p.Points.Add(new Point(0, 0));
//                       p.Geometry = new Geometries.Point(0, 0, 0); // { X = 0, Y = 0, Z= 0 };
//                       p.Position = new Position(0, 0);
//                   }

                   PoIs.Add(p);
                   count += 1;
               }

               IsSubscribed = true;
               return null; // Nothing went wrong.
           }
            catch (Exception e)
            {
                return e;
            }

           
        }
    }
}
