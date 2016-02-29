using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using csShared.Interfaces;
using csUSDomainPlugin;
using csUSDomainPlugin.Effects.ViewModels;
using Caliburn.Micro;
using IMB3;
using IMB3.ByteBuffers;
using csUSDomainPlugin.Effects.Views;
using Newtonsoft.Json.Linq;

namespace csUSDomainPlugin.Effects
{
    [Export(typeof(IMapToolPlugin))]
    [Export(typeof(IEffectsMapToolPlugin))]
    public class EffectsMapToolPlugin : IMapToolPlugin, IEffectsMapToolPlugin
    {
        private USDomainPlugin _usDomainPlugin;

        protected List<EffectsModelSettingsViewModel> _EffectsSettingsViewModels = new List<EffectsModelSettingsViewModel>();

        public void RegisterEffectsMapTool(EffectsModelSettingsViewModel effectsSettingsViewModel)
        {
            _EffectsSettingsViewModels.Add(effectsSettingsViewModel);

            if (_effectsEvent == null)
            {
                ImbSubscribe();
            }

            if (_models == null)
            {
                GetModels();
            }
            else
            {
                effectsSettingsViewModel.SetModelData(_models);
            }

            if (_chemicalNames == null)
            {
                GetChemicalNames();
            }
            else
            {
                effectsSettingsViewModel.SetChemicalsData(_chemicalNames);
            }
        }

        public void UnRegisterEffectsMapTool(EffectsModelSettingsViewModel effectsSettingsViewModel)
        {
            if (_EffectsSettingsViewModels.Contains(effectsSettingsViewModel))
            {
                _EffectsSettingsViewModels.Remove(effectsSettingsViewModel);
            }
        }

        public Type Control
        {
            get { return typeof (EffectsMapToolView); }
        }

        public string Name
        {
            get { return "EffectsMapTool"; }
        }

        public USDomainPlugin UsDomainPlugin
        {
            get { return _usDomainPlugin; }
            set
            {
                _usDomainPlugin = value;

                
            }
        }


        public bool Enabled { get; set; }

        public bool IsOnline
        {
            get { return false; }
        }

        public void Init()
        {
        }

        public void Start()
        {
            
        }

        public void Stop()
        {
            
        }



        private TEventEntry _effectsEvent;
        private JArray _models;
        private JArray _chemicalNames;
        
        public void ImbUnsubscribe()
        {
            if (_effectsEvent != null)
            {
                _effectsEvent.OnNormalEvent -= Effects_OnNormalEvent;
            }
            _effectsEvent = null;
        }

        private void Effects_OnNormalEvent(TEventEntry aevent, TByteBuffer apayload)
        {
            Execute.OnUIThread(() =>
            {
                int command = -1;
                string strJson = "";

                apayload.Read(out command);
                apayload.Read(out strJson);

                //                Debug.WriteLine("NormalEvent received from IMB: [id:{0}, command:{1}]:\n{2}", aevent.ID, command, strJson);
                Debug.WriteLine("NormalEvent received from IMB: [id:{0}, command:{1}]:\n{2} bytes", this.GetHashCode(), command, strJson.Length);
                if (!string.IsNullOrWhiteSpace(strJson))
                {
                    //todo: fixed in new effects binary, this replace WILL produce problems and MUST be removed:
                    strJson = strJson.Replace("NAN", "NaN");
                    var body = JObject.Parse(strJson);
                    Guid id;

                    switch (command)
                    {
                        case 1102:
                            Debug.WriteLine("ImbEvent hash: {0}", this.GetHashCode());
                            SetModelData(body["Models"] as JArray);
                            break;
                        case 1104:
                            var reply = body["ParametersReply"];
                            id = (Guid)reply["ID"];
                            SetModelPropertyData(id, reply["Parameters"] as JArray);
                            break;
                        case 1106:
                            var result = body["Result"];
                            id = (Guid)result["ID"];
                            SetCalculatedData(id, result["Parameters"] as JArray);
                            break;
                        case 1108:
                            //SetUnitsData
                            break;
                        case 1110:
                            SetChemicalsData(body["ChemicalNames"] as JArray);
                            break;
                    }
                }
            });
        }

        private void SetChemicalsData(JArray data)
        {
            _chemicalNames = data;
            _EffectsSettingsViewModels.ForEach(vm=>vm.SetChemicalsData(data));
        }

        private void SetCalculatedData(Guid id, JArray data)
        {
            foreach (var viewModel in _EffectsSettingsViewModels.Where(vm => vm.CalculationId == id))
            {
                viewModel.SetCalculatedData(data);
            }
        }

        private void SetModelPropertyData(Guid id, JArray data)
        {
            foreach(var viewModel in _EffectsSettingsViewModels.Where(vm=>vm.CalculationId == id))
            {
                viewModel.SetModelPropertyData(data);
            }
        }

        private void SetModelData(JArray data)
        {
            _models = data;
            _EffectsSettingsViewModels.ForEach(vm=>vm.SetModelData(data));
        }

        public void ImbSubscribe()
        {
            ImbUnsubscribe();
            if (UsDomainPlugin.Connection.Connected)
            {
                if (UsDomainPlugin == null || UsDomainPlugin.CurrentSession == null || string.IsNullOrWhiteSpace(UsDomainPlugin.CurrentSession.eventName))
                {
                    Debug.WriteLine("Cannot determine current session from USDomain plugin");
                    return;
                }
                _effectsEvent = UsDomainPlugin.Connection.Subscribe(UsDomainPlugin.CurrentSession.eventName + ".Effects", false);
                _effectsEvent.OnNormalEvent += Effects_OnNormalEvent;

                Debug.WriteLine(string.Format("Subscribed to IMB event {0}: {1}", UsDomainPlugin.CurrentSession.eventName + ".Effects", _effectsEvent.ID));
            }
            else
            {
                Debug.WriteLine("USDomain plugin is not connected to IMB");
            }
        }

        private void SignalEvent(int commandId, string body = null)
        {
            if (_effectsEvent == null)
            {
                ImbSubscribe();

                if (_effectsEvent == null) return;
            }

            Debug.WriteLine(string.Format("Send IMB command {0}:\n{1}", commandId, body));

            TByteBuffer Payload = new TByteBuffer();
            Payload.Prepare((int)commandId);
            if (body != null)
            {
                Payload.Prepare(body);
            }
            Payload.PrepareApply();
            Payload.QWrite((int)commandId);
            if (body != null)
            {
                Payload.QWrite(body);
            }

            _effectsEvent.SignalEvent(TEventEntry.TEventKind.ekNormalEvent, Payload.Buffer);
        }

        public void GetChemicalNames()
        {
            SignalEvent(1109);
        }

        public void GetUnits()
        {
            // Units are supplied with parameters
            //SignalEvent(1107);
        }

        public void GetModels()
        {
            SignalEvent(1101);
        }

        public void GetModelParameters(Guid calculationId, string modelName)
        {
            var data = new JObject();
            var request = data["ParametersRequest"] = new JObject();
            request["ID"] = calculationId;
            request["ModelName"] = modelName;

            SignalEvent(1103, data.ToString());
        }

        public void Calculate(Guid calculationId, string modelName, JArray inputParameters)
        {
            //            var parameters = new JObject {{"Parameters", new JArray(InputParameters.Select(ip => ip.Data))}};
            var parameters = new JObject();
            var request = parameters["CalculationRequest"] = new JObject();
            request["ID"] = calculationId;
            request["ModelName"] = modelName;
            request["Parameters"] = inputParameters;

            var strParameters = parameters.ToString();
            SignalEvent(1105, strParameters);
        }
    }
}
