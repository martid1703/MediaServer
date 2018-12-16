using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoProcessing
{
    public class VideoThumbnail
    {

        // using FFmpeg to get thumbnail from video
        public static Boolean CreateThumbnail(string video, string thumbnail)
        {
            String cmd = $"-i \"{video}\" -ss 00:00:05.000 -vframes 1 \"{thumbnail}\"";

            var startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = @"C:\ffmpeg\bin\ffmpeg.exe",
                Arguments = cmd
            };

            var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();

            bool isClosed = process.WaitForExit(1000);
            if (!process.HasExited)
            {
            process.Kill();
            }
            return isClosed;
        }


        // use image path and convert it to Bitmap
        public static Bitmap LoadImage(string path)
        {
            var ms = new MemoryStream(File.ReadAllBytes(path));
            return (Bitmap)Image.FromStream(ms);
        }
    }
}
