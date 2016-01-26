using System;
using System.Linq;

namespace NativeInjector
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var pidOpt = args.FirstOrDefault();
            uint pid;
            if (!uint.TryParse(pidOpt, out pid))
            {
                throw new Exception("Invalid PID argument");
            }
            var sizeOpt = args.Skip(1).FirstOrDefault();
            int payloadSize;
            if (!int.TryParse(sizeOpt, out payloadSize))
            {
                throw new Exception("Invalid payload size argument");
            }
            var sharedMemName = args.Skip(2).FirstOrDefault();
            var dllName = args.Skip(3).FirstOrDefault();
            var payload = new byte[payloadSize];
            using (var stdin = Console.OpenStandardInput())
            {
                stdin.Read(payload, 0, payloadSize);
            }
            Injector.Inject(pid, dllName, dllName, sharedMemName, payload);
        }
    }
}