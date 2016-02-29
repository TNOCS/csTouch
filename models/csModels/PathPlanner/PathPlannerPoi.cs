using csDataServerPlugin;

namespace csModels.PathPlanner
{
    public class PathPlannerPoi : ModelPoiBase
    {
        public VisitedLocations VisitedLocations { get; set; }

        

        public PathPlannerPoi()
        {
            VisitedLocations = new VisitedLocations();    
        }

        public override void Start()
        {
            base.Start();
            var pathPlannerViewModel = new PathPlannerViewModel
            {
                DisplayName      = Model.Id,
                VisitedLocations = VisitedLocations,
                Model            = Model,
                PoI              = Poi
            };
            pathPlannerViewModel.Initialize();
            ViewModel = pathPlannerViewModel;
        }

    }
}
