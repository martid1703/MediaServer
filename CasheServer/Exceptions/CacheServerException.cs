using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CasheServer.Exceptions
{
    public class CacheServerException:Exception
    {
        public CacheServerException()
        {
        }

        public CacheServerException(string message)
            : base(message)
        {
        }

        public CacheServerException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}