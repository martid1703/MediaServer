using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using DTO;

namespace CasheServer
{
    class HelperMethods
    {
        public static AuthenticationHeaderValue UserCredentials(CUserInfo userInfo)
        {
            // set Authorization header
            string credentials = $"{userInfo.Name}:{userInfo.Password}";
            byte[] namePassBytes = Encoding.ASCII.GetBytes(credentials);
            string namePassEncoded = Convert.ToBase64String(namePassBytes);
            return new AuthenticationHeaderValue("Basic", namePassEncoded);
        }

        // MediaServer connection setup (access for FileController, UserController, PlaylistController)
        public static void SetupHttpClient(HttpClient httpClient)
        {
            httpClient.BaseAddress = new Uri("http://localhost:50352/"); //debug from VS
            //httpClient.BaseAddress = new Uri("http://localhost:86/"); // test on IIS
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

    }
}
