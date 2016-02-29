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
  public class Log {
    
    /// <summary>
    /// Gets or Sets Ts
    /// </summary>
    [DataMember(Name="ts", EmitDefaultValue=false)]
    public long? Ts { get; set; }

    
    /// <summary>
    /// Gets or Sets Prop
    /// </summary>
    [DataMember(Name="prop", EmitDefaultValue=false)]
    public string Prop { get; set; }

    
    /// <summary>
    /// Gets or Sets Value
    /// </summary>
    [DataMember(Name="value", EmitDefaultValue=false)]
    public Object Value { get; set; }

    

    /// <summary>
    /// Get the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()  {
      var sb = new StringBuilder();
      sb.Append("class Log {\n");
      
      sb.Append("  Ts: ").Append(Ts).Append("\n");
      
      sb.Append("  Prop: ").Append(Prop).Append("\n");
      
      sb.Append("  Value: ").Append(Value).Append("\n");
      
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
