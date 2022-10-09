using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;

namespace MediaServer.Controllers
{
    /// <summary>
    /// Opens file stream for a given file name and writes it to the output stream
    /// </summary>
    public class FileStreamDownload
    {
        private readonly string _filename;
        private Int64 _streamPosition;// to start video stream from given position

        public FileStreamDownload(string fileName)
        {
            _filename = fileName;
        }

        public FileStreamDownload(string fileName, Int64 streamPosition)
        {
            _filename = fileName;
            _streamPosition = streamPosition;
        }

        public async void WriteToStreamAsync(Stream outputStream, HttpContent content, TransportContext context)
        {
                FileStream video=null;
            try
            {
                Int32 bytesToRead = 65536;
                var buffer = new byte[bytesToRead];
                using (video = File.Open(_filename, FileMode.Open, FileAccess.Read))
                {

                    Int64 remainingLength = video.Length-video.Position;
                    var bytesRead = 1;

                    while (remainingLength > 0 && bytesRead > 0)
                    {
                        if (bytesToRead>remainingLength)
                        {
                            bytesToRead = (Int32)remainingLength;
                        }
                        bytesRead = video.Read(buffer, 0, bytesToRead);
                        await outputStream.WriteAsync(buffer, 0, bytesRead);
                        remainingLength -= bytesRead;
                    }
                }
            }
            catch (HttpException ex)
            {
                return;
            }
            finally
            {
                //video.Close();
                outputStream.Close();
            }
        }

    }



}
