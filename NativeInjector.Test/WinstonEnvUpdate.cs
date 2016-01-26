using System.Runtime.InteropServices;

namespace NativeInjector.Test
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    struct WinstonEnvUpdate
    {
        public const string SharedMemName = "WinstonEnvUpdate";
        public const string Dll32Name = "WinstonEnvUpdate.32.dll";
        public const string Dll64Name = "WinstonEnvUpdate.64.dll";

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