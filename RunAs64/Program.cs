using System.Linq;
using System.Reflection;

namespace RunAs64
{
    class Program
    {
        static void Main(string[] args)
        {
            var assemblyName = args[0];
            var assembly = Assembly.LoadFile(assemblyName);
            var arguments = args.Skip(1).ToArray();
            assembly.EntryPoint.Invoke(null, new object[] { arguments });
        }
    }
}