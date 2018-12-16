using DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CasheServer.Models
{
    // Extension methods here to convert DAL objects to DTO objects.
    public static class ExtensionMethods
    {
        public static CUserInfo ToCUserInfo(this CUser user)
        {
            CUserInfo userInfo = new CUserInfo();
            userInfo.Id = user.Guid;
            userInfo.Name = user.Name;
            userInfo.Email = user.Email;
            //userInfo.Password -  we do not return user his password-hash
            return userInfo;
        }

        public static CPlaylistInfo ToCPlaylistInfo(this CPlaylist playlist)
        {
            CPlaylistInfo playlistInfo = new CPlaylistInfo();
            playlistInfo.Id = playlist.Guid;
            playlistInfo.Name = playlist.Name;
            playlistInfo.UserId = playlist.UserId;
            playlistInfo.IsPublic = playlist.IsPublic;
            //userInfo.Password -  we do not return user his password-hash
            return playlistInfo;
        }

        public static CFileInfo ToCFileInfo(this CFile file)
        {
            CFileInfo fileInfo = new CFileInfo();
            fileInfo.Guid = file.Guid;
            fileInfo.Name = file.Name;
            fileInfo.Size = file.Size;
            fileInfo.UserId = file.UserId;
            fileInfo.IsPublic = file.IsPublic;
            fileInfo.LoadDate = file.LoadDate;
            fileInfo.Views = file.Views;
            fileInfo.Likes = file.Likes;
            fileInfo.Dislikes = file.Dislikes;
            return fileInfo;
        }

    }
}