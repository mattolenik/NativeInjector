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
            var pid = ParentProcessId((uint)Process.GetCurrentProcess().Id);
            if (pid == null)
            {
                throw new Exception("Could not get parent process ID");
            }
            var data = new WinstonEnvUpdate { Operation = WinstonEnvUpdate.Prepend, Path = args[0] };
            Injector.Inject(pid.Value, WinstonEnvUpdate.SharedMemName, data);
        }
    }
}