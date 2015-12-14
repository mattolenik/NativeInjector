using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using static NativeInjector.NativeMethods;
using static NativeInjector.Utils;

namespace NativeInjector
{
    // TODO: fix ANSI/unicode differences?
    internal static class Injector
    {
        public static void UpdateEnv(uint pid, string variable, string value)
        {
            using (var mapFile = WriteSharedMemory(variable, value))
            {
                var dll = GetInjectionDllForProcess(pid);
                Inject(pid, dll);
                Eject(pid, dll);
            }
        }

        private static string GetInjectionDllForProcess(uint pid)
        {
            var platform = Is64BitProcess(pid) ? 64 : 32;
            return $"WinstonEnvUpdate.{platform}.dll";
        }

        private static SafeHandle WriteSharedMemory(string variable, string value)
        {
            var data = new WinstonEnvUpdate { Variable = variable, Value = value };
            var dataSize = (uint)Marshal.SizeOf<WinstonEnvUpdate>();

            var bbuf = Marshal.AllocHGlobal((int)dataSize);
            Marshal.StructureToPtr(data, bbuf, true);
            using (var dataBuf = new SafeWin32Handle(bbuf))
            {
                var mapFile = CreateFileMapping(
                    new IntPtr(-1),
                    IntPtr.Zero,
                    FileMapProtection.PageReadWrite,
                    0,
                    dataSize,
                    WinstonEnvUpdate.SharedMemName);
                if (mapFile == null)
                {
                    throw new Exception("Failed to create shared memory");
                }

                using (var buf = MapViewOfFile(mapFile, FileMapAccess.FileMapAllAccess, 0, 0, new UIntPtr(dataSize)))
                {
                    if (buf == null)
                    {
                        throw new Exception("Failed to map view of file");
                    }
                    CopyMemory(buf, dataBuf, dataSize);
                }
                return mapFile;
            }
        }

        private static void Inject(uint pid, string injectionDll)
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(path, injectionDll);
            var pathBuf = Encoding.Unicode.GetBytes(path + "\0");

            using (var process = OpenProcess(
                ProcessAccessFlags.QueryInformation |
                ProcessAccessFlags.CreateThread |
                ProcessAccessFlags.VirtualMemoryOperation |
                ProcessAccessFlags.VirtualMemoryWrite,
                false, pid))
            using (var remoteDllPath = VirtualAllocEx(
                process,
                IntPtr.Zero,
                new IntPtr(pathBuf.Length),
                AllocationType.Commit | AllocationType.Reserve,
                MemoryProtection.ReadWrite))
            {
                if (remoteDllPath == null)
                {
                    throw new Exception("Unable to allocate memory in remote process");
                }
                // Copy DLL path to remote processes' address space
                var numWritten = IntPtr.Zero;
                if (!WriteProcessMemory(process, remoteDllPath, pathBuf, pathBuf.Length, out numWritten) ||
                    numWritten != new IntPtr(pathBuf.Length))
                {
                    throw new Exception(
                        $"Failed to write remote process memory, expected to write {pathBuf.Length} bytes but wrote {numWritten} instead");
                }

                var threadId = IntPtr.Zero;
                using (var moduleHandle = GetModuleHandle("kernel32.dll"))
                using (var loadLibraryAddress = GetProcAddress(moduleHandle, "LoadLibraryW"))
                using (
                    var remoteThread = CreateRemoteThread(process, IntPtr.Zero, 0, loadLibraryAddress,
                        remoteDllPath,
                        0, out threadId))
                {
                    if (remoteThread == null)
                    {
                        throw new Exception($"Failed to inject DLL into process {pid}");
                    }

                    WaitForSingleObject(remoteThread, Timeout.Infinite);
                }
            }
        }

        private static void Eject(uint pid, string injectionDll)
        {
            using (var snapshot = CreateToolhelp32Snapshot(SnapshotFlags.Module, pid))
            {
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
                    throw new Exception($"Module not found in pid {pid}");
                }

                IntPtr threadId;
                using (var process = OpenProcess(
                    ProcessAccessFlags.QueryInformation |
                    ProcessAccessFlags.CreateThread |
                    ProcessAccessFlags.VirtualMemoryOperation,
                    false, pid))
                using (var moduleHandle = GetModuleHandle("kernel32.dll"))
                using (var freeLibraryAddress = GetProcAddress(moduleHandle, "FreeLibrary"))
                using (var remoteThread = CreateRemoteThread(
                    process,
                    IntPtr.Zero,
                    0,
                    freeLibraryAddress,
                    new SafeWin32Handle(me.modBaseAddr),
                    0,
                    out threadId))
                {
                    WaitForSingleObject(remoteThread, Timeout.Infinite);
                }
            }
        }
    }
}