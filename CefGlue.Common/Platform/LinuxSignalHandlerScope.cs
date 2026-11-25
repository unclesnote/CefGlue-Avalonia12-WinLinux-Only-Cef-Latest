using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Xilium.CefGlue.Common.Platform
{
    /// <summary>
    /// Saves POSIX signal handlers installed by CoreCLR/native hosts before CefRuntime.Initialize()
    /// and restores them afterward so Chromium's signal setup does not break managed debugging or
    /// hardware exception handling on Linux.
    /// </summary>
    internal sealed class LinuxSignalHandlerScope : IDisposable
    {
        private const int SigActionBufferSize = 256;
        private const int MaxStandardSignal = 31;
        private const int SigKill = 9;
        private const int SigStop = 19;

        private static readonly byte[] ZeroBuffer = new byte[SigActionBufferSize];

        private readonly List<(int Signal, IntPtr ActionBuffer)> _savedHandlers;
        private bool _disposed;

        [DllImport("libc.so.6", EntryPoint = "sigaction", SetLastError = true)]
        private static extern int SigAction(int signum, IntPtr act, IntPtr oldact);

        public LinuxSignalHandlerScope()
        {
            if (CefRuntime.Platform != CefRuntimePlatform.Linux)
            {
                return;
            }

            _savedHandlers = new List<(int Signal, IntPtr ActionBuffer)>();

            for (var sig = 1; sig <= MaxStandardSignal; sig++)
            {
                if (sig == SigKill || sig == SigStop)
                {
                    continue;
                }

                var buffer = Marshal.AllocHGlobal(SigActionBufferSize);
                Marshal.Copy(ZeroBuffer, 0, buffer, SigActionBufferSize);

                if (SigAction(sig, IntPtr.Zero, buffer) == 0 && Marshal.ReadIntPtr(buffer) != IntPtr.Zero)
                {
                    _savedHandlers.Add((sig, buffer));
                }
                else
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed || _savedHandlers == null)
            {
                return;
            }

            _disposed = true;

            foreach (var (signal, buffer) in _savedHandlers)
            {
                try
                {
                    SigAction(signal, buffer, IntPtr.Zero);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }

            _savedHandlers.Clear();
        }
    }
}
