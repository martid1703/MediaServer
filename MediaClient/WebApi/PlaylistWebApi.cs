using DTO;
using MediaClient.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;


namespace MediaClient.WebApi
{
    public class PlaylistWebApi
    {
        static HttpClient client = new HttpClient();

        public PlaylistWebApi()
        {
            // setup httpclient
            HelperMethods.SetupHttpClient(out client);
        }
        
        #region IPlaylistWebApi

        public async Task<HttpResponseMessage> GetPlaylistsByUserId(CUserInfo userInfo)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(userInfo);
            HttpResponseMessage response = await client.GetAsync($"api/playlist/GetByUserId?userId={userInfo.Id}");
            return response;
        }

        #endregion 


    }
}
