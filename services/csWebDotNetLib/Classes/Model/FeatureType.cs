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
  public class FeatureType {
    
    /// <summary>
    /// Gets or Sets Id
    /// </summary>
    [DataMember(Name="id", EmitDefaultValue=false)]
    public string Id { get; set; }

    
    /// <summary>
    /// Gets or Sets Name
    /// </summary>
    [DataMember(Name="name", EmitDefaultValue=false)]
    public string Name { get; set; }

    
    /// <summary>
    /// Gets or Sets ShowAllProperties
    /// </summary>
    [DataMember(Name="showAllProperties", EmitDefaultValue=false)]
    public bool? ShowAllProperties { get; set; }

    
    /// <summary>
    /// Gets or Sets Style
    /// </summary>
    [DataMember(Name="style", EmitDefaultValue=false)]
    public FeatureTypeStyle Style { get; set; }

    
    /// <summary>
    /// Gets or Sets PropertyTypeKeys
    /// </summary>
    [DataMember(Name="propertyTypeKeys", EmitDefaultValue=false)]
    public string PropertyTypeKeys { get; set; }

    

    /// <summary>
    /// Get the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()  {
      var sb = new StringBuilder();
      sb.Append("class FeatureType {\n");
      
      sb.Append("  Id: ").Append(Id).Append("\n");
      
      sb.Append("  Name: ").Append(Name).Append("\n");
      
      sb.Append("  ShowAllProperties: ").Append(ShowAllProperties).Append("\n");
      
      sb.Append("  Style: ").Append(Style).Append("\n");
      
      sb.Append("  PropertyTypeKeys: ").Append(PropertyTypeKeys).Append("\n");
      
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
