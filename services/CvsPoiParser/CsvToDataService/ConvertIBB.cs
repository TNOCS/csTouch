using BagDataAccess;
using csShared;
using DataServer;
using GeoCoding.Google;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace CsvToDataService
{
    public class ConvertIBB // TODO Clean up. There seems to be quite some duplicate code in here (with other classes).
    {
        //private static readonly AddressLookup AddressLookup = new AddressLookup();

        private const string DefaultConnectionString =
            "Server=127.0.0.1;Port=5432;User Id=bag_user;Password=bag4all;Database=bag;SearchPath=bagactueel,public"; // TODO Connection settings should be moved elsewhere (in config/settings, not in code).

        private static readonly GoogleGeoCoder GeoCoder = new GoogleGeoCoder();

        private readonly bool canUseBag;

        /// <summary>
        ///     Default constructor, optionally using a connection string to the BAG database.
        /// </summary>
        /// <param name="connectionString">Connection string to connect to a PostgreSQL+PostGIS database.</param>
        public ConvertIBB(string connectionString = DefaultConnectionString)
        {
            canUseBag = BagAccessible.IsAccessible(connectionString);
        }

        public void SetLocation(BaseContent poi, string city, string zipCode, string street, string houseNumber,
            string country = "NL")
        {
            if (canUseBag)
            {
                double[] position = LatLongLookup.FromZipCode(zipCode, houseNumber);
                if (position != null)
                {
                    poi.Position = new Position(position[0], position[1]);
                    return;
                }
            }
            try
            {
                Thread.Sleep(1000);
                GoogleAddress address =
                    GeoCoder.GeoCode(string.Format("{0} {1}, {2}, {3}", street, houseNumber, city, country))
                        .FirstOrDefault();
                if (address == null) return;
                poi.Position = new Position(address.Coordinates.Longitude, address.Coordinates.Latitude);
            }
            catch (Exception)
            {
                try
                {
                    Thread.Sleep(3000);
                    GoogleAddress address =
                        GeoCoder.GeoCode(string.Format("{0} {1}, {2}, {3}", street, houseNumber, city, country))
                            .FirstOrDefault();
                    if (address == null) return;
                    poi.Position = new Position(address.Coordinates.Longitude, address.Coordinates.Latitude);
                }
                catch (Exception e)
                {
                    AppStateSettings.Instance.TriggerNotification(e.Message);
                }
            }
        }

        /// <summary>
        ///     Convert the XML file to a data service.
        /// </summary>
        /// <param name="file">XML file.</param>
        /// <returns>Returns a PoiService if successfull, null otherwise.</returns>
        public PoiService ProcessXml(string filename)
        {
            var poiService = new PoiService
            {
                Name = Path.GetFileNameWithoutExtension(filename),
                Id = Guid.NewGuid(),
                IsFileBased = true,
                Folder = Path.GetDirectoryName(filename),
                StaticService = false
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

            //var defaultPoiType = new PoI
            //{
            //    Name = "Default",
            //    ContentId = "Default",
            //    Style = new PoIStyle
            //    {
            //        Name = "default",
            //        FillColor = Colors.White,
            //        StrokeColor = Color.FromArgb(255, 128, 128, 128),
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
            //    },
            //    Id = Guid.NewGuid(),
            //    DrawingMode = DrawingModes.Image,
            //    MetaInfo = new List<MetaInfo>()
            //};

            return poiService;
        }
    }
}