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
  public class PropertyType {
    
    /// <summary>
    /// Gets or Sets Label
    /// </summary>
    [DataMember(Name="label", EmitDefaultValue=false)]
    public string Label { get; set; }

    
    /// <summary>
    /// Gets or Sets Title
    /// </summary>
    [DataMember(Name="title", EmitDefaultValue=false)]
    public string Title { get; set; }

    
    /// <summary>
    /// Gets or Sets Description
    /// </summary>
    [DataMember(Name="description", EmitDefaultValue=false)]
    public string Description { get; set; }

    
    /// <summary>
    /// Gets or Sets Type
    /// </summary>
    [DataMember(Name="type", EmitDefaultValue=false)]
    public string Type { get; set; }

    
    /// <summary>
    /// Gets or Sets Section
    /// </summary>
    [DataMember(Name="section", EmitDefaultValue=false)]
    public string Section { get; set; }

    
    /// <summary>
    /// Gets or Sets StringFormat
    /// </summary>
    [DataMember(Name="stringFormat", EmitDefaultValue=false)]
    public string StringFormat { get; set; }

    
    /// <summary>
    /// Gets or Sets VisibleInCallout
    /// </summary>
    [DataMember(Name="visibleInCallout", EmitDefaultValue=false)]
    public bool? VisibleInCallout { get; set; }

    
    /// <summary>
    /// Gets or Sets CanEdit
    /// </summary>
    [DataMember(Name="canEdit", EmitDefaultValue=false)]
    public bool? CanEdit { get; set; }

    
    /// <summary>
    /// Gets or Sets FilterType
    /// </summary>
    [DataMember(Name="filterType", EmitDefaultValue=false)]
    public string FilterType { get; set; }

    
    /// <summary>
    /// Gets or Sets IsSearchable
    /// </summary>
    [DataMember(Name="isSearchable", EmitDefaultValue=false)]
    public bool? IsSearchable { get; set; }

    
    /// <summary>
    /// Gets or Sets MinValue
    /// </summary>
    [DataMember(Name="minValue", EmitDefaultValue=false)]
    public double? MinValue { get; set; }

    
    /// <summary>
    /// Gets or Sets MaxValue
    /// </summary>
    [DataMember(Name="maxValue", EmitDefaultValue=false)]
    public double? MaxValue { get; set; }

    
    /// <summary>
    /// Gets or Sets DefaultValue
    /// </summary>
    [DataMember(Name="defaultValue", EmitDefaultValue=false)]
    public double? DefaultValue { get; set; }

    

    /// <summary>
    /// Get the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()  {
      var sb = new StringBuilder();
      sb.Append("class PropertyType {\n");
      
      sb.Append("  Label: ").Append(Label).Append("\n");
      
      sb.Append("  Title: ").Append(Title).Append("\n");
      
      sb.Append("  Description: ").Append(Description).Append("\n");
      
      sb.Append("  Type: ").Append(Type).Append("\n");
      
      sb.Append("  Section: ").Append(Section).Append("\n");
      
      sb.Append("  StringFormat: ").Append(StringFormat).Append("\n");
      
      sb.Append("  VisibleInCallout: ").Append(VisibleInCallout).Append("\n");
      
      sb.Append("  CanEdit: ").Append(CanEdit).Append("\n");
      
      sb.Append("  FilterType: ").Append(FilterType).Append("\n");
      
      sb.Append("  IsSearchable: ").Append(IsSearchable).Append("\n");
      
      sb.Append("  MinValue: ").Append(MinValue).Append("\n");
      
      sb.Append("  MaxValue: ").Append(MaxValue).Append("\n");
      
      sb.Append("  DefaultValue: ").Append(DefaultValue).Append("\n");
      
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
