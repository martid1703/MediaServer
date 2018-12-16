using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using DTO;
using System.Web.Http;
using System.Threading.Tasks;

namespace CasheServer.Interfaces
{
    public interface IUserWebApi
    {

        Task<HttpResponseMessage> RegisterAsync(CUserInfo user);

        Task<HttpResponseMessage> EditAsync(CUserInfo user);

        Task<HttpResponseMessage> UnregisterAsync(CUserInfo user);

        Task<HttpResponseMessage> GetUsersAsync(CUserInfo user);

        Task<HttpResponseMessage> UserByNameAsync(CUserInfo user, string userName);

        Task<HttpResponseMessage> LoginAsync(CUserInfo user);

    }
}