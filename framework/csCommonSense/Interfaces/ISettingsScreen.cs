namespace csShared.Interfaces
{
  public interface ISettingsScreen
  {
    string Name { get; }

    void Save();
  }
}