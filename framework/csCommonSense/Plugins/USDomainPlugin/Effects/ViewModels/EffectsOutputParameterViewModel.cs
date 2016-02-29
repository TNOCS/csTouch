using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Newtonsoft.Json.Linq;

namespace csUSDomainPlugin.Effects.ViewModels
{
    public class EffectsOutputParameterViewModel : EffectsInputParameterViewModel
    {
        public object Value
        {
            get
            {
                switch ((int) _data["ParameterType"])
                {
                    case 6:
                        return (string)_data["Data"]["ChemicalName"];
                        break;
                    default:
                        return _data["Data"]["Value"].ToString();
                }
            }
        }
    }
}
