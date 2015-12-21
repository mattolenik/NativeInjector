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
            var expected = "injected from .NET";
            using (var mutex = new Mutex(true, "WinstonInjectHostMutex"))
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "InjectHost.64.exe",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = Encoding.Unicode,
                        StandardErrorEncoding = Encoding.Unicode,
                        CreateNoWindow = false
                    }
                };
                process.Start();
                Thread.Sleep(2000);
                Injector.UpdateEnv((uint)process.Id, "TEST", expected);
                mutex.ReleaseMutex();
                process.WaitForExit();
                var stdout = process.StandardOutput.ReadToEnd().Trim();
                Assert.AreEqual(expected, stdout);
            }
        }
    }
}