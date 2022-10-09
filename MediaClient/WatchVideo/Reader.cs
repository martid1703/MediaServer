using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Controls;
using System.Windows;

namespace MediaClient.WatchVideo
{
    public class Reader
    {
        private MediaElement _media;
        private Queue<FileInfo> _watchQueue;
        private bool _mediaEnded;
        private FileInfo _current;


        public Reader(MediaElement media, Queue<FileInfo> watchQueue)
        {
            _media = media;
            _watchQueue = watchQueue;

            // _isMediaEnded is static sync field from WatchVideoViewModel
            _media.MediaEnded += Element_MediaEnded;
        }

        // When the media playback is finished. Stop() the media to seek to media start.
        private void Element_MediaEnded(object sender, RoutedEventArgs e)
        {
            _mediaEnded = true;
        }

        // Subscribe to MediaElement event "Media Ended" to call this method
        // Run in separate thread!
        public FileInfo Read(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return null;
            }

            lock (_watchQueue)
            {
                // if nothing to watch, wait for the Writer class to add some data
                while (_watchQueue.Count == 0)
                {
                    Monitor.Wait(_watchQueue); //release lock
                }

                _current = _watchQueue.Dequeue();
                Monitor.Pulse(_watchQueue); // release lock
                return _current;
            }
        }
    }
}