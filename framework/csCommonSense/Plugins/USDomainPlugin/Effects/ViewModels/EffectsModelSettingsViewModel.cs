using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using IMB3;
using IMB3.ByteBuffers;
using Newtonsoft.Json.Linq;
using csUSDomainPlugin.Effects.Service;
using csUSDomainPlugin.Effects.Util;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;

namespace csUSDomainPlugin.Effects.ViewModels
{
    public class EffectsModelSettingsViewModel : PropertyChangedBase
    {
        public Guid CalculationId { get; private set; }
        private IEffectsService _service;
        private ObservableCollection<ModelSelectionViewModel> _models = new ObservableCollection<ModelSelectionViewModel>();
        private ModelSelectionViewModel _selectedModel;
        private IEnumerable<EffectsInputParameterViewModel> _inputParameters;
        private IEnumerable<EffectsOutputParameterViewModel> _outputParameters;
        private IEnumerable<JObject> _chemicals;

        public GraphicCollection Contours
        {
            get { return _contours; }
            set
            {
                if (Equals(value, _contours)) return;
                _contours = value;
                NotifyOfPropertyChange(() => Contours);
            }
        }

        public IEnumerable<JObject> Chemicals
        {
            get { return _chemicals; }
            set
            {
                if (Equals(value, _chemicals)) return;
                _chemicals = value;
                NotifyOfPropertyChange(() => Chemicals);
                NotifyOfPropertyChange(()=>CanSelectModels);
            }
        }

        public IEnumerable<EffectsInputParameterViewModel> InputParameters
        {
            get { return _inputParameters; }
            set
            {
                if (Equals(value, _inputParameters)) return;
                _inputParameters = value;
                NotifyOfPropertyChange(() => InputParameters);
            }
        }
        
        public IEnumerable<EffectsOutputParameterViewModel> OutputParameters
        {
            get { return _outputParameters; }
            set
            {
                if (Equals(value, _outputParameters)) return;
                _outputParameters = value;
                NotifyOfPropertyChange(() => OutputParameters);
            }
        }

        public bool CanSelectModels
        {
            get { 
                return _models != null && _models.Any() && _chemicals != null && _chemicals.Any();
            }
        }

        public ObservableCollection<ModelSelectionViewModel> Models
        {
            get { return _models; }
            private set
            {
                if (Equals(value, _models)) return;
                _models = value;
                NotifyOfPropertyChange(() => Models);
                NotifyOfPropertyChange(()=>CanSelectModels);
            }
        }

        public ModelSelectionViewModel SelectedModel
        {
            get { return _selectedModel; }
            set
            {
                if (Equals(value, _selectedModel)) return;
                _selectedModel = value;
                NotifyOfPropertyChange(() => SelectedModel);

                if (value != null)
                {
                    _effectsPlugin.GetModelParameters(CalculationId, _selectedModel.Name);
                }
                else
                {
                    SetModelPropertyData(new JArray());
                }
            }
        }

        private IEffectsMapToolPlugin _effectsPlugin;
        private GraphicCollection _contours;

        public void Initialize()
        {
            CalculationId = Guid.NewGuid();
            _effectsPlugin = IoC.Get<IEffectsMapToolPlugin>();

            _effectsPlugin.RegisterEffectsMapTool(this);

            Contours = new GraphicCollection();
        }

        public void SetModelData(JArray modelData)
        {
            Models.Clear();
            foreach (
                var model in modelData.OfType<JObject>().Select(model => new ModelSelectionViewModel() {Data = model}))
            {
                Models.Add(model);
            }
        }

        public void SetModelPropertyData(JArray modelPropertyData)
        {
            InputParameters = modelPropertyData.Cast<JObject>().Where(inputParameter=>!(bool)inputParameter["IsResult"]).Select<JObject,EffectsInputParameterViewModel>(inputParameter =>
            {
                switch ((int) inputParameter["ParameterType"])
                {
                    case 1:
                        // Integer
                        // todo: if both min and max are specified create slider input
                        if (inputParameter["Data"]["Min"] != null && inputParameter["Data"]["Max"] != null && !double.IsNaN((double)inputParameter["Data"]["Min"]) && !double.IsNaN((double)inputParameter["Data"]["Max"]))
                        {
                            return new EffectsRangeInputParameterViewModel() {Data = inputParameter};
                        }
                        else
                        {
                            return new EffectsDoubleInputParameterViewModel() {Data = inputParameter};
                        }
                        break;
                    case 6:
                        // Chemical
                        return new EffectsChemicalInputParameterViewModel(){Data = inputParameter, ChemicalData = new JArray(Chemicals)};
                    default:
                        return null;
                }
            }).Where(vm=>vm != null);
        }

        public void SetChemicalsData(JArray chemicalsData)
        {
            Chemicals = chemicalsData.Select(chemical => new JObject { { "ChemicalName", (string)chemical["ChemicalName"] }, { "ChemicalId", (string)chemical["ChemicalID"] } });
        }

        public void SetCalculatedData(JArray calculatedData)
        {
            //.Where(p=>!((new int[]{5,3}).Contains((int)p["ParameterType"])))
            OutputParameters = calculatedData.OfType<JObject>().Where(ShowOutputParameter).Select(p => new EffectsOutputParameterViewModel() {Data = p});

            Contours = new GraphicCollection(calculatedData.OfType<JObject>().Where(IsContourParameter).Select(p=>((string)p["Data"]["Value"]).ToGraphic(new SpatialReference(28992), new SpatialReference(28992))));
        }

        public static bool IsContourParameter(JObject data)
        {
            var parameterType = (int)data["ParameterType"];
            if (parameterType != 3) return false;
            var strWkt = (string) data["Data"]["Value"];
            if (string.IsNullOrWhiteSpace(strWkt)) return false;

            return true;
        }

        private static bool ShowOutputParameter(JObject data)
        {
            // 3 = Contour, 5 = Profile, 8 = Grid, 4 = ProfileGrid
            var excludeParameterTypes = new[] {3, 4, 5, 8};

            var parameterType = (int) data["ParameterType"];
            if (excludeParameterTypes.Contains(parameterType)) return false;

            switch (parameterType)
            {
                case 6:
                    if (string.IsNullOrWhiteSpace((string) data["Data"]["ChemicalName"])) return false;
                    break;
                case 1:
                    string strValue = (string)data["Data"]["Value"];
                    if (string.IsNullOrWhiteSpace(strValue)) return false;

                    double dValue = double.NaN;
                    if (!double.TryParse(strValue, out dValue)) return false;
                    if (double.IsNaN(dValue) || double.IsInfinity(dValue)) return false;
                    break;
                default:
                    if (string.IsNullOrWhiteSpace((string) data["Data"]["Value"])) return false;
                    break;
            }

            return true;
        }

        public void Calculate()
        {
            _effectsPlugin.Calculate(CalculationId, SelectedModel.Name, new JArray(InputParameters.Select(ip => ip.Data) ));
        }
    }
}
