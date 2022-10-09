using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace MediaClient.WatchVideo
{
    public class VideoWatcher
    {
        private readonly MediaElement _mediaElement;
        private bool _isPlaying;
        private bool _isOnPause;
        private readonly Writer _writer;
        private readonly Reader _reader;
        private readonly CancellationTokenSource _cts;
        private readonly SynchronizationContext _sc;
        FileInfo _current;

        private static readonly log4net.ILog _log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public VideoWatcher(
            MediaElement media,
            Writer writer,
            Reader reader,
            CancellationTokenSource cts,
            SynchronizationContext sc)
        {
            _mediaElement = media;
            _writer = writer;
            _reader = reader;
            _cts = cts;
            _sc = sc;
            _mediaElement.MediaEnded += Element_MediaEnded;
        }

        // When the media playback is finished. Stop() the media to seek to media start.
        private void Element_MediaEnded(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        public void Play(long startPosition)
        {
            if (_isOnPause)
            {
                ResumePlay();
                return;
            }
            
            _writer.DownloadVideo(startPosition, _cts.Token);

            while (true)
            {
                if (_cts.IsCancellationRequested)
                {
                    Stop();
                    return;
                }

                var chunk = _reader.Read(_cts.Token);
                if (chunk == null)
                {
                    break;
                }

                if (_current != null && _current.Name == chunk.Name)
                {
                    continue;
                }

                _current = chunk;

                // if (_isPlaying)
                // {
                //     _log.Info($"Video timespan:{_mediaElement.Clock.Timeline.Duration.TimeSpan}");
                //     _log.Info($"Video position, ticks:{_mediaElement.Position.Ticks}");
                //     
                //     await Task.Yield();
                //     continue;
                // }

                PlayMedia();
            }

            _isPlaying = true;
        }

        private void PlayMedia()
        {
            //as MediaElement runs in UI thread use SynchronizationContext
            _sc.Post(delegate
            {
                _mediaElement.Source = new Uri(_current.FullName);
                _mediaElement.Play();
            }, null);
        }
        
        private void ResumePlay()
        {
            //as MediaElement runs in UI thread use SynchronizationContext
            _sc.Post(delegate
            {
                _mediaElement.Play();
            }, null);
        }

        public void Pause()
        {
            _sc.Post(delegate { _mediaElement.Pause(); }, null);
            _isOnPause = true;
        }

        public void Stop()
        {
            _sc.Post(delegate { _mediaElement.Stop(); }, null);
            _isPlaying = false;
            _current?.Delete();
        }

        ~VideoWatcher()
        {
            Stop();
        }
    }
}