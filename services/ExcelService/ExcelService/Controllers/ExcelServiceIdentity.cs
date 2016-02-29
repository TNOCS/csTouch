using System.Security.Principal;

namespace ExcelService.Controllers
{
    public class ExcelServiceIdentity : IIdentity
    {
        public ExcelServiceIdentity(string username)
        {
            Name = username;
        }

        public string Name               { get; private set; }
        public string AuthenticationType { get { return "Basic"; } }
        public bool   IsAuthenticated    { get { return true; } }
    }
}
