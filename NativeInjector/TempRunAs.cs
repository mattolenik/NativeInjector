using System;
using System.IO;
using System.Reflection;

namespace NativeInjector
{
    class TempRunAs : IDisposable
    {
        public string Path { get; }
        readonly string temp;

        public TempRunAs(int platform)
        {
            if (platform != 32 && platform != 64)
            {
                throw new ArgumentException("Must be 32 or 64", nameof(platform));
            }
            temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
            Directory.CreateDirectory(temp);
            Path = System.IO.Path.Combine(temp, $"RunAs{platform}.exe");
            using (var res = GetType().Assembly.GetManifestResourceStream($"NativeInjector.Resources.RunAs{platform}.exe"))
            using (var file = File.Create(Path))
            {
                if (res == null)
                {
                    throw new Exception($"Resource for RunAs{platform} not found in assembly");
                }
                res.CopyTo(file);
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(temp)) Directory.Delete(temp, true);
        }
    }
}
