using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using static NativeInjector.NativeMethods;
using static NativeInjector.Utils;

namespace NativeInjector
{
    // TODO: fix ANSI/unicode differences?
    public static class Injector
    {
        public static void UpdateEnv(uint pid, string variable, string value)
        {
            var mapFile = IntPtr.Zero;
            var dll = GetInjectionDllForProcess(pid);
            try
            {
                mapFile = WriteSharedMemory(variable, value);
                Inject(pid, dll);
            }
            finally
            {
                if (mapFile != IntPtr.Zero) CloseHandle(mapFile);
            }
            Eject(pid, dll);
        }

        static string GetInjectionDllForProcess(uint pid)
        {
            var platform = Is64BitProcess(pid) ? 64 : 32;
            return $"WinstonEnvUpdate.{platform}.dll";
        }

        static IntPtr WriteSharedMemory(string variable, string value)
        {
            var data = new WinstonEnvUpdate { Variable = variable, Value = value };
            var dataSize = (uint)Marshal.SizeOf<WinstonEnvUpdate>();
            var buf = IntPtr.Zero;
            var view = IntPtr.Zero;
            try
            {
                buf = Marshal.AllocHGlobal((int)dataSize);
                Marshal.StructureToPtr(data, buf, true);
                var mapFile = CreateFileMapping(
                    new IntPtr(-1),
                    IntPtr.Zero,
                    FileMapProtection.PageReadWrite,
                    0,
                    dataSize,
                    WinstonEnvUpdate.SharedMemName);

                view = MapViewOfFile(mapFile, FileMapAccess.FileMapAllAccess, 0, 0, new UIntPtr(dataSize));
                CopyMemory(view, buf, dataSize);
                return mapFile;
            }
            finally
            {
                if (buf != IntPtr.Zero) Marshal.FreeHGlobal(buf);
                if (view != IntPtr.Zero) UnmapViewOfFile(view);
            }
        }

        static bool Inject(uint pid, string injectionDll)
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(path, injectionDll);
            var pathBuf = Encoding.Unicode.GetBytes(path + "\0");
            var pathBufLength = new IntPtr(pathBuf.Length);

            var process = IntPtr.Zero;
            var remoteDllPath = IntPtr.Zero;
            var remoteThread = IntPtr.Zero;
            try
            {
                process = OpenProcess(
                    ProcessAccessFlags.QueryInformation |
                    ProcessAccessFlags.CreateThread |
                    ProcessAccessFlags.VirtualMemoryOperation |
                    ProcessAccessFlags.VirtualMemoryWrite,
                    false,
                    pid);

                remoteDllPath = VirtualAllocEx(
                    process,
                    IntPtr.Zero,
                    pathBufLength,
                    AllocationType.Commit,
                    MemoryProtection.ReadWrite);

                // Copy DLL path to remote processes' address space
                var numWritten = IntPtr.Zero;
                if (!WriteProcessMemory(process, remoteDllPath, pathBuf, pathBuf.Length, out numWritten) ||
                    numWritten != new IntPtr(pathBuf.Length))
                {
                    throw new Exception(
                        $"Failed to write remote process memory, expected to write {pathBuf.Length} bytes but wrote {numWritten} instead");
                }

                var threadId = IntPtr.Zero;
                var remoteKernel32 = GetKernel32Module(Process.GetProcessById((int) pid));
                var loadLibraryAddress = GetFunctionAddress(remoteKernel32, "LoadLibraryW");
                remoteThread = CreateRemoteThread(
                    process,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    loadLibraryAddress,
                    remoteDllPath,
                    0,
                    out threadId);
                var err = Marshal.GetLastWin32Error();

                WaitForSingleObject(remoteThread, Timeout.Infinite);
                return true;
            }
            finally
            {
                if (remoteThread != IntPtr.Zero) CloseHandle(remoteThread);
                if (remoteDllPath != IntPtr.Zero) VirtualFreeEx(process, remoteDllPath, pathBufLength, FreeType.Release);
                if (process != IntPtr.Zero) CloseHandle(process);
            }
        }

        static bool Eject(uint pid, string injectionDll)
        {
            var snapshot = IntPtr.Zero;
            var process = IntPtr.Zero;
            var remoteThread = IntPtr.Zero;
            try
            {
                snapshot = CreateToolhelp32Snapshot(SnapshotFlags.Module, pid);
                var me = new MODULEENTRY32
                {
                    dwSize = (uint)Marshal.SizeOf<MODULEENTRY32>()
                };
                var found = false;
                // TODO: resolve unicode/ANSI issue?
                var more = Module32First(snapshot, ref me);

                for (; more; more = Module32Next(snapshot, ref me))
                {
                    found =
                        string.Equals(me.szModule, injectionDll, StringComparison.InvariantCultureIgnoreCase) ||
                        string.Equals(me.szExePath, injectionDll, StringComparison.InvariantCultureIgnoreCase);
                    if (found) break;
                }
                if (!found)
                {
                    return false;
                }

                IntPtr threadId;
                process = OpenProcess(
                    ProcessAccessFlags.QueryInformation |
                    ProcessAccessFlags.CreateThread |
                    ProcessAccessFlags.VirtualMemoryOperation,
                    false, pid);
                var moduleHandle = GetModuleHandle("kernel32.dll");
                var freeLibraryAddress = GetProcAddress(moduleHandle, "FreeLibrary");
                remoteThread = CreateRemoteThread(
                    process,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    freeLibraryAddress,
                    me.modBaseAddr,
                    0,
                    out threadId);
                {
                    WaitForSingleObject(remoteThread, Timeout.Infinite);
                }
                return true;
            }
            finally
            {

                if (remoteThread != IntPtr.Zero) CloseHandle(remoteThread);
                if (process != IntPtr.Zero) CloseHandle(process);
                if (snapshot != IntPtr.Zero) CloseHandle(snapshot);
            }
        }
    }
}