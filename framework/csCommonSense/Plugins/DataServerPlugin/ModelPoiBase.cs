using DataServer;
using System;

namespace csDataServerPlugin
{

    public class ModelLabelEventArgs : EventArgs
    {

    }

    public class ModelPoiBase : IModelPoiInstance
    {
        protected bool IsStopping = false;

        public PoI Poi { get; set; }

        public IModel Model { get; set; }

        public IEditableScreen ViewModel { get; set; }

        public void RegisterModelLabel(string name)
        {

        }

        public virtual void Start() {
            IsStopping = false;
        }

        public virtual void Stop() {
            IsStopping = true;
        }

    }
}