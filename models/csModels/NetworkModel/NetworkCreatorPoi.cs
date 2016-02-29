using Caliburn.Micro;
using csDataServerPlugin;

namespace NetworkModel
{
    public class NetworkCreatorPoi : ModelPoiBase
    {
        public BindableCollection<Network> Networks { get; set; }

        public override void Start()
        {
            base.Start(); 
            ViewModel = new NetworkCreatorViewModel {
                DisplayName = Model.Id, 
                Networks = Networks, 
                Model = Model,
                PoI = Poi
            };
        }

    }
}
