using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using DTO;

namespace MediaServer.Interfaces
{
    public interface IFileController
    {
        /// <summary>
        /// Upload file from the client. Credentials are taken from http-header: authorization (later - token?)
        /// </summary>
        /// <returns></returns>
        Task<HttpResponseMessage> UploadChunkAsync();

        // Download/Stream file
        HttpResponseMessage DownloadAsync(Guid fileId);

        // Delete file
        HttpResponseMessage DeleteFile(Guid filed);

        // Get files from user playlist
        HttpResponseMessage GetFilesFromPlaylist(Guid playlistId);

        // Get all files of given user without playlist info
        HttpResponseMessage GetUserFiles(Guid userId);


        // Add file to playlist
        HttpResponseMessage AddFileToPlaylist(Guid fileId, Guid playlistId);

    }
}