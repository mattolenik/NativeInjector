using System;
using System.Diagnostics;
using static NativeInjector.Utils;

namespace NativeInjector
{
    class Program
    {
        static void Main(string[] args)
        {
            //Debugger.Launch();
            var pid = ParentProcessId((uint) Process.GetCurrentProcess().Id);
            if (pid == null)
            {
                throw new Exception("Could not get parent process ID");
            }
            Injector.PrependPath(pid.Value, args[0]);
        }
    }
}