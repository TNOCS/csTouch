using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Newtonsoft.Json.Linq;

namespace csUSDomainPlugin.Effects.ViewModels
{
    public class ModelSelectionViewModel : PropertyChangedBase
    {
        private JObject _data;

        public JObject Data
        {
            set
            {
                _data = value;
                NotifyOfPropertyChange(()=>Name);
                NotifyOfPropertyChange(()=>Description);
                NotifyOfPropertyChange(()=>Version);
            }
        }

        public string Name
        {
            get { return (string)_data["Name"]; }
        }
        
        public string Description
        {
            get { return (string)_data["Description"]; }
        }
        
        public string Version
        {
            get { return (string)_data["Version"]; }
        }
    }
}
