using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace link.io.csharp.exception
{
    public class NotConnectedException : Exception
    {
        public NotConnectedException(String message)
           : base(message) { }

    }
}
