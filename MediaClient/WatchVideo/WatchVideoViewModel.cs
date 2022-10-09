using DTO;
using MediaClient.WatchVideo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MediaClient
{
    public class WatchVideoViewModel : FrameworkElement, INotifyPropertyChanged
    {
        #region Variables

        private CUserInfo _userInfo;
        private CFileInfo _fileInfo;
        private Uri _videoSource; // video location to play
        private String _userFolder;
        private FileCatalogViewModel _fileCatalogViewModel;
        private TimeSpan _streamPosition;
        private static Double _seekPosition;
        private Boolean _sliderDragEnd;
        private StatusUpdate _statusUpdate; // link to status bar to show progress messages
        private CancellationToken ct;
        public MediaElement myMedia;

        private Queue<FileInfo> watchQueue; // concurrent queue for chunks of the video to play
        private VideoWatcher _videoWatcher;
        private readonly CancellationTokenSource _cts;

        //----------------------------------------------------
        public static readonly DependencyProperty SeekPositionProperty
            = DependencyProperty.Register("SeekPosition", typeof(Double), typeof(WatchVideoViewModel));

        public Double SeekPosition
        {
            get { return (Double)GetValue(SeekPositionProperty); }
            set { SetValue(SeekPositionProperty, value); }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
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

        public ICommand PlayCommand { get; set; }
        public ICommand PauseCommand { get; set; }
        public ICommand StopCommand { get; set; }

        #region CTORs

        public WatchVideoViewModel(
            CUserInfo userInfo, 
            CFileInfo fileInfo, 
            FileCatalogViewModel fileCatalogViewModel,
            StatusUpdate statusUpdate)
        {
            _cts = new CancellationTokenSource();

            _fileCatalogViewModel = fileCatalogViewModel;
            _userInfo = userInfo;
            _fileInfo = fileInfo;
            _statusUpdate = statusUpdate;

            watchQueue = new Queue<FileInfo>();
            PlayCommand = new RelayCommand(new Action<object>(Play));
            PauseCommand = new RelayCommand(new Action<object>(Pause));
            StopCommand = new RelayCommand(new Action<object>(Stop));
        }
        #endregion

        private void Play(object obj)
        {
            Writer writer = new Writer(_fileInfo, _userInfo, watchQueue, _statusUpdate);
            Int64 startPosition = (Int64)((SeekPosition / 100) * _fileInfo.Size);
            Reader reader = new Reader(myMedia, watchQueue);

            var sc = SynchronizationContext.Current;
            _videoWatcher = new VideoWatcher(myMedia, writer, reader, _cts, sc);
            Task.Run(()=>_videoWatcher.Play(startPosition), _cts.Token);
        }

        private void Pause(object obj)
        {
            _videoWatcher.Pause();
        }

        private void Stop(object obj)
        {
            _cts.Cancel(); // to stop watcher/writer/reader
            _videoWatcher.Stop();
            
            FileCatalogPage fcp = new FileCatalogPage();
            fcp.DataContext = _fileCatalogViewModel;
            Switcher.Switch(fcp);
        }

        public void SliderDragEnded()
        {
            // Seek position already set by Dependency property
            //Stop the media
            myMedia.Stop();
            // Clear the queue
            watchQueue.Clear();
            // Stop writer & reader threads
            _cts.Cancel();
            _cts.Dispose();

            // Start from new position
            Play(null);
        }
        #endregion
    }
}