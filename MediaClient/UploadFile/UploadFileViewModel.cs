using DTO;
using Hashing;
using MediaClient.Exceptions;
using MediaClient.WebApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace MediaClient
{
    class UploadFileViewModel : FrameworkElement
    {

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #region Variables
        private CUserInfo _userInfo;
        public CUserInfo UserInfo { get; set; }

        FileCatalogViewModel _fcvm;

        public ObservableCollection<CPlaylistInfo> PlaylistsObservable { get; set; }

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

        // After browse save here file details
        FileInfo uploadFileInfo;


        //----------------------------------------------------
        public static readonly DependencyProperty FileNameProperty
            = DependencyProperty.Register("FileName", typeof(String), typeof(UploadFileViewModel));

        public String FileName
        {
            get
            {
                return (String)GetValue(FileNameProperty);
            }
            set
            {
                SetValue(FileNameProperty, value);
            }
        }

        //----------------------------------------------------
        public static readonly DependencyProperty FileCatalogProperty
            = DependencyProperty.Register("FileCatalog", typeof(String), typeof(UploadFileViewModel));

        public String FileCatalog
        {
            get { return (String)GetValue(FileCatalogProperty); }
            set
            {
                SetValue(FileCatalogProperty, value);
            }
        }
        //----------------------------------------------------
        public static readonly DependencyProperty FileSizeProperty
            = DependencyProperty.Register("FileSize", typeof(String), typeof(UploadFileViewModel));

        public String FileSize
        {
            get { return (String)GetValue(FileSizeProperty); }
            set
            {
                SetValue(FileSizeProperty, value);
            }
        }
        //----------------------------------------------------

        private Boolean _isPublicFile;
        public Boolean IsPublicFile
        {
            get { return _isPublicFile; }
            set
            {
                _isPublicFile = value;
                OnPropertyChanged("IsPublicFile");
            }
        }


        // list of user playlists
        List<CPlaylistInfo> _userPlaylists = new List<CPlaylistInfo>();
        public List<CPlaylistInfo> UserPlaylists { get => _userPlaylists; set => _userPlaylists = value; }
        #endregion

        //--------------------------------------------------------------------
        private StatusUpdate _statusUpdate;

        //--------------------------------------------------------------------

        #region CTORs
        public UploadFileViewModel(IEnumerable<CPlaylistInfo> playlists, CUserInfo userInfo, StatusUpdate statusUpdate)
        {
            _statusUpdate = statusUpdate;

            UserInfo = new CUserInfo(userInfo.Id, userInfo.Name, userInfo.Email, userInfo.Password);

            PlaylistsObservable = new ObservableCollection<CPlaylistInfo>(playlists);

            BrowseCommand = new RelayCommand(new Action<object>(Browse));
            UploadCommand = new RelayCommand(new Action<object>(Upload));

        }
        #endregion

        //--------------------------------------------------------------------
        private ICommand _browseCommand;
        public ICommand BrowseCommand { get => _browseCommand; set => _browseCommand = value; }
        private void Browse(object obj)
        {
            try
            {
                // upload file to Media Server
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.InitialDirectory = UserSettings.UserFolder;
                DialogResult dialogResult = openFileDialog.ShowDialog();
                String path = openFileDialog.FileName;

                uploadFileInfo = new FileInfo(path);

                FileName = uploadFileInfo.Name;
                FileCatalog = uploadFileInfo.DirectoryName;
                double sizeInMB= ((double)uploadFileInfo.Length)/1024/1024;
                FileSize = String.Format($"{sizeInMB:0.00} Mb");

                // switching back to the FileCatalogPage
                //sc.Post(delegate
                //{
                //}, null);

            }

            catch (WebApiException ex)
            {
                throw;
            }
        }
        //--------------------------------------------------------------------
        private ICommand _uploadCommand;
        public ICommand UploadCommand { get => _uploadCommand; set => _uploadCommand = value; }

        void Upload(object obj)
        {
            try
            {
                CancellationToken ct = new CancellationToken();
                var sc = SynchronizationContext.Current;

                // uploading file asyncronously
                Task.Factory.StartNew(() =>
                {
                    UploadFileInChunksAsync(uploadFileInfo, IsPublicFile, UserInfo, ct);
                }).ContinueWith(delegate
                   {

                       System.Windows.MessageBox.Show($"File: {uploadFileInfo.Name} successfully uploaded");

                       _fcvm = new FileCatalogViewModel(UserInfo, _statusUpdate);

                       // switching back to the FileCatalogPage
                       sc.Post(delegate
                       {
                           _statusUpdate.Progress = 0;
                           FileCatalogPage fcp = new FileCatalogPage();
                           fcp.DataContext = _fcvm;
                           Switcher.Switch(fcp);
                       }, null);
                   });

            }
            catch (WebApiException ex)
            {
                throw;
            }
        }


        FilesWebApi _filesWebApi = new FilesWebApi();
        // Call web api function to upload file in small chunks (to overcome 2GB limit on IIS)
        internal Boolean UploadFileInChunksAsync(FileInfo fileInfo, bool isPublic, CUserInfo userInfo, CancellationToken ct)
        {
            try
            {
                // response from Media SErver
                string responseMsg = "";

                // generate hash of the file
                // todo: make it multithreaded
                //Console.WriteLine("Calculating file hash...");
                byte[] hash = MD5Generator.MD5Hash(fileInfo.FullName);
                //Console.WriteLine($"{fileInfo.Name} hash = {BitConverter.ToString(hash)}");


                // set DTO values
                CFileInfo cFileInfo = new CFileInfo(
                    Guid.Empty, fileInfo.Name, "", fileInfo.Length, userInfo.Id, isPublic, hash, DateTime.Now, 0, 0, 0);

                // get loadIndex from server (if file was not loaded completely. If new file size==0)
                CFileInfo fileInfoFromMediaServer = _filesWebApi.FileDetails(cFileInfo, userInfo).Result;

                Int64 loadIndex = fileInfoFromMediaServer.Size;

                if (loadIndex >= fileInfo.Length)
                {
                    //Console.WriteLine("This file is fully uploaded");
                    return false;
                }

                // how much bytes to send to server each time
                Int32 chunkSize = UserSettings.ChunkSize;
                ////_statusUpdate.Max = fileInfo.Length;

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

                        Int32 readBytes = fs.ReadAsync(chunk, 0, chunk.Length).Result;

                        // if chunk is uploaded successfully - server returns 'OK'
                        response = _filesWebApi.UploadChunkAsync(cFileInfo, userInfo, chunk, isLastChunk).Result;
                        responseMsg = response.Content.ReadAsStringAsync().Result;
                        double percentage = ((double)fs.Position / (double)fs.Length) * 100;

                        // update progress bar value
                        _statusUpdate.Progress = percentage;
                        // update status bar value
                        _statusUpdate.Message = "Uploading...";

                    }
                }
                // update status bar value
                _statusUpdate.Message = "Done uploading!";
                return true;
            }
            catch (Exception ex)
            {

                throw new FileUploadException($"Could not upload file {fileInfo.Name} to MediaServer!", ex);
            }



        }
    }
}
