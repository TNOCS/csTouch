using System;
using System.Diagnostics;
using System.Xml.Serialization;

namespace DataServer.SqlProcessing
{
    public enum SqlOutputType
    {
        /// <summary>
        /// The column represents label data.
        /// </summary>
        Label,
        /// <summary>
        /// The column represents the name.
        /// The default name is Name.
        /// </summary>
        Name,
        /// <summary>
        /// The unique identifier, default name is Id.
        /// </summary>
        Id,
        /// <summary>
        /// The column represents the position in WGS84, for example use
        /// ST_AsText(ST_Transform(geom,4326)) to transform the geometry to a point.
        /// The default name is Point, so if you use 
        /// ST_AsText(ST_Transform(geom,4326)) as Point, you don't need to specify the Point output.
        /// </summary>
        Point,
        /// <summary>
        /// The column represents the polygon shape (single outer polygon) in WGS84, for example use
        /// ST_AsText(ST_Transform(geom,4326)) to transform the geometry to a point collection.
        /// The default name is Points, so if you use 
        /// ST_AsText(ST_Transform(geom,4326)) as Points, you don't need to specify the Points output.
        /// </summary>
        Points
    }

    [Serializable]
    [DebuggerDisplay("Type: {Type}, Name: {Name}")]
    public class SqlOutputParameter
    {
        /// <summary>
        /// The name of the column that is returned in the SQL result.
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// The output type.
        /// </summary>
        [XmlAttribute("type")]
        public SqlOutputType OutputType { get; set; }
    }
}