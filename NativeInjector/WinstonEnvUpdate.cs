using System.Runtime.InteropServices;

namespace NativeInjector
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    struct WinstonEnvUpdate
    {
        public static readonly string SharedMemName = "WinstonEnvUpdate";

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Variable;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16384)]
        public string Value;
    }
}