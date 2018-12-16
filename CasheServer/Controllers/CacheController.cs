using CasheServer.Models;
using DTO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using CasheServer.WebApi;
using CasheServer.Exceptions;

namespace CasheServer.Controllers
{
    public class CacheController : ApiController
    {
        // this is the cashe for list of user files, contained in the playlists, as supposedly most frequent operation
        // for the Media Server
        public static Dictionary<Guid, List<CPlaylistInfo>> UserFiles = new Dictionary<Guid, List<CPlaylistInfo>>();

        static UserWebApi userWebApi = new UserWebApi();
        static FilesWebApi filesWebApi = new FilesWebApi();
        static PlaylistWebApi playlistWebApi = new PlaylistWebApi();

        CUserInfo casheServerUser;

        HttpClient httpClientForMediaServer;

        #region CTOR
        public CacheController()
        {
            httpClientForMediaServer = new HttpClient();
            HelperMethods.SetupHttpClient(httpClientForMediaServer);

            casheServerUser = new CUserInfo { Name = "casheServerUser", Password = "123", Email = "casheServerUser@mediaserver.com" };
            // try login, if not successful - register as new user
            HttpResponseMessage response = userWebApi.LoginAsync(casheServerUser).Result;
            if (!response.IsSuccessStatusCode)
            {
                // couldn't login, try register
                response = userWebApi.RegisterAsync(casheServerUser).Result;
                if (!response.IsSuccessStatusCode)
                {
                    throw new CacheServerException("Couldn't login or register with Media Server");
                }
            }

            // at this point CacheServer is either logged in or registered
            // get Id from MediaServer
            casheServerUser = response.Content.ReadAsAsync<CUserInfo>().Result;

            // get users, their playlists and files from Media Server and write them to Dictionary 'UserFiles'
            InitialFillOfTheUserFiles();

        }
        #endregion


        // get users, their playlists and files from Media Server and write them to Dictionary 'UserFiles'
        private void InitialFillOfTheUserFiles()
        {
            // initial fill of the Dictionary
            // get all users
            HttpResponseMessage getUsersResult = userWebApi.GetUsersAsync(casheServerUser).Result;
            List<CUserInfo> users = getUsersResult.Content.ReadAsAsync<List<CUserInfo>>().Result;

            // get playlistInfos for each user
            foreach (CUserInfo userInfo in users)
            {
                // in no such user is present in CacheServer - add it to Dictionary
                if (!UserFiles.Keys.Contains(userInfo.Id))
                {
                    UserFiles.Add(userInfo.Id, new List<CPlaylistInfo>());
                }

                // get user playlists
                HttpResponseMessage getPlaylistsResult = playlistWebApi.GetPlaylistsByUserId(userInfo).Result;
                List<CPlaylistInfo> userPlaylistsInfo = getPlaylistsResult.Content.ReadAsAsync<List<CPlaylistInfo>>().Result;

                foreach (CPlaylistInfo playlistInfo in userPlaylistsInfo)
                {
                    // add playlist to user in Dictionary if absent
                    CPlaylistInfo checkPlaylist = UserFiles[userInfo.Id].Where(p => p.Id.Equals(playlistInfo.Id)).FirstOrDefault();
                    if (checkPlaylist == null)
                    {
                        UserFiles[userInfo.Id].Add(playlistInfo);
                    }

                    // get files fore each playlist
                    HttpResponseMessage getFilesInPlaylistsResult = filesWebApi.FilesFromPlaylistAsync(casheServerUser, playlistInfo).Result;
                    List<CFileInfo> filesInPlaylist = getFilesInPlaylistsResult.Content.ReadAsAsync<List<CFileInfo>>().Result;

                    // Add userfiles to playlist
                    foreach (CFileInfo fileInfo in filesInPlaylist)
                    {
                        UserFiles[userInfo.Id].Where(p => p.Id.Equals(playlistInfo.Id)).First().Files.Add(fileInfo);
                    }
                }
            }
        }

        #region RequestsToMediaService
        public async Task<HttpResponseMessage> GetAllUsers(CUserInfo userInfo)
        {
            // clear all headers
            httpClientForMediaServer.DefaultRequestHeaders.Clear();
            httpClientForMediaServer.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(userInfo);
            HttpResponseMessage response = await httpClientForMediaServer.GetAsync($"api/playlist/GetByUserId?userId={userInfo.Id}");
            return response;
        }
        public async Task<HttpResponseMessage> GetPlaylistsByUserId(CUserInfo userInfo)
        {
            // clear all headers
            httpClientForMediaServer.DefaultRequestHeaders.Clear();
            httpClientForMediaServer.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(userInfo);
            HttpResponseMessage response = await httpClientForMediaServer.GetAsync($"api/playlist/GetByUserId?userId={userInfo.Id}");
            return response;
        }
        #endregion

        #region ResponsesFromCacheService


        #region FileActions
        [HttpGet]
        // Requst can come from frontender(or client if frontender is absent)
        public HttpResponseMessage GetFilesFromPlaylist(CPlaylistInfo playlistInfo)
        {

            HttpResponseMessage response;
            List<CFileInfo> fileInfos = new List<CFileInfo>();
            try
            {
                fileInfos = SlimReaderWriter.Get(playlistInfo);
                response = Request.CreateResponse(HttpStatusCode.OK, fileInfos);
            }
            catch (Exception ex)
            {
                response = Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
            return response;
        }

        [HttpGet]
        // Called by MediaServer.FileController
        // If user uploaded/updated(rename, watch, like, dislike) file in Media Server - update cash dictionary.
        public HttpResponseMessage AddOrUpdateFile(CFileInfo fileInfo)
        {

            HttpResponseMessage response;
            try
            {
                if (!UserFiles.Keys.Contains(fileInfo.UserId))
                {
                    response = Request.CreateResponse(HttpStatusCode.BadRequest, "User not found in Cache Server");
                    return response;
                }

                //todo: for all playlists add or update fileINfo
                foreach (CPlaylistInfo playlistInfo in fileInfo.playlists)
                {
                    // add playlist to user in Dictionary if absent
                    CPlaylistInfo checkPlaylist = UserFiles[fileInfo.UserId]
                        .Where(p => p.Id.Equals(playlistInfo.Id)).FirstOrDefault();
                    if (checkPlaylist == null)
                    {
                        UserFiles[fileInfo.UserId].Add(playlistInfo);
                    }

                    // Add userfile to playlist in Cache Server
                    // todo: can we not assign again checkPlaylist, just assign?
                    checkPlaylist = UserFiles[fileInfo.UserId]
                        .Where(p => p.Id.Equals(playlistInfo.Id)).FirstOrDefault();
                    checkPlaylist.Files.Add(fileInfo);
                }
                response = Request.CreateResponse(HttpStatusCode.OK, "File added/updated in Cache Server");
            }
            catch (Exception ex)
            {
                response = Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
            return response;
        }

        public HttpResponseMessage DeleteFile(CPlaylistInfo playlistInfo)
        {
            HttpResponseMessage response;

            try
            {
                if (!UserFiles.Keys.Contains(playlistInfo.UserId))
                {
                    response = Request.CreateResponse(HttpStatusCode.BadRequest, "No user found in Cache Server!");
                }

                CPlaylistInfo checkPlaylistInfo = UserFiles[playlistInfo.UserId]
                    .Where(p => p.Id.Equals(playlistInfo.Id)).FirstOrDefault();

                if (checkPlaylistInfo == null)
                {
                    response = Request.CreateResponse(HttpStatusCode.BadRequest, "Not found playlist in Cache Server!");
                }

                UserFiles[playlistInfo.UserId].Remove(checkPlaylistInfo);
                response = Request.CreateResponse(HttpStatusCode.OK, playlistInfo);
            }
            catch (Exception ex)
            {
                response = Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
            return response;
        }


        #endregion

        #region PlaylistActions
        [HttpGet]
        // Requst can come from frontender(or client if frontender is absent)
        public HttpResponseMessage GetUserPlaylists(Guid userId)
        {

            HttpResponseMessage response;
            List<CPlaylistInfo> playlistInfos = new List<CPlaylistInfo>();
            try
            {
                if (!UserFiles.Keys.Contains(userId))
                {
                    response = Request.CreateResponse(HttpStatusCode.BadRequest, "No playlist found in Cache Server!");
                }
                else
                {
                    playlistInfos = UserFiles[userId];
                    response = Request.CreateResponse(HttpStatusCode.OK, playlistInfos);
                }
            }
            catch (Exception ex)
            {
                response = Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
            return response;
        }

        [HttpGet]
        //Requst can come from frontender(or client if frontender is absent)
        public HttpResponseMessage AddOrUpdatePlaylist(CPlaylistInfo playlistInfo)
        {
            HttpResponseMessage response;

            try
            {
                if (!UserFiles.Keys.Contains(playlistInfo.UserId))
                {
                    response = Request.CreateResponse(HttpStatusCode.BadRequest, "No user found in Cache Server!");
                }

                CPlaylistInfo checkPlaylistInfo = UserFiles[playlistInfo.UserId]
                    .Where(p => p.Id.Equals(playlistInfo.Id)).FirstOrDefault();

                if (checkPlaylistInfo != null)
                {
                    checkPlaylistInfo = playlistInfo;
                    response = Request.CreateResponse(HttpStatusCode.BadRequest, "Updated playlist in Cache Server!");
                }
                else
                {
                    UserFiles[playlistInfo.UserId].Add(playlistInfo);
                    response = Request.CreateResponse(HttpStatusCode.OK, playlistInfo);
                }
            }
            catch (Exception ex)
            {
                response = Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
            return response;
        }

        public HttpResponseMessage DeletePlaylist(CPlaylistInfo playlistInfo)
        {
            HttpResponseMessage response;

            try
            {
                if (!UserFiles.Keys.Contains(playlistInfo.UserId))
                {
                    response = Request.CreateResponse(HttpStatusCode.BadRequest, "No user found in Cache Server!");
                }

                CPlaylistInfo checkPlaylistInfo = UserFiles[playlistInfo.UserId]
                    .Where(p => p.Id.Equals(playlistInfo.Id)).FirstOrDefault();

                if (checkPlaylistInfo == null)
                {
                    response = Request.CreateResponse(HttpStatusCode.BadRequest, "Not found playlist in Cache Server!");
                }

                UserFiles[playlistInfo.UserId].Remove(checkPlaylistInfo);
                response = Request.CreateResponse(HttpStatusCode.OK, playlistInfo);
            }
            catch (Exception ex)
            {
                response = Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
            return response;
        }

        #endregion

        #region UserActions
        //[HttpGet]
        // Requst can come from frontender(or client if frontender is absent)
        public HttpResponseMessage AddUser(CUserInfo userInfo)
        {

            HttpResponseMessage response;
            if (UserFiles.Keys.Contains(userInfo.Id))
            {
                response = Request.CreateResponse(HttpStatusCode.BadRequest, "Such user already exists!");
            }

            try
            {
                UserFiles.Add(userInfo.Id, new List<CPlaylistInfo>());
                response = Request.CreateResponse(HttpStatusCode.OK, "User created at Cache Server");
            }
            catch (Exception ex)
            {
                response = Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
            return response;
        }

        [HttpGet]
        public HttpResponseMessage DeleteUser(CUserInfo userInfo)
        {

            HttpResponseMessage response;
            if (!UserFiles.Keys.Contains(userInfo.Id))
            {
                response = Request.CreateResponse(HttpStatusCode.BadRequest, "No user found in Cache Server!");
            }

            try
            {
                UserFiles.Remove(userInfo.Id);
                response = Request.CreateResponse(HttpStatusCode.OK, "User deleted from Cache Server");
            }
            catch (Exception ex)
            {
                response = Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
            return response;
        }

        #endregion

        #endregion
    }
}
