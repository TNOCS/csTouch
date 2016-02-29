using System.Collections.Concurrent;
using System.Globalization;
using csCommon.Types.Geometries;
using csDataServerPlugin;
using csGeoLayers;
using csShared;
using csShared.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using System.Xml.Serialization;
using OxyPlot;
using Point = System.Windows.Point;
using csPoint = csCommon.Types.Geometries.Point;

namespace DataServer.SqlProcessing
{
    // TODO Voeg een max/min resolutie toe om te zien of je hem nog wel executeren wil.
    // TODO Voeg een optie toe dat een poitypeid in een kolom terug gegeven kan worden.
    // TODO Zorg dat IsEnabled is gelinked aan de Layer die aan/uit staat.

    public enum UpdateTypes
    {
        Add,
        AutoClear,
        Diff,
        DiffGeo,
    }

    /// <summary>
    /// A single SQL query, which uses a set of parameters to define the query dynamically.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("Name: {Name}, Query: {Query}, IsEnabled: {IsEnabled}")]
    public class SqlQuery : BaseContent
    {
        private bool isInitialized;
        private string nameField;
        private string idField;
        private string pointField;
        private string pointsField;
        private bool isExecuting;

        public SqlQuery()
        {
            InputParameters = new List<SqlInputParameter>();
            OutputParameters = new List<SqlOutputParameter>();
            IsEnabled = true;
            PoiTypeId = "Default";
            UpdateType = UpdateTypes.AutoClear;
        }

        public override string XmlNodeId
        {
            get { return "SqlQuery"; }
        }

        [XmlIgnore]
        private string ConnectionString
        {
            get
            {
                return string.IsNullOrEmpty(ConnectionStringFull)
                    ? AppStateSettings.Instance.Config.Get(ConnectionStringReference, "server=localhost;Port=5432;User Id=bag_user;Password=bag4all;Database=bag;SearchPath=bag8jan2014,public")
                    : ConnectionStringFull;
            }
        }

        /// <summary>
        /// Connection string to connect to the database, e.g.
        /// Server=localhost;Port=5432;User Id=postgres;Password=secret;Database=Foo;
        /// </summary>
        [XmlAttribute("connection")]
        public string ConnectionStringFull { get; set; }

        /// <summary>
        /// Refers to the configoffline.xml label that contains the connection string. 
        /// </summary>
        [XmlAttribute("connectionReference")]
        public string ConnectionStringReference { get; set; }

        /// <summary>
        /// When true, run the query.
        /// </summary>
        [XmlAttribute("isEnabled")]
        public bool IsEnabled { get; set; }

        ///// <summary>
        ///// When true, before running a new query, clear all previous results.
        ///// </summary>
        //[XmlAttribute("autoClear")]
        //public bool AutoClear { get; set; }

        [XmlAttribute("updateType")]
        public UpdateTypes UpdateType { get; set; }


        /// <summary>
        /// The actual SQL, potentially containing parameter place holders like {0} or {1}.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// In case we are dealing with an iterative query, here we can specify specific options.
        /// </summary>
        public SqlQueryOptions Options { get; set; }

        /// <summary>
        /// A list of input parameters.
        /// </summary>
        [XmlArray("Inputs"), XmlArrayItem("Input", typeof(SqlInputParameter))]
        public List<SqlInputParameter> InputParameters { get; set; }

        /// <summary>
        /// A list of output parameters.
        /// </summary>
        [XmlArray("Outputs"), XmlArrayItem("Output", typeof(SqlOutputParameter))]
        public List<SqlOutputParameter> OutputParameters { get; set; }

        private void InitializeFieldNames(IDataRecord dr)
        {
            if (isInitialized) return;
            isInitialized = true;
            idField = RetreiveFieldName(dr, SqlOutputType.Id);
            nameField = RetreiveFieldName(dr, SqlOutputType.Name);
            pointField = RetreiveFieldName(dr, SqlOutputType.Point);
            pointsField = RetreiveFieldName(dr, SqlOutputType.Points);
        }

        private readonly ConcurrentDictionary<PoI, CancellationTokenSource> queue = new ConcurrentDictionary<PoI, CancellationTokenSource>();

        private CancellationTokenSource cts;
        private readonly object myLock = new object();

        /// <summary>
        /// Execute the SQL query and update the service.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="refPoi"></param>
        /// <param name="zone"></param>
        /// <param name="isQueueAble"></param>
        /// <param name="forceExecute"></param>
        /// <returns></returns>
        public async void Execute(PoiService service, PoI refPoi, List<Point> zone, bool isQueueAble = false, bool forceExecute = false)
        {
            Caliburn.Micro.Execute.OnUIThread(() =>
            {
                var layer = service.Layer as dsStaticLayer;
                if (layer == null) return;
                var l = layer.GetSubLayer(Layer);
                if (l != null) IsEnabled = l.Visible;
            });
            if (!IsEnabled && !forceExecute) return;

            lock (myLock)
            {
                // Cancel the last task, if any.
                if (cts != null)
                {
                    if (refPoi != null && isQueueAble)
                    {
                        if (queue.ContainsKey(refPoi) && queue[refPoi] != null)
                            queue[refPoi].Cancel();
                    }
                    else
                        cts.Cancel();
                }

                // Create Cancellation Token for this task.
                cts = new CancellationTokenSource();

                if (isQueueAble && refPoi != null)
                    queue[refPoi] = cts;
            }
            var token = cts.Token;

            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    while (isExecuting)
                    {
                        token.ThrowIfCancellationRequested();
                        Thread.Sleep(100);
                    }

                    isExecuting = true;
                    var query = DefineParametrizedQuery(refPoi, zone);
                    token.ThrowIfCancellationRequested();
                    var poiList = new List<PoI>();

                    using (var conn = new NpgsqlConnection(ConnectionString))
                    {
                        try
                        {
                            token.ThrowIfCancellationRequested();

                            conn.Open();
                            var command = new NpgsqlCommand(query, conn); // EV Specify in connection string. {CommandTimeout = 30};

                            var dr = command.ExecuteReader();
                            token.ThrowIfCancellationRequested();

                            InitializeFieldNames(dr);

                            #region Processing an iterative SQL query

                            if (Options != null && refPoi != null)
                            {
                                try
                                {
                                    var tries = 0;
                                    var maxTries = (int)Math.Round(refPoi.LabelToDouble(InputParameters[Options.MaxTriesInputParameterIndex].LabelName), 0);
                                    var desiredResult = Options.ComputeDesiredResult(refPoi, InputParameters);
                                    var desiredAccurcy = refPoi.LabelToDouble(InputParameters[Options.DesiredAccuracyInputParameterIndex].LabelName);
                                    var acceptableDiff = desiredResult * desiredAccurcy / 100;
                                    var outputName = OutputParameters[Options.ActualResultOutputParameterIndex].Name;
                                    var currentResult = refPoi.LabelToDouble(outputName);
                                    var success = Math.Abs(currentResult - desiredResult) <= acceptableDiff;
                                    if (success)
                                    {
                                        dr.Close();
                                        return;
                                    }
                                    var controlLabelName = InputParameters[Options.ControlInputParameterIndex].LabelName;
                                    var control = refPoi.LabelToDouble(controlLabelName);
                                    var origControl = control;
                                    do
                                    {
                                        var actualResult = 0.0;
                                        while (dr.Read())
                                        {
                                            token.ThrowIfCancellationRequested();
                                            var result = dr[outputName].ToString();
                                            if (string.IsNullOrEmpty(result)) continue;
                                            actualResult = double.Parse(result, NumberStyles.Any, CultureInfo.InvariantCulture);
                                            success = Math.Abs(actualResult - desiredResult) <= acceptableDiff;
                                        }
                                        if (success) continue;
                                        switch (Options.Relationship)
                                        {
                                            case SqlInputOutputRelationship.Linear:
                                                // actual = control * alpha
                                                var alpha1 = actualResult / (control.IsZero() ? 0.0001 : control);
                                                control = alpha1.IsZero() ? 2 * control : desiredResult / alpha1;
                                                break;
                                            case SqlInputOutputRelationship.Quadratic:
                                                // actual = control^2 * alpha
                                                var alpha2 = actualResult / Math.Pow(control.IsZero() ? 0.0001 : control, 2);
                                                control = alpha2.IsZero() ? 2 * control : Math.Sqrt(desiredResult / alpha2);
                                                break;
                                        }
                                        refPoi.Labels[controlLabelName] = control.ToString(CultureInfo.InvariantCulture);
                                        query = DefineParametrizedQuery(refPoi, zone);
                                        command = new NpgsqlCommand(query, conn) { CommandTimeout = 30 };
                                        dr = command.ExecuteReader();
                                        if (++tries > maxTries)
                                        {
                                            Logger.Log("SqlProcessing.SqlQuery", refPoi.Name + ": " + "Could not resolve query", refPoi.Name + ": Max tries exceeded", Logger.Level.Warning, true);
                                            return;
                                        }
                                        tries++;
                                    } while (!success);
                                    if (Math.Abs(origControl - control) > 0.001)
                                        Caliburn.Micro.Execute.OnUIThread(() => refPoi.TriggerLabelChanged(controlLabelName));
                                    // NOTE I need to redo the same query again to be able to continue, 
                                    // as I cannot find a way to reset dr.
                                    dr = command.ExecuteReader();
                                }
                                catch (SystemException e)
                                {
                                    Logger.Log("SqlProcessing.SqlQuery", refPoi.Name + ": " + "Error running iterative query", e.Message, Logger.Level.Error, true);
                                }
                            }

                            #endregion Processing an iterative SQL query

                            while (dr.Read())
                            {
                                token.ThrowIfCancellationRequested();
                                var contentId = RetreiveId(dr);
                                var position = RetreivePosition(dr);
                                var geometry = String.IsNullOrEmpty(pointsField)
                                    ? null
                                    : dr[pointsField].ToString().ConvertFromWkt();
                                if (position == null && geometry == null)
                                {
                                    AddOutputsAsLabel(refPoi, dr);
                                    conn.Close();
                                    isExecuting = false;
                                    return;
                                }

                                if (refPoi != null && !String.IsNullOrEmpty(refPoi.ContentId))
                                    contentId = refPoi.ContentId + "_" + contentId;
                                var poi = new PoI
                                {
                                    ContentId = contentId,
                                    Service = service,
                                    Name = RetreiveName(dr),
                                    PoiTypeId = PoiTypeId,
                                    UserId = Id.ToString(),
                                    Layer = Layer,
                                    Position = position,
                                    Style = PoiType == null ? null : PoiType.Style,
                                    Geometry = geometry,
                                    WktText = dr[pointsField].ToString(),
                                };

                                SetGeometry(poi);
                                AddOutputsAsLabel(poi, dr);
                                poi.UpdateEffectiveStyle();
                                poiList.Add(poi);
                            }

                            var currPois = service.PoIs.Where(k => k.PoiTypeId == PoiTypeId).ToList();

                            Caliburn.Micro.Execute.OnUIThread(() =>
                            {
                                service.PoIs.StartBatch();
                                switch (UpdateType)
                                {
                                    case UpdateTypes.Add:
                                        poiList.ForEach(p => service.PoIs.Add(p));
                                        break;
                                    case UpdateTypes.AutoClear:
                                        currPois.ForEach(p => service.PoIs.Remove(p));
                                        poiList.ForEach(p => service.PoIs.Add(p));
                                        break;
                                    case UpdateTypes.Diff:
                                        //Remove Pois that are no longer needed
                                        var poicheck = "";
                                        if (refPoi != null && !String.IsNullOrEmpty(refPoi.ContentId))
                                            poicheck = refPoi.ContentId + "_";
                                        var remlist = currPois.Where(k => k.ContentId.StartsWith(poicheck) && poiList.All(g => g.ContentId != k.ContentId)).ToList();
                                        remlist.ForEach(p => service.RemovePoi((PoI)p));
                                        poiList.ForEach(
                                            p =>
                                            {
                                                if (currPois.All(c => c.ContentId != p.ContentId))
                                                    service.PoIs.Add(p);
                                            });
                                        break;
                                    case UpdateTypes.DiffGeo:
                                        var addDiffGeo = new List<PoI>();
                                        foreach (var p in poiList)
                                        {
                                            var first = currPois.FirstOrDefault(k => k.ContentId.ToString() == p.ContentId.ToString());
                                            if (first == null || first.WktText == p.WktText) continue;
                                            addDiffGeo.Add(p);
                                        }
                                        var rem = currPois.Where(k => !string.IsNullOrEmpty(k.UserId) && addDiffGeo.Any(g => g.ContentId == k.ContentId)).ToList();
                                        foreach (var r in rem)
                                        {
                                            service.RemovePoi((PoI)r);
                                        }
                                        foreach (var a in addDiffGeo)
                                        {
                                            service.PoIs.Add(a);
                                        }
                                        //Remove pois wich are no longer needed
                                        poicheck = "";
                                        if (refPoi != null && !String.IsNullOrEmpty(refPoi.ContentId))
                                            poicheck = refPoi.ContentId + "_";
                                        remlist = currPois.Where(k => k.ContentId.StartsWith(poicheck) && poiList.All(g => g.ContentId != k.ContentId)).ToList();
                                        remlist.ForEach(p => service.RemovePoi((PoI)p));
                                        //Add pois which are new
                                        poiList.ForEach(p =>
                                        {
                                            if (currPois.All(c => c.ContentId != p.ContentId))
                                                service.PoIs.Add(p);
                                        });
                                        break;
                                }
                                UpdateMinMax(service);
                                service.PoIs.FinishBatch();
                                //if (refPoi != null && !String.IsNullOrEmpty(refPoi.ContentId))
                                //    poicheck = refPoi.ContentId + "_";
                                //var remlist =currPoi.Where(k =>k.ContentId.StartsWith(poicheck) && poiList.All(g => g.ContentId != k.ContentId)).ToList();
                            });
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (SystemException e)
                        {
                            Logger.Log("SqlProcessing.SqlQuery", refPoi.Name + ": " + "Error accessing PostgreSQL server", e.Message, Logger.Level.Error, true);
                        }
                        finally
                        {
                            lock (myLock)
                            {
                                if (isQueueAble && refPoi != null) queue[refPoi] = null;
                            }
                            isExecuting = false;
                        }
                    }
                }, token);
            }
            catch (OperationCanceledException)
            {
                isExecuting = false;
            }

        }

        private void AddOutputsAsLabel(BaseContent refPoi, IDataRecord dr)
        {
            foreach (var output in OutputParameters.Where(p => p.OutputType == SqlOutputType.Label))
            {
                var oldValue = refPoi.Labels.ContainsKey(output.Name) ? refPoi.Labels[output.Name] : string.Empty;
                var strings = dr[output.Name] as string[];
                if (strings != null)
                {
                    refPoi.Labels[output.Name] = string.Join("\r\n", strings);
                    //refPoi.Labels[output.Name] = "";
                    //foreach (var str in strings)
                    //    refPoi.Labels[output.Name] += str + "\r\n";
                }
                else if (dr[output.Name] == null) return;
                else
                    refPoi.Labels[output.Name] = dr[output.Name].ToString();
                var name = output.Name;
                if (!string.Equals(oldValue, refPoi.Labels[output.Name]))
                    Caliburn.Micro.Execute.OnUIThread(() => refPoi.TriggerLabelChanged(name));
            }
        }

        private void UpdateMinMax(PoiService service)
        {
            //Todo: Find better way of updating min/max
            Caliburn.Micro.Execute.OnUIThread(() =>
            {
                if (PoiType == null) return;
                foreach (var hl in PoiType.Style.Analysis.Highlights)
                {
                    if (!hl.IsDynamic) continue;
                    hl.CalculateMax(service);
                    hl.CalculateMin(service);
                }
            });
        }

        private static void SetGeometry(PoI poi)
        {
            if (poi.Geometry == null) return;
            if (poi.Geometry as csPoint != null)
            {
                var g = poi.Geometry as csCommon.Types.Geometries.Point;
                if (g != null) poi.Position = new Position(g.X, g.Y);
                poi.DrawingMode = DrawingModes.Point;
            }
            else if (poi.Geometry as LineString != null)
            {
                var gl = poi.Geometry as LineString;
                if (gl != null)
                    foreach (var p in gl.Line) poi.Points.Add(new Point(p.X, p.Y));
                poi.DrawingMode = DrawingModes.Polyline;                
            }
            else if (poi.Geometry as Polygon != null)
            {
                poi.DrawingMode = DrawingModes.Polygon;
            }
            else if (poi.Geometry as MultiPolygon != null)
            {
                poi.DrawingMode = DrawingModes.MultiPolygon;
            }
        }

        /// <summary>
        /// Remove all PoIs created by this service (checks whether the UserId == SqlQuery.Id).
        /// </summary>
        /// <param name="poiService"></param>
        /// <param name="currPois"></param>
        private void RemoveExistingPoIs(PoiService poiService, IEnumerable<BaseContent> currPois)
        {
            poiService.PoIs.StartBatch();
            lock (myLock)
            {
                foreach (var rp in currPois)
                    poiService.PoIs.Remove(rp);
            }
            Caliburn.Micro.Execute.OnUIThread(() => poiService.PoIs.FinishBatch());
        }

        private string RetreiveFieldName(IDataRecord dr, SqlOutputType type)
        {
            // Type name is explicitly set
            var outputParameter = OutputParameters.FirstOrDefault(o => o.OutputType == type);
            if (outputParameter != null) return outputParameter.Name;
            // Is name the default: 
            string defaultName;
            switch (type)
            {
                case SqlOutputType.Id: defaultName = "Id"; break;
                case SqlOutputType.Name: defaultName = "Name"; break;
                case SqlOutputType.Point: defaultName = "Point"; break;
                case SqlOutputType.Points: defaultName = "Points"; break;
                default: return string.Empty;
            }
            for (var i = 0; i < dr.FieldCount; i++)
            {
                var name = dr.GetName(i);
                if (string.Equals(name, defaultName, StringComparison.InvariantCultureIgnoreCase)) return name;
            }
            // Field name not found
            return string.Empty;
        }

        private string RetreiveName(IDataRecord dr)
        {
            return string.IsNullOrEmpty(nameField) ? string.Empty : dr[nameField].ToString();
        }

        private string RetreiveId(IDataRecord dr)
        {
            return string.IsNullOrEmpty(idField) ? string.Empty : dr[idField].ToString();
        }

        private Position RetreivePosition(IDataRecord dr)
        {
            if (string.IsNullOrEmpty(pointField)) return null;
            var p = dr[pointField].ToString().ConvertToPointCollection().FirstOrDefault();
            return new Position(p.X, p.Y);
        }

        //private ObservableCollection<Point> RetreivePoints(IDataRecord dr)
        //{
        //    return string.IsNullOrEmpty(pointsField)
        //        ? null
        //        : new ObservableCollection<Point>(dr[pointsField].ToString().ConvertToPointCollection().ToList());
        //}

        /// <summary>
        /// Replace the parameter placeholders in the query with their values.
        /// </summary>
        /// <returns></returns>
        private string DefineParametrizedQuery(PoI poi, List<Point> zone)
        {
            var query = Query;
            for (var i = 0; i < InputParameters.Count; i++)
            {
                var inputParameter = InputParameters[i];
                inputParameter.SetValue(poi, zone);
                query = query.Replace(string.Format("{{{0}}}", i), inputParameter.SqlParameterValue);
            }
            return query;
        }

        public override XElement ToXml(ServiceSettings settings)
        {
            return new XElement("SqlQuery",
                new XAttribute("name", Name),
                new XAttribute("layer", Layer),
                new XAttribute("isEnabled", IsEnabled),
                new XAttribute("updateType", UpdateType),
                new XAttribute("poiTypeId", PoiTypeId), // TODO featureTypeId?
                new XAttribute("connection", ConnectionString),
                new XAttribute("id", Id),
                new XElement("Query", Query),
                Options == null ? null :
                new XElement("Options",
                    new XAttribute("controlInputParameterIndex", Options.ControlInputParameterIndex),
                    new XAttribute("desiredResultFormula", Options.DesiredResultFormula),
                    new XAttribute("desiredResultLabel", Options.DesiredResultLabel),
                    new XAttribute("desiredAccuracyInputParameterIndex", Options.DesiredAccuracyInputParameterIndex),
                    new XAttribute("maxTriesInputParameterIndex", Options.MaxTriesInputParameterIndex),
                    new XAttribute("actualResultOutputParameterIndex", Options.ActualResultOutputParameterIndex),
                    new XAttribute("relationship", Options.Relationship)),
                new XElement("Inputs", from input in InputParameters
                                       select new XElement("Input",
                                           new XAttribute("type", input.Type),
                                           string.IsNullOrEmpty(input.LabelName) ? null : new XAttribute("labelName", input.LabelName))),
                new XElement("Outputs", from output in OutputParameters
                                        select new XElement("Output",
                                            new XAttribute("name", output.Name),
                                            new XAttribute("type", output.OutputType)
                                            ))
                    );
        }

        public override void FromXml(XElement element, string directoryName)
        {
            Id = element.GetGuid("id");
            Name = element.GetString("name", string.Empty);
            Layer = element.GetString("layer", string.Empty);
            ConnectionStringFull = element.GetString("connection", string.Empty);
            ConnectionStringReference = element.GetString("connectionReference", string.Empty);
            Query = (string)element.GetElement("Query");
            PoiTypeId = element.GetString("poiTypeId", string.Empty);
            IsEnabled = element.GetBool("isEnabled");
            UpdateType = (UpdateTypes)Enum.Parse(typeof(UpdateTypes), element.GetString("updateType", string.Empty));

            var options = element.GetElement("Options");
            if (options != null)
            {
                var relationShip = options.GetString("relationship", string.Empty);
                Options = new SqlQueryOptions
                {
                    ControlInputParameterIndex = options.GetInt("controlInputParameterIndex"),
                    DesiredResultFormula = options.GetString("desiredResultFormula"),
                    DesiredResultLabel = options.GetString("desiredResultLabel"),
                    DesiredAccuracyInputParameterIndex = options.GetInt("desiredAccuracyInputParameterIndex"),
                    MaxTriesInputParameterIndex = options.GetInt("maxTriesInputParameterIndex", 5),
                    ActualResultOutputParameterIndex = options.GetInt("actualResultOutputParameterIndex"),
                    Relationship = string.IsNullOrEmpty(relationShip) ? SqlInputOutputRelationship.Linear : (SqlInputOutputRelationship)Enum.Parse(typeof(SqlInputOutputRelationship), relationShip)
                };
            }
            var inputs = element.Element("Inputs");
            if (inputs != null)
                foreach (var input in inputs.Elements())
                {
                    InputParameters.Add(new SqlInputParameter
                    {
                        LabelName = input.GetString("labelName", string.Empty),
                        Type = (SqlParameterTypes)Enum.Parse(typeof(SqlParameterTypes), input.GetString("type", string.Empty))
                    });
                }
            var outputs = element.Element("Outputs");
            if (outputs == null) return;
            foreach (var output in outputs.Elements())
            {
                OutputParameters.Add(new SqlOutputParameter
                {
                    Name = output.GetString("name", string.Empty),
                    OutputType = (SqlOutputType)Enum.Parse(typeof(SqlOutputType), output.GetString("type", string.Empty))
                });
            }
        }

        //public string ToXmlString()
        //{
        //    return this.ToXml();
        //}

        //public static SqlQuery FromXmlString(string xml)
        //{
        //    return xml.FromXml<SqlQuery>();
        //}
    }
}