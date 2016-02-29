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
  public class FeatureTypeStyle {
    
    /// <summary>
    /// Gets or Sets NameLabel
    /// </summary>
    [DataMember(Name="nameLabel", EmitDefaultValue=false)]
    public string NameLabel { get; set; }

    
    /// <summary>
    /// Gets or Sets FillColor
    /// </summary>
    [DataMember(Name="fillColor", EmitDefaultValue=false)]
    public string FillColor { get; set; }

    
    /// <summary>
    /// Gets or Sets StrokeColor
    /// </summary>
    [DataMember(Name="strokeColor", EmitDefaultValue=false)]
    public string StrokeColor { get; set; }

    
    /// <summary>
    /// Gets or Sets SelectedFillColor
    /// </summary>
    [DataMember(Name="selectedFillColor", EmitDefaultValue=false)]
    public string SelectedFillColor { get; set; }

    
    /// <summary>
    /// Gets or Sets SelectedStrokeColor
    /// </summary>
    [DataMember(Name="selectedStrokeColor", EmitDefaultValue=false)]
    public string SelectedStrokeColor { get; set; }

    
    /// <summary>
    /// Gets or Sets SelectedStrokeWidth
    /// </summary>
    [DataMember(Name="selectedStrokeWidth", EmitDefaultValue=false)]
    public double? SelectedStrokeWidth { get; set; }

    
    /// <summary>
    /// Gets or Sets Height
    /// </summary>
    [DataMember(Name="height", EmitDefaultValue=false)]
    public double? Height { get; set; }

    
    /// <summary>
    /// Gets or Sets Opacity
    /// </summary>
    [DataMember(Name="opacity", EmitDefaultValue=false)]
    public double? Opacity { get; set; }

    
    /// <summary>
    /// Gets or Sets FillOpacity
    /// </summary>
    [DataMember(Name="fillOpacity", EmitDefaultValue=false)]
    public double? FillOpacity { get; set; }

    
    /// <summary>
    /// Gets or Sets Stroke
    /// </summary>
    [DataMember(Name="stroke", EmitDefaultValue=false)]
    public double? Stroke { get; set; }

    
    /// <summary>
    /// Gets or Sets DrawingMode
    /// </summary>
    [DataMember(Name="drawingMode", EmitDefaultValue=false)]
    public string DrawingMode { get; set; }

    
    /// <summary>
    /// Gets or Sets StrokeWidth
    /// </summary>
    [DataMember(Name="strokeWidth", EmitDefaultValue=false)]
    public double? StrokeWidth { get; set; }

    
    /// <summary>
    /// Gets or Sets IconWidth
    /// </summary>
    [DataMember(Name="iconWidth", EmitDefaultValue=false)]
    public double? IconWidth { get; set; }

    
    /// <summary>
    /// Gets or Sets IconHeight
    /// </summary>
    [DataMember(Name="iconHeight", EmitDefaultValue=false)]
    public double? IconHeight { get; set; }

    
    /// <summary>
    /// Gets or Sets IconUri
    /// </summary>
    [DataMember(Name="iconUri", EmitDefaultValue=false)]
    public string IconUri { get; set; }

    
    /// <summary>
    /// Gets or Sets CornerRadius
    /// </summary>
    [DataMember(Name="cornerRadius", EmitDefaultValue=false)]
    public double? CornerRadius { get; set; }

    
    /// <summary>
    /// Gets or Sets MaxTitleResolution
    /// </summary>
    [DataMember(Name="maxTitleResolution", EmitDefaultValue=false)]
    public double? MaxTitleResolution { get; set; }

    
    /// <summary>
    /// Gets or Sets Rotate
    /// </summary>
    [DataMember(Name="rotate", EmitDefaultValue=false)]
    public double? Rotate { get; set; }

    
    /// <summary>
    /// Gets or Sets InnerTextProperty
    /// </summary>
    [DataMember(Name="innerTextProperty", EmitDefaultValue=false)]
    public double? InnerTextProperty { get; set; }

    
    /// <summary>
    /// Gets or Sets InnerTextSize
    /// </summary>
    [DataMember(Name="innerTextSize", EmitDefaultValue=false)]
    public double? InnerTextSize { get; set; }

    
    /// <summary>
    /// Gets or Sets RotateProperty
    /// </summary>
    [DataMember(Name="rotateProperty", EmitDefaultValue=false)]
    public string RotateProperty { get; set; }

    

    /// <summary>
    /// Get the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()  {
      var sb = new StringBuilder();
      sb.Append("class FeatureTypeStyle {\n");
      
      sb.Append("  NameLabel: ").Append(NameLabel).Append("\n");
      
      sb.Append("  FillColor: ").Append(FillColor).Append("\n");
      
      sb.Append("  StrokeColor: ").Append(StrokeColor).Append("\n");
      
      sb.Append("  SelectedFillColor: ").Append(SelectedFillColor).Append("\n");
      
      sb.Append("  SelectedStrokeColor: ").Append(SelectedStrokeColor).Append("\n");
      
      sb.Append("  SelectedStrokeWidth: ").Append(SelectedStrokeWidth).Append("\n");
      
      sb.Append("  Height: ").Append(Height).Append("\n");
      
      sb.Append("  Opacity: ").Append(Opacity).Append("\n");
      
      sb.Append("  FillOpacity: ").Append(FillOpacity).Append("\n");
      
      sb.Append("  Stroke: ").Append(Stroke).Append("\n");
      
      sb.Append("  DrawingMode: ").Append(DrawingMode).Append("\n");
      
      sb.Append("  StrokeWidth: ").Append(StrokeWidth).Append("\n");
      
      sb.Append("  IconWidth: ").Append(IconWidth).Append("\n");
      
      sb.Append("  IconHeight: ").Append(IconHeight).Append("\n");
      
      sb.Append("  IconUri: ").Append(IconUri).Append("\n");
      
      sb.Append("  CornerRadius: ").Append(CornerRadius).Append("\n");
      
      sb.Append("  MaxTitleResolution: ").Append(MaxTitleResolution).Append("\n");
      
      sb.Append("  Rotate: ").Append(Rotate).Append("\n");
      
      sb.Append("  InnerTextProperty: ").Append(InnerTextProperty).Append("\n");
      
      sb.Append("  InnerTextSize: ").Append(InnerTextSize).Append("\n");
      
      sb.Append("  RotateProperty: ").Append(RotateProperty).Append("\n");
      
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
