using System;

namespace csShared.Interfaces
{
  public interface IMapToolPlugin
  {
    Type Control { get; }
    string Name { get; }
    void Init();
    void Start();
    void Stop();
    bool Enabled { get; set; }
      bool IsOnline { get; }
  }
}