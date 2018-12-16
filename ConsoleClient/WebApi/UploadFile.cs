using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace ConsoleClient
{
    [Obsolete]
    // not used class, just for testing
    public class CUploadFile
    {
        private const Int32 MAX_CHUNK_SIZE = (1024 * 5000);
        private HttpWebRequest webRequest = null;
        private FileStream fileReader = null;
        private Stream requestStream = null;

        public bool SendFile(string uri, string file)
        {
            byte[] fileData;
            fileReader = new FileStream(file, FileMode.Open, FileAccess.Read);
            webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Method = "POST";
            webRequest.ContentLength = fileReader.Length;
            webRequest.Timeout = 600000;
            //webRequest.Credentials = ;
            webRequest.AllowWriteStreamBuffering = false;
            requestStream = webRequest.GetRequestStream();

            Int64 fileSize = fileReader.Length;
            Int64 remainingBytes = fileSize;
            Int32 numberOfBytesRead = 0;
            Int32 done = 0;

            while (numberOfBytesRead < fileSize)
            {
                SetByteArray(out fileData, remainingBytes);
                done = WriteFileToStream(fileData, requestStream);
                numberOfBytesRead += done;
                remainingBytes -= done;
            }
            fileReader.Close();
            return true;
        }

        public Int32 WriteFileToStream(byte[] fileData, Stream requestStream)
        {
            Int32 done = fileReader.Read(fileData, 0, fileData.Length);
            requestStream.Write(fileData, 0, fileData.Length);
            return done;
        }

        private void SetByteArray(out byte[] fileData, Int64 bytesLeft)
        {
            fileData = bytesLeft < MAX_CHUNK_SIZE ? new byte[bytesLeft] : new byte[MAX_CHUNK_SIZE];
        }
    }
    
}
