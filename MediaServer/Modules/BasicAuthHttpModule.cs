using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web;
using MediaServer.Controllers;
using MediaServer.DAL;
using MediaServer.Models;

namespace MediaServer.Modules
{
    public class BasicAuthHttpModule : IHttpModule
    {
        private const string Realm = "Media Server";

        public void Init(HttpApplication context)
        {
            // Register event handlers
            context.AuthenticateRequest += OnApplicationAuthenticateRequest;
            context.EndRequest += OnApplicationEndRequest;
        }

        private static void SetPrincipal(IPrincipal principal)
        {
            Thread.CurrentPrincipal = principal;
            if (HttpContext.Current != null)
            {
                HttpContext.Current.User = principal;
            }
        }


        private static void AuthenticateUser(string credentials)
        {
            UserContext usersContext = new UserContext();

            try
            {
                Tuple<string, string> nameAndPassword = HelperMethods.NamePasswordFromAuthHeader(credentials);

                CUser user = usersContext.GetByName(nameAndPassword.Item1);
                if (user.Name != null)
                {
                    if (user.IsAuthentic(nameAndPassword.Item2))
                    {
                        var identity = new GenericIdentity(nameAndPassword.Item1);
                        // todo: assign a list of roles for the authenticated user, replacing 'null' parameter
                        SetPrincipal(new GenericPrincipal(identity, null));
                    }
                    else
                    {
                        // Invalid password.
                        HttpContext.Current.Response.StatusCode = 401;
                    }
                }
                else
                {
                    // Invalid username.
                    HttpContext.Current.Response.StatusCode = 401;
                }
            }
            catch (FormatException)
            {
                // Credentials were not formatted correctly.
                HttpContext.Current.Response.StatusCode = 401;
            }
        }

        private static void OnApplicationAuthenticateRequest(object sender, EventArgs e)
        {
            var request = HttpContext.Current.Request;
            var authHeader = request.Headers["Authorization"];
            if (authHeader != null)
            {
                var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);

                // RFC 2617 sec 1.2, "scheme" name is case-insensitive
                if (authHeaderVal.Scheme.Equals("basic",
                        StringComparison.OrdinalIgnoreCase) &&
                    authHeaderVal.Parameter != null)
                {
                    AuthenticateUser(authHeaderVal.Parameter);
                }
            }
        }

        // If the request was unauthorized, add the WWW-Authenticate header 
        // to the response.
        private static void OnApplicationEndRequest(object sender, EventArgs e)
        {
            var response = HttpContext.Current.Response;
            if (response.StatusCode == 401)
            {
                response.Headers.Add("WWW-Authenticate", $"Basic realm=\"{Realm}\"");
            }
        }

        public void Dispose()
        {
        }
    }
}
