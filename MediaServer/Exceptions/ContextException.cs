using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MediaServer.Exceptions
{
    public class ContextException:Exception
    {
        public ContextException()
        {
        }

        public ContextException(string message)
            : base(message)
        {
        }

        public ContextException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}