using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using ExcelService.Properties;

namespace ExcelService
{
    [RunInstaller(true)]
    public class ExcelServiceInstaller : Installer
    {
        private ServiceProcessInstaller _process;
        private ServiceInstaller _service;

        public ExcelServiceInstaller()
        {
            _process = new ServiceProcessInstaller {Account = ServiceAccount.LocalSystem};
            _service = new ServiceInstaller {ServiceName = Settings.Default.ServiceName};

            Installers.Add(_process);
            Installers.Add(_service);
        }
    }
}
