using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using MediaServer.Models;
using DTO;
using System.Web.Http;

namespace MediaServer.Interfaces
{
    public interface IUserController
    {

        // Create(Register) user
        HttpResponseMessage Register(CUserInfo user);

        // Login user
        HttpResponseMessage Login();

        // Update user
        HttpResponseMessage Edit(CUserInfo user);

        // Delete user
        HttpResponseMessage Unregister(Guid userId);

        //[Authorize(Roles="Admin")]
        IEnumerable<CUserInfo> GetAllUsers();

    }
}