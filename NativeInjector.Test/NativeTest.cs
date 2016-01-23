using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NativeInjector.Test
{
    [TestClass]
    public class NativeTest
    {
        [TestMethod]
        public void TestNative()
        {
            var injected = @"C:\path\injected\from\dotnet";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "InjectHost.64.exe",
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
                Injector.PrependPath((uint) process.Id, injected);
                stdin.WriteLine("continue");
                stdin.Flush();
                process.WaitForExit(10000);
                var stdout = process.StandardOutput.ReadToEnd().Trim();
                Assert.IsTrue(stdout.Contains(injected), "stdout does not contain '{0}'. stdout:\n{1}", injected, stdout);
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