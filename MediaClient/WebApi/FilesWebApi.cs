using DTO;
using MediaClient.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;


namespace MediaClient.WebApi
{
    public class FilesWebApi : IFilesWebApi
    {
        static HttpClient client = new HttpClient();

        public FilesWebApi()
        {
            // setup httpclient
            HelperMethods.SetupHttpClient(out client);
        }
        
        #region IFilesWebApi

        public async Task<HttpResponseMessage> DeleteFileAsync(Guid fileId, CUserInfo userInfo)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(userInfo);
            HttpResponseMessage response = await client.GetAsync($"api/file/DeleteFile?fileId={fileId}");
            return response;
        }

        public async Task<HttpResponseMessage> DownloadFileAsync(CFileInfo fileInfo, CUserInfo userInfo)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();
            // set Authorization header
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(userInfo);

            string fileInfoJson = JsonConvert.SerializeObject(fileInfo);
            client.DefaultRequestHeaders.Add("fileInfo", fileInfoJson);

            HttpResponseMessage response = await client.GetAsync($"api/file/DownloadAsync?fileId={fileInfo.Guid}");
            return response;
        }

        public async Task<HttpResponseMessage> GetUserFilesAsync(CUserInfo userInfo)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();

            // set Authorization header
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(userInfo);

            HttpResponseMessage response = await client.GetAsync($"api/file/GetUserFiles?userId={userInfo.Id}");
            return response;
        }

        public async Task<HttpResponseMessage> FilesFromPlaylistAsync(CUserInfo userInfo, CPlaylistInfo playlistInfo)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();

            // set Authorization header
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(userInfo);

            HttpResponseMessage response = await client.GetAsync($"api/file/GetFilesFromPlaylist?playlistId={playlistInfo.Id}");
            return response;
        }

        public async Task<HttpResponseMessage> RenameFileAsync(Guid fileId, string newName, CUserInfo userInfo)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(userInfo);
            HttpResponseMessage response = await client.GetAsync($"api/file/RenameFile?fileId={fileId}&newName={newName}");
            return response;
        }


        public async Task<HttpResponseMessage> LikeFileAsync(CFileInfo fileInfo, CUserInfo userInfo)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(userInfo);
            HttpResponseMessage response = await client.PostAsJsonAsync<CFileInfo>($"api/file/LikeFile", fileInfo);
            return response;
        }

        public async Task<HttpResponseMessage> DislikeFileAsync(CFileInfo fileInfo, CUserInfo userInfo)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(userInfo);
            HttpResponseMessage response = await client.PostAsJsonAsync<CFileInfo>($"api/file/DislikeFile", fileInfo);
            return response;
        }

        public async Task<HttpResponseMessage> UploadFileAsync(CFileInfo fileInfo, CUserInfo userInfo)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();
            // set Authorization header
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(userInfo);

            string fileInfoJson = JsonConvert.SerializeObject(fileInfo);
            client.DefaultRequestHeaders.Add("fileInfo", fileInfoJson);

            HttpContent content = new StreamContent(File.OpenRead(fileInfo.Path));
            content.Headers.ContentType=new MediaTypeHeaderValue("application/octet-stream");
            
            HttpResponseMessage response = await client.PostAsync("api/file/UploadAsync", content);
            return response;
        }

        public async Task<HttpResponseMessage> UploadChunkAsync(CFileInfo fileInfo, CUserInfo userInfo, byte[] chunk, bool isLastChunk)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();

            // set Authorization header
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(userInfo);

            string fileInfoJson = JsonConvert.SerializeObject(fileInfo);
            client.DefaultRequestHeaders.Add("fileInfo", fileInfoJson);

            string chunkSizeJson = JsonConvert.SerializeObject(chunk.Length);
            client.DefaultRequestHeaders.Add("chunkSize", chunkSizeJson);

            string isLastChunkJson = JsonConvert.SerializeObject(isLastChunk);
            client.DefaultRequestHeaders.Add("isLastChunk", isLastChunkJson);

            HttpContent content = new ByteArrayContent(chunk); 
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            HttpResponseMessage response = await client.PostAsync("api/file/UploadChunkAsync", content);
            return response;
        }

        public async Task<HttpResponseMessage> DownloadChunkAsync(CFileInfo fileInfo, CUserInfo userInfo, byte[] chunk, bool isLastChunk)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();

            // set Authorization header
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(userInfo);

            string fileInfoJson = JsonConvert.SerializeObject(fileInfo);
            client.DefaultRequestHeaders.Add("fileInfo", fileInfoJson);

            string chunkSizeJson = JsonConvert.SerializeObject(chunk.Length);
            client.DefaultRequestHeaders.Add("chunkSize", chunkSizeJson);

            string isLastChunkJson = JsonConvert.SerializeObject(isLastChunk);
            client.DefaultRequestHeaders.Add("isLastChunk", isLastChunkJson);

            HttpContent content = new ByteArrayContent(chunk);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            HttpResponseMessage response = await client.GetAsync($"api/file/DownloadChunkAsync");
            return response;
        }

        public async Task<CFileInfo> FileDetails(CFileInfo fileInfo, CUserInfo userInfo)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();

            // set Authorization header
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(userInfo);

            HttpResponseMessage response = await client.PostAsJsonAsync<CFileInfo>($"api/file/FileDetails", fileInfo);
            CFileInfo fileInfoFromMediaServer= response.Content.ReadAsAsync<CFileInfo>().Result;

            return fileInfoFromMediaServer;
        }

        public async Task<byte[]> GetFileThumbnail(Guid fileId, CUserInfo userInfo)
        {
            // clear all headers
            client.DefaultRequestHeaders.Clear();

            // set Authorization header
            client.DefaultRequestHeaders.Authorization = HelperMethods.UserCredentials(userInfo);

            HttpResponseMessage response = await client.GetAsync($"api/file/GetFileThumbnail?fileId={fileId}");
            byte[] result=response.Content.ReadAsByteArrayAsync().Result;

            return result;
        }

        #endregion 


    }
}
