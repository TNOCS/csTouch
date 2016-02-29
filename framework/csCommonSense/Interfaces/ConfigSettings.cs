using System;

namespace csShared.Interfaces
{
  [AttributeUsage(AttributeTargets.Property|AttributeTargets.Struct,AllowMultiple=false,Inherited=false)]
  public class ConfigSettings : Attribute
  {

    public ConfigTypes Type { get; set; }
    public object DefaultValue { get; set; }
    public bool Required { get; set; }

    public enum ConfigTypes
    {
      Tag,
      String,
      Double
    }

    public ConfigSettings(ConfigTypes type, object defaultValue, bool required)
    {
      Type = type;
      DefaultValue = defaultValue;
      Required = required;
    }
  }
}