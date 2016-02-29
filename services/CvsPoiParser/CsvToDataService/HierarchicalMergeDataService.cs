using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using csCommon.Types.DataServer;
using csCommon.Types.DataServer.PoI.IO;
using csCommon.Utils;
using csCommon.Utils.IO;
using CsvToDataService.Model;
using DataServer;

namespace CsvToDataService
{
    public class HierarchicalMergeDataService
    {
        /// <summary>
        /// Merge data services.
        /// </summary>
        /// <param name="layers"></param>
        /// <param name="filename"></param>
        /// <param name="forceDsExport"></param>
        /// <param name="createSupertypePoi"></param>
        /// <param name="includeMetaData"></param>
        /// <param name="processingErrors"></param>
        /// <returns>The file name we saved to, or null if we did not.</returns>
        public static string Merge(ObservableCollection<LayerFileDescription> layers, string filename, 
            bool forceDsExport, bool createSupertypePoi, bool includeMetaData, ObservableCollection<ProcessingError> processingErrors)
        {
            // Read all the files.
            ObservableCollection<PoiService> services = new ObservableCollection<PoiService>();
            foreach (LayerFileDescription layerFileDescription in layers)
            {
                IOResult<PoiService> import = PoiServiceImporters.Instance.Import(layerFileDescription.File);
                PoiService dataService = (import != null && import.Successful) ? import.Result : null;
                if (dataService == null)
                {
                    processingErrors.Add(new ProcessingError("Cannot read file", layerFileDescription.File.LocationString,
                        layers.IndexOf(layerFileDescription) + 1));
                    services.Add(null);
                }
                else
                {
                    services.Add(dataService);                    
                }
            }

            // Remove any errors.
            ObservableCollection<LayerFileDescription> newLayers = new ObservableCollection<LayerFileDescription>();
            ObservableCollection<PoiService> newPoiServices = new ObservableCollection<PoiService>();
            for (int i = 0; i < services.Count; i++)
            {
                if (services[i] != null)
                {
                    newLayers.Add(layers[i]);
                    newPoiServices.Add(services[i]);
                }
            }
            layers = newLayers;
            services = newPoiServices;

            // Do we have enough files to merge?
            if (services.Count < 2)
            {
                processingErrors.Add(new ProcessingError("Merging requires at least two files", "", -1));
                return null;
            }

            // We use the first service and put everything in there.
            // Rename the service before we accidentally overwrite something important.
            PoiService mergedPoiService = services[0];
            string name = Path.GetFileNameWithoutExtension(filename);
            mergedPoiService.Name = StripGuid.Strip(name, '.');

            // Make sure the service is static if any sublayer is.
            // FIXME StaticService never returns true for sublayer services, so we set the new layer to static always.
            mergedPoiService.StaticService = true;
//            if (!mergedPoiService.StaticService)
//            {
//                foreach (PoiService service in services)
//                {
//                    if (service.StaticService) 
//                    {
//                        mergedPoiService.StaticService = true;
//                    }
//                    break;
//                }
//            }

            // Change the PoiId for all PoI types so they are unique; also change them in the PoIs.
            for (int i = 0; i < layers.Count; i++)
            {
                LayerFileDescription layerFileDescription = layers[i];
                string shortLayerId = ShortLayerId(layerFileDescription.Description);
                PoiService poiService = services[i];
                foreach (BaseContent poiType in poiService.PoITypes)
                {
                    poiType.PoiId = poiType.PoiId + "_" + shortLayerId;
                }
                foreach (BaseContent poi in poiService.PoIs)
                {
                    poi.PoiTypeId = poi.PoiTypeId + "_" + shortLayerId;
                    poi.Layer = layerFileDescription.Description;
                }
            }

            // Add all into the service.
            for (int i = 1; i < layers.Count; i++)
            {
                PoiService poiService = services[i];
                foreach (BaseContent poiType in poiService.PoITypes)
                {
                    mergedPoiService.PoITypes.Add(poiType);
                }
                foreach (BaseContent poi in poiService.PoIs)
                {
                    mergedPoiService.PoIs.Add(poi);
                }
            }

            // Save the file and return.
            try
            {
                if (forceDsExport)
                {
                    mergedPoiService.SaveXml();
                    return mergedPoiService.FileName;
                }
                else
                {
                    IOResult<FileLocation> ioResult = PoiServiceExporters.Instance.Export(mergedPoiService, new FileLocation(filename), includeMetaData);
                    if (ioResult.Successful)
                    {
                        return ioResult.Result.LocationString;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                processingErrors.Add(new ProcessingError("Cannot save file: " + e.Message, mergedPoiService.FileName, -1));
                return null;
            }
        }

        private static readonly Random random = new Random();

        private static string ShortLayerId(string layerDescription)
        {
            string ret = layerDescription.Replace(" ","");
            // layerDescription.Split(' ').ToList().ForEach(i => ret += i[0]);
            if (ret.Length > 12)
            {
                ret = ret.Substring(0, 3) + "..." + ret.Substring(ret.Length - 6, ret.Length);
            }
            ret += random.Next(100, 999);
            return ret;
        }
    }
}
