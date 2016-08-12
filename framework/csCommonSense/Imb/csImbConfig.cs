using csShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csCommon.Imb
{
    public class csImbConfig
    {
        public const string CfgNameImbHost = "IMB.Host";
        public const string CfgNameImbPort = "IMB.Port";
        public const string CfgNameImbFederation = "IMB.Federation";
        public const string CfgNameImbIsEnabled = "IMB.Enabled";
        public const string CfgNameImbType = "IMB.Type";
        public const string CfgNameImbHistory = "ImbHistory";

        public static Dictionary<string, object> DefaultValues = new Dictionary<string, object>()
        {
           { CfgNameImbHost, "localhost" },
           { CfgNameImbPort, (int)4000 } ,
           { CfgNameImbFederation, "cs" },
           { CfgNameImbIsEnabled, true },
           { CfgNameImbType,  "TouchTable" },
           { CfgNameImbHistory,  "" },
        };


        public csImbConfig(Config pConfig)
        {
            Cfg = pConfig;
        }

        public Config Cfg { get; private set; }

        public bool ImbIsEnabled
        {
            get
            {
                return Cfg.GetBool(csImbConfig.CfgNameImbIsEnabled, (bool)csImbConfig.DefaultValues[CfgNameImbIsEnabled]);
            }
        }

        public string ImbHostName
        {
            get
            {
                return Cfg.Get(csImbConfig.CfgNameImbHost, (string)csImbConfig.DefaultValues[CfgNameImbHost]);
            }
        }

        public int ImbPortNumber
        {
            get
            {
                return Cfg.GetInt(csImbConfig.CfgNameImbPort, (int)csImbConfig.DefaultValues[CfgNameImbPort]);
            }
        }

        
        public string ImbFederation
        {
            get
            {
                return Cfg.Get(csImbConfig.CfgNameImbFederation, (string)csImbConfig.DefaultValues[CfgNameImbFederation]);
            }
        }

        public string ImbType
        {
            get
            {
                return Cfg.Get(csImbConfig.CfgNameImbType, (string)csImbConfig.DefaultValues[CfgNameImbType]);
            }
        }

        public string ImbHistory
        {
            get
            {
                return Cfg.Get(csImbConfig.CfgNameImbHistory, (string)csImbConfig.DefaultValues[CfgNameImbHistory]);
            }
        }

        public override string ToString()
        {
            return String.Format("IMB{0}{1}@{2} (3)}", ImbIsEnabled ? "" : " IS NOT ENABLED: ", ImbHostName, ImbPortNumber, ImbFederation);
        }
    }
}
