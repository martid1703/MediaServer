using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.IO;

namespace MediaServer.Controllers
{
    public class HelperMethods
    {

        // Extract name & password from the encoded credentials ("Name:Password") of the Authorization attribute of the HttpRequestMessage
        public static Tuple<string, string> NamePasswordFromAuthHeader(string encodedCredentials)
        {
            var encoding = Encoding.GetEncoding("iso-8859-1");
            string credentials = encoding.GetString(Convert.FromBase64String(encodedCredentials));
            Int32 separator = credentials.IndexOf(':');
            string name = credentials.Substring(0, separator);
            string password = credentials.Substring(separator + 1);
            return new Tuple<string, string>(name, password);
        }

    }
}