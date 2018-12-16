using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO
{
    public class CFileInfo
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public Int64 Size { get; set; }// in bytes
        public Guid UserId { get; set; }
        public bool IsPublic { get; set; }
        public byte[] Hash { get; set; }
        public DateTime LoadDate { get; set; }
        public Int32 Views { get; set; }
        public Int32 Likes { get; set; }
        public Int32 Dislikes { get; set; }
        public byte[] Thumbnail { get; set; }

        public List<CPlaylistInfo> playlists { get; set; }


        #region CTORs
        public CFileInfo()
        { }

        public CFileInfo(string name, bool isPublic) :
            this(Guid.Empty, name, "", 0, Guid.Empty, isPublic, null, DateTime.Now, 0, 0, 0)
        { }

        public CFileInfo(CFileInfo fi) : this(
            fi.Guid, fi.Name, fi.Path, fi.Size, fi.UserId, fi.IsPublic, fi.Hash, fi.LoadDate, fi.Views, fi.Likes, fi.Dislikes)
        { }

        public CFileInfo(
            Guid guid, string name, string path, Int64 size,
            Guid userId, bool isPublic, byte[] hash, DateTime loadDate,
            Int32 views, Int32 likes, Int32 dislikes)
        {
            Guid = guid;
            Name = name;
            Path = path;
            Size = size;
            UserId = userId;
            IsPublic = isPublic;
            Hash = hash;
            LoadDate = loadDate;
            Views = views;
            Likes = likes;
            Dislikes = dislikes;
            playlists = new List<CPlaylistInfo>();
        }

        #endregion
    }
}
