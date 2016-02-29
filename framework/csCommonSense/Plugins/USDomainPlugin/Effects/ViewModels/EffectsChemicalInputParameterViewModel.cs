using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Newtonsoft.Json.Linq;

namespace csUSDomainPlugin.Effects.ViewModels
{
    public class EffectsChemicalInputParameterViewModel : EffectsInputParameterViewModel
    {
        private JArray _chemicalData;
        private IEnumerable<ChemicalSelectionViewModel> _chemicals;

        public IEnumerable<ChemicalSelectionViewModel> Chemicals
        {
            get { return _chemicals; }
            set
            {
                if (Equals(value, _chemicals)) return;
                _chemicals = value;
                NotifyOfPropertyChange(() => Chemicals);
            }
        }

        public override JObject Data
        {
            get { return base.Data; }
            set
            {
                base.Data = value;
                NotifyOfPropertyChange(()=>SelectedChemical);
            }
        }

        public ChemicalSelectionViewModel SelectedChemical
        {
            get
            {
                var selectedVm = _chemicals.FirstOrDefault(cVm=>cVm.ChemicalId == (Guid)_data["Data"]["ChemicalID"]);
                return selectedVm;
            }
            set
            {
                if (value == null)
                {
                    _data["Data"]["ChemicalId"] = "";
                    NotifyOfPropertyChange(()=>SelectedChemical);
                    return;
                }

                if (value.ChemicalId == (Guid)_data["Data"]["ChemicalID"]) return;
                _data["Data"]["ChemicalID"] = value.ChemicalId.ToString("D").ToUpper();
                _data["Data"]["ChemicalName"] = value.ChemicalName;
                NotifyOfPropertyChange(() => SelectedChemical);
            }
        }

        public JArray ChemicalData
        {
            set { Chemicals = value.OfType<JObject>().Select(cd => new ChemicalSelectionViewModel() {Data = cd}); }
        }

        public EffectsChemicalInputParameterViewModel()
        {
            
        }

    }
}
