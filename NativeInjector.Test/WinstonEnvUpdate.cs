using System.Runtime.InteropServices;

namespace NativeInjector.Test
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    struct WinstonEnvUpdate
    {
        public const string SharedMemName = "WinstonEnvUpdate";

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Operation;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32767)]
        public string Path;

        public static WinstonEnvUpdate Prepend(string path)
        {
            return new WinstonEnvUpdate { Operation = "prepend", Path = path };
        }
    }
}