using DTO;
using System;
using System.Net.Http;
using ConsoleClient.WebApi;
using System.IO;
using Hashing;
using System.Threading;
using System.Threading.Tasks;
using ConsoleClient.Exceptions;
using System.Collections.Generic;

namespace ConsoleClient
{
    static class ActionsOnFiles
    {
        static FilesWebApi filesWebApi = new FilesWebApi();


        // Get fileName to be used in following actions
        internal static FileInfo GetFileInfo()
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(UserSettings.UserFolder);
                Console.WriteLine("Enter file number:");
                HelperMethods.PrintFilesInDirectory(directoryInfo);
                int number = 0;
                bool ok = false;
                do
                {
                 ok=Int32.TryParse(Console.ReadLine(), out number);
                    if (!ok)
                    {
                        Console.WriteLine("cannot parse this number. Try again? Y/N");
                        string answer = Console.ReadLine();
                        if (answer.Equals("n", StringComparison.InvariantCultureIgnoreCase))
                        {
                            return null;
                        }
                    }
                } while (!ok);
                FileInfo[] files = directoryInfo.GetFiles();
                return files[number];
            }
            catch (Exception ex)
            {

                throw new FileNotFoundException("Cant open such file", ex);
            }
        }

        // Call web api function to upload file from the client
        internal static void UploadFileAsync(FileInfo fileInfo, bool isPublic, CUserInfo userInfo)
        {
            byte[] hash = MD5Generator.MD5Hash(fileInfo.FullName);
            CFileInfo cFileInfo = new CFileInfo(
                Guid.Empty, fileInfo.Name, fileInfo.FullName, fileInfo.Length, userInfo.Id, isPublic, hash, DateTime.Now, 0, 0, 0);
            HttpResponseMessage response = filesWebApi.UploadFileAsync(cFileInfo, userInfo).Result;

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"File {cFileInfo.Name} has been successfully uploaded to MediaServer!");
            }
            else
            {
                Console.WriteLine($"File {cFileInfo.Name} couldn't be uploaded to MediaServer! " +
                    $"Response content:{response.ReasonPhrase}");
            }

        }


        // Call web api function to upload file in small chunks (to overcome 2GB limit on IIS)
        internal static async void UploadFileInChunksAsync(FileInfo fileInfo, bool isPublic, CUserInfo userInfo, CancellationToken ct)
        {
            try
            {
                // response from Media SErver
                string responseMsg = "";

                // generate hash of the file
                // todo: make it multithreaded
                Console.WriteLine("Calculating file hash...");
                byte[] hash = MD5Generator.MD5Hash(fileInfo.FullName);
                Console.WriteLine($"{fileInfo.Name} hash = {BitConverter.ToString(hash)}");


                // set DTO values
                CFileInfo cFileInfo = new CFileInfo(
                    Guid.Empty, fileInfo.Name, "", fileInfo.Length, userInfo.Id, isPublic, hash, DateTime.Now, 0, 0, 0);

                // get loadIndex from server (if file was not loaded completely. If new file size==0)
                CFileInfo fileInfoFromMediaServer = filesWebApi.FileDetails(cFileInfo, userInfo).Result;
                Int64 loadIndex = fileInfoFromMediaServer.Size;

                if (loadIndex >= fileInfo.Length)
                {
                    Console.WriteLine("This file is fully uploaded");
                    return;
                }

                // how much bytes to send to server each time
                Int32 chunkSize = UserSettings.ChunkSize;

                FileStream fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, chunkSize);
                using (fs)
                {
                    // set cursor of the filestream to the loadIndex
                    fs.Seek(loadIndex, SeekOrigin.Begin);

                    HttpResponseMessage response = new HttpResponseMessage();
                    response.StatusCode = System.Net.HttpStatusCode.OK;

                    Boolean isLastChunk = false;
                    byte[] chunk;

                    // read file to the end and send it to server in chunks
                    while (fs.Position < fs.Length & response.IsSuccessStatusCode)
                    {
                        Int64 remaining = fs.Length - fs.Position;
                        if (remaining <= chunkSize)
                        {
                            isLastChunk = true;
                            chunk = new byte[remaining];
                        }
                        else
                        {
                            chunk = new byte[chunkSize];
                        }

                        Int32 readBytes = await fs.ReadAsync(chunk, 0, chunk.Length);

                        // if chunk is uploaded successfully - server returns 'OK'
                        response = filesWebApi.UploadChunkAsync(cFileInfo, userInfo, chunk, isLastChunk).Result;
                        responseMsg = response.Content.ReadAsStringAsync().Result;
                        double percentage = ((double)fs.Position / (double)fs.Length) * 100;
                        Console.Write($"\rUpload progress: {percentage:0.##}%");

                    }
                }
                Console.WriteLine(responseMsg);
            }
            catch (Exception ex)
            {

                throw new FileUploadException($"Could not upload file {fileInfo.Name} to MediaServer!", ex);
            }

        }

        // Rename the file in Media Server
        internal static async void RenameFileAsync(CFileInfo fileInfo, String newName, CUserInfo userInfo)
        {
            try
            {
                string oldName = fileInfo.Name;
                HttpResponseMessage response = await filesWebApi.RenameFileAsync(fileInfo.Guid, newName, userInfo);

                string msg = "";
                if (response.IsSuccessStatusCode)
                {
                    msg = $"File {oldName} has been successfully renamed to {newName}!";
                }
                else
                {
                    msg = $"Error: File {oldName} has not been renamed to {newName}!";
                }
                Console.WriteLine(msg);

            }
            catch (Exception ex)
            {

                throw new WebApiException($"File {fileInfo.Name} was not renamed!", ex);
            }
        }

        // Delete file from Media Server
        internal static async void DeleteFileAsync(CFileInfo fileInfo, CUserInfo userInfo)
        {
            try
            {
                HttpResponseMessage response = await filesWebApi.DeleteFileAsync(fileInfo.Guid, userInfo);

                string msg = "";
                if (response.IsSuccessStatusCode)
                {
                    msg = $"File {fileInfo.Name} has been successfully deleted from Media Server!";
                }
                else
                {
                    msg = $"Error: File {fileInfo.Name} has not been deleted from Media Server!";

                }
                Console.WriteLine(msg);

            }
            catch (Exception ex)
            {
                throw new WebApiException($"File {fileInfo.Name} was not deleted!", ex);
            }
        }


        // doesn't work
        internal static async void DownloadFileAsync(CFileInfo fileInfo, CUserInfo userInfo, CancellationToken ct)
        {
            try
            {
                HttpResponseMessage response = filesWebApi.DownloadFileAsync(fileInfo, userInfo).Result;
                FileStream fs = new FileStream(fileInfo.Path + fileInfo.Name, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write, UserSettings.ChunkSize, true);
                using (fs)
                {
                    await response.Content.CopyToAsync(fs);
                }
                Console.WriteLine($"File {fileInfo.Name} has been successfully downloaded!");

            }
            catch (Exception ex)
            {

                throw new WebApiException($"File {fileInfo.Name} was not downloaded!", ex);
            }
        }

        // Downloading file from server in chunks
        internal static async void DownloadFileInChunksAsync(CFileInfo fileInfoMediaServer, CUserInfo userInfo, CancellationToken ct)
        {
            try
            {
                // how much bytes to send to server each time
                Int32 chunkSize = UserSettings.ChunkSize;

                // copy all fields from fileInfoMediaServer
                CFileInfo fileInfo = new CFileInfo(fileInfoMediaServer);
                // update path
                fileInfo.Path = UserSettings.UserFolder;

                FileStream fs = new FileStream(fileInfo.Path + fileInfo.Name, FileMode.Append, FileAccess.Write, FileShare.Read, chunkSize);
                using (fs)
                {
                    HttpResponseMessage response = new HttpResponseMessage();
                    response.StatusCode = System.Net.HttpStatusCode.OK;

                    byte[] chunk;
                    bool lastChunk = false;
                    // while not all bytes received
                    while (fs.Position < fileInfoMediaServer.Size & response.IsSuccessStatusCode)
                    {
                        Int64 remaining = fileInfoMediaServer.Size - fs.Position;
                        if (remaining <= chunkSize)
                        {
                            chunk = new byte[remaining];
                            lastChunk = true;
                        }
                        else
                        {
                            chunk = new byte[chunkSize];
                        }

                        // update fileInfo.Size to match current file stream position
                        fileInfo.Size = fs.Position;

                        // request next chunk of data
                        response = filesWebApi.DownloadChunkAsync(fileInfo, userInfo, chunk, lastChunk).Result;

                        double percentage = 0;
                        if (response.IsSuccessStatusCode)
                        {
                            await response.Content.CopyToAsync(fs);
                            percentage = ((double)fs.Position / (double)fileInfoMediaServer.Size) * 100;
                            Console.Write($"\rDownload progress: {percentage:0.##}%");
                        }
                        else
                        {
                            Console.Write($"\rDownload chunk failed on: {percentage:0.##}%");
                            return;
                        }

                    }
                }
                Console.WriteLine($"File {fileInfo.Name} is successfully downloaded from Media Server!");
            }
            catch (Exception ex)
            {

                throw new FileUploadException($"Could not download file {fileInfoMediaServer.Name} from MediaServer!", ex);
            }

        }


        internal static async Task<List<CFileInfo>> GetUserFiles(CUserInfo userInfo)
        {
            try
            {
                List<CFileInfo> userFiles = new List<CFileInfo>();
                HttpResponseMessage response = filesWebApi.GetUserFilesAsync(userInfo).Result;

                if (response.IsSuccessStatusCode)
                {
                    userFiles = await response.Content.ReadAsAsync<List<CFileInfo>>();
                    return userFiles;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new WebApiException("Couldn't load user files!", ex);
            }

        }

    }
}
