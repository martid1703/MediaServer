using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace ConsoleClient
{
    public class CUploadFile
    {
        private const int MAX_CHUNK_SIZE = (1024 * 5000);
        private HttpWebRequest webRequest = null;
        private FileStream fileReader = null;
        private Stream requestStream = null;

        public bool SendFile(string uri, string file, FileInfo fileInfo)
        {
            byte[] fileData;
            fileReader = new FileStream(file, FileMode.Open, FileAccess.Read);
            webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Method = "POST";
            webRequest.ContentLength = fileReader.Length;
            webRequest.Timeout = 600000;
            webRequest.Credentials = CredentialCache.DefaultCredentials;
            webRequest.AllowWriteStreamBuffering = false;
            requestStream = webRequest.GetRequestStream();

            long fileSize = fileReader.Length;
            long remainingBytes = fileSize;
            int numberOfBytesRead = 0, done = 0;

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

        public int WriteFileToStream(byte[] fileData, Stream requestStream)
        {
            int done = fileReader.Read(fileData, 0, fileData.Length);
            requestStream.Write(fileData, 0, fileData.Length);

            return done;
        }

        private void SetByteArray(out byte[] fileData, long bytesLeft)
        {
            fileData = bytesLeft < MAX_CHUNK_SIZE ? new byte[bytesLeft] : new byte[MAX_CHUNK_SIZE];
        }
    }
    
}
