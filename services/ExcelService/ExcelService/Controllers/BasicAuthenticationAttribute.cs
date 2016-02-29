using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Web.Http.Filters;
using ExcelService.Properties;

namespace ExcelService.Controllers
{
    public class BasicAuthenticationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            if (!actionContext.Request.Headers.GetValues(Settings.Default.UsernameHeader).Any())
            {
                actionContext.Response = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }

            else
            {
                var username = actionContext.Request.Headers.GetValues(Settings.Default.UsernameHeader).First();

                if (string.IsNullOrWhiteSpace(username))
                {
                    actionContext.Response = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
                }
                actionContext.Request.GetOwinContext().Authentication.User = new GenericPrincipal(new ExcelServiceIdentity(username), new string[] { });
                base.OnActionExecuting(actionContext);
            }

        }
    }
}
