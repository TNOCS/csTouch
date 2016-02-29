using DataServer;

namespace csDataServerPlugin
{
    public interface IdsChildLayer
    {
        IServiceLayer parent { get; set; }

        void AddPoi(PoI poi);
        void UpdatePoi(PoI poi);
        void RemovePoi(PoI poi);
    }
}