using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using DTO;

namespace CasheServer.Interfaces
{
    public interface IFilesWebApi
    {

        Task<HttpResponseMessage> UploadFileAsync(CFileInfo fileInfo, CUserInfo userInfo);

        Task<HttpResponseMessage> DownloadFileAsync(CFileInfo fileInfo, CUserInfo user);

        Task<HttpResponseMessage> DeleteFileAsync(Guid fileId, CUserInfo userInfo);

        Task<HttpResponseMessage> RenameFileAsync(Guid fileId, string newName, CUserInfo userInfo);

        Task<HttpResponseMessage> GetUserFilesAsync(CUserInfo user);


    }
}