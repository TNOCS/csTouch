using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace DataServer.SqlProcessing
{
    [Serializable]
    public class SqlQueries : ContentList
    {
        public System.Threading.Tasks.Task Execute(PoiService poiService, PoI poi, List<Point> zone)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                if (Items == null || Items.Count == 0) return;
                foreach (var sqlQuery in Items.OfType<SqlQuery>())
                {
                    sqlQuery.Execute(poiService, poi, zone);
                }
            });
        }

        public string XmlNode
        {
            get { return "SqlQueries"; }
        }
    }
}