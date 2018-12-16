using DTO;
using MediaClient.Exceptions;
using MediaClient.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MediaClient
{
    public class WatchVideoViewModel : INotifyPropertyChanged
    {
        #region Variables
        private CUserInfo _userInfo;
        private CFileInfo _fileInfo;
        private Uri _videoSource;// video location to play
        private String _userFolder;
        private FileCatalogViewModel _fcvm;
        public CUserInfo UserInfo
        {
            get { return _userInfo; }
            set
            {
                _userInfo = value;
                OnPropertyChanged("UserInfo");
            }
        }
        public CFileInfo FileInfo
        {
            get { return _fileInfo; }
            set
            {
                _fileInfo = value;
                OnPropertyChanged("FileInfo");
            }
        }
        public Uri VideoSource
        {
            get { return _videoSource; }
            set
            {
                _videoSource = value;
                OnPropertyChanged("VideoSouce");
            }
        }

        #endregion


        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }


        #region EventHandlersForMediaElement
        public event EventHandler PlayRequested;
        public event EventHandler PauseRequested;
        public event EventHandler StopRequested;
        #endregion



        #region ICommand

        private ICommand _watchCommand;
        public ICommand PlayCommand { get => _watchCommand; set => _watchCommand = value; }

        private void Play(object obj)
        {
            var sc = SynchronizationContext.Current;

            sc.Post(delegate
            {
                if (this.PlayRequested != null)
                {
                    this.PlayRequested(this, EventArgs.Empty);
                }
            }, null);
        }

        private ICommand _pauseCommand;
        public ICommand PauseCommand { get => _pauseCommand; set => _pauseCommand = value; }

        private void Pause(object obj)
        {
            var sc = SynchronizationContext.Current;

            sc.Post(delegate
            {
                if (this.PauseRequested != null)
                {
                    this.PauseRequested(this, EventArgs.Empty);
                }
            }, null);
        }

        private ICommand _stopCommand;
        public ICommand StopCommand { get => _stopCommand; set => _stopCommand = value; }

        private void Stop(object obj)
        {
            var sc = SynchronizationContext.Current;

            sc.Post(delegate
            {
                if (this.StopRequested != null)
                {
                    this.StopRequested(this, EventArgs.Empty);
                }

                FileCatalogPage fcp = new FileCatalogPage();
                fcp.DataContext = _fcvm;
                Switcher.Switch(fcp);
            }, null);
        }

       
        #endregion

        #region CTORs
        public WatchVideoViewModel(CUserInfo userInfo, CFileInfo fileInfo, FileCatalogViewModel fcvm)
        {
            _fcvm = fcvm;
            _userInfo = userInfo;
            _fileInfo = fileInfo;
            _userFolder = @"C:\Users\dmitry.martirosyan\Source\Workspaces\martirosyan.workspace\MediaServer\MediaClient\UserFiles";

            VideoSource = new Uri("http://localhost:86/" + $"api/file/PlayAsync?fileId={fileInfo.Guid}");// test on IIS
            //VideoSource = new Uri("http://localhost:50352/" + $"api/file/PlayAsync?fileId={fileInfo.Guid}");// test on IIS Express
            //VideoSource = new Uri(Path.Combine(_userFolder, _fileInfo.Name), UriKind.Absolute);

            PlayCommand = new RelayCommand(new Action<object>(Play));
            PauseCommand = new RelayCommand(new Action<object>(Pause));
            StopCommand = new RelayCommand(new Action<object>(Stop));

            // call WatchVideo method, which should return stream data to some property like VideoStream,
            // which will be binded to MediaElement source
        }
        #endregion


       

    }
}
