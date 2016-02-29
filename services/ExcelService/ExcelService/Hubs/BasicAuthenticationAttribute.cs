using System;
using System.Security.Principal;
using ExcelService.Controllers;
using ExcelService.Properties;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Owin;

namespace ExcelService.Hubs
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class BasicAuthenticationAttribute : AuthorizeAttribute
    {
        public override bool AuthorizeHubConnection(HubDescriptor hubDescriptor, IRequest request)
        {
            var username = request.Headers[Settings.Default.UsernameHeader]??request.QueryString[Settings.Default.UsernameHeader];
            request.Environment["server.User"] = new GenericPrincipal(new ExcelServiceIdentity(username), new string[] { });
            //request.GetOwinContext().Authentication.User = new GenericPrincipal(new ExcelServiceIdentity(username), new string[] { });
            if (!string.IsNullOrWhiteSpace(username))
            {
                return true;
            }

            return false;
        }

        public override bool AuthorizeHubMethodInvocation(IHubIncomingInvokerContext hubIncomingInvokerContext, bool appliesToMethod)
        {
            var connectionId = hubIncomingInvokerContext.Hub.Context.ConnectionId;
            var environment = hubIncomingInvokerContext.Hub.Context.Request.Environment;
            var principal = hubIncomingInvokerContext.Hub.Context.Request.Environment["server.User"] as IPrincipal;

            if (principal == null || !principal.Identity.IsAuthenticated) return false;
            hubIncomingInvokerContext.Hub.Context = new HubCallerContext(new ServerRequest(environment), connectionId);
            return true;
        }

        protected override bool UserAuthorized(IPrincipal user)
        {
            return user.Identity.Name != null;
        }
    }
}
