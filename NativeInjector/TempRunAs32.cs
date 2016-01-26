using System;
using System.IO;
using System.Reflection;

namespace NativeInjector
{
    class TempRunAs32 : IDisposable
    {
        public string Path { get; }
        readonly string temp;

        public TempRunAs32()
        {
            temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
            Directory.CreateDirectory(temp);
            Path = System.IO.Path.Combine(temp, "RunAs32.exe");
            using (var res = GetType().Assembly.GetManifestResourceStream("NativeInjector.Resources.RunAs32.exe"))
            using (var file = File.Create(Path))
            {
                if (res == null)
                {
                    throw new Exception("Resource for RunAs32 not found in assembly");
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
