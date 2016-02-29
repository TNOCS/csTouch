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
  public class Layer {
    
    /// <summary>
    /// Gets or Sets Storage
    /// </summary>
    [DataMember(Name="storage", EmitDefaultValue=false)]
    public string Storage { get; set; }

    
    /// <summary>
    /// Gets or Sets UseLog
    /// </summary>
    [DataMember(Name="useLog", EmitDefaultValue=false)]
    public bool? UseLog { get; set; }

    
    /// <summary>
    /// Gets or Sets Features
    /// </summary>
    [DataMember(Name="features", EmitDefaultValue=false)]
    public List<Feature> Features { get; set; }

    
    /// <summary>
    /// Gets or Sets Id
    /// </summary>
    [DataMember(Name="id", EmitDefaultValue=false)]
    public string Id { get; set; }

    
    /// <summary>
    /// Gets or Sets Updated
    /// </summary>
    [DataMember(Name="updated", EmitDefaultValue=false)]
    public double? Updated { get; set; }

    
    /// <summary>
    /// Gets or Sets Tags
    /// </summary>
    [DataMember(Name="tags", EmitDefaultValue=false)]
    public List<string> Tags { get; set; }

    
    /// <summary>
    /// Gets or Sets Dynamic
    /// </summary>
    [DataMember(Name="dynamic", EmitDefaultValue=false)]
    public bool? Dynamic { get; set; }

    
    /// <summary>
    /// Gets or Sets Title
    /// </summary>
    [DataMember(Name="title", EmitDefaultValue=false)]
    public string Title { get; set; }

    
    /// <summary>
    /// Gets or Sets Type
    /// </summary>
    [DataMember(Name="type", EmitDefaultValue=false)]
    public string Type { get; set; }

    
    /// <summary>
    /// Gets or Sets Url
    /// </summary>
    [DataMember(Name="url", EmitDefaultValue=false)]
    public string Url { get; set; }

    
    /// <summary>
    /// Gets or Sets Description
    /// </summary>
    [DataMember(Name="description", EmitDefaultValue=false)]
    public string Description { get; set; }

    
    /// <summary>
    /// Gets or Sets DefaultFeatureType
    /// </summary>
    [DataMember(Name="defaultFeatureType", EmitDefaultValue=false)]
    public string DefaultFeatureType { get; set; }

    
    /// <summary>
    /// Gets or Sets TypeUrl
    /// </summary>
    [DataMember(Name="typeUrl", EmitDefaultValue=false)]
    public string TypeUrl { get; set; }

    

    /// <summary>
    /// Get the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()  {
      var sb = new StringBuilder();
      sb.Append("class Layer {\n");
      
      sb.Append("  Storage: ").Append(Storage).Append("\n");
      
      sb.Append("  UseLog: ").Append(UseLog).Append("\n");
      
      sb.Append("  Features: ").Append(Features).Append("\n");
      
      sb.Append("  Id: ").Append(Id).Append("\n");
      
      sb.Append("  Updated: ").Append(Updated).Append("\n");
      
      sb.Append("  Tags: ").Append(Tags).Append("\n");
      
      sb.Append("  Dynamic: ").Append(Dynamic).Append("\n");
      
      sb.Append("  Title: ").Append(Title).Append("\n");
      
      sb.Append("  Type: ").Append(Type).Append("\n");
      
      sb.Append("  Url: ").Append(Url).Append("\n");
      
      sb.Append("  Description: ").Append(Description).Append("\n");
      
      sb.Append("  DefaultFeatureType: ").Append(DefaultFeatureType).Append("\n");
      
      sb.Append("  TypeUrl: ").Append(TypeUrl).Append("\n");
      
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
