using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using DTO;

namespace MediaClient.WebApi
{
    class HelperMethods
    {
        /// <summary>
        /// set base address of the server etc.
        /// </summary>
        /// <param name="client"></param>
        public static void SetupHttpClient(out HttpClient client)
        {
            client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:50352/"); //debug from VS
            //client.BaseAddress = new Uri("http://localhost:86/"); // test on IIS

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public static AuthenticationHeaderValue UserCredentials(CUserInfo userInfo)
        {
            // set Authorization header
            string credentials = $"{userInfo.Name}:{userInfo.Password}";
            byte[] namePassBytes = Encoding.ASCII.GetBytes(credentials);
            string namePassEncoded = Convert.ToBase64String(namePassBytes);
            return new AuthenticationHeaderValue("Basic", namePassEncoded);
        }

        public static void PrintFilesInDirectory(DirectoryInfo directory)
        {
            FileInfo[] files = directory.GetFiles();
                for (int i = 0; i < files.Length; i++)
                {
                    Console.WriteLine($"{i}) {files[i].Name}, Size: {files[i].Length/1024f/1024f:F2} MB");
                }
                
        }

    }
}
