using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using csUSDomainPlugin;
using csUSDomainPlugin.Effects.ViewModels;
using Newtonsoft.Json.Linq;

namespace csUSDomainPlugin.Effects
{
    public interface IEffectsMapToolPlugin
    {
        USDomainPlugin UsDomainPlugin { get; set; }
        void RegisterEffectsMapTool(EffectsModelSettingsViewModel effectsSettingsViewModel);

        void GetModelParameters(Guid calculationId, string modelName);
        void Calculate(Guid calculationId, string modelName, JArray inputParameters);
    }
}
