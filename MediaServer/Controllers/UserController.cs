using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Web;
using System.Web.Http;
using DTO;
using MediaServer.DAL;
using MediaServer.Exceptions;
using MediaServer.Interfaces;
using MediaServer.Models;

namespace MediaServer.Controllers
{
    public class UserController : ApiController, IUserController
    {
        private readonly UserContext _userContext = new UserContext();
        private readonly PlaylistContext _playlistContext = new PlaylistContext();
        private readonly FileContext _fileContext = new FileContext();

        #region IUserController
        [HttpPost]
        public HttpResponseMessage Register(CUserInfo userInfo)
        {
            HttpResponseMessage response;
            if (userInfo == null)
            {
                response = Request.CreateResponse(HttpStatusCode.BadRequest, "User has no data to register!");
                return response;
            }

            try
            {
                // check if such user already exists in context
                CUser checkCUser = _userContext.GetByName(userInfo.Name);

                if (checkCUser.Name != null)
                {
                    response = Request.CreateResponse(HttpStatusCode.BadRequest, "Such user already exists!");
                    return response;
                }


                // check if such email is already taken by someone else
                CUser checkEmail = _userContext.GetByName(userInfo.Email);
                if (checkEmail.Email != null)
                {
                    response = Request.CreateResponse(HttpStatusCode.BadRequest, "Such email is already taken!");
                    return response;
                }

                // in CUser CTOR new Guid and passwordHash is generated from given password
                CUser newUser = new CUser(Guid.NewGuid(), userInfo.Name, userInfo.Password, userInfo.Email);

                // Insert new user in DB
                Int32 rowsInserted = _userContext.Create(newUser);

                // Create 'default' playlist for user files
                CPlaylist defaultPlaylist = new CPlaylist(Guid.NewGuid(), "default", newUser.Guid, false);
                _playlistContext.Create(defaultPlaylist);

                // Create new folder for user files
                DirectoryInfo directoryInfo = new DirectoryInfo(System.Web.Hosting.HostingEnvironment.MapPath($@"~/App_Data"));
                if (!Directory.Exists(directoryInfo.ToString() + "\\UserFiles"))
                {
                    Directory.CreateDirectory(directoryInfo.ToString() + "\\UserFiles");
                }
                // create directory for user files
                string fullPath = System.Web.Hosting.HostingEnvironment.MapPath($@"~/App_Data/UserFiles/{newUser.Name}");
                Directory.CreateDirectory(fullPath);

                // create directory for thumbnails of the videos
                string thumbnailsPath = System.Web.Hosting.HostingEnvironment.MapPath($@"~/App_Data/UserFiles/{newUser.Name}/Thumbnails");
                Directory.CreateDirectory(thumbnailsPath);

                userInfo = newUser.ToCUserInfo();
                response = Request.CreateResponse(HttpStatusCode.OK, userInfo);

                return response;
            }
            catch (Exception e)
            {
                //todo: log
                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }
        }

        [HttpGet]
        [Authorize]
        // todo: all requests, beside Register should use userId or userName? End client should keep his Id?
        // userName, userEmail are considered to be UNIQUE
        public HttpResponseMessage Unregister(Guid userId)
        {
            try
            {


                //create full path for the uploaded file 
                CUser user = _userContext.GetById(userId);
                string userFolder = HttpContext.Current.Server.MapPath($@"~/App_Data/UserFiles/{user.Name}/");
                DirectoryInfo dInfo = new DirectoryInfo(userFolder);

                //delete PHYSICAL folder & files on user delete
                if (dInfo != null)
                {
                    Directory.Delete(userFolder,true);
                }

                // todo: delete all files on user delete - for now they are CASCADE DELETE on USER DELETE

                // delete all user playlists
                List<CPlaylist> userPlaylists = _playlistContext.GetByUserId(userId).ToList();
                foreach (CPlaylist playlist in userPlaylists)
                {
                    if (_playlistContext.DeleteById(playlist.Guid) <= 0)
                    {
                        Request.CreateResponse(HttpStatusCode.BadRequest, $"Couldn't delete playlist:{playlist.Name} for user:{userId}");
                    }
                }

                // delete user from DB
                if (_userContext.Delete(userId) <= 0)
                {
                    Request.CreateResponse(HttpStatusCode.BadRequest, $"Couldn't delete user:{userId}");
                }


                return Request.CreateResponse(HttpStatusCode.OK, $"User:{userId} successfully deleted from Media Server!");
            }
            catch (ContextException e)
            {
                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }
        }

        [HttpPost]
        [Authorize]
        public HttpResponseMessage Edit(CUserInfo userInfo)
        {
            HttpResponseMessage response;
            try
            {
                CUser user = _userContext.GetByName(userInfo.Name);
                if (user.Name == null)
                {
                    response = Request.CreateResponse(HttpStatusCode.BadRequest, "User not found");
                }
                else
                {
                    // update fields if they differ from userInfo
                    user.UpdateFields(userInfo);
                    Int32 result = _userContext.Update(user);
                    if (result > 0)
                    {
                        response = Request.CreateResponse(HttpStatusCode.OK, userInfo);
                    }
                    else
                    {
                        response = Request.CreateResponse(HttpStatusCode.BadRequest, $"couldn't update User {userInfo.Name}");
                    }
                }

                return response;
            }
            catch (ContextException e)
            {
                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }
        }

        [HttpGet]
        [Authorize]
        //[Authorize(Roles="Admin")]
        public IEnumerable<CUserInfo> GetAllUsers()
        {
            List<CUserInfo> usersInfo = new List<CUserInfo>();
            try
            {
                List<CUser> users = _userContext.GetAll().ToList();
                foreach (CUser user in users)
                {
                    CUserInfo userInfo = new CUserInfo();
                    userInfo = user.ToCUserInfo();
                    usersInfo.Add(userInfo);
                }
                return usersInfo;

            }
            catch (Exception e)// in case program crashes?
            {
                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }

        }

        [HttpPost]
        [Authorize]
        //todo: [Authorize(Roles="Admin")]
        public HttpResponseMessage UserByName(string name)
        {
            CUserInfo userInfo = new CUserInfo();
            HttpResponseMessage response;
            try
            {
                CUser user = _userContext.GetByName(name);
                userInfo = user.ToCUserInfo();
                if (user.Name != null)// if user was found in DB
                {
                    response = Request.CreateResponse(HttpStatusCode.OK, userInfo);
                }
                else
                {
                    response = Request.CreateResponse(HttpStatusCode.BadRequest);
                }
                return response;

            }
            catch (Exception e)// in case program crashes?
            {
                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }


        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage Login()
        {
            try
            {
                //getu user name
                var authHeader = Request.Headers.Authorization.Parameter;
                string name = HelperMethods.NamePasswordFromAuthHeader(authHeader).Item1;

                CUser user = _userContext.GetByName(name);
                CUserInfo userInfo = user.ToCUserInfo();

                //Request.CreateResponse(HttpStatusCode.OK, userInfo);

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new ObjectContent<CUserInfo>(userInfo, new JsonMediaTypeFormatter());
                response.ReasonPhrase = $"User \"{name}\" has successfully logged in to MediaServer!";
                return response;
            }
            catch (Exception e)
            {

                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }

        }


        #endregion

    }
}
