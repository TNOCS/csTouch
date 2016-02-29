namespace csShared.Interfaces
{
  public interface IConfigClass
  {
    string ConfigFile { get; set; }
    void Save();
    void Load();
  }
}