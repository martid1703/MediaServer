using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO
{
    public class CPlaylistInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid UserId { get; set; }
        public bool IsPublic { get; set; }
        public List<CFileInfo> Files { get; set; }



        public CPlaylistInfo()
        {

        }

        public CPlaylistInfo(Guid id, string name, Guid userId, bool isPublic, List<CFileInfo> files)
        {
            Id = id;
            Name = name;
            UserId = userId;
            IsPublic = isPublic;
            Files = files;
        }

    }
}
