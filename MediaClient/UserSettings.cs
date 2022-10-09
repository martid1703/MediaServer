using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaClient
{
    /// <summary>
    /// Keeps technical data about WPF Looks, sending chunk size, ...
    /// </summary>
    static class UserSettings
    {
        static public Int32 ChunkSize { get; set; }
        static public String UserFolder { get; set; }
    }
}
