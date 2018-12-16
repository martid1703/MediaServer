using DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MediaServer.Interfaces
{
   public interface IPlaylistController
    {
        // Create playlist
        HttpResponseMessage Create(CPlaylistInfo playlistInfo);

        // Delete playlist
        HttpResponseMessage Delete(Guid playlistGuid);

        // Update playlist
        HttpResponseMessage Update(CPlaylistInfo playlistInfo);

        // Get all playlists
        HttpResponseMessage GetAllPublic(Guid userId);

        // Get by playlistId
        HttpResponseMessage GetById(Guid playlistId);

        /// <summary>
        /// Get all playlists for specified user. From authorization we know if the user requesting info is other user.
        /// If so, return only public playlists, otherwise return all.
        /// </summary>
        /// <param name="playlistId"></param>
        /// <returns></returns>
        HttpResponseMessage GetByUserId(Guid userId);

    }
}
