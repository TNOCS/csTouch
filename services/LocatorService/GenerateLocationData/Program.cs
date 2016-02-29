using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenerateLocationData.BAG;
using GenerateLocationData.CBS;

namespace GenerateLocationData
{
    class Program
    {
        static void Main(string[] args)
        {
            var osm = new OsmNeighbourhoodFilter(Properties.Settings.Default.CbsConnectionString);
            var bag = new BagFilter(Properties.Settings.Default.BagConnectionString);
            var osm = new OsmFilter(Properties.Settings.Default.OsmConnectionString);
            bag.Enhance(osm.LocationDescriptions);
            foreach (var location in osm.LocationDescriptions)
            {
                Console.WriteLine(location);
            }

            Console.WriteLine("Press a key to exit.");
            Console.ReadKey();
        }
    }
}
