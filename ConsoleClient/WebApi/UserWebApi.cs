using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using DTO;
using System.Net.Http.Headers;
using ConsoleClient.Interfaces;

namespace ConsoleClient.WebApi
{
    class UserWebApi:IUserWebApi
    {

        static HttpClient client = new HttpClient();

        public UserWebApi()
        {
            // setup httpclient
            HelperMethods.SetupHttpClient(out client);
        }

        public async Task<HttpResponseMessage> RegisterAsync(CUserInfo user)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();
            HttpResponseMessage response = await client.PostAsJsonAsync<CUserInfo>("api/user/Register", user);
            return response;
        }

        public async Task<HttpResponseMessage> EditAsync(CUserInfo user)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(user);
            HttpResponseMessage response = await client.PostAsJsonAsync<CUserInfo>("api/user/edit", user);
            return response;
        }

        public async Task<HttpResponseMessage> UnregisterAsync(CUserInfo user)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(user);
            HttpResponseMessage response = await client.GetAsync($"api/user/Unregister?userId={user.Id}");
            return response;
        }



        public async Task<HttpResponseMessage> GetUsersAsync(CUserInfo user)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(user);
            HttpResponseMessage response = await client.GetAsync("api/user/getallusers");
            return response;
        }

        public async Task<HttpResponseMessage> UserByNameAsync(CUserInfo user, string userName)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(user);
            HttpResponseMessage response = await client.PostAsJsonAsync("api/user/userbyname", userName);
            return response;
        }

        public async Task<HttpResponseMessage> LoginAsync(CUserInfo user)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(user);
            HttpResponseMessage response = await client.GetAsync("api/user/login");
            return response;
        }
    }
}
