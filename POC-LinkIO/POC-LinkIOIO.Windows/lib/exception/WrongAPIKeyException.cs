using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace link.io.csharp.exception
{
    class WrongAPIKeyException : Exception
    {
        public WrongAPIKeyException(String message)
           : base(message) { }
    }
}
