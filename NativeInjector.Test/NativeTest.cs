using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Xunit;

namespace NativeInjector.Test
{
    public class NativeTest
    {
        [Theory, ClassData(typeof(Platforms))]
        public void InjectionEmptyPath(int platform)
        {
            TestPlatform(platform, e => e["PATH"] = "");
        }

        [Theory, ClassData(typeof(Platforms))]
        public void InjectionNoSetPath(int platform)
        {
            TestPlatform(platform, e => { });
        }

        [Theory, ClassData(typeof(Platforms))]
        public void InjectionWithPath(int platform)
        {
            TestPlatform(platform, e => e["PATH"] = @"C:\windows;c:\windows\system32");
        }

        void TestPlatform(int platform, Action<StringDictionary> env)
        {
            var injectedPath = @"C:\path\injected\from\dotnet";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = $"InjectHost.{platform}.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    StandardOutputEncoding = Encoding.Unicode,
                    StandardErrorEncoding = Encoding.Unicode,
                    CreateNoWindow = true
                }
            };
            env(process.StartInfo.EnvironmentVariables);
            process.StartInfo.EnvironmentVariables["PATH"] = null;
            process.Start();
            var stdin = process.StandardInput;
            try
            {
                Thread.Sleep(100);
                var data = EnvUpdate.Prepend(injectedPath);
                Injector.Inject(
                    (uint)process.Id,
                    EnvUpdate.Dll32Name,
                    EnvUpdate.Dll64Name,
                    $"{EnvUpdate.SharedMemName}-{process.Id}",
                    data);
                stdin.WriteLine("continue");
                stdin.Flush();
                process.WaitForExit(10000);
                var stdout = process.StandardOutput.ReadToEnd().Trim();
                Assert.StartsWith(stdout, injectedPath, StringComparison.InvariantCultureIgnoreCase);
            }
            finally
            {
                // Always write something to the test program so it can cleanly exit
                stdin?.WriteLine("exit");
                stdin?.Flush();
                process?.Dispose();
            }
        }
    }

    public class Platforms : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { 32 };
            yield return new object[] { 64 };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}