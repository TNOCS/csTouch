namespace csShared.Interfaces
{
  public interface IModule
  {
    string Name { get; }    
    void InitializeApp();
    void StartApp();
  }
}
