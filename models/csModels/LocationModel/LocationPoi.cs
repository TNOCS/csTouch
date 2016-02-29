using Caliburn.Micro;
using csDataServerPlugin;

namespace csModels.LocationModel
{
    public class LocationPoi : ModelPoiBase
    {        

        public override void Start()
        {
            base.Start(); 
            ViewModel = new LocationViewModel {
                DisplayName = Model.Id,                 
                Model = Model,
                PoI = Poi
            };
        }

    }
}
