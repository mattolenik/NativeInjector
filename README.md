[![Build status](https://ci.appveyor.com/api/projects/status/l3s9xjgjx90fpdeh?svg=true)](https://ci.appveyor.com/project/MattOlenik/nativeinjector)

# About

A simple .NET library for injecting native([PE](https://en.wikipedia.org/wiki/Portable_Executable)) DLLs into other processes from managed processes. It uses named shared memory to make a `byte[]` payload available to the remote process. It works by running LoadLibrary in the target process, resulting in `dllmain()` in your DLL being called, where your injected code will be. There are [important restrictions and best practices](https://msdn.microsoft.com/en-us/library/windows/desktop/dn633971.aspx#general_best_practices) regarding code that runs in dllmain(), so caution is advised. Doubly so because you're running code in a foreign process you know nothing about.

It was written for simple, short-lived functions, such as the PATH injection used in [Winston](https://github.com/mattolenik/winston).

# Example

```csharp
byte[] payload = Encoding.Unicode.GetBytes("some argument, struct, or other data");
// Synchronously waits for dllmain() to finish, then unloads the DLL
Injector.Inject(pid, dll32path, dll64path, "my shared memory name", payload);
```

## Mechanism

1. Copy the payload to shared memory
2. [Allocate memory](https://msdn.microsoft.com/en-us/library/windows/desktop/aa366890.aspx) in the target process
3. Copy the path of the DLL to be injected into previously allocated memory
4. [Create a remote thread](https://msdn.microsoft.com/en-us/library/windows/desktop/ms682437.aspx) to execute LoadLibrary, passing a pointer to the above memory for its argument
5. LoadLibrary calls the DLL's dllmain(), where your injected code runs
6. Injector waits for code to finish then unloads the DLL


