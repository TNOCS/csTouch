using BagDataAccess;
using csCommon.Types.DataServer.Interfaces;
using csCommon.Types.DataServer.PoI.IO;
using csCommon.Types.Geometries;
using csCommon.Utils;
using csCommon.Utils.IO;
using csShared;
using csShared.Utils;
using CsvToDataService.Model;
using CsvToDataService.Properties;
using DataServer;
using GeoCoding.Google;
using LumenWorks.Framework.IO.Csv;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Media;
using System.Xml.Linq;

namespace CsvToDataService
{
    /// <summary>
    ///     Utility class to convert CSV to Data Service (and back).
    ///     NOTE That the GeoCoding (lookup via Google) only works in Release mode (since there is a query limit).
    /// </summary>
    public class ConvertCsv : IImporter<FileLocation, PoiService> // Export methods moved into framework.
    {
        private static readonly LatLongLookup LatLongLookup = new LatLongLookup();
        private static readonly Regex SplitStreetAndNumber = new Regex("^('?\\d?\\d?\\d?['-\\.a-zA-Z ]*)(\\d*).*$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        // WARNING in case you don't have a zip code, you will only use the house number to store a location
        private static readonly Dictionary<string, Position> LocationCache = new Dictionary<string, Position>();

        private const string MarkColumnWithoutHeader = "_Unknown_";

        public string DataFormat { get { return "Comma Separated Values"; } }
        public string DataFormatExtension { get { return "csv"; } }

        public IOResult<PoiService> ImportData(FileLocation source)
        {
            var csvHeaders = new ObservableCollection<CsvHeader>();
            var processingErrors = new ObservableCollection<ProcessingError>();
            LoadCsv(source.LocationString, ';', csvHeaders, processingErrors);

            // TODO REVIEW We can only do this with default settings here.
            // EV Make sure that you don't start to lookup locations too, so useGoogle = false.
            var poiService = LoadCsvData(source.LocationString, null, ';', true, csvHeaders, new ProgressStruct(),
                processingErrors, null, null, null, false, false, null); // Do not auto-save! (Last arg: bag connection string, not provided, so do not look up addresses!
            if (poiService == null)
            {
                const string logFile = "csvtods.log";
                DumpObject.DumpToXml(processingErrors, logFile);
                return new IOResult<PoiService>(new Exception("Could not convert CSV to data service. Check the log file:\n" + Path.GetFullPath(logFile)));
            }
            poiService.ContentLoaded = true;
            return new IOResult<PoiService>(poiService);
        }

        public bool SupportsMetaData
        {
            get { return false; } // CSV files cannot store meta-data.
        }

        /// <summary>
        ///     Load the CSV file into the specified collection of CSV headers, separated using semi-colons, and parse the header.
        ///     Note that the CSV file needs a header, and each key needs to be unique!
        /// </summary>
        /// <param name="filename">CSV file name</param>
        /// <param name="separator">Field separator, e.g. semi-colon or comma</param>
        /// <param name="csvHeaders">Will be filled with the CSV headers</param>
        /// <param name="processingErrors">Contains the errors, if any</param>
        // REVIEW TODO This method is present for backwards compatibility. It simply calls LoadCsvHeaders.
        public static void LoadCsv(string filename, char separator, ObservableCollection<CsvHeader> csvHeaders,
            ObservableCollection<ProcessingError> processingErrors)
        {
            int numEntries;
            LoadCsvHeaders(filename, separator, csvHeaders, processingErrors, out numEntries);
        }

        /// <summary>
        ///     Load the CSV file into the specified collection of CSV headers, separated using semi-colons, and parse the header.
        ///     Note that the CSV file needs a header!
        /// </summary>
        /// <param name="filename">CSV file name</param>
        /// <param name="separator">Field separator, e.g. semi-colon or comma</param>
        /// <param name="csvHeaders">Will be filled with the CSV headers</param>
        /// <param name="processingErrors">Contains the errors, if any</param>
        /// <param name="numEntries">Returns how many entries where in the file.</param>
        public static void LoadCsvHeaders(string filename, char separator, ObservableCollection<CsvHeader> csvHeaders,
            ObservableCollection<ProcessingError> processingErrors, out int numEntries)
        {
            csvHeaders.Clear();
            processingErrors.Clear();

            numEntries = 0;

            try
            {
                // First, make sure any missing headers are replaced, and double headers are changed.
                var lineChanged = false;
                var fixedFile = Path.ChangeExtension(filename, "fixed");
                using (var reader = new StreamReader(filename, Encoding.UTF8))
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        CreateErrorMessage(processingErrors, new Exception("The file seems to be empty."), 0);
                        return;
                    }
                    var headers = new Dictionary<string, int>();
                    var lineSplit = line.Split(new[] { separator }, StringSplitOptions.None);
                    for (var col = 0; col < lineSplit.Length; col++)
                    {
                        if (headers.Keys.Contains(lineSplit[col]))
                        {
                            lineChanged = true;
                            CreateErrorMessage(processingErrors,
                                new FormatException(string.Format("Column {0} ('{1}') was defined multiple times.", col + 1, lineSplit[col])), 1);
                            headers[lineSplit[col]]++;
                            lineSplit[col] = lineSplit[col] + " (" + headers[lineSplit[col]] + ")";
                        }
                        headers[lineSplit[col]] = 1;
                        if (lineSplit[col].Trim().Length != 0) continue;
                        lineChanged = true;
                        lineSplit[col] = MarkColumnWithoutHeader + col;
                        CreateErrorMessage(processingErrors, new FormatException("Column " + (col + 1) + " had an empty header."), 1);
                    }
                    if (lineChanged)
                    {
                        using (var writer = new StreamWriter(fixedFile)) // UTF-8 is default.
                        {
                            var fixedLine = string.Join("" + separator, lineSplit);
                            writer.WriteLine(fixedLine);

                            while ((line = reader.ReadLine()) != null)
                                writer.WriteLine(line);
                        }
                    }
                }

                // If we made a new file, copy it back.
                if (lineChanged)
                {
                    File.Delete(filename); // Remove the old file.
                    File.Move(fixedFile, filename);
                }

                // TODO All these mappings from typical header names to ProcessingActions should be defined externally.
                using (var csv = new CsvReader(new StreamReader(filename, Encoding.UTF8), true, separator))
                {
                    csv.MissingFieldAction = MissingFieldAction.ReplaceByEmpty;
                    try
                    {
                        var fieldCount = csv.FieldCount;
                        var headers = csv.GetFieldHeaders();
                        csvHeaders.Clear();
                        foreach (var header in headers)
                        {
                            var action = ProcessingActions.Label;
                            switch (header.ToLower())
                            {
                                case "naam":
                                case "name":
                                    action = ProcessingActions.Name;
                                    break;

                                case "type":
                                case "typename":
                                    action = ProcessingActions.TypeName;
                                    break;

                                case "description":
                                    action = ProcessingActions.Description;
                                    break;

                                case "housenumber":
                                case "huisnummer":
                                case "huisnr":
                                case "nummer":
                                case "number":
                                    action = ProcessingActions.AddressNumber;
                                    break;

                                case "adres":
                                case "address":
                                    action = ProcessingActions.AddressStreetAndNumber;
                                    break;

                                case "straat":
                                case "loc_straat":
                                case "street":
                                    action = ProcessingActions.AddressStreet;
                                    break;

                                case "zip":
                                case "postcode":
                                    action = ProcessingActions.AddressZipCode;
                                    break;

                                case "stad":
                                case "plaats":
                                case "gemeente":
                                case "gem":
                                case "city":
                                    action = ProcessingActions.AddressCity;
                                    break;

                                case "land":
                                case "country":
                                    action = ProcessingActions.AddressCountry;
                                    break;

                                case "latitude":
                                case "lat":
                                case "lat.":
                                    action = ProcessingActions.Latitude;
                                    break;

                                case "lat.dms":
                                case "lat_dms":
                                    action = ProcessingActions.LatitudeDegrees;
                                    break;

                                case "longitude":
                                case "lon":
                                case "lon.":
                                case "long":
                                case "long.":
                                    action = ProcessingActions.Longitude;
                                    break;

                                case "lon.dms":
                                case "lon_dms":
                                case "long.dms":
                                case "long_dms":
                                    action = ProcessingActions.LongitudeDegrees;
                                    break;

                                case "folder":
                                case "path":
                                    action = ProcessingActions.Folder;
                                    break;

                                case "x_rd":
                                case "x":
                                    action = ProcessingActions.X_RD;
                                    break;

                                case "y_rd":
                                case "y":
                                    action = ProcessingActions.Y_RD;
                                    break;

                                case "wkt":
                                case "vlak":
                                    action = ProcessingActions.WKT;
                                    break;
                            }
                            if (header.StartsWith(MarkColumnWithoutHeader))
                            {
                                action = ProcessingActions.Ignore;
                            }
                            csvHeaders.Add(new CsvHeader(csvHeaders.Count + 1, header, action));
                        }

                        numEntries = 0;
                        var stopReading = false; // Stop reading once we have more than maxNumberOfExamples of values in the header preview.
                        var stopReadingHeader = new bool[fieldCount];
                        // TODO This needs to be one more than maxNumberOfOptions in CsvHeader
                        const int maxNumberOfExamples = 30;
                        while (csv.ReadNextRecord() && !stopReading)
                        {
                            stopReading = true;
                            for (int i = 0; i < fieldCount; i++)
                            {
                                if (stopReadingHeader[i]) continue;
                                string cell = csv[i];
                                if (string.IsNullOrEmpty(cell)) continue;
                                CsvHeader csvHeader = csvHeaders[i];
                                // cell = csvHeader.RemoveThousandSeparators(cell); // We do this only when we save the file.
                                if (csvHeader.ExampleCellValues.Contains(cell)) continue;
                                if (csvHeader.ExampleCellValues.Count > maxNumberOfExamples) // Show only the first 30 entries to save memory and processing time with files that have many rows.
                                {
                                    csvHeader.ExampleCellValues.Add("[and more]");
                                    stopReadingHeader[i] = true;
                                }
                                else
                                {
                                    csvHeader.ExampleCellValues.Add(cell);
                                    stopReading = false;
                                }
                            }
                            if (stopReading)
                            {
                                // Check that we really want to stop, i.e. all headers have more than 30 examples.
                                stopReading = !stopReadingHeader.Any(h => h == false);
                            }
                            numEntries++;
                        }
                        while (csv.ReadNextRecord()) // Read to the end, quickly, to determine how many entries are in the file.
                        {
                            numEntries++;
                        }
                    }
                    catch (MissingFieldCsvException e)
                    {
                        string[] fieldHeaders = csv.GetFieldHeaders();
                        int index = Array.FindIndex(fieldHeaders, f => f == null);
                        string specificMessage =
                            string.Format(
                                "Error occured when processing header {0}. Is header {1}, '{2}' defined twice?", index,
                                index - 1, fieldHeaders[index - 1]);
                        CreateErrorMessage(processingErrors, e, -1, specificMessage);
                    }
                    catch (SystemException e)
                    {
                        string[] fieldHeaders = csv.GetFieldHeaders();
                        int index = Array.FindIndex(fieldHeaders, f => f == null);
                        string specificMessage =
                            string.Format(
                                "Error occured when processing header {0}. Is header {1}, '{2}' defined twice?", index,
                                index - 1, fieldHeaders[index - 1]);
                        CreateErrorMessage(processingErrors, e, -1, specificMessage);
                    }
                }
            }
            catch (IOException e)
            {
                const string specificMessage = "Cannot read file. Perhaps it is open in another process (e.g. Excel)?";
                CreateErrorMessage(processingErrors, e, -1, specificMessage);
            }
        }

        private static void CreateErrorMessage(ICollection<ProcessingError> processingErrors, Exception e, int lineNumber = -1,
            string specificMessage = "")
        {
            if (processingErrors == null) throw new ArgumentNullException("processingErrors");
            string innerMessage = string.Empty;
            if (e.InnerException != null) innerMessage = e.InnerException.Message;
            processingErrors.Add(new ProcessingError(e.Message + " " + specificMessage, innerMessage, lineNumber));
        }

        // REVIEW TODO Legacy method name. Used once in ConvertUsbDrive. Simply calls LoadCsvData.
        public static PoiService ProcessCsv(string filename,
            char separator, bool isStaticLayer,
            ObservableCollection<CsvHeader> csvHeaders,
            ObservableCollection<ProcessingError> processingErrors,
            string nameFormatString = null,
            string descriptionFormatString = null,
            bool autoSave = true, bool useGoogle = true,
            string connectionString =
                "Server=127.0.0.1;Port=5432;User Id=bag_user;Password=bag4all;Database=bag;SearchPath=bagactueel,public") // REVIEW TODO: Bad place for default connection string.
        {
            ProgressStruct progressStruct = new ProgressStruct();
            return LoadCsvData(filename, null, separator, isStaticLayer, csvHeaders, progressStruct,
                processingErrors, null, nameFormatString, descriptionFormatString, autoSave, useGoogle, connectionString);
        }


        /// <summary>
        ///     Convert the CSV file to a data service. Remember to load the headers first!
        /// </summary>
        /// <param name="filename">CSV file.</param>
        /// <param name="destination"></param>
        /// <param name="separator">Field separator, e.g. semi-colon or comma</param>
        /// <param name="isStaticLayer">If true, layer will be static (accellerated).</param>
        /// <param name="csvHeaders">Will be filled with the CSV headers</param>
        /// <param name="progressStruct">Will be updated to keep track of progress.</param>
        /// <param name="processingErrors">Contains the errors, if any</param>
        /// <param name="callingWorker">Pass the background worker calling this method, so the method can cancel whenever the worker is cancelled.</param>
        /// <param name="descriptionFormatString"></param>
        /// <param name="useGoogle">Optional, if  true (default), use Google search to find a location in case our database does not yield a result.</param>
        /// <param name="autoSave">Optional, if true (default), automatically save the PoI service.</param>
        /// <param name="connectionString">Connection string to connect to a PostgreSQL+PostGIS database.</param>
        /// <param name="nameFormatString"></param>
        /// <returns>Returns a PoiService if successfull, null otherwise.</returns>
        /// // TODO This method only works after LoadCsvHeaders. We also still have ProcessCsv as a legacy method. This class needs cleaning up.
        public static PoiService LoadCsvData(
            string filename,
            string destination,
            char separator, bool isStaticLayer,
            ObservableCollection<CsvHeader> csvHeaders,
            ProgressStruct progressStruct,
            ObservableCollection<ProcessingError> processingErrors,
            BackgroundWorker callingWorker = null,
            string nameFormatString = null,
            string descriptionFormatString = null,
            bool autoSave = true, bool useGoogle = true,
            string connectionString = null) // TODO Ugly place for default connection string. Removed: "Server=127.0.0.1;Port=5432;User Id=bag_user;Password=bag4all;Database=bag;SearchPath=bagactueel,public"
        {
            processingErrors.Clear();

            bool canUseBag;
            try
            {
                canUseBag = BagAccessible.IsAccessible(connectionString);
                LatLongLookup.ConnectionString = connectionString; // Make sure that we are using the same connectionstring // TODO was ConvertCsv.AddressLookup, but the connection string is static.
            }
            catch (NpgsqlException e)
            {
                AppStateSettings.Instance.TriggerNotification(e.Message);
                canUseBag = false;
            }

            if (!ValidateInput(processingErrors)) return null;

            var styles = new Dictionary<string, PoI>();

            if (destination == null)
            {
                destination = filename;
            }

            var poiService = new PoiService
            {
                Name = Path.GetFileNameWithoutExtension(destination),
                Id = Guid.NewGuid(),
                IsFileBased = true,
                Folder = Path.GetDirectoryName(destination),
                StaticService = isStaticLayer
            };
            poiService.InitPoiService();
            //poiService.Settings.BaseStyles = new List<PoIStyle>
            //{
            //    new PoIStyle
            //    {
            //        Name = "default",
            //        FillColor = Colors.White,
            //        StrokeColor = Color.FromArgb(255,128,128,128),
            //        CallOutOrientation = CallOutOrientation.Right,
            //        FillOpacity = 0.3,
            //        TitleMode = TitleModes.Bottom,
            //        NameLabel = "Name",
            //        DrawingMode = DrawingModes.Image,
            //        StrokeWidth = 2,
            //        IconWidth = 48,
            //        IconHeight = 48,
            //        CallOutFillColor = Colors.White,
            //        CallOutForeground = Colors.Black,
            //        TapMode = TapMode.CallOut
            //    }
            //};

            if (isStaticLayer)
            {
                ServiceSettings settings = poiService.Settings;
                settings.CanCreate = settings.CanEdit = false;
            }

            var defaultPoiType = new PoI
            {
                Name = "Default",
                ContentId = "Default",
                Service = poiService,
                Style = new PoIStyle
                {
                    Name = "default",
                    FillColor = Colors.Transparent,
                    StrokeColor = Color.FromArgb(255, 128, 128, 128),
                    CallOutOrientation = CallOutOrientation.Right,
                    FillOpacity = 0.3,
                    TitleMode = TitleModes.Bottom,
                    NameLabel = "Name",
                    DrawingMode = DrawingModes.Image,
                    StrokeWidth = 2,
                    IconWidth = 24,
                    IconHeight = 24,
                    Icon = "images/missing.png",
                    CallOutFillColor = Colors.White,
                    CallOutForeground = Colors.Black,
                    TapMode = TapMode.CallOut
                },
                Id = Guid.NewGuid(),
                DrawingMode = DrawingModes.Image,
                MetaInfo = new List<MetaInfo>()
            };

            if (!poiService.PoITypes.Any())
            {
                poiService.PoITypes.Add(defaultPoiType);
            }

            var ignoredLabels = new HashSet<string>();

            progressStruct.NumDone = 1;
            progressStruct.Done = false;

            using (var csv = new CsvReader(new StreamReader(filename, Encoding.UTF8), true, separator))
            {
                csv.MissingFieldAction = MissingFieldAction.ReplaceByEmpty;

                PoI lastPoI = null;
                try
                {
                    var warnedColumnNames = new HashSet<string>();
                    var fieldCount = csv.FieldCount;
                    var rowCount = -1;
                    while (csv.ReadNextRecord())
                    {
                        rowCount++;

                        if (callingWorker != null && callingWorker.CancellationPending)
                        {
                            return null;
                        }

                        Debug.WriteLine("* PROCESS RECORD");
                        var useNameFormatString = !string.IsNullOrEmpty(nameFormatString);
                        double x = double.MinValue, y = double.MinValue; // RD coordinate placeholder

                        var poi = new PoI { Id = Guid.NewGuid() };
                        lastPoI = poi; // for debugging
                        double latitude = double.MinValue, longitude = double.MinValue;
                        string zipCode, fullAddress, streetAndNumber, street, number, country, folder, gemeente, wkt;
                        var city = wkt = zipCode = fullAddress = streetAndNumber = street = number = country = folder = gemeente = string.Empty;
                        var poiType = defaultPoiType;
                        var handledPosition = false;
                        var columnsToRemove = new List<string>(); // e.g. lat and lon, because they are represented in poi.Position
                        for (var fieldNumber = 0; fieldNumber < fieldCount; fieldNumber++)
                        {
                            var csvHeader = csvHeaders[fieldNumber];
                            var header = csvHeader.Header;
                            var cell = csv[fieldNumber];

                            cell = RemoveThousandSeparators(cell);
                            cell = StringCleanupExtensions.RestoreDelimiters(cell, separator);

                            if (string.IsNullOrEmpty(cell)) continue;

                            // Always add the cell to the labels, unless we ignore it.
                            if (csvHeader.ProcessingAction != ProcessingActions.Ignore)
                            {
                                poi.Labels[header] = cell;
                            }
                            else
                            {
                                // TODO BUG This statement is never executed, even if the user did set "Ignore" as the processing action in the GUI.
                                ignoredLabels.Add(header);
                            }

                            if (x > double.MinValue && y > double.MinValue)
                                CoordinateUtils.ConvertRDToLonLat(x, y, out longitude, out latitude);

                            var warnForDotsAndCommas = false;
                            switch (csvHeader.ProcessingAction)
                            {
                                case ProcessingActions.TypeName:
                                    cell = cell.Replace(" ", "");
                                    if (styles.ContainsKey(cell))
                                    {
                                        poiType = styles[cell];
                                        break;
                                    }
                                    var pt = defaultPoiType.Clone() as PoI;
                                    pt.PoiTypeId = defaultPoiType.PoiId;
                                    if (pt == null) continue;
                                    pt.Name = pt.ContentId = cell;
                                    pt.Style.Icon = cell + ".png";
                                    poiService.PoITypes.Add(pt);
                                    styles[cell] = poiType = pt;
                                    break;

                                case ProcessingActions.Name:
                                    // Remove the original label from the list of labels
                                    if (poi.Labels.ContainsKey(csvHeader.Header)) poi.Labels.Remove(csvHeader.Header);
                                    useNameFormatString = false;
                                    poi.Labels["Name"] = poi.Name = cell;
                                    break;

                                case ProcessingActions.Description:
                                    poi.Labels["Description"] = cell;
                                    break;

                                case ProcessingActions.LatitudeDegrees:
                                    latitude = ConvertDegreeMinuteSecondToWGS84(cell);
                                    columnsToRemove.Add(csvHeader.Header);
                                    break;

                                case ProcessingActions.Latitude:
                                    if (!(double.TryParse(cell, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out latitude)
                                        || double.TryParse(cell, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out latitude)))
                                    {
                                        processingErrors.Add(new ProcessingError("Error parsing latitude", header, csv.CurrentRecordIndex));
                                    }
                                    else
                                    {
                                        columnsToRemove.Add(csvHeader.Header);
                                    }
                                    break;

                                case ProcessingActions.LongitudeDegrees:
                                    longitude = ConvertDegreeMinuteSecondToWGS84(cell);
                                    columnsToRemove.Add(csvHeader.Header);
                                    break;

                                case ProcessingActions.Longitude:
                                    if (!(double.TryParse(cell, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out longitude)
                                        || double.TryParse(cell, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out longitude)))
                                    {
                                        processingErrors.Add(new ProcessingError("Error parsing X (RD)", header, csv.CurrentRecordIndex));
                                    }
                                    else
                                    {
                                        columnsToRemove.Add(csvHeader.Header);
                                    }
                                    if (x > double.MinValue && y > double.MinValue)
                                        CoordinateUtils.ConvertRDToLonLat(x, y, out longitude, out latitude);
                                    break;

                                case ProcessingActions.X_RD:
                                    if (!(double.TryParse(cell, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out x)
                                        || double.TryParse(cell, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out x)))
                                    {
                                        processingErrors.Add(new ProcessingError("Error parsing X (RD)", header, csv.CurrentRecordIndex));
                                    }
                                    else
                                    {
                                        columnsToRemove.Add(csvHeader.Header);
                                    }
                                    if (x > double.MinValue && y > double.MinValue)
                                        CoordinateUtils.ConvertRDToLonLat(x, y, out longitude, out latitude);
                                    break;

                                case ProcessingActions.Y_RD:
                                    if (!(double.TryParse(cell, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out y)
                                        || double.TryParse(cell, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out y)))
                                    {
                                        processingErrors.Add(new ProcessingError("Error parsing Y (RD)", header, csv.CurrentRecordIndex));
                                    }
                                    else
                                    {
                                        columnsToRemove.Add(csvHeader.Header);
                                    }
                                    if (x > double.MinValue && y > double.MinValue)
                                        CoordinateUtils.ConvertRDToLonLat(x, y, out longitude, out latitude);
                                    break;

                                case ProcessingActions.AddressCity:
                                    city = cell;
                                    break;

                                case ProcessingActions.AddressComplete:
                                    fullAddress = cell;
                                    break;

                                case ProcessingActions.AddressCountry:
                                    country = cell;
                                    break;

                                case ProcessingActions.AddressStreet:
                                    street = cell;
                                    break;

                                case ProcessingActions.AddressStreetAndNumber:
                                    streetAndNumber = cell;
                                    break;

                                case ProcessingActions.AddressNumber:
                                    warnForDotsAndCommas = true;
                                    number = cell;
                                    break;

                                case ProcessingActions.AddressZipCode:
                                    warnForDotsAndCommas = true;
                                    zipCode = cell.Replace(" ", string.Empty); // Remove empty spaces, e.g. 2640 AB
                                    break;

                                case ProcessingActions.AddressMunicipality:
                                    gemeente = cell;
                                    break;

                                case ProcessingActions.Folder:
                                    folder = cell;
                                    break;

                                case ProcessingActions.WKT:
                                    wkt = cell;
                                    columnsToRemove.Add(csvHeader.Header);
                                    break;

                                default:
                                    if (csvHeaders[fieldNumber].Type == MetaTypes.number &&
                                        csvHeaders[fieldNumber].MinValue > Double.MinValue)
                                    {
                                        warnForDotsAndCommas = true;
                                        double val;
                                        if (double.TryParse(cell, out val))
                                        {
                                            if (val < csvHeaders[fieldNumber].MinValue)
                                            {
                                                if (poi.Labels.ContainsKey(csvHeader.Header))
                                                {
                                                    poi.Labels.Remove(csvHeader.Header);  // Remove the value.
                                                }
                                            }
                                        }
                                    }
                                    break;
                            }

                            if (warnForDotsAndCommas)
                            {
                                warnForDotsAndCommas = !warnedColumnNames.Contains(csvHeader.Header);
                            }

                            if (!warnForDotsAndCommas) continue;
                            if (!cell.Contains(".") && !cell.Contains(",")) continue;
                            double parsedCell;
                            double.TryParse(cell, out parsedCell);
                            processingErrors.Add(new ProcessingError(
                                string.Format(
                                    "WARNING: Dots and/or comma's found in a numeric cell ({0})). This may be intentional. Check: is {1} intended as {2} (rounded off: {3})?",
                                    csvHeader.Header, cell, parsedCell, (int)parsedCell), cell, rowCount));
                            warnedColumnNames.Add(csvHeader.Header);
                        }

                        if (!string.IsNullOrWhiteSpace(wkt))
                        {
                            poiType.DrawingMode = DrawingModes.MultiPolygon; // TODO REVIEW Not always a MultiPolygon!?
                            poi.WktText = wkt;
                            // See whether it is a point.
                            try
                            {
                                var convertFromWkt = wkt.ConvertFromWkt();
                                var point = convertFromWkt as Point;
                                if (point != null)
                                {
                                    latitude = point.Y;
                                    longitude = point.X;
                                    poiType.DrawingMode = DrawingModes.Point;
                                }
                            }
                            // ReSharper disable once EmptyGeneralCatchClause
                            catch (Exception e) { } // It apparently is not a point.
                        }

                        #region retrieve location

                        var lookupKey = string.Format("{0}-{1}", zipCode, number);
                        if (LocationCache.ContainsKey(lookupKey))
                        {
                            var position = LocationCache[lookupKey];
                            longitude = position.Longitude;
                            latitude = position.Latitude;
                            handledPosition = true;
                        }
                        // Did we already provide the lat, lon? 
                        if (!handledPosition && latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180)
                        {
                            // poi.Position = new Position(longitude, latitude); will be done below.
                            handledPosition = true;
                        }

                        // If not, lookup the address via BAG or Google.
                        if (!handledPosition)
                        {
                            // Start with BAG.
                            if (canUseBag)
                            {
                                // If we have a ZIP code, it is way faster and more stable to query for zip + number, so try to get the number from the full address.
                                if (!string.IsNullOrEmpty(streetAndNumber) && string.IsNullOrEmpty(number) &&
                                    !string.IsNullOrEmpty(zipCode))
                                {
                                    try
                                    {
                                        string[] split = SplitStreetAndNumber.Split(streetAndNumber);
                                        number = split[2];
                                        street = split[1];
                                    }
                                    catch (Exception e) { } // Pity, that did not work.                                    
                                }

                                double[] position = null;
                                if (!string.IsNullOrEmpty(zipCode) && !string.IsNullOrEmpty(number))
                                {
                                    position = LatLongLookup.FromZipCode(zipCode, number);
                                }
                                if (position == null && (!string.IsNullOrEmpty(streetAndNumber) && !string.IsNullOrEmpty(city)))
                                {
                                    position = LatLongLookup.FromAddress(streetAndNumber, city);
                                }
                                if (position == null && (!string.IsNullOrEmpty(street) && !string.IsNullOrEmpty(number) && !string.IsNullOrEmpty(city)))
                                {
                                    position = LatLongLookup.FromAddress(street, number, city);
                                }
                                if (position == null && (!string.IsNullOrEmpty(street) && !string.IsNullOrEmpty(number) && !string.IsNullOrEmpty(city)))
                                {
                                    int intNumber;
                                    if (Int32.TryParse(number, out intNumber))
                                    {
                                        position = LatLongLookup.FromAddress(street, intNumber, gemeente);
                                    }
                                }

                                if (position != null)
                                {
                                    longitude = position[0];
                                    latitude = position[1];
                                }
                            }

                            // Look up the address via Google if BAG did not work.
                            if (useGoogle && (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180))
                            {
                                if (!poi.Labels.ContainsKey("GeoAddress"))
                                {
                                    string geoAddress = string.Empty;
                                    if (string.IsNullOrEmpty(streetAndNumber)
                                        && !string.IsNullOrEmpty(street)) streetAndNumber = street + " " + number;
                                    // Use Geo processing service to determine the location based on the address.
                                    if (!string.IsNullOrEmpty(fullAddress)) geoAddress = fullAddress;
                                    else if (!string.IsNullOrEmpty(zipCode)
                                             && !string.IsNullOrEmpty(city)
                                             && !string.IsNullOrEmpty(streetAndNumber))
                                        geoAddress = streetAndNumber + ", " + zipCode + ", " + city;
                                    else if (!string.IsNullOrEmpty(city)
                                             && !string.IsNullOrEmpty(streetAndNumber))
                                        geoAddress = streetAndNumber + ", " + city;
                                    if (!string.IsNullOrEmpty(country)) geoAddress += ", " + country;
                                    if (!string.IsNullOrEmpty(geoAddress))
                                    {

                                        List<SerializableGoogleAddress> addresses = GetAddresses(geoAddress).ToList(); // Cached now.
                                        //if (string.IsNullOrEmpty(geoAddress))
                                        //{
                                        if (addresses == null)
                                        {
                                            geoAddress += ", The Netherlands"; // TODO This country addition is ugly hardcoded :)
                                            addresses = GetAddresses(geoAddress).ToList(); // Cached now.
                                        }
                                        if (addresses != null && addresses.Count > 0)
                                        {
                                            SerializableGoogleAddress address = addresses.FirstOrDefault(a => !a.IsPartialMatch) ??
                                                                    addresses.First();
                                            if (address == null && !string.IsNullOrEmpty(country))
                                            {
                                                // Try again, guessing that the country is The Netherlands
                                                geoAddress += ", The Netherlands";
                                                addresses = GetAddresses(geoAddress).ToList();
                                                address = addresses.FirstOrDefault(a => !a.IsPartialMatch) ??
                                                          addresses.First();
                                            }
                                            if (address != null)
                                            {
                                                latitude = address.Latitude;
                                                longitude = address.Longitude;
                                                poi.Labels.Add("GeoAddress",
                                                    geoAddress + (address.IsPartialMatch ? "!" : string.Empty));
                                                if (address.IsPartialMatch)
                                                {
                                                    processingErrors.Add(
                                                        new ProcessingError(
                                                            string.Format(
                                                                "WARNING: Only partial match found for location {0} ({1}, {2}).",
                                                                geoAddress, longitude, latitude),
                                                                poi.Name, csv.CurrentRecordIndex));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
                        {
                            string miscInfo = "";
                            if (!string.IsNullOrEmpty(street)) miscInfo = street + " | ";
                            else miscInfo = "Street? | ";
                            if (!string.IsNullOrEmpty(number)) miscInfo += number + " | ";
                            else miscInfo += "Number? | ";
                            if (!string.IsNullOrEmpty(streetAndNumber)) miscInfo += streetAndNumber + " | ";
                            else miscInfo += "Street & Number? | ";
                            if (!string.IsNullOrEmpty(zipCode)) miscInfo += zipCode + " | ";
                            else miscInfo += "Zip Code? | ";
                            if (!string.IsNullOrEmpty(city)) miscInfo += city + " | ";
                            else miscInfo += "City? | ";
                            if (miscInfo.Length > 3) miscInfo = miscInfo.Substring(0, miscInfo.Length - 3);

                            processingErrors.Add(new ProcessingError(
                                string.Format("ERROR: No match found for location {0} ({1})", poi.Name, miscInfo),
                                poi.Name, csv.CurrentRecordIndex));
                        }
                        else
                        {
                            var position = new Position(longitude, latitude);
                            poi.Position = position;
                            LocationCache[lookupKey] = position;
                        }

                        #endregion retrieve location

                        if (useNameFormatString && !string.IsNullOrEmpty(nameFormatString))
                        {
                            poi.Labels["Name"] = poi.Name = FormatStringConversion(csv, nameFormatString);
                        }
                        if (!string.IsNullOrEmpty(descriptionFormatString))
                        {
                            poi.Labels["Description"] = FormatStringConversion(csv, descriptionFormatString);
                        }
                        if (!string.IsNullOrEmpty(folder))
                        {
                            poi.Labels["PresenterPath"] = folder;
                        }

                        foreach (string header in columnsToRemove)
                        {
                            poi.Labels.Remove(header);
                            ignoredLabels.Add(header); // Removes this column from the meta-info as well.
                        }

                        poi.PoiTypeId = poiType.Name;
                        //poi.Style = defaultPoiType.Style;
                        //poi.MetaInfo = defaultPoiType.MetaInfo;

                        poiService.PoIs.Add(poi);

                        progressStruct.NumDone = progressStruct.NumDone + 1; // Keep progress!
                    }

                    AddMetaInfo(csvHeaders, defaultPoiType, ignoredLabels);
                    // TODO Add meta info for name and descriptions.
                    //if (!string.IsNullOrEmpty(nameFormatString)) 
                    //    defaultPoiType.AddMetaInfo("Naam", "Name");
                    progressStruct.Done = true; // Indicate that we are done!

                    if (!autoSave)
                    {
                        return poiService;
                    }
                    poiService.Folder = Path.GetDirectoryName(destination);
                    poiService.ToXml().Save(poiService.FileName);
                    return poiService;
                }
                catch (SystemException e)
                {
                    processingErrors.Add(new ProcessingError(e.Message + " (POI: " + lastPoI + ")",
                        e.InnerException == null ? "" : e.InnerException.Message, -1));
                    Logger.Log("ConvertCsv", "Error parsing CSV", e.Message, Logger.Level.Error, true);

                    progressStruct.Done = true;

                    return null;
                }
            }
        }

        /// <summary>
        ///     Remove thousand separators to prevent incorrect number parsing across locales. Note that we use
        ///     a crude method here (no cultures et cetera).
        /// </summary>
        /// <param name="data">The string to remove thousand separators from (if it is a number).</param>
        /// <returns>A string without any thousand separators, if there were any.</returns>
        private static string RemoveThousandSeparators(string data)
        {
            double n;
            bool isNumeric = double.TryParse(data, out n);
            if (!isNumeric)
            {
                // Do nothing.
                return data;
            }
            if (Settings.Default.CommaIsThousandSeparator)
            {
                // Remove all comma's, so we don't erroneously apply e.g. a NL locale to GB data.
                data = data.Replace(",", String.Empty);
            }
            if (Settings.Default.DotIsThousandSeparator)
            {
                // Remove all periods, so we don't erroneously apply e.g. a NL locale to GB data.
                data = data.Replace(".", String.Empty);
            }
            return data;
        }

        /// <summary>
        ///     Add the meta info to the default PoI type.
        /// </summary>
        /// <param name="csvHeaders"></param>
        /// <param name="defaultPoiType"></param>
        /// <param name="ignoredLabels"></param>
        private static void AddMetaInfo(IEnumerable<CsvHeader> csvHeaders, BaseContent defaultPoiType,
            IEnumerable<string> ignoredLabels)
        {
            foreach (CsvHeader csvHeader in csvHeaders.OrderBy(c => c.Index).ToList())
            {
                if (!ignoredLabels.Contains(csvHeader.Header))
                {
                    defaultPoiType.MetaInfo.Add(csvHeader);
                }
            }
        }

        /// <summary>
        /// ???
        /// </summary>
        /// <param name="csv"></param>
        /// <param name="formatString"></param>
        /// <returns></returns>
        private static string FormatStringConversion(CsvReader csv, string formatString)
        {
            var ms = Regex.Matches(formatString);
            if (ms.Count == 0) return formatString;
            foreach (var groups in from Match match in ms select match.Groups)
            {
                int index;
                var curValue = groups["Index"].Value;
                if (!int.TryParse(curValue, NumberStyles.Any, CultureInfo.InvariantCulture, out index) || --index < 0) return formatString;
                var replace = "{" + curValue + "}";
                formatString = formatString.Replace(replace, csv[index]);
            }
            return formatString;
        }

        #region Google address lookup, including a persistent cache.

        private static int delay = 1000;
        private static Dictionary<string, List<SerializableGoogleAddress>> googleAddressCache;

        private static IEnumerable<SerializableGoogleAddress> GetAddresses(string geoAddress)
        {
            if (googleAddressCache == null)
            {
                googleAddressCache = ReadPersistentDictionary<SerializableGoogleAddress>(googleCacheFile);
            }

            if (string.IsNullOrEmpty(geoAddress)) return null;
            List<SerializableGoogleAddress> cachedAddress;
            if (googleAddressCache.TryGetValue(geoAddress, out cachedAddress))
            {
                return cachedAddress;
            }

            try
            {
                Thread.Sleep(delay);
                var addresses = (new GoogleGeoCoder()).GeoCode(geoAddress).ToList(); // TODO REVIEW
                var serializableGoogleAddresses = new List<SerializableGoogleAddress>();
                serializableGoogleAddresses.AddRange(addresses.Select(googleAddress => new SerializableGoogleAddress(googleAddress)));
                googleAddressCache[geoAddress] = serializableGoogleAddresses;
                WritePersistentDictionary(googleCacheFile, googleAddressCache);
                return serializableGoogleAddresses;
            }
            catch (Exception)
            {
                // We might end up here, since there is a query limit.
                delay += 1000;
                return delay > 5000 ? null : GetAddresses(geoAddress);
            }
        }

        private static Dictionary<string, List<T>> ReadPersistentDictionary<T>(string sourceFile) where T : IConvertibleXml, new()
        {
            Dictionary<string, List<T>> cache = new Dictionary<string, List<T>>();
            try
            {
                XDocument xDocument = XDocument.Load(sourceFile);
                XElement rElement = xDocument.Element(XName.Get("Dictionary"));
                if (rElement == null)
                {
                    return cache;
                }
                IEnumerable<XElement> xElements = rElement.Elements();
                foreach (var xElement in xElements)
                {
                    string key = xElement.GetString("_dictKey");
                    cache[key] = new List<T>();
                    IEnumerable<XElement> childElements = xElement.Elements();
                    foreach (var childElement in childElements)
                    {
                        T t = new T();
                        t.FromXml(childElement);
                        cache[key].Add(t);
                    }

                }
            }
            catch (Exception)
            {
                // No cache.
            }
            return cache;
        }

        private static bool WritePersistentDictionary<T>(string targetFile, Dictionary<string, List<T>> dictionary) where T : IConvertibleXml, new()
        {
            try
            {
                XDocument xDocument = new XDocument();
                XElement rElement = new XElement(XName.Get("Dictionary"));
                xDocument.Add(rElement);

                foreach (KeyValuePair<string, List<T>> keyValuePair in dictionary)
                {
                    XElement xElement = new XElement(XName.Get("Items"));
                    xElement.SetAttributeValue("_dictKey", keyValuePair.Key);
                    foreach (T t in keyValuePair.Value)
                    {
                        XElement tElement = t.ToXml();
                        xElement.Add(tElement);
                    }
                    rElement.Add(xElement);
                }
                xDocument.Save(targetFile);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // GoogleAddress is not serializable, so in order to cache it we need to make a serializable version.
        private class SerializableGoogleAddress : IConvertibleXml
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public bool IsPartialMatch { get; set; }

            public SerializableGoogleAddress()
            {

            }

            public SerializableGoogleAddress(GoogleAddress sourceAddress)
            {
                IsPartialMatch = sourceAddress.IsPartialMatch;
                Latitude = sourceAddress.Coordinates.Latitude;
                Longitude = sourceAddress.Coordinates.Longitude;
            }

            public string XmlNodeId
            {
                get { return "GoogleAddress"; }
            }

            public XElement ToXml()
            {
                XElement rootElement = new XElement(XName.Get(XmlNodeId));
                rootElement.SetAttributeValue("latitude", Latitude);
                rootElement.SetAttributeValue("longitude", Longitude);
                rootElement.SetAttributeValue("isPartialMatch", IsPartialMatch);
                return rootElement;
            }

            public void FromXml(XElement element)
            {
                Latitude = element.GetDouble("latitude");
                Longitude = element.GetDouble("longitude");
                IsPartialMatch = element.GetBool("isPartialMatch");
            }
        }

        #endregion Google address lookup, including a persistent cache.

        #region Utility stuff.

        /// <summary>
        ///     Validate the input.
        /// </summary>
        /// <param name="processingErrors">Contains the errors, if any</param>
        /// <returns></returns>
        private static bool ValidateInput(ObservableCollection<ProcessingError> processingErrors)
        {
            // TODO Check that Name, Description, TypeName, Address, etc is only specified once
            // TODO Check that ProcessingAction.Label is unique
            return true;
        }

        private static double ConvertDegreeMinuteSecondToWGS84(string dms)
        {
            var result = -1D;
            var split = dms.Split(new[] { '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length > 3) return result;
            int degrees, minutes, seconds;
            if (!int.TryParse(split[0], NumberStyles.Any, CultureInfo.InvariantCulture, out degrees)) return result;
            if (!int.TryParse(split[1], NumberStyles.Any, CultureInfo.InvariantCulture, out minutes)) return result;
            if (!int.TryParse(split[2], NumberStyles.Any, CultureInfo.InvariantCulture, out seconds)) return result;
            result = (degrees + minutes / 60D + seconds / 3600D) * (degrees < 0 ? -1 : 1);
            return result;
        }

        #endregion Utility stuff.

        #region NameFormatString regex

        //  using System.Text.RegularExpressions;

        /// <summary>
        ///     Regular expression built for C# on: Tue, Mar 26, 2013, 10:41:11 AM
        ///     Using Expresso Version: 3.0.4750, http://www.ultrapico.com
        ///
        ///     A description of the regular expression:
        ///
        ///     \w*{
        ///     Alphanumeric, any number of repetitions
        ///     {
        ///     [Indexes]: A named capture group. [\d+]
        ///     Any digit, one or more repetitions
        ///     }\w*
        ///     }
        ///     Alphanumeric, any number of repetitions
        ///
        ///
        /// </summary>
        public static Regex Regex = new Regex("\\w*{(?<Index>\\d+)}\\w*", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static string googleCacheFile = Path.Combine(AppStateSettings.CacheFolder, "csv_google_addresses.xml");

        //// Split the InputText wherever the regex matches
        // string[] results = regex.Split(InputText);

        //// Capture the first Match, if any, in the InputText
        // Match m = regex.Match(InputText);

        //// Capture all Matches in the InputText
        // MatchCollection ms = regex.Matches(InputText);

        //// Test to see if there is a match in the InputText
        // bool IsMatch = regex.IsMatch(InputText);

        //// Get the names of all the named and numbered capture groups
        // string[] GroupNames = regex.GetGroupNames();

        //// Get the numbers of all the named and numbered capture groups
        // int[] GroupNumbers = regex.GetGroupNumbers();

        #endregion NameFormatString regex
    }
}