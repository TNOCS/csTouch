using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csCommon
{
    public class ConfigCmdLineOptions
    {
        [Option('c', "ConfigurationFile", Required = false, HelpText = "Excel configuration file.")]
        public string ConfigurationFile { get; set; }

        [Option('n', "ConfigurationName", DefaultValue = "", HelpText = "Configurationname in excel sheet")]
        public string ConfigurationName { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
