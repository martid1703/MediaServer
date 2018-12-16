using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MediaServer.Interfaces;
using MediaServer.Exceptions;
using MediaServer.Models;
using MediaServer.DAL;
using DTO;
using System.Threading.Tasks;
using System.Web;

namespace MediaServer.Controllers
{
    public class PlaylistController : ApiController, IPlaylistController
    {
        private readonly PlaylistContext _playlistContext = new PlaylistContext();

        public HttpResponseMessage Create(CPlaylistInfo playlistInfo)
        {
            HttpResponseMessage response;
            try
            {
                CPlaylist newPlaylist = new CPlaylist(playlistInfo);
                Int32 created = _playlistContext.Create(newPlaylist);
                if (created < 1)
                {
                    response = Request.CreateResponse(HttpStatusCode.BadRequest, "Couldn't create playlist");
                }
                else
                {
                    response = Request.CreateResponse(HttpStatusCode.OK, $"Created playlist {playlistInfo.Name}!");
                }

                return response;
            }
            catch (ContextException e)
            {
                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }
        }

        public HttpResponseMessage Delete(Guid playlistGuid)
        {
            try
            {
                if (_playlistContext.DeleteById(playlistGuid) > 0)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
            catch (ContextException e)
            {
                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }
        }

        public HttpResponseMessage GetAllPublic(Guid userId)
        {
            HttpResponseMessage response;

            List<CPlaylistInfo> playlistsInfo= new List<CPlaylistInfo>();
            try
            {
                List<CPlaylist> playlists = _playlistContext.GetAllPublic(userId).ToList();
                foreach (CPlaylist playlist in playlists)
                {
                    CPlaylistInfo playlistInfo = new CPlaylistInfo();
                    playlistInfo = playlist.ToCPlaylistInfo();
                    playlistsInfo.Add(playlistInfo);
                }

                response = Request.CreateResponse(HttpStatusCode.OK, playlistsInfo);
                return response;

            }
            catch (Exception e)// in case program crashes?
            {

                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }
        }

        public HttpResponseMessage GetById(Guid playlistId)
        {
            HttpResponseMessage response;

            CPlaylistInfo playlistInfo = new CPlaylistInfo();
            try
            {
                CPlaylist playlist = _playlistContext.GetById(playlistId);
                playlistInfo = playlist.ToCPlaylistInfo();
                response = Request.CreateResponse(HttpStatusCode.OK, playlistInfo);
                return response;

            }
            catch (Exception e)// in case program crashes?
            {

                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }
        }

        public HttpResponseMessage GetByUserId(Guid userId)
        {
            HttpResponseMessage response;

            List<CPlaylistInfo> playlistsInfo = new List<CPlaylistInfo>();
            try
            {
                List<CPlaylist> playlists = _playlistContext.GetByUserId(userId).ToList();
                foreach (CPlaylist playlist in playlists)
                {
                    CPlaylistInfo playlistInfo = new CPlaylistInfo();
                    playlistInfo = playlist.ToCPlaylistInfo();
                    playlistsInfo.Add(playlistInfo);
                }

                response = Request.CreateResponse(HttpStatusCode.OK, playlistsInfo);
                return response;

            }
            catch (Exception e)// in case program crashes?
            {

                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }
        }

        public HttpResponseMessage Update(CPlaylistInfo playlistInfo)
        {
            HttpResponseMessage response;

            try
            {
                CPlaylist updatePlaylist = new CPlaylist(playlistInfo);
                Int32 updated = _playlistContext.Update(updatePlaylist);

                if (updated < 1)
                {
                    response = Request.CreateResponse(HttpStatusCode.BadRequest, $"Couldn't update playlist {playlistInfo.Name}");
                }
                else
                {
                    response = Request.CreateResponse(HttpStatusCode.OK, $"Updated playlist {playlistInfo.Name}!");
                }
                return response;

            }
            catch (Exception e)// in case program crashes?
            {

                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }
        }
    }
}
