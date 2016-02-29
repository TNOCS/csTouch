using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Caliburn.Micro;
using csShared.Utils;

namespace csShared.Geo
{

  [Serializable]
  public class StoredLayerCollection : List<StoredLayer>
  {
    public string SerializeObject()
    {
      try
      {
        XmlSerializer serializer = new XmlSerializer(this.GetType());
        using (StringWriter writer = new StringWriter())
        {
          serializer.Serialize(writer, this);
          return writer.ToString();
        }
      }
      catch (Exception e)
      {

        Logger.Log("Stored Layers", "Error serializing", e.Message, Logger.Level.Error);
        return null;
      }
      
    }

    public StoredLayerCollection DeSerializeObject(string input)
    {
      try
      {
        XmlSerializer serializer = new XmlSerializer(this.GetType());
        StringReader writer = new StringReader(input);
        StoredLayerCollection slc = serializer.Deserialize(writer) as StoredLayerCollection;
        return slc;
      }
      catch(Exception e)
      {
        Logger.Log("Stored Layers","Error deserializing",e.Message,Logger.Level.Error);
        return null;
      }
      
    }

    public void Load()
    {
      try
      {
        string st = AppStateSettings.Instance.Config.Get("storedlayers", "");
        if (st != "")
        {
          Clear();
          foreach (var a in this.DeSerializeObject(st))
          {
            Add(a);
          }
        }
      }
      catch (Exception e)
      {
          // FIXME TODO Deal with exception!
      }
    }

    public void Save()
    {
      try
      {
        var s = this.SerializeObject();
        AppStateSettings.Instance.Config.SetLocalConfig("storedlayers", "<![CDATA[" + s + "]]>", true);
      }
      catch (Exception e)
      {
          // FIXME TODO Deal with exception!
      }
    }
  }
	
  [Serializable]
  public class StoredLayer : PropertyChangedBase  
  {

    private string _id;

    public string Id
    {
      get { return _id; }
      set { _id = value; }
    }
    

    private string _title;

    public string Title
    {
      get { return _title; }
      set { _title = value; NotifyOfPropertyChange(()=>Title); }
    }

    private string _type;

    public string Type
    {
      get { return _type; }
      set { _type = value; NotifyOfPropertyChange(()=>Type); }
    }

    private string _image;

    public string Image
    {
      get { return _image; }
      set { _image = value; NotifyOfPropertyChange(()=>Image); }
    }

    private string _source;

    public string Source
    {
      get { return _source; }
      set { _source = value; NotifyOfPropertyChange(()=>Source); }
    }

    private string _path;

    public string Path
    {
      get { return _path; }
      set { _path = value; NotifyOfPropertyChange(()=>Path); }
    }

    private List<Attribute> _attributes;

    public List<Attribute> Attributes
    {
      get { return _attributes; }
      set { _attributes = value; NotifyOfPropertyChange(()=>Attributes); }
    }
        
  }

  [Serializable]
  public class Attribute
  {
    private string _key;

    public string Key
    {
      get { return _key; }
      set { _key = value; }
    }

    private string _value;

    public string Value
    {
      get { return _value; }
      set { _value = value; }
    }
    
    
  }
}
