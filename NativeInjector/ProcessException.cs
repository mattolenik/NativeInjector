using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NativeInjector
{
    class ProcessException : Exception
    {
        public ProcessException(string stdout, string stderr, int exitCode) :
            base($"Process exited with code {exitCode}\nstdout:\n---\n{stdout}\n\nstderr:\n---\n{stderr}\n")
        {
        }
    }
}
