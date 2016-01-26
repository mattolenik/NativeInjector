using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NativeInjector.Test
{
    [TestClass]
    public class NativeTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Injection32And64()
        {
            TestPlatform(32);
            TestPlatform(64);
        }

        void TestPlatform(int platform)
        {
            TestContext.WriteLine($"Running test {nameof(TestPlatform)}, injecting into {platform}-bit process");
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
            //process.StartInfo.EnvironmentVariables["PATH"] = @"C:\windows";
            //process.StartInfo.EnvironmentVariables["PATH"] = @"";
            //process.StartInfo.EnvironmentVariables.Remove("PATH");
            //process.StartInfo.EnvironmentVariables["PATH"] = null;
            process.Start();
            var stdin = process.StandardInput;
            try
            {
                Thread.Sleep(2000);
                var data = WinstonEnvUpdate.Prepend(injectedPath);
                Injector.Inject(
                    (uint) process.Id,
                    WinstonEnvUpdate.Dll32Name,
                    WinstonEnvUpdate.Dll64Name,
                    $"{WinstonEnvUpdate.SharedMemName}-{process.Id}",
                    data);
                stdin.WriteLine("continue");
                stdin.Flush();
                process.WaitForExit(10000);
                var stdout = process.StandardOutput.ReadToEnd().Trim();
                Assert.IsTrue(
                    stdout.StartsWith(injectedPath, StringComparison.InvariantCultureIgnoreCase),
                    "stdout does not start with '{0}'. stdout:\n{1}", injectedPath, stdout);
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
}