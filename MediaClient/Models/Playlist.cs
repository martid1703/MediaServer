using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DTO;

namespace MediaClient.Models
{
    /// <summary>
    /// Each user can create many playlists. 
    /// </summary>
    public class CPlaylist
    {
        public Guid Guid { get; set; }
        public string Name { get; set; } // not unique
        public Guid UserId { get; set; } // FK
        public bool IsPublic { get; set; }
        public List<CFile> files { get; set; }// many to many


        public CPlaylist()
        {

        }

        public CPlaylist(CPlaylistInfo playlistInfo):this(playlistInfo.Id, playlistInfo.Name,playlistInfo.UserId, playlistInfo.IsPublic)
        {

        }
        public CPlaylist(Guid guid, string name, Guid userId, bool isPublic)
        {
            Guid = guid;
            Name = name;
            UserId = userId;
            IsPublic = isPublic;
        }

    }
}