using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleClient
{
    /// <summary>
    /// Keeps technical data about WPF Looks, sending chunk size, ...
    /// </summary>
    static class UserSettings
    {
        //todo: chunkSize - somewhat about 100 kB shoud be good?
        static public Int32 ChunkSize { get; set; }
        static public String UserFolder { get; set; }
    }
}
