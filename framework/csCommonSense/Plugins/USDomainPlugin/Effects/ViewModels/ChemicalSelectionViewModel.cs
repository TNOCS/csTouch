using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Newtonsoft.Json.Linq;

namespace csUSDomainPlugin.Effects.ViewModels
{
    public class ChemicalSelectionViewModel : PropertyChangedBase
    {
        private JObject _data;

        public string ChemicalName
        {
            get { return (string)_data["ChemicalName"]; }
        }

        public Guid ChemicalId
        {
            get { return (Guid) _data["ChemicalId"]; }
        }

        public JObject Data
        {
            get { return _data; }
            set { _data = value; }
        }
    }
}
