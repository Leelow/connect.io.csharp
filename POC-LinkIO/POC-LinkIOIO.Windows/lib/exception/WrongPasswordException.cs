using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace link.io.csharp.exception
{
    class WrongPasswordException : Exception
    {
        public WrongPasswordException(String message)
           : base(message) { }
    }
}
