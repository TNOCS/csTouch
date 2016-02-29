using System;
using Caliburn.Micro;
using csDataServerPlugin;
using DataServer;


namespace csModels.Flow
{
    public class FlowPoi : ModelPoiBase
    {
       
        public static void AddStop(PoI target, string name, DateTime eta, DateTime etd, PoI source)
        {
            var fs = new FlowStop
                {
                    Id = source.Name + "-" + target.Name,
                    Name = name,
                    Eta = eta,
                    Etd = etd,
                    Target = target,
                    Source = source,
                    Direction = FlowDirection.source
                    
                };
            var fs1 = new FlowStop
            {
                Id = source.Name + "-" + target.Name,
                Name = name,
                Eta = eta,
                Etd = etd,
                Target = target,
                Source = source,
                Direction = FlowDirection.target
            };
            source.Flow().AddFlowStop(fs1);
            target.InFlow().AddFlowStop(fs);        

        }

        public override void Start()
        {
            base.Start();
            ViewModel = new FlowViewModel
            {
                DisplayName = Model.Id,
                Poi = Poi
            };
        }

    }
}
