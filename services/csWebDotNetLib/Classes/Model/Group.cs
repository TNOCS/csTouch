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
  public class Group {
    
    /// <summary>
    /// Gets or Sets Title
    /// </summary>
    [DataMember(Name="title", EmitDefaultValue=false)]
    public string Title { get; set; }

    
    /// <summary>
    /// Gets or Sets Id
    /// </summary>
    [DataMember(Name="id", EmitDefaultValue=false)]
    public string Id { get; set; }

    
    /// <summary>
    /// Gets or Sets Description
    /// </summary>
    [DataMember(Name="description", EmitDefaultValue=false)]
    public string Description { get; set; }

    
    /// <summary>
    /// Gets or Sets Clustering
    /// </summary>
    [DataMember(Name="clustering", EmitDefaultValue=false)]
    public bool? Clustering { get; set; }

    
    /// <summary>
    /// Gets or Sets Layers
    /// </summary>
    [DataMember(Name="layers", EmitDefaultValue=false)]
    public List<Layer> Layers { get; set; }

    

    /// <summary>
    /// Get the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()  {
      var sb = new StringBuilder();
      sb.Append("class Group {\n");
      
      sb.Append("  Title: ").Append(Title).Append("\n");
      
      sb.Append("  Id: ").Append(Id).Append("\n");
      
      sb.Append("  Description: ").Append(Description).Append("\n");
      
      sb.Append("  Clustering: ").Append(Clustering).Append("\n");
      
      sb.Append("  Layers: ").Append(Layers).Append("\n");
      
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
