using System;
using System.CodeDom;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static NativeInjector.NativeMethods;

namespace NativeInjector
{
    class Utils
    {
        public static bool Is64BitProcess(uint pid)
        {
            var si = new SYSTEM_INFO();
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
            var pe32 = new PROCESSENTRY32 { dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32)) };
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

        // TODO: remove?
        public static void AdjustDebugPrivileges(uint pid)
        {
            var process = IntPtr.Zero;
            var token = IntPtr.Zero;
            try
            {
                process = OpenProcess(ProcessAccessFlags.All, false, pid);

                var tp = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Attributes = SE_PRIVILEGE_ENABLED
                };

                if (!LookupPrivilegeValue(null, SE_DEBUG_NAME, out tp.Luid))
                {
                    CloseHandle(process);
                    throw new Exception("Can't lookup value");
                }

                if (!OpenProcessToken(process, TOKEN_ACCESS.TOKEN_ADJUST_PRIVILEGES, out token))
                {
                    CloseHandle(process);
                    throw new Exception("Can't open process token value");
                }

                if (!AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
                {
                    CloseHandle(process);
                    CloseHandle(token);
                    throw new Exception("Can't AdjustTokenPrivileges");
                }
            }
            finally
            {

                if (process != IntPtr.Zero) CloseHandle(process);
                if (token != IntPtr.Zero) CloseHandle(token);
            }
        }

        public static ProcessModule GetKernel32Module(Process process)
        {
            var processes = process.Modules;
            for (var i = 0; i < processes.Count; i++)
            {
                if (processes[i].ModuleName.ToLower() == "kernel32.dll")
                {
                    return processes[i];
                }
            }
            throw new Exception($"kernel32.dll not present in process with pid = {process.Id}");
        }

        public static IntPtr GetFunctionAddress(ProcessModule remoteKernel32, string name)
        {
            Process process = Process.GetCurrentProcess();
            ProcessModule kernel32 = GetKernel32Module(process);
            IntPtr proc = GetProcAddress(kernel32.BaseAddress, name);

            if (IntPtr.Zero == proc)
            {
                throw new Exception("Can't get process address");
            }

            return new IntPtr(remoteKernel32.BaseAddress.ToInt64() + (proc.ToInt64() - kernel32.BaseAddress.ToInt64()));
        }
    }
}