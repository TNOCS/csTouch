using Caliburn.Micro;

namespace csCommon
{
    public class WmsLayerLayer : PropertyChangedBase
    {
        private string _title;

        public string Title
        {
            get { return _title; }
            set { _title = value; NotifyOfPropertyChange(() => Title); }
        }

        private bool _selected;

        public bool Selected
        {
            get { return _selected; }
            set { _selected = value; NotifyOfPropertyChange(()=>Selected); }
        }


    }
}