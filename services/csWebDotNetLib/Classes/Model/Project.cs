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
  public class Project {
    
    /// <summary>
    /// Gets or Sets Storage
    /// </summary>
    [DataMember(Name="storage", EmitDefaultValue=false)]
    public string Storage { get; set; }

    
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
    /// Gets or Sets Connected
    /// </summary>
    [DataMember(Name="connected", EmitDefaultValue=false)]
    public bool? Connected { get; set; }

    
    /// <summary>
    /// Gets or Sets Logo
    /// </summary>
    [DataMember(Name="logo", EmitDefaultValue=false)]
    public string Logo { get; set; }

    
    /// <summary>
    /// Gets or Sets Url
    /// </summary>
    [DataMember(Name="url", EmitDefaultValue=false)]
    public string Url { get; set; }

    
    /// <summary>
    /// Gets or Sets Groups
    /// </summary>
    [DataMember(Name="groups", EmitDefaultValue=false)]
    public List<Group> Groups { get; set; }

    

    /// <summary>
    /// Get the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()  {
      var sb = new StringBuilder();
      sb.Append("class Project {\n");
      
      sb.Append("  Storage: ").Append(Storage).Append("\n");
      
      sb.Append("  Title: ").Append(Title).Append("\n");
      
      sb.Append("  Id: ").Append(Id).Append("\n");
      
      sb.Append("  Connected: ").Append(Connected).Append("\n");
      
      sb.Append("  Logo: ").Append(Logo).Append("\n");
      
      sb.Append("  Url: ").Append(Url).Append("\n");
      
      sb.Append("  Groups: ").Append(Groups).Append("\n");
      
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
