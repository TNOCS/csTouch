using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using csCommon.Types.DataServer.PoI.IO;
using csCommon.Utils.IO;
using DataServer;

namespace CsvToDataService
{
    public class MergeDataService
    {
        public static void Merge(PoiService mainDataService, string mainKey, PoiService secondaryDataService,
            string secKey,
            bool includeSecondaryPois,
            bool excludeNonExistentSecondaryPois,
            bool includeSecondaryColumns,
            bool overwriteDuplicateLabels, bool stopOnFirstHit, bool includeMetaData, FileLocation destination = null,
            TextBox txtMergeDebugOutput = null) {
            if (txtMergeDebugOutput != null)
                txtMergeDebugOutput.Text = "Merging files on [" + mainKey + " = " + secKey + "]";

            // Determine whether to use well-known text.
            var mainPoiType = mainDataService.PoITypes.FirstOrDefault();
            if (mainPoiType == null) {
                mainPoiType                = new PoI {
                    Name                   = "Default",
                    ContentId              = "Default",
                    Service                = mainDataService,
                    Style                  = new PoIStyle {
                        Name               = "default",
                        FillColor          = Colors.Transparent,
                        StrokeColor        = Color.FromArgb(255, 128, 128, 128),
                        CallOutOrientation = CallOutOrientation.Right,
                        FillOpacity        = 0.3,
                        TitleMode          = TitleModes.Bottom,
                        NameLabel          = "Name",
                        DrawingMode        = DrawingModes.Image,
                        StrokeWidth        = 2,
                        IconWidth          = 24,
                        IconHeight         = 24,
                        Icon               = "images/missing.png",
                        CallOutFillColor   = Colors.White,
                        CallOutForeground  = Colors.Black,
                        TapMode            = TapMode.CallOut
                    },
                    Id                     = Guid.NewGuid(),
                    DrawingMode            = DrawingModes.Image,
                    MetaInfo               = new List<MetaInfo>()
                };
                mainDataService.PoITypes.Add(mainPoiType);
            }

            if (mainPoiType != null && string.IsNullOrEmpty(mainPoiType.ContentId)) mainPoiType.ContentId = "Default";
            BaseContent secondaryPoiType = null;
            if (secondaryDataService.PoITypes != null && secondaryDataService.PoITypes.Any()) {
                secondaryPoiType = secondaryDataService.PoITypes.FirstOrDefault();
            }
            ;
            bool useWKT = mainPoiType != null && mainPoiType.Style != null && mainPoiType.Style.Name == "WKT";
            useWKT = (useWKT || secondaryPoiType != null && secondaryPoiType.Style != null) &&
                     secondaryPoiType.Style.Name == "WKT" && includeSecondaryColumns;
            if (useWKT) {
                if (mainPoiType != null) {
                    mainPoiType.ContentId = "WKT"; // We directly set the main PoI's "PoiId" to WKT.                    
                }
                mainDataService.StaticService = true; // Make sure we save the file as static.
                if (txtMergeDebugOutput != null)
                    txtMergeDebugOutput.AppendText(
                        "\nOne of the files uses well-known text; the output will be a static layer.");
            }

            // Merge the meta info. 
            if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\nMerging meta info.");
            if (secondaryPoiType != null && secondaryPoiType.MetaInfo.Count == 0) {
                //if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\nSecondary file has no meta info; nothing to merge.");
            }
            if (secondaryPoiType != null && includeSecondaryColumns)
                foreach (MetaInfo secMetaInfo in secondaryPoiType.MetaInfo) {
                    string secMetaInfoLabel = secMetaInfo.Label;
                    bool doAdd = true;
                    if (secMetaInfoLabel == secKey) {
                        //if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\nLabel " + secMetaInfo.Label + " is the one we merge on, so we skip it.");
                        doAdd = false;
                    }
                    if (doAdd) {
                        if (mainPoiType != null) {
                            if (mainPoiType.MetaInfo.Any(mainMetaInfo => mainMetaInfo.Label == secMetaInfoLabel)) {
                                doAdd = false; // label already there.
                            }
                        }
                    }
                    if (!doAdd) continue;
                    if (mainPoiType != null)
                        mainPoiType.MetaInfo.Add(secMetaInfo);
                    //if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\nInserted meta-info from secondary file into main file: " + secMetaInfo.Label + ".");
                }

            // Merge the Style info. If the right file has WKT, set the drawing mode to MultiPolygon.
            // TODO For now we completely ignore some important aspects of poitypes:
            // 1. Multiple PoITypes.
            // 2. Different PoiId attributes.
            if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\nMerging styles.");
            if ((secondaryPoiType != null && secondaryPoiType.Style != null && (mainPoiType.Style == null))) {
                // We need to overwrite, or the main does not have something yet.                
                mainPoiType.Style = secondaryPoiType.Style;
                //if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\nInserted style from secondary file into main file.");
            }
            else {
                //if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\nMain file already has a style definition.");
            }
            if (useWKT) {
                mainPoiType.Style.DrawingMode = DrawingModes.MultiPolygon;
//                if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText(
//                    "\nEnsuring main file uses drawing mode 'multi polygon' given the fact that well-known text is used.");
            }

            // Remember which PoIs were found in the main as well as secondary file.
            HashSet<BaseContent> unvisitedMainPois = new HashSet<BaseContent>();
            foreach (var poI in mainDataService.PoIs) {
                unvisitedMainPois.Add(poI);
            }

            // Merge data.
            if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\n\nMerging data.");
            foreach (var secPoi in secondaryDataService.PoIs) {
                if (!secPoi.Labels.ContainsKey(secKey)) {
//                    if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\n" + secPoi.Id + " does not have label " + secKey +
//                                                   " and will be skipped.");
                    continue;
                }
                var mainPoiFound = false;
                //var mainPoi = mainDataService.PoIs.FirstOrDefault(p => p.Labels.ContainsKey(mainKey) 
                //    && string.Equals(p.Labels[mainKey], secPoi.Labels[secKey], StringComparison.InvariantCultureIgnoreCase));
                foreach (var mainPoi in mainDataService.PoIs.Where(mainPoi =>
                    mainPoi.Labels.ContainsKey(mainKey)
                    &&
                    string.Equals(mainPoi.Labels[mainKey], secPoi.Labels[secKey],
                        StringComparison.InvariantCultureIgnoreCase)))
                    if (mainPoi != null) {
                        mainPoiFound = true;
                        if (mainPoi.PoiType == null) {
                            mainPoi.PoiType = mainPoiType;
                            mainPoi.ContentId = mainPoiType.ContentId;
                        }
                        unvisitedMainPois.Remove(mainPoi); // We visited this PoI! :)

                        if (!includeSecondaryColumns)
                            continue; // Do not copy information from the secondary to the first.
                        //if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\nAdding new data to " + mainPoi.Labels[mainKey] + ".");
                        //GetShortFriendlyName(mainPoi.Name) + "." + mainKey + " = " + GetShortFriendlyName(secPoi.Name) + "." + secKey + "] = " + );

                        // Merge the labels part.
                        if (overwriteDuplicateLabels) {
                            //if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\nCopying label/values with overwrite: ");
                            foreach (var label in secPoi.Labels) {
                                if (label.Key != secKey) {
                                    //if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\n->" + label.Key + "=" + label.Value + ", ");
                                    mainPoi.Labels[label.Key] = label.Value;
                                }
                            }
                        }
                        else {
                            //if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\nCopying label/values without overwrite: ");
                            foreach (var label in secPoi.Labels.Where(l => l.Key != secKey)) {
                                string value;
                                if (mainPoi.Labels.TryGetValue(label.Key, out value)) {
                                    //if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\n->" + label.Key + " skipped (value already set to " + value + "), ");
                                    continue;
                                }
                                //if (label.Key == secKey)
                                //{
                                //    //if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\n->" + label.Key + " is the label on which we merge; skipped (value " + mainPoi.Labels[mainKey] + "), ");
                                //    continue;
                                //}
                                //if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\n->Setting " + label.Key + " to " + label.Value);
                                mainPoi.Labels[label.Key] = label.Value;
                            }
                        }

                        // Also merge WKT information there may be.
                        if (useWKT) {
                            mainPoi.PoiTypeId = "WKT";
                        }
                        if (!string.IsNullOrWhiteSpace(secPoi.WktText)) {
                            if (overwriteDuplicateLabels || string.IsNullOrWhiteSpace(mainPoi.WktText)) {
                                //if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\nAlso merged well-known text elements.");
                                mainPoi.WktText = secPoi.WktText;
                            }
                        }

                        // Stop if needed.
                        if (!stopOnFirstHit) continue;
                        if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\nStopping on first hit!");
                        break;

                        // And that's it.
                        //if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\n");
                    }

                if (mainPoiFound) continue;
                if (includeSecondaryPois) {
                    var copyPoI = new PoI();
                    copyPoI.FromXml(secPoi.ToXml());
                    if (useWKT) {
                        copyPoI.PoiTypeId = "WKT";
                    }
                    else {
                        copyPoI.PoiType = mainPoiType;
                        copyPoI.PoiTypeId = mainPoiType.PoiId;
                    }
                    copyPoI.Labels[mainKey] = copyPoI.Labels[secKey]; // Rename the merge label.
                    copyPoI.Labels.Remove(secKey);
                    mainDataService.PoIs.Add(copyPoI);
                    if (txtMergeDebugOutput != null)
                        txtMergeDebugOutput.AppendText("\nMain file does not have PoI with label " + mainKey +
                                                       " set to " + secPoi.Labels[secKey] +
                                                       ", including PoI from secondary file.");
                }
                else {
                    if (txtMergeDebugOutput != null)
                        txtMergeDebugOutput.AppendText("\nMain file does not have PoI with label " + mainKey +
                                                       " set to " + secPoi.Labels[secKey] +
                                                       " and will be skipped.");
                }
            }

            // Report on unvisited PoIs in main file.
            // Remove any PoIs in the main file that are not in the secondary file (if the user selected this option).
            if (txtMergeDebugOutput != null) {
                if (unvisitedMainPois.Any()) {
                    if (txtMergeDebugOutput != null)
                        txtMergeDebugOutput.AppendText(
                            "\nSome PoIs in the main file were not matched with a PoI in the secondary file.");
                    if (excludeNonExistentSecondaryPois && txtMergeDebugOutput != null)
                        txtMergeDebugOutput.AppendText(" These PoIs will be removed from the main file.");
                }
                foreach (BaseContent unvisitedMainPoi in unvisitedMainPois) {
                    if (txtMergeDebugOutput != null) {
                        string unvisitedMainPoiValue;
                        bool found = unvisitedMainPoi.Labels.TryGetValue(mainKey, out unvisitedMainPoiValue);
                        if (!found) {
                            unvisitedMainPoiValue = "UNDEFINED";
                        }
                        txtMergeDebugOutput.AppendText("\n" + unvisitedMainPoi + ", " + mainKey + " = " +
                                                       unvisitedMainPoiValue + ".");
                    }
                }
                if (excludeNonExistentSecondaryPois) {
                    // Annoyingly, calling "remove" on a ContentList throws an exception.
                    // Therefore, we construct a new list of PoIs that should be kept, instead of removing the PoIs we should remove.
                    ContentList newMainPoIs = new ContentList();
                    foreach (BaseContent poI in mainDataService.PoIs.Where(poI => !unvisitedMainPois.Contains(poI))) {
                        newMainPoIs.Add(poI);
                    }
                    mainDataService.PoIs = newMainPoIs;
                }
            }

            // We are done.
            if (txtMergeDebugOutput != null) txtMergeDebugOutput.AppendText("\nMerge completed!");

            if (destination == null) {
                mainDataService.SaveXml();
            }
            else {
                PoiServiceExporters.Instance.Export(mainDataService, destination, includeMetaData);
            }
        }

        private static string GetShortFriendlyName(string serviceName) {
            int slashIndex = serviceName.LastIndexOf("\\");
            if (slashIndex >= 0) {
                serviceName = serviceName.Substring(slashIndex + 1);
            }
            int dotIndex = serviceName.IndexOf(".");
            if (dotIndex >= 0) {
                serviceName = serviceName.Substring(0, dotIndex);
            }
            return serviceName.ToUpper();
        }
    }
}
