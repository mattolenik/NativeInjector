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
    public static class Injector
    {
        public static void Inject<T>(uint pid, string dll32, string dll64, string sharedMemName, T payload)
        {
            var thisPlatform = Environment.Is64BitProcess ? 64 : 32;
            var targetPlatform = Is64BitProcess(pid) ? 64 : 32;
            var dll = targetPlatform == 64 ? dll64 : dll32;

            if (targetPlatform == 64 && dll64 == null)
            {
                throw new ArgumentNullException(nameof(dll64), "64-bit DLL required when injecting into 64-bit process");
            }
            if (targetPlatform == 32 && dll32 == null)
            {
                throw new ArgumentNullException(nameof(dll32), "32-bit DLL required when injecting into 32-bit process");
            }

            if (thisPlatform == targetPlatform)
            {
                DirectInject(pid, dll, sharedMemName, payload);
            }
            else if (thisPlatform == 64 && targetPlatform == 32)
            {
                IndirectInject(pid, dll, sharedMemName, payload);
            }
            else if (thisPlatform == 32 && targetPlatform == 64)
            {
                throw new InvalidOperationException(
                    "Injecting into 64-bit processes from 32-bit is unsupported. Instead, injector should just be run as 64-bit to begin with.");
            }
        }

        static void IndirectInject<T>(uint pid, string dll, string sharedMemName, T payload)
        {
            using (var runAs32 = new TempRunAs32())
            {
                var data = SerializePayload(payload);
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = runAs32.Path,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        CreateNoWindow = true,
                        Arguments = $"\"{typeof (Injector).Assembly.Location}\" {pid} {data.Length} {sharedMemName} \"{dll}\""
                    }
                };
                process.Start();
                process.StandardInput.BaseStream.Write(data, 0, data.Length);
                process.StandardInput.Flush();
                process.WaitForExit(10000);
                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                if (process.ExitCode != 0)
                {
                    throw new ProcessException(stdout, stderr, process.ExitCode);
                }
            }
        }

        static void DirectInject<T>(uint pid, string dll, string sharedMemName, T payload)
        {
            var mapFile = IntPtr.Zero;
            try
            {
                mapFile = WriteSharedMemory(sharedMemName, payload);
                InjectDll(pid, dll);
                EjectDll(pid, dll);
            }
            finally
            {
                if (mapFile != IntPtr.Zero) CloseHandle(mapFile);
            }
        }

        static byte[] SerializePayload<T>(T value)
        {
            if (value is byte[])
            {
                return value as byte[];
            }
            var size = Marshal.SizeOf<T>();
            var buf = IntPtr.Zero;
            try
            {
                buf = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(value, buf, true);
                var result = new byte[size];
                Marshal.Copy(buf, result, 0, size);
                return result;
            }
            finally
            {
                if (buf != IntPtr.Zero) Marshal.FreeHGlobal(buf);
            }
        }

        static IntPtr WriteSharedMemory<T>(string sharedMemName, T data)
        {
            var dataBytes = data as byte[];
            var dataSize = dataBytes != null ? (uint)dataBytes.Length : (uint) Marshal.SizeOf<T>();
            var buf = IntPtr.Zero;
            var view = IntPtr.Zero;
            try
            {
                buf = Marshal.AllocHGlobal((int)dataSize);
                if (dataBytes != null)
                {
                    Marshal.Copy(dataBytes, 0, buf, dataBytes.Length);
                }
                else
                {
                    Marshal.StructureToPtr(data, buf, true);
                }
                var mapFile = CreateFileMapping(
                    new IntPtr(-1),
                    IntPtr.Zero,
                    FileMapProtection.PageReadWrite,
                    0,
                    dataSize,
                    sharedMemName);

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

        static void InjectDll(uint pid, string injectionDll)
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

                WaitForSingleObject(remoteThread, Timeout.Infinite);
            }
            finally
            {
                if (remoteThread != IntPtr.Zero) CloseHandle(remoteThread);
                if (remoteDllPath != IntPtr.Zero) VirtualFreeEx(process, remoteDllPath, pathBufLength, FreeType.Release);
                if (process != IntPtr.Zero) CloseHandle(process);
            }
        }

        static bool EjectDll(uint pid, string injectionDll)
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