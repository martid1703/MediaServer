using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using DTO;

namespace CasheServer.Models
{
    public class CFile:INotifyPropertyChanged
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

        #region CTORs
        public CFile()
        {

        }


        public CFile(CFileInfo fileInfo, CUser user, string userFolder) : this(
        Guid.Empty, fileInfo.Name, userFolder, 0,
        user.Guid, fileInfo.IsPublic, null, DateTime.Now, 0, 0, 0)
        {

        }

        public CFile(
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
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}