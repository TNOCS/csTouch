using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csCommon.Types.DataServer
{
    public class JoinSharedServiceEventArgs : EventArgs
    { 

        public JoinSharedServiceEventArgs(string pSharedServiceName, Guid pSharedServiceGuid, int pHostingImbClientId)
        {
            SharedServiceName = pSharedServiceName;
            SharedServiceGuid = pSharedServiceGuid;
            HostingImbClientId = pHostingImbClientId;

            JoinSharedService = null;
        }

        public string SharedServiceName { get; private set; }
        public Guid SharedServiceGuid { get; private set; }
        public int HostingImbClientId { get; private set; }
        public bool? JoinSharedService { get; set; }
    }
}
