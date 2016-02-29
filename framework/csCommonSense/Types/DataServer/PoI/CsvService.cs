using System;

namespace csCommon.Types.DataServer.PoI
{
    // TODO Add the code to parse CSV files.
    // TODO Check whether we already have a DS that represents a CSV
    // TODO See csDataServerPlugin.DataServerPlugin.InitShapeLayers to instantiate the Csv
    // TODO Watch the folders for new shape files or CSVs
    // TODO Generate a CSV from a layer

    public abstract class CsvService : ExtendedPoiService // TODO Review: made abstract so it is not used.
    {
         

        public new static CsvService CreateService(string name, Guid id, string folder = "", string relativeFolder = "")
        {
            throw new NotImplementedException("CsvService implementation requires a lot of reviewing & may not be needed anymore.");
            return ExtendedPoiService.CreateService(name, id, folder, relativeFolder) as CsvService;
        }

        protected override Exception ProcessFile()
        {
            return base.ProcessFile();

            // TODO Add the code to process the CSV
        }
    }
}