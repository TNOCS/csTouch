using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using csCommon.Types.Geometries;
using csCommon.Types.TextAnalysis;
using csShared.Utils;
using CsvToDataService.Model;
using DataServer;
using Shapefile;

namespace CsvToDataService
{
    public static class AggregateDataService
    {
        private const string AggregationCountLabel = "Aggregation_count";

        public static PoiService Aggregate(PoiService source, string aggregationLabel, Dictionary<string, AggregationPolicy> aggregationPolicies)
        {
            // Make sure the aggregation label itself is not erased or compromised in any way.
            AggregationPolicy aggregationLabelPolicy;
            if (aggregationPolicies.TryGetValue(aggregationLabel, out aggregationLabelPolicy))
            {
                if (aggregationLabelPolicy.DataIsNumeric)
                {
                    aggregationLabelPolicy.NumericAggregationPolicy = AggregationPolicy.NumericAggregation.KeepFirst;
                }
                else
                {
                    aggregationLabelPolicy.NonNumericAggregationPolicy = AggregationPolicy.NonNumericAggregation.KeepFirst;
                }
            }

            // Master dictionary: aggregation label -> all other labels -> values for those labels
            Dictionary<string, Dictionary<string, List<string>>> aggregations =
                new Dictionary<string, Dictionary<string, List<string>>>();

            // Counter dictionary: aggregation label value -> number of PoIs having that value
            Dictionary<string, int> count = new Dictionary<string, int>();

            // Gather data.
            foreach (BaseContent poi in source.PoIs)
            {
                string aggregationLabelValue;
                if (!poi.Labels.TryGetValue(aggregationLabel, out aggregationLabelValue))
                {
                    continue;
                }
                if (! aggregations.ContainsKey(aggregationLabelValue))
                {
                    aggregations[aggregationLabelValue] = new Dictionary<string, List<string>>();
                }
                foreach (KeyValuePair<string, string> kv in poi.Labels)
                {
                    string poiLabelName = kv.Key;
                    string poiLabelValue = kv.Value;
                    if (!aggregations[aggregationLabelValue].ContainsKey(poiLabelName))
                    {
                        aggregations[aggregationLabelValue][poiLabelName] = new List<string>();
                    }
                    aggregations[aggregationLabelValue][poiLabelName].Add(poiLabelValue);
                }

                if (!count.ContainsKey(aggregationLabelValue))
                {
                    count[aggregationLabelValue] = 1;
                }
                else
                {
                    count[aggregationLabelValue]++;
                }
            }
            
            // Aggregate.
            var aggregate = new PoiService { PoITypes = ClonePoiTypes(source) };
            foreach (var kv in aggregations)
            {
                var labelsWithValues = kv.Value;
                var newPoI = new PoI {PoiType = aggregate.PoITypes.FirstOrDefault()};
                foreach (var kw in labelsWithValues)
                {
                    ApplyAggregation(aggregationPolicies, kw, newPoI);
                }
                aggregate.PoIs.Add(newPoI);
            }

            // Handle keyword aggregation and remove obsolete columns.
            CompressColumns(aggregationPolicies, aggregate);

            // Add count information.
            foreach (BaseContent poi in aggregate.PoIs)
            {
                string aggregationLabelValue;
                if (poi.Labels.TryGetValue(aggregationLabel, out aggregationLabelValue))
                {
                    int aggregationLabelValueFrequency;
                    if (count.TryGetValue(aggregationLabelValue, out aggregationLabelValueFrequency))
                    {
                        poi.Labels[AggregationCountLabel] = "" + aggregationLabelValueFrequency;
                    }                    
                }
            }

            // Return.
            return aggregate;
        }

        private static void ApplyAggregation(Dictionary<string, AggregationPolicy> aggregationPolicies, KeyValuePair<string, List<string>> labelWithValues, PoI destinationPoI)
        {
            AggregationPolicy policy;
            if (!aggregationPolicies.TryGetValue(labelWithValues.Key, out policy)) return;
            if (!policy.IsKeywordAggregation)
            {
                string aggregation = policy.Aggregrate(labelWithValues.Value);
                if (aggregation != null)
                {
                    destinationPoI.Labels[labelWithValues.Key] = aggregation;
                }
                else
                {
                    destinationPoI.Labels.Remove(labelWithValues.Key);
                }
            }
            else // Throw it all together.
            {
                List<string> values = labelWithValues.Value;
                StringBuilder concat = new StringBuilder();
                foreach (var value in values)
                {
                    concat.Append(value).Append(" ");
                }
                destinationPoI.Labels[labelWithValues.Key] = concat.ToString();
            }
        }

        public static PoiService Aggregate(PoiService source, PoiService shapes, string shapeIdLabel, Dictionary<string, AggregationPolicy> aggregationPolicies, bool addLabelsFromShapeFile)
        {
            // Build a dictionary of shape -> labels -> values.
            Dictionary<BaseGeometry, Dictionary<string, List<string>>> aggregations = new Dictionary<BaseGeometry, Dictionary<string, List<string>>>();

            // Copy the shape PoIs to the aggregate PoI.
            Dictionary<BaseGeometry, PoI> aggregatePois = new Dictionary<BaseGeometry, PoI>(); // Allows to look up PoIs by geometry.
            PoiService aggregate = new PoiService { PoITypes = ClonePoiTypes(source, shapeIdLabel) }; // TODO BUG Note that we omit metadata for the shape layer.
            foreach (BaseContent shapePoi in shapes.PoIs)
            {
                if (shapePoi.Geometry == null)
                {
                    continue;
                }
                var newPoI = new PoI {Geometry = shapePoi.Geometry};
                aggregate.PoIs.Add(newPoI);
                aggregatePois[newPoI.Geometry] = newPoI;
                aggregations[newPoI.Geometry] = new Dictionary<string, List<string>>();
                if (addLabelsFromShapeFile) {
                    foreach (KeyValuePair<string, string> kv in shapePoi.Labels) {
                        newPoI.Labels[kv.Key] = kv.Value;
                        aggregations[newPoI.Geometry][kv.Key] = new List<string>() {kv.Value};
                    }
                }
                else {
                    // Remember at least the ID of the shape layer, if possible.
                    string shapeIdLabelValue;
                    if (!shapePoi.Labels.TryGetValue(shapeIdLabel, out shapeIdLabelValue)) continue;
                    newPoI.Labels[shapeIdLabel] = shapeIdLabelValue;
                    aggregations[newPoI.Geometry][shapeIdLabel] = new List<string> { shapeIdLabelValue };
                }
            }

            // Counter dictionary: aggregation shape -> number of PoIs within that shape
            Dictionary<BaseGeometry, int> count = new Dictionary<BaseGeometry, int>();

            // Fill the dictionary.
            foreach (BaseContent poi in source.PoIs)
            {
                Point pos = poi.Geometry as Point; // NOTE: The following is wrong, because 'Position' is not in sync! -- new Point(poi.Position.Longitude, poi.Position.Latitude);
                if (pos == null)
                {
                    continue;
                }
                foreach (var kv in aggregations)
                {
                    BaseGeometry geometry = kv.Key;
                    Dictionary<string, List<string>> labels = kv.Value;

                    if (! geometry.Contains(pos)) continue;
                    
                    foreach (KeyValuePair<string, string> kw in poi.Labels)
                    {
                        string poiLabel = kw.Key;
                        string poiLabelValue = kw.Value;

                        List<string> labelValues;
                        if (!labels.TryGetValue(poiLabel, out labelValues))
                        {
                            labelValues = new List<string>();
                            labels[poiLabel] = labelValues;
                        }
                        labels[poiLabel].Add(poiLabelValue);
                    }

                    if (!count.ContainsKey(geometry))
                    {
                        count[geometry] = 1;
                    }
                    else
                    {
                        count[geometry]++;
                    }
                }                
            }

            // Aggregate and add count information.
            foreach (KeyValuePair<BaseGeometry, Dictionary<string, List<string>>> kv in aggregations)
            {
                PoI poi;
                if (aggregatePois.TryGetValue(kv.Key, out poi))
                {
                    foreach (KeyValuePair<string, List<string>> kw in kv.Value)
                    {
                        ApplyAggregation(aggregationPolicies, kw, poi);
                    }
                    int numPoisInRegion;
                    if (count.TryGetValue(kv.Key, out numPoisInRegion))
                    {
                        poi.Labels[AggregationCountLabel] = "" + numPoisInRegion;
                    }
                }
            }

            // Handle keyword aggregation and remove obsolete columns.
            CompressColumns(aggregationPolicies, aggregate);

            // Return.
            return aggregate;
        }

        private static ContentList ClonePoiTypes(PoiService source, string shapeIdLabel = null)
        {
            var clonedPoiTypes = new ContentList();
            // Add all other PoI types.
            foreach (var baseContent in source.PoITypes)
            {
                var clone = new PoI(); // Important that this is a PoI! Otherwise we have a casting problem elsewhere.
                clone.FromXml(baseContent.ToXml()); // Deep clone.
                // Add a MetaInfo for the counter.
                if (clone.MetaInfo == null)
                {
                    clone.MetaInfo = new List<MetaInfo>();
                }
                clone.MetaInfo.Add(new MetaInfo()
                {
                    Label       = AggregationCountLabel,
                    Description = "The number of features belonging to this aggregation",
                    IsEditable  = false,
                    Type        = MetaTypes.number
                });
                if (shapeIdLabel != null) clone.MetaInfo.Add(new MetaInfo()
                {
                    Label      = "Shape_Name",
                    IsEditable = false,
                    Type       = MetaTypes.text
                });
                clonedPoiTypes.Add(clone);
            }
            return clonedPoiTypes;
        }

        private static void CompressColumns(Dictionary<string, AggregationPolicy> aggregationPolicies, PoiService aggregate)
        {
            // Compress all keyword columns to a single column (this may be a setting later).
            var labelsWithKeywordAggregation = aggregationPolicies.Where(kv => kv.Value.IsKeywordAggregation).Select(kv => kv.Key);
            foreach (BaseContent poi in aggregate.PoIs)
            {
                WordHistogram sumHistogram = null;
                foreach (string label in labelsWithKeywordAggregation)
                {
                    string labelValue;
                    if (poi.Labels.TryGetValue(label, out labelValue))
                    {
                        WordHistogram wordHistogram = new WordHistogram();
                        wordHistogram.Append(labelValue);

                        if (sumHistogram == null)
                        {
                            sumHistogram = wordHistogram.Clone();
                        }
                        else
                        {
                            sumHistogram.Merge(wordHistogram);
                        }
                    }
                }
                if (sumHistogram != null)
                {
                    poi.Keywords = sumHistogram;
                }
            }

            // Remove all columns that have been compressed to keywords.
            foreach (BaseContent poI in aggregate.PoIs)
            {
                foreach (KeyValuePair<string, AggregationPolicy> kv in aggregationPolicies)
                {
                    if (!kv.Value.DataIsNumeric &&
                        kv.Value.NonNumericAggregationPolicy == AggregationPolicy.NonNumericAggregation.Keywords)
                    {
                        poI.Labels.Remove(kv.Key);
                    }
                }
            }

            // Remove obsolete labels; only in the PoiTypes; they are already gone from the Pois.
            foreach (BaseContent baseContent in aggregate.PoITypes)
            {
                List<string> toOmit = new List<string>();
                foreach (string label in baseContent.Labels.Keys)
                {
                    AggregationPolicy policy;
                    bool remove = true;
                    if (aggregationPolicies.TryGetValue(label, out policy))
                    {
                        if (!policy.IsOmit && 
                            !(policy.DataIsNumeric &&
                            policy.NonNumericAggregationPolicy == AggregationPolicy.NonNumericAggregation.Keywords))
                        {
                            remove = false;
                        }
                    }
                    if (remove)
                    {
                        toOmit.Add(label);
                    }
                }
                foreach (string label in toOmit)
                {
                    baseContent.Labels.Remove(label);
                }
            }
        }
    }
}
