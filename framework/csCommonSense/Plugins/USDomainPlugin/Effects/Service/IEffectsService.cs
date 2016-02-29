using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace csUSDomainPlugin.Effects.Service
{
    public interface IEffectsService
    {
        JArray GetQuantities();
        JArray GetChemicals();
        JArray GetModels();
        JArray GetModelProperties(string modelName);
    }
}
