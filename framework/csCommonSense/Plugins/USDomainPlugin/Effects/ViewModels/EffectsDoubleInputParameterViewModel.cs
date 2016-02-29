using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Newtonsoft.Json.Linq;

namespace csUSDomainPlugin.Effects.ViewModels
{
    public class EffectsDoubleInputParameterViewModel : EffectsInputParameterViewModel
    {
        public override JObject Data
        {
            get { return base.Data; }
            set
            {
                base.Data = value;
                NotifyOfPropertyChange(() => MinValue);
                NotifyOfPropertyChange(() => MaxValue);
                NotifyOfPropertyChange(() => MaxInclusive);
                NotifyOfPropertyChange(() => MinInclusive);
                NotifyOfPropertyChange(() => Value);
            }
        }

        public double MinValue { get { return (double) _data["Data"]["Min"]; } }
        public double MaxValue { get { return (double)_data["Data"]["Max"]; } }
        public bool MaxInclusive { get { return (bool) _data["Data"]["MaxInclusive"]; } }
        public bool MinInclusive { get { return (bool)_data["Data"]["MinInclusive"]; } }

        public double Value
        {
            get { return (double) _data["Data"]["Value"]; }
            set
            {
                if ((double) Data["Data"]["Value"] == value) return;
                Data["Data"]["Value"] = value;
                NotifyOfPropertyChange(()=>Value);
            }
        }
    }
}
