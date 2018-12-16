using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Routing;
using MediaServer.DAL;
using MediaServer.Models;

namespace MediaServer.ActionFilters
{
    // NOT USED A.T.M. - see Modules/BasicAuthModule
    public class BasicAuthenticationAttribute : ActionFilterAttribute
    {
        private readonly UserContext _usersContext = new UserContext();
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            // if we don't have header  - not authorized
            if (actionContext.Request.Headers.Authorization == null)
            {
                actionContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
            else // parse the value
            {
                string authToken = actionContext.Request.Headers.Authorization.Parameter;
                string decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(authToken));
                string username = decodedToken.Substring(0, decodedToken.IndexOf(":"));
                string password = decodedToken.Substring(decodedToken.IndexOf(":") + 1);

                // transform password-> passwordHash with a separate class
                byte[] passwordHash = new byte[32];
                // get the user entity from DAL
                CUser user = _usersContext.GetByName(username);

                if (user != null)
                {
                    HttpContext.Current.User = new GenericPrincipal(new GenericIdentity(username), new string[] { });
                    base.OnActionExecuting(actionContext);
                }
                else
                {
                    actionContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }
            }

           
        }
    }
}