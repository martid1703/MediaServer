using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DTO;
using MediaClient.Exceptions;
using MediaClient.WebApi;

namespace MediaClient.WatchVideo
{
    public class Writer
    {
        private CUserInfo _userInfo;
        private Queue<FileInfo> _watchQueue;
        private Int32 _queueMaxSize;
        private Int32 _maxChunks; // max chunks in the video file
        private Int32 _downloadedChunks;
        private CFileInfo _downloadFileInfo;
        private CFileInfo _fileInfoMediaServer;
        private StatusUpdate _statusUpdate; // link to status bar to show progress messages


        public Writer(CFileInfo fileInfoMediaServer, CUserInfo userInfo, Queue<FileInfo> watchQueue, StatusUpdate statusUpdate)
        {
            _fileInfoMediaServer = fileInfoMediaServer;
            _userInfo = userInfo;
            _watchQueue = watchQueue;
            _statusUpdate = statusUpdate;
            _queueMaxSize = 4;

            _maxChunks = (Int32)(fileInfoMediaServer.Size / UserSettings.ChunkSize);
            if (_maxChunks * UserSettings.ChunkSize < fileInfoMediaServer.Size)
            {
                _maxChunks++;
            }

            _downloadedChunks = 0;
            _downloadFileInfo = new CFileInfo(fileInfoMediaServer);
        }
        
        // Downloading file from server in chunks
        internal async Task DownloadVideo(Int64 startPosition, CancellationToken ct)
        {
            try
            {
                // update path
                _downloadFileInfo.Path = UserSettings.UserFolder;
                // set start size to continue watching from this position
                _downloadFileInfo.Size = startPosition;

                HttpResponseMessage response = new HttpResponseMessage();
                response.StatusCode = System.Net.HttpStatusCode.OK;

                bool lastChunk = false;
                // while not all bytes received
                
                string name = Path.GetFileNameWithoutExtension(_downloadFileInfo.Name);
                //name += "_" + _downloadedChunks;
                string ext = Path.GetExtension(_downloadFileInfo.Name);
                name += ext;
                
                // how much bytes to send to server each time
                //Int32 chunkSize = (Int32)_fileInfoMediaServer.Size;
                Int32 chunkSize = UserSettings.ChunkSize;


                while (_downloadFileInfo.Size < _fileInfoMediaServer.Size & response.IsSuccessStatusCode)
                {
                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    byte[] chunk;
                    // string name = Path.GetFileNameWithoutExtension(_downloadFileInfo.Name);
                    // name += "_" + _downloadedChunks;
                    // string ext = Path.GetExtension(_downloadFileInfo.Name);
                    // name += ext;

                    FileStream fs = new FileStream(name, FileMode.Append, FileAccess.Write, FileShare.Read, chunkSize);

                    using (fs)
                    {
                        Int64 remaining = _fileInfoMediaServer.Size - _downloadFileInfo.Size;
                        if (remaining <= chunkSize)
                        {
                            chunk = new byte[remaining];
                            lastChunk = true;
                        }
                        else
                        {
                            chunk = new byte[chunkSize];
                        }

                        // request next chunk of data
                        FilesWebApi filesWebApi = new FilesWebApi();
                        //response = await filesWebApi.DownloadFileAsync(_downloadFileInfo, _userInfo);
                        response = await filesWebApi.DownloadChunkAsync(_downloadFileInfo, _userInfo, chunk, lastChunk);

                        double percentage = 0;
                        if (response.IsSuccessStatusCode)
                        {
                            // update fileInfo.Size to match current file stream position
                            _downloadFileInfo.Size += chunkSize;
                            // copy chunk to file stream (separate file
                            await response.Content.CopyToAsync(fs);
                            // copy chunk to byte array
                            //chunk = await response.Content.ReadAsByteArrayAsync();
                            // enqueue chunk to Queue<FileInfo>
                            var fi = new FileInfo(name);
                            EnqueueChunk(fi);
                            // update downloaded chunks counter
                            _downloadedChunks++;

                            percentage = ((double)_downloadFileInfo.Size / (double)_fileInfoMediaServer.Size) * 100;

                            // update progress bar value
                            _statusUpdate.Progress = percentage;

                            // update status bar value
                            _statusUpdate.Message = "Downloading...";
                        }
                        else
                        {
                            // update status bar value
                            _statusUpdate.Message = "Download failed!";
                            return;
                        }
                    }
                }

                // update status bar value
                _statusUpdate.Message = "File is successfully downloaded from Media Server!";
            }
            catch (Exception ex)
            {
                throw new FileUploadException($"Could not download file {_fileInfoMediaServer.Name} from MediaServer!",
                    ex);
            }
        }

        //download one chunk
        private void EnqueueChunk(FileInfo fi)
        {
            try
            {
                lock (_watchQueue)
                {
                    while (_watchQueue.Count > _queueMaxSize)
                    {
                        Monitor.Wait(_watchQueue); //release lock
                    }

                    if (_downloadedChunks == _maxChunks)
                    {
                        _watchQueue.Enqueue(null);
                    }
                    else
                    {
                        _watchQueue.Enqueue(fi);
                    }

                    Monitor.Pulse(_watchQueue);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}