using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace NativeInjector
{
    [SuppressUnmanagedCodeSecurity, HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
    sealed class SafeWin32Handle : SafeHandleMinusOneIsInvalid
    {
        internal SafeWin32Handle() : base(true)
        {
        }

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal SafeWin32Handle(IntPtr handle) : base(true)
        {
            base.SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            return CloseHandle(base.handle);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        private static extern bool CloseHandle(IntPtr handle);
    }
}
