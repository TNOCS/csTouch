using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Newtonsoft.Json.Linq;

namespace csUSDomainPlugin.Effects.ViewModels
{
    public abstract class EffectsInputParameterViewModel : PropertyChangedBase
    {
        public const float LABELWIDTH = 150f;
        public const float INPUTWIDTH = 250f;

        protected JObject _data;

        protected EffectsInputParameterViewModel()
        {
        }

        public virtual JObject Data
        {
            get { return _data; }
            set
            {
                _data = value;
                NotifyOfPropertyChange(() => ModelName);
                NotifyOfPropertyChange(() => ValueUnit);
                NotifyOfPropertyChange(() => Description);
                NotifyOfPropertyChange(() => ParameterType);
                NotifyOfPropertyChange(() => Name);
            }
        }

        public string ModelName
        {
            get { return (string)_data["ModelName"]; }
        }

        public string ValueUnit
        {
            get { return (string)_data["ValueUnit"]; }
        }

        public string Description
        {
            get { return (string) _data["Description"]; }
        }

        public int ParameterType
        {
            get { return (int) _data["ParameterType"]; }
        }

        public string Name
        {
            get { return (string) _data["Name"]; }
        }

        public string LabelWidth { get { return string.Format("{0}px",LABELWIDTH); } }
        public string InputWidth { get { return string.Format("{0}px",INPUTWIDTH); } }
    }
}
