using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace csUSDomainPlugin.Effects.Service
{
    [Export(typeof(IEffectsService))]
    public class EffectsService : IEffectsService
    {
        public JArray GetQuantities()
        {
            var localDefinition = new FileInfo(@"Plugins\Effects\Data\AllQuantitiesWithUnitDefinitions.json");
            if (!localDefinition.Exists) return new JArray();
            var strDefinition = "";

            using (var reader = localDefinition.OpenText())
            {
                strDefinition = localDefinition.OpenText().ReadToEnd();
                reader.Close();
            }

            var quantities = JObject.Parse(strDefinition);
            if (quantities == null) return new JArray();
            return quantities["QuantitiesWithUnits"] as JArray;
        }

        public JArray GetChemicals()
        {
            var localDefinition = new FileInfo(@"Plugins\Effects\Data\chemicals.json");
            if (!localDefinition.Exists) return new JArray();
            var strDefinition = "";

            using (var reader = localDefinition.OpenText())
            {
                strDefinition = localDefinition.OpenText().ReadToEnd();
                reader.Close();
            }

            var chemicals = JObject.Parse(strDefinition);
            if (chemicals == null) return new JArray();
            return chemicals["ChemicalNames"] as JArray;
        }

        public JArray GetModels()
        {
            var localDefinition = new FileInfo(@"Plugins\Effects\Data\effects_us_request_models_reply.json");
            if (!localDefinition.Exists) return new JArray();
            var strDefinition = "";

            using (var reader = localDefinition.OpenText())
            {
                strDefinition = localDefinition.OpenText().ReadToEnd();
                reader.Close();
            }

            var models = JObject.Parse(strDefinition);
            if (models == null) return new JArray();
            return models["Models"] as JArray;
        }

        public JArray GetModelProperties(string modelName)
        {
            var strModelDataFile = "";
            switch (modelName)
            {
                case "Neutral Gas Dispersion: Explosive mass":
                    strModelDataFile = "Dispersion explosive.json";
                    break;
                case "BLEVE (Static or Dynamic model)":
                    strModelDataFile = "Bleve.json";
                    break;
                case "Pool fire":
                    strModelDataFile = "Poolfire.json";
                    break;
                case "Jet Fire (Chamberlain model)":
                    strModelDataFile = "Jetfire.json";
                    break;
                case "Dense Gas Dispersion: Toxic dose":
                    strModelDataFile = "Dispersion Toxic.json";
                    break;
                default:
                    return new JArray();
                    break;
            }

            var localDefinition = new FileInfo(Path.Combine(@"Plugins\Effects\Data\", strModelDataFile));
            if (!localDefinition.Exists) return new JArray();
            var strDefinition = "";

            using (var reader = localDefinition.OpenText())
            {
                strDefinition = localDefinition.OpenText().ReadToEnd();
                reader.Close();
            }

            var modelParameters = JObject.Parse(strDefinition);
            if (modelParameters == null) return new JArray();
            return modelParameters["Parameters"] as JArray;
        }
    }
}
