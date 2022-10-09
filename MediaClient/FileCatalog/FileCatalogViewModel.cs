using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using MediaClient.Models;
using DTO;
using System.Net.Http;
using MediaClient.Exceptions;
using MediaClient.WebApi;
using System.Windows.Input;
using System.Threading;
using System.IO;
using Hashing;
using Microsoft.Win32;

namespace MediaClient
{
    public class FileCatalogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #region Variables
        private StatusUpdate _statusUpdate;
        public ObservableCollection<CFileInfo> FilesObservable { get; set; }

        public ObservableCollection<CPlaylistInfo> PlaylistsObservable { get; set; }


        // file selected by user
        private CFileInfo _selectedFile;
        public CFileInfo SelectedFile
        {
            get { return _selectedFile; }
            set
            {
                _selectedFile = value;
                OnPropertyChanged("SelectedFile");
            }
        }

        // playlist selected by user
        private CPlaylistInfo _selectedPlaylist;
        public CPlaylistInfo SelectedPlaylist
        {
            get { return _selectedPlaylist; }
            set
            {
                _selectedPlaylist = value;
                OnPropertyChanged("SelectedPlaylist");
            }
        }

        // list of user playlists
        List<CPlaylistInfo> _userPlaylists = new List<CPlaylistInfo>();
        public List<CPlaylistInfo> UserPlaylists { get => _userPlaylists; set => _userPlaylists = value; }

        // list of user files within selected playlist
        List<CFileInfo> _userFiles = new List<CFileInfo>();
        public List<CFileInfo> UserFiles { get => _userFiles; set => _userFiles = value; }

        public CUserInfo UserInfo { get; set; }
        #endregion

        // Load user files from Media Server
        public void LoadUserFiles(string playlistName)
        {
            // get user playlists
            UserPlaylists = GetUserPlaylists(UserInfo).Result;

            // create observable collection from List<...>
            PlaylistsObservable = new ObservableCollection<CPlaylistInfo>(UserPlaylists);


            // get user files from the default playlist
            UserFiles = GetUserFiles(UserInfo, UserPlaylists.
                Where(p => p.Name.Equals(playlistName, StringComparison.InvariantCultureIgnoreCase)).First()).Result;

            // create observable collection from List<...>
            FilesObservable = new ObservableCollection<CFileInfo>(UserFiles);
            SelectedFile = FilesObservable.FirstOrDefault();

            // get file thumbnails for each user file from FilesObservable collection
            GetFileThumbnails();
        }

        #region ICommand
        //--------------------------------------------------------------------
        private ICommand _refreshCommand;
        public ICommand RefreshCommand { get => _refreshCommand; set => _refreshCommand = value; }
        private void Refresh(object obj)
        {
            try
            {
                var sc = SynchronizationContext.Current;
               
                    sc.Post(delegate
                    {
                        Task refreshTask = new Task(delegate { LoadUserFiles("default"); });
                        refreshTask.Start();
                        refreshTask.Wait();
                    }, null);
            }

            catch (WebApiException ex)
            {

                throw;
            }
        }
        //--------------------------------------------------------------------
        private ICommand _watchCommand;
        public ICommand WatchCommand { get => _watchCommand; set => _watchCommand = value; }
        private void Watch(object obj)
        {
            SelectedFile.Views += 1;

            var sc = SynchronizationContext.Current;
            WatchVideoViewModel wvvm = new WatchVideoViewModel(UserInfo, SelectedFile, this, _statusUpdate);

            sc.Post(delegate
            {
                    //System.Windows.MessageBox.Show(_userInfo.ToString());
                    WatchVideoPage watchVideoPage = new WatchVideoPage(wvvm);
                Switcher.Switch(watchVideoPage);
            }, null);

        }
        //--------------------------------------------------------------------
        private ICommand _renameCommand;
        public ICommand RenameCommand { get => _renameCommand; set => _renameCommand = value; }
        private void Rename(object obj)
        {
            try
            {

                var sc = SynchronizationContext.Current;

                // rename file on Media Server
                Task<Boolean> renameTask = RenameFileAsync(SelectedFile, SelectedFile.Name, UserInfo);

                renameTask.ContinueWith(delegate
                {
                    sc.Post(delegate
                    {
                        //System.Windows.MessageBox.Show($"File: {SelectedFile.Name} successfully renamed");
                        _statusUpdate.Message = "FILE RENAMED";
                    }, null);
                });
            }

            catch (WebApiException ex)
            {

                throw;
            }
        }
        //--------------------------------------------------------------------
        private ICommand _likeCommand;
        public ICommand LikeCommand { get => _likeCommand; set => _likeCommand = value; }
        private void Like(object obj)
        {
            try
            {
                SelectedFile.Likes += 1;

                var sc = SynchronizationContext.Current;

                // rename file on Media Server
                Task<Boolean> likeTask = LikeFileAsync(SelectedFile, UserInfo);

                likeTask.ContinueWith(delegate
                {
                    sc.Post(delegate
                    {
                        //System.Windows.MessageBox.Show($"File: {SelectedFile.Name} successfully liked");
                        _statusUpdate.Message = "LIKE ADDED";
                    }, null);
                });
            }

            catch (WebApiException ex)
            {

                throw;
            }
        }
        //--------------------------------------------------------------------
        private ICommand _dislikeCommand;
        public ICommand DislikeCommand { get => _dislikeCommand; set => _dislikeCommand = value; }
        private void Dislike(object obj)
        {
            try
            {
                SelectedFile.Dislikes += 1;

                var sc = SynchronizationContext.Current;

                // rename file on Media Server
                Task<Boolean> likeTask = DislikeFileAsync(SelectedFile, UserInfo);

                likeTask.ContinueWith(delegate
                {
                    sc.Post(delegate
                    {
                        //System.Windows.MessageBox.Show($"File: {SelectedFile.Name} successfully disliked");
                        _statusUpdate.Message = "DISLIKE ADDED";
                    }, null);
                });
            }

            catch (WebApiException ex)
            {

                throw;
            }
        }
        //--------------------------------------------------------------------
        private ICommand _deleteCommand;
        public ICommand DeleteCommand { get => _deleteCommand; set => _deleteCommand = value; }
        private void Delete(object obj)
        {
            try
            {
                if (SelectedFile == null)
                {
                    return;
                }
                var sc = SynchronizationContext.Current;

                // delete file from Media Server
                Task<Boolean> deleteTask = DeleteFileAsync(SelectedFile, UserInfo);
                deleteTask.ContinueWith(delegate
                {
                    sc.Post(delegate
                    {
                        System.Windows.MessageBox.Show($"File: {SelectedFile.Name} successfully deleted");
                        // update listbox by removing object from collection
                        FilesObservable.Remove(SelectedFile);
                    }, null);
                });
            }
            catch (WebApiException ex)
            {

                throw;
            }
        }
        //--------------------------------------------------------------------
        private ICommand _uploadCommand;
        public ICommand UploadCommand { get => _uploadCommand; set => _uploadCommand = value; }
        private void Upload(object obj)
        {
            try
            {
                var sc = SynchronizationContext.Current;

                UploadFileViewModel uploadFileVM = new UploadFileViewModel(UserPlaylists, UserInfo, _statusUpdate);
                sc.Post(delegate
                {
                    UploadFilePage uploadFilePage = new UploadFilePage();
                    uploadFilePage.DataContext = uploadFileVM;
                    Switcher.Switch(uploadFilePage);
                }, null);
            }
            catch (WebApiException ex)
            {

                throw;
            }
        }
        //--------------------------------------------------------------------
        private ICommand _downloadCommand;
        public ICommand DownloadCommand { get => _downloadCommand; set => _downloadCommand = value; }
        private void Download(object obj)
        {
            try
            {
                CancellationToken ct = new CancellationToken();
                var sc = SynchronizationContext.Current;


                Task<Boolean> downloadTask = DownloadFileInChunksAsync(SelectedFile, UserInfo, ct);
                downloadTask.ContinueWith(delegate
                {
                    sc.Post(delegate
                    {
                        System.Windows.MessageBox.Show($"File: {SelectedFile.Name} successfully uploaded");
                    }, null);

                });
            }
            catch (WebApiException ex)
            {

                throw;
            }
        }
        //--------------------------------------------------------------------
        #endregion

        #region CTORs
        public FileCatalogViewModel(CUserInfo userInfo, StatusUpdate statusUpdate)
        {
            _statusUpdate = statusUpdate;

            UserInfo = new CUserInfo(userInfo.Id, userInfo.Name, userInfo.Email, userInfo.Password);
            LoadUserFiles("default");

            WatchCommand = new RelayCommand(new Action<object>(Watch));
            RenameCommand = new RelayCommand(new Action<object>(Rename));
            DeleteCommand = new RelayCommand(new Action<object>(Delete));
            UploadCommand = new RelayCommand(new Action<object>(Upload));
            DownloadCommand = new RelayCommand(new Action<object>(Download));
            LikeCommand = new RelayCommand(new Action<object>(Like));
            DislikeCommand = new RelayCommand(new Action<object>(Dislike));
            RefreshCommand = new RelayCommand(new Action<object>(Refresh));
        }
        #endregion

        #region WebApiMethods

        PlaylistWebApi _playlistWebApi = new PlaylistWebApi();
        internal async Task<List<CPlaylistInfo>> GetUserPlaylists(CUserInfo userInfo)
        {
            try
            {
                List<CPlaylistInfo> userPlaylists = new List<CPlaylistInfo>();
                HttpResponseMessage response = _playlistWebApi.GetPlaylistsByUserId(userInfo).Result;

                if (response.IsSuccessStatusCode)
                {
                    userPlaylists = await response.Content.ReadAsAsync<List<CPlaylistInfo>>();
                    return userPlaylists;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new WebApiException("Couldn't load user files!", ex);
            }

        }

        FilesWebApi _filesWebApi = new FilesWebApi();
        internal async Task<List<CFileInfo>> GetUserFiles(CUserInfo userInfo, CPlaylistInfo playlistInfo)
        {
            try
            {
                List<CFileInfo> userFiles = new List<CFileInfo>();
                HttpResponseMessage response = _filesWebApi.FilesFromPlaylistAsync(userInfo, playlistInfo).Result;

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

        // Downloading file from server in chunks
        internal async Task<Boolean> DownloadFileInChunksAsync(CFileInfo fileInfoMediaServer, CUserInfo userInfo, CancellationToken ct)
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
                        response = _filesWebApi.DownloadChunkAsync(fileInfo, userInfo, chunk, lastChunk).Result;

                        double percentage = 0;
                        if (response.IsSuccessStatusCode)
                        {
                            await response.Content.CopyToAsync(fs);
                            percentage = ((double)fs.Position / (double)fileInfoMediaServer.Size) * 100;
                            //Console.Write($"\rDownload progress: {percentage:0.##}%");
                        }
                        else
                        {
                            //Console.Write($"\rDownload chunk failed on: {percentage:0.##}%");
                            return false;
                        }

                    }
                }
                //Console.WriteLine($"File {fileInfo.Name} is successfully downloaded from Media Server!");
                return true;
            }
            catch (Exception ex)
            {

                throw new FileUploadException($"Could not download file {fileInfoMediaServer.Name} from MediaServer!", ex);
            }

        }

        // Rename the file in Media Server
        internal async Task<Boolean> RenameFileAsync(CFileInfo fileInfo, String newName, CUserInfo userInfo)
        {
            try
            {
                string oldName = fileInfo.Name;
                HttpResponseMessage response = await _filesWebApi.RenameFileAsync(fileInfo.Guid, newName, userInfo);

                string msg = "";
                if (response.IsSuccessStatusCode)
                {
                    msg = $"File {oldName} has been successfully renamed to {newName}!";
                    return true;
                }
                else
                {
                    msg = $"Error: File {oldName} has not been renamed to {newName}!";
                    return false;
                }

            }
            catch (Exception ex)
            {

                throw new WebApiException($"File {fileInfo.Name} was not renamed!", ex);
            }
        }

        // Like file properties in Media Server
        internal async Task<Boolean> LikeFileAsync(CFileInfo fileInfo, CUserInfo userInfo)
        {
            try
            {
                string oldName = fileInfo.Name;
                HttpResponseMessage response = await _filesWebApi.LikeFileAsync(fileInfo, userInfo);

                string msg = "";
                msg = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {

                throw new WebApiException($"Error: File {fileInfo.Name} was not updated!", ex);
            }
        }

        // Dislike file in Media Server
        internal async Task<Boolean> DislikeFileAsync(CFileInfo fileInfo, CUserInfo userInfo)
        {
            try
            {
                string oldName = fileInfo.Name;
                HttpResponseMessage response = await _filesWebApi.DislikeFileAsync(fileInfo, userInfo);

                string msg = "";
                msg = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {

                throw new WebApiException($"Error: File {fileInfo.Name} was not updated!", ex);
            }
        }

        // Delete file from Media Server
        internal async Task<Boolean> DeleteFileAsync(CFileInfo fileInfo, CUserInfo userInfo)
        {
            try
            {
                HttpResponseMessage response = await _filesWebApi.DeleteFileAsync(fileInfo.Guid, userInfo);

                // Message can be returned to the caller and shown in status panel with last action.
                string msg = "";

                if (response.IsSuccessStatusCode)
                {
                    msg = $"File {fileInfo.Name} has been successfully deleted from Media Server!";
                    return true;
                }
                else
                {
                    msg = $"Error: File {fileInfo.Name} has not been deleted from Media Server!";
                    return false;
                }

            }
            catch (Exception ex)
            {
                throw new WebApiException($"File {fileInfo.Name} was not deleted!", ex);
            }
        }

        #endregion
        void GetFileThumbnails()
        {
            for (int i = 0; i < FilesObservable.Count; i++)
            {
                FilesObservable[i].Thumbnail = _filesWebApi.GetFileThumbnail(FilesObservable[i].Guid, UserInfo).Result;
            }
        }
    }
}
