using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace MediaServer.FFMPEG
{
    public class OutputPackage
    {
        public MemoryStream VideoStream { get; set; }
        public System.Drawing.Image PreviewImage { get; set; }
        public string RawOutput { get; set; }
        public bool Success { get; set; }
    }
}