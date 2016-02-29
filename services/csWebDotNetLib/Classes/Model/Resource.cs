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
  public class Resource {

        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or Sets Url
        /// </summary>
        [DataMember(Name="url", EmitDefaultValue=false)]
    public string Url { get; set; }

    
    /// <summary>
    /// Gets or Sets FeatureTypes
    /// </summary>
    [DataMember(Name="featureTypes", EmitDefaultValue=false)]
    public Dictionary<string, FeatureType> FeatureTypes { get; set; }

    
    /// <summary>
    /// Gets or Sets PropertyTypeData
    /// </summary>
    [DataMember(Name="propertyTypeData", EmitDefaultValue=false)]
    public Dictionary<string, PropertyType> PropertyTypeData { get; set; }

    

    /// <summary>
    /// Get the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()  {
      var sb = new StringBuilder();
      sb.Append("class Resource {\n");
      
      sb.Append("  Url: ").Append(Url).Append("\n");
      
      sb.Append("  FeatureTypes: ").Append(FeatureTypes).Append("\n");
      
      sb.Append("  PropertyTypeData: ").Append(PropertyTypeData).Append("\n");
      
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
