using csDataServerPlugin;

namespace csModels.Router
{
    public class RouterPoi : ModelPoiBase
    {
        public override void Start()
        {
            base.Start();
            var routerViewModel = new RouterViewModel
            {
                DisplayName = Model.Id,
                Model       = Model,
                Poi         = Poi
            };
            routerViewModel.Initialize();
            ViewModel = routerViewModel;
        }
    }
}
