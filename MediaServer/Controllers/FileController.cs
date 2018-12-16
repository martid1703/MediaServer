using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using MediaServer.Interfaces;
using DTO;
using MediaServer.DAL;
using MediaServer.Models;
using MediaServer.Exceptions;
using System.Security.Cryptography;
using System.Net.Http.Headers;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Data.SqlClient;
using VideoProcessing;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;

namespace MediaServer.Controllers
{
    //[Authorize]
    public class FileController : ApiController, IFileController
    {
        private readonly UserContext _userContext = new UserContext();
        private readonly FileContext _fileContext = new FileContext();
        private readonly PlaylistContext _playlistContext = new PlaylistContext();

        // todo: this variables should be specific per client request. Is it safe to keep them static??
        private static CUser user;
        private static string userName = "";
        private static string userFolder = "";// user folder based on userName
        IEnumerable<String> fileInfoHeader;// file info - from Request
        IEnumerable<String> chunkSizeHeader;// chunk info - from Request
        IEnumerable<String> isLastChunkHeader;// is last chunk - from Request

        CFileInfo fileInfo;// deserialized file details from fileInfoHeader
        Int32 chunkSize = 500000;// 500 kB
        Boolean isLastChunk = false;

        private void SetUserDetails()
        {
            // get user credentials
            Tuple<string, string> credentials = HelperMethods.NamePasswordFromAuthHeader(Request.Headers.Authorization.Parameter);
            userName = credentials.Item1;

            user = _userContext.GetByName(userName);

            //create full path for the uploaded file 
            userFolder = HttpContext.Current.Server.MapPath($@"~/App_Data/UserFiles/{userName}/");

            // get fileInfo from the request header
            fileInfoHeader = Request.Headers.GetValues("fileInfo");

            // get chunk info from the request header
            chunkSizeHeader = Request.Headers.GetValues("chunkSize");

            // get fileInfo from the request header
            isLastChunkHeader = Request.Headers.GetValues("isLastChunk");

            // deserialize file description
            fileInfo = JsonConvert.DeserializeObject<CFileInfo>(fileInfoHeader.ElementAt(0));
            if (String.IsNullOrEmpty(fileInfo.Name))
            {
                fileInfo.Name = $"{userName}_{DateTime.Now}";
            }

            // deserialize chunk size
            chunkSize = JsonConvert.DeserializeObject<Int32>(chunkSizeHeader.ElementAt(0));

            // is last chunk? 
            isLastChunk = JsonConvert.DeserializeObject<Boolean>(isLastChunkHeader.ElementAt(0));
        }

        #region IFileController

        [HttpPost]
        [Authorize]
        // check fileLoadIndex for given fileInfo header.
        public CFileInfo FileDetails(CFileInfo fileInfo)
        {
            // get user credentials
            Tuple<string, string> credentials = HelperMethods.NamePasswordFromAuthHeader(Request.Headers.Authorization.Parameter);
            userName = credentials.Item1;
            CUser user = _userContext.GetByName(userName);
            CFile file = _fileContext.GetByFileName(fileInfo.Name, user.Guid);
            return file.ToCFileInfo();
        }

        [HttpPost]
        //[Authorize]
        public async Task<HttpResponseMessage> UploadChunkAsync()
        {
            SqlConnection myConnection = new SqlConnection();
            SqlTransaction myTransaction;

            using (myConnection)
            {
                myConnection.ConnectionString = ConnectionContext.GetConnectionString();
                myConnection.Open();

                // TRANSACTION - in case of SQL error or File write to disk error - rollback will be awailable
                myTransaction = myConnection.BeginTransaction("UploadFileTransaction");

                try
                {
                    SetUserDetails();
                    HttpResponseMessage response;
                    String responseMsg;

                    // check if such hash has record in DB.
                    CFile newFile = _fileContext.GetByHash(fileInfo.Hash, myConnection, myTransaction);

                    // if such hash and exist in DB and user names match
                    if (!newFile.Guid.Equals(Guid.Empty) && newFile.UserId.Equals(user.Guid))
                    {
                        response = Request.CreateResponse(HttpStatusCode.BadRequest);
                        response.ReasonPhrase = $"Another file {newFile.Name} with same hash than{fileInfo.Name} already exists on Media Server!";
                        return response;
                    }

                    // check if such file name has record in DB.
                    newFile = _fileContext.GetByFileName(fileInfo.Name, user.Guid, myConnection, myTransaction);
                    // if file with such name already exists in DB
                    if (!newFile.Guid.Equals(Guid.Empty))
                    {
                        // file exits, it has hash (so it is 100% loaded), but hash is different, which means this is the other file!
                        if (newFile.Hash != null)
                        {
                            response = Request.CreateResponse(HttpStatusCode.BadRequest);
                            response.ReasonPhrase = $"Another file {newFile.Name} with same hash than{fileInfo.Name} already exists on Media Server!";
                            return response;
                        }
                        else
                        {
                            // file exists, but it has no hash, so it hasn't been 100% loaded - continue loading
                        }
                    }
                    // fileName not found in DB - create new file record
                    else
                    {
                        // add record to DB with file info
                        newFile = new CFile(fileInfo, user, userFolder);

                        // create record in DB about new file
                        Int32 added = _fileContext.Create(newFile, myConnection, myTransaction);

                        if (added == 0)
                        {
                            response = Request.CreateResponse(HttpStatusCode.BadRequest, $"Cannot add file to DB!");
                            return response;
                        }

                        // SQL generates primary field values, so get created file back with proper Guid
                        newFile = _fileContext.GetByFileName(newFile.Name, user.Guid, myConnection, myTransaction);

                        // add file to all playlists specified by user. Default playlist is 'ON' if no other playlists are specified.
                        if (fileInfo.playlists.Count == 0)
                        {
                            CPlaylist playlist = _playlistContext.GetByName("default", user.Guid);
                            _fileContext.AddToPlaylist(newFile.Guid, playlist.Guid, myConnection, myTransaction);
                        }
                        else
                        {
                            // add files to other playlists specified by user
                            foreach (CPlaylistInfo playlistInfo in fileInfo.playlists)
                            {
                                CPlaylist playlist = new CPlaylist(playlistInfo);
                                _fileContext.AddToPlaylist(newFile.Guid, playlist.Guid, myConnection, myTransaction);
                            }
                        }
                    }

                    // After all checks on existing files, continue loading given chunk of byte data
                    //todo: upload directly to memory stream or to file stream?
                    MemoryStream ms = new MemoryStream(new byte[chunkSize], true);
                    using (ms)
                    {
                        await Request.Content.CopyToAsync(ms);

                        // check if all bytes from the chunk have been loaded (no disconnect or program crash happened during load)
                        if (ms.Length == chunkSize)
                        {
                            using (FileStream fs = new FileStream
                                (newFile.Path + newFile.Name, FileMode.Append, FileAccess.Write, FileShare.None, chunkSize, useAsync: true))
                            {
                                ms.WriteTo(fs);
                            }
                        }
                        else
                        {
                            // couldn't upload file chunk - so rollback record from DB
                            myTransaction.Rollback();

                            response = Request.CreateResponse(HttpStatusCode.OK, $"Chunk has not been uploaded correctly!");
                            return response;
                        }
                    }

                    responseMsg = $"Chunk for file:{newFile.Name} successfully uploaded!";

                    // calculate Hash only if file has been loaded 100%
                    if (isLastChunk)
                    {
                        // calculate Hash only if file has been loaded 100%
                        await Task.Factory.StartNew(() => HashFile(newFile)).ContinueWith(delegate
                          {
                            // calculate Thumbnail for video only if file has been loaded 100%
                            CreateThumbnail(newFile);
                          });

                        responseMsg = $"File  successfully uploaded!";
                    }

                    // update file data in DB: hash, fileSize, ... 
                    newFile.Size += chunkSize;
                    _fileContext.Update(newFile, myConnection, myTransaction);

                    // if all went OK, commit records to DB
                    myTransaction.Commit();

                    // return response if success
                    response = Request.CreateResponse(HttpStatusCode.OK, responseMsg);
                    return response;
                }
                catch (Exception e)
                {
                    myTransaction.Rollback();
                    HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                    throw new FileUploadException(e.Message, e);
                }
            }

        }

        // create hash of the file if 100% uploaded
        void HashFile(CFile file)
        {
            file.Hash = Task.Run(() =>
            {
                byte[] hash;
                hash = Hashing.MD5Generator.MD5Hash(userFolder + file.Name);
                return hash;
            }).Result;

            _fileContext.Update(file);

        }

        // create video thumbnail if 100% loaded
        void CreateThumbnail(CFile file)
        {
            Task.Run(() =>
            {
                string fileName = Path.ChangeExtension(file.Name, ".jpg");
                bool isOk=VideoThumbnail.CreateThumbnail(
                  Path.Combine(file.Path + file.Name),
                  Path.Combine(file.Path, "Thumbnails", fileName));
            });
        }


        [HttpPost]
        // reads all stream 
        [Obsolete]
        public HttpResponseMessage BasicUploadAsync()
        {
            var task = this.Request.Content.ReadAsStreamAsync();
            task.Wait();
            Stream requestStream = task.Result;
            HttpResponseMessage response = new HttpResponseMessage();

            try
            {
                Tuple<string, string> credentials = HelperMethods.NamePasswordFromAuthHeader(Request.Headers.Authorization.Parameter);
                string userName = credentials.Item1;
                string root = HttpContext.Current.Server.MapPath($@"~/App_Data/UserFiles/{userName}/");
                IEnumerable<string> headerValues = Request.Headers.GetValues("fileName");
                string fileName = headerValues.FirstOrDefault();
                if (fileName == null)
                {
                    fileName = $"{userName}_{DateTime.Now}";
                }
                Stream fileStream = File.Create(root + fileName);
                // error "maximum request length exceeded"
                requestStream.CopyTo(fileStream);
                fileStream.Close();
                requestStream.Close();
            }
            catch (IOException)
            {
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }

            response.StatusCode = HttpStatusCode.Created;
            return response;
        }

        [HttpGet]
        [Authorize]
        [Obsolete]
        //todo: not using 'Using' on file stream because: https://stackoverflow.com/questions/9541351/returning-binary-file-from-controller-in-asp-net-web-api
        public HttpResponseMessage DownloadAsync(Guid fileId)
        {
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();

                Tuple<string, string> credentials = HelperMethods.NamePasswordFromAuthHeader(Request.Headers.Authorization.Parameter);
                string userName = credentials.Item1;
                CFile file = _fileContext.GetByFileId(fileId);
                FileStream fs = new FileStream(
                    file.Path + file.Name, FileMode.Open, FileAccess.Read, FileShare.Read, chunkSize, true);
                response.Content = new StreamContent(fs);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage DownloadChunkAsync()
        {
            try
            {
                SetUserDetails();
                HttpResponseMessage response = new HttpResponseMessage();

                CFile file = _fileContext.GetByFileId(fileInfo.Guid);
                FileStream fs = new FileStream(
                    file.Path + file.Name, FileMode.Open, FileAccess.Read, FileShare.Read, chunkSize, true);

                // nothin to send here
                if (fs.Length < fileInfo.Size)
                {
                    Request.CreateResponse(HttpStatusCode.BadRequest, "Index of fileInfo.Size > file.Length on server");
                    return response;
                }

                Int32 remaining = (Int32)(fs.Length - fileInfo.Size);
                if (remaining < chunkSize)
                {
                    chunkSize = remaining;
                }

                using (fs)
                {
                    // seek position as per user file load size
                    fs.Position = fileInfo.Size;

                    // read next chunk
                    byte[] buffer = new byte[chunkSize];
                    fs.ReadAsync(buffer, 0, chunkSize);

                    // pass byte chunk to response.content
                    response.Content = new ByteArrayContent(buffer);
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    return response;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        [HttpGet]
        // todo: how to make async? 'fileStreamDownload.WriteToStream' is awaitable, but error: "cannot await method group"?
        public HttpResponseMessage PlayAsync(Guid fileId)
        {
            //Tuple<string, string> credentials = HelperMethods.NamePasswordFromAuthHeader(Request.Headers.Authorization.Parameter);
            //string userName = credentials.Item1;
            CFile cFile = _fileContext.GetByFileId(fileId);
            cFile.Views += 1;
            _fileContext.Update(cFile);

            String fileFullName = cFile.Path + cFile.Name;

            FileStreamDownload fileStreamDownload = new FileStreamDownload(fileFullName);
            var response = Request.CreateResponse();
            response.Content = new PushStreamContent(
                new Action<Stream, HttpContent, TransportContext>(fileStreamDownload.WriteToStreamAsync),
                new MediaTypeHeaderValue("video/" + Path.GetExtension(fileFullName)));

            return response;
        }


        [HttpGet]
        [Authorize]
        public HttpResponseMessage AddFileToPlaylist(Guid fileId, Guid playlistId)
        {
            try
            {
                HttpResponseMessage responseMsg;
                Int32 inserted = _fileContext.AddToPlaylist(fileId, playlistId);

                if (inserted < 1)
                {
                    responseMsg = Request.CreateResponse(HttpStatusCode.BadRequest, $"Cannot add file to playlist!");

                }
                else
                {
                    // return response if success
                    responseMsg = Request.CreateResponse(HttpStatusCode.OK, $"File successfully added to playlist!");
                }
                return responseMsg;
            }
            catch (Exception e)
            {
                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new FileUploadException(e.Message, e);
            }
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage DeleteFile(Guid fileId)
        {
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();

                CFile file = _fileContext.GetByFileId(fileId);

                // if file exist - delete it, if not continue to delete DB record cuz user wanted to delete real file anyway
                if (File.Exists(file.Path + file.Name))
                {
                    File.Delete(file.Path + file.Name);
                }

                // delete record from DB
                if (_fileContext.Delete(fileId) > 0)
                {
                    Request.CreateResponse(HttpStatusCode.OK, $"File {file.Name} has been deleted!");
                    return response;
                }

                Request.CreateResponse(HttpStatusCode.BadRequest, $"Error: File {file.Name} has not been deleted!");
                return response;
            }
            catch (ContextException e)
            {
                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage RenameFile(Guid fileId, string newName)
        {
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                CFile file = _fileContext.GetByFileId(fileId);

                if (File.Exists(file.Path + file.Name))
                {
                    // try rename physical file
                    File.Move(file.Path + file.Name, file.Path + newName);


                    if (File.Exists(Path.Combine(file.Path, "Thumbnails", Path.ChangeExtension(file.Name, ".jpg"))))
                    {
                        // try rename physical thumbnail of the file
                        File.Move(Path.Combine(file.Path,"Thumbnails", Path.ChangeExtension(file.Name, ".jpg")), Path.Combine(file.Path, "Thumbnails", Path.ChangeExtension(newName, ".jpg")));
                    }

                        // try rename record in DB
                        file.Name = newName;
                    if (_fileContext.Update(file) > 0)
                    {
                        Request.CreateResponse(HttpStatusCode.OK, $"File has been renamed.");
                        return response;
                    }

                }
                Request.CreateResponse(HttpStatusCode.BadRequest, $"Error: File cannot be renamed.");
                return response;
            }
            catch (ContextException e)
            {
                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }
        }

        [HttpPost]
        [Authorize]
        public HttpResponseMessage LikeFile(CFileInfo fileInfo)
        {
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                CFile file = _fileContext.GetByFileId(fileInfo.Guid);
                file.Likes += 1;

                if (_fileContext.Update(file) > 0)
                {
                    Request.CreateResponse(HttpStatusCode.OK, $"File has been Liked in Media Server.");
                    return response;
                }
                Request.CreateResponse(HttpStatusCode.BadRequest, $"Error: File cannot be updated.");
                return response;
            }
            catch (ContextException e)
            {
                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }
        }

        [HttpPost]
        [Authorize]
        public HttpResponseMessage DislikeFile(CFileInfo fileInfo)
        {
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                CFile file = _fileContext.GetByFileId(fileInfo.Guid);
                file.Dislikes += 1;

                if (_fileContext.Update(file) > 0)
                {
                    Request.CreateResponse(HttpStatusCode.OK, $"File has been Disliked in Media Server.");
                    return response;
                }
                Request.CreateResponse(HttpStatusCode.BadRequest, $"Error: File cannot be updated.");
                return response;
            }
            catch (ContextException e)
            {
                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }
        }

        [HttpGet]
        [Authorize]
        // Playlist GUIDs are unique for each user
        public HttpResponseMessage GetFilesFromPlaylist(Guid playlistId)
        {
            HttpResponseMessage response;

            List<CFileInfo> filesInfo = new List<CFileInfo>();
            try
            {
                List<CFile> filesInPlaylist = _fileContext.FilesFromPlaylist(playlistId).ToList();

                foreach (CFile file in filesInPlaylist)
                {
                    CFileInfo fileInfo = file.ToCFileInfo();
                    filesInfo.Add(fileInfo);
                }

                response = Request.CreateResponse(HttpStatusCode.OK, filesInfo);
                return response;

            }
            catch (Exception e)// in case program crashes?
            {

                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }
        }

        [HttpGet]
        [Authorize]
        // get all files for given user no matter what playlists
        public HttpResponseMessage GetUserFiles(Guid userId)
        {
            HttpResponseMessage response;

            List<CFileInfo> filesInfo = new List<CFileInfo>();
            try
            {
                List<CFile> files = _fileContext.GetByUserId(userId).ToList();

                foreach (CFile file in files)
                {
                    CFileInfo fileInfo = file.ToCFileInfo();
                    filesInfo.Add(fileInfo);
                }

                response = Request.CreateResponse(HttpStatusCode.OK, filesInfo);
                return response;

            }
            catch (Exception e)// in case program crashes?
            {

                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }
        }

        [HttpGet]
        [Authorize]
        // get all files for given user
        public HttpResponseMessage GetUserFiles(Guid userId, Guid playlistId)
        {
            HttpResponseMessage response;

            List<CFileInfo> filesInfo = new List<CFileInfo>();
            try
            {
                List<CFile> files = _fileContext.GetByPlaylistId(playlistId).ToList();

                foreach (CFile file in files)
                {
                    // do not return to user files that are not fully loaded
                    if (file.Hash != null)
                    {
                        CFileInfo fileInfo = file.ToCFileInfo();
                        filesInfo.Add(fileInfo);
                    }
                }

                response = Request.CreateResponse(HttpStatusCode.OK, filesInfo);
                return response;

            }
            catch (Exception e)// in case program crashes?
            {

                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }
        }

        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetFileThumbnail(Guid fileId)
        {
            try
            {
                String thumbnailPath;

                // get user credentials
                Tuple<string, string> credentials = HelperMethods.NamePasswordFromAuthHeader(Request.Headers.Authorization.Parameter);
                userName = credentials.Item1;
                user = _userContext.GetByName(userName);
                //user folder
                userFolder = HttpContext.Current.Server.MapPath($@"~/App_Data/UserFiles/{userName}/");

                CFile file = _fileContext.GetByFileId(fileId);


                // if file exist - delete it, if not continue to delete DB record cuz user wanted to delete real file anyway
                string thumbnailName = Path.ChangeExtension(file.Name, ".jpg");
                thumbnailPath = Path.Combine(userFolder, "Thumbnails", thumbnailName);

                if (!File.Exists(thumbnailPath))
                {
                    // in no such image exists - send NoImage.jpeg as response
                    thumbnailPath = HttpContext.Current.Server.MapPath($@"~/App_Data/UserFiles/Common/Thumbnails/NoImage.jpg");
                    //Request.CreateResponse(HttpStatusCode.BadRequest, $"Error: File {thumbnailName} has not been found!");
                }

                MemoryStream ms = new MemoryStream(File.ReadAllBytes(thumbnailPath));
                string extension = Path.GetExtension(thumbnailPath);

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(ms.ToArray());
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                return response;
            }
            catch (ContextException e)
            {
                HttpContext.Current.Response.StatusCode = (Int32)HttpStatusCode.BadRequest;
                throw new ContextException(e.Message, e);
            }
        }

        // get ImageFormat from file extension
        public static ImageFormat GetImageFormat(string extension)
        {
            ImageFormat result = null;
            PropertyInfo prop = typeof(ImageFormat).GetProperties().Where(p => p.Name.Equals(extension, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (prop != null)
            {
                result = prop.GetValue(prop) as ImageFormat;
            }
            return result;
        }

        #endregion



        #region Not used a.t.m.
        // list files from physical directory of the user
        [Obsolete]
        private IEnumerable<FileInfo> ListUserFiles(Int32 userId)
        {
            if (userId < 0)
            {
                throw new ArgumentException("UserId cannot be negative!");
            }

            DirectoryInfo dir;

            if (userId == 0) //this is reference to 'common' user - get files from folder 'Videos'
            {
                dir = new DirectoryInfo(HttpContext.Current.Server.MapPath("~/Videos"));
            }
            else // this is for specific user
            {
                dir = new DirectoryInfo(HttpContext.Current.Server.MapPath("~/Videos/user") + userId);
            }

            FileInfo[] userFiles = dir.GetFiles();
            return userFiles;
        }
        #endregion


    }
}
