using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using static NativeInjector.NativeMethods;

namespace NativeInjector
{
    class Utils
    {
        public static bool Is64BitProcess(uint pid)
        {
            var si = new SystemInfo();
            GetNativeSystemInfo(ref si);

            if (si.processorArchitecture == 0)
            {
                return false;
            }

            bool result;
            var process = OpenProcess(ProcessAccessFlags.QueryInformation, false, pid);
            if (process == null)
            {
                throw new Exception($"Cannot open process {pid}");
            }

            if (!IsWow64Process(process, out result))
            {
                throw new InvalidOperationException();
            }

            return !result;
        }

        public static uint? ParentProcessId(uint id)
        {
            var pe32 = new ProcessEntry32 { dwSize = (uint)Marshal.SizeOf(typeof(ProcessEntry32)) };
            var hSnapshot = CreateToolhelp32Snapshot(SnapshotFlags.Process, id);
            //if (hSnapshot.IsInvalid)
            //{
            //    throw new Win32Exception();
            //}

            if (!Process32First(hSnapshot, ref pe32))
            {
                int errno = Marshal.GetLastWin32Error();
                if (errno == ERROR_NO_MORE_FILES)
                    return null;
                throw new Win32Exception(errno);
            }
            do
            {
                if (pe32.th32ProcessID == id)
                    return pe32.th32ParentProcessID;
            } while (Process32Next(hSnapshot, ref pe32));
            return null;
        }
    }
}