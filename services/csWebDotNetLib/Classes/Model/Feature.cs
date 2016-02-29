using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace IO.Swagger.Model {

  /// <summary>
  /// 
  /// </summary>
  [DataContract]
  public class Feature {
    
    /// <summary>
    /// Gets or Sets Id
    /// </summary>
    [DataMember(Name="id", EmitDefaultValue=false)]
    public string Id { get; set; }

    
    /// <summary>
    /// Gets or Sets Geometry
    /// </summary>
    [DataMember(Name="geometry", EmitDefaultValue=false)]
    public Geometry Geometry { get; set; }

    
    /// <summary>
    /// Gets or Sets Type
    /// </summary>
    [DataMember(Name="type", EmitDefaultValue=false)]
    public string Type { get; set; }

    
    /// <summary>
    /// Gets or Sets Properties
    /// </summary>
    [DataMember(Name="properties", EmitDefaultValue=false)]
    public Dictionary<string, Object> Properties { get; set; }

    
    /// <summary>
    /// Gets or Sets Logs
    /// </summary>
    [DataMember(Name="logs", EmitDefaultValue=false)]
    public Dictionary<string, List<Log>> Logs { get; set; }

    

    /// <summary>
    /// Get the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()  {
      var sb = new StringBuilder();
      sb.Append("class Feature {\n");
      
      sb.Append("  Id: ").Append(Id).Append("\n");
      
      sb.Append("  Geometry: ").Append(Geometry).Append("\n");
      
      sb.Append("  Type: ").Append(Type).Append("\n");
      
      sb.Append("  Properties: ").Append(Properties).Append("\n");
      
      sb.Append("  Logs: ").Append(Logs).Append("\n");
      
      sb.Append("}\n");
      return sb.ToString();
    }

    /// <summary>
    /// Get the JSON string presentation of the object
    /// </summary>
    /// <returns>JSON string presentation of the object</returns>
    public string ToJson() {
      return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

}
}
