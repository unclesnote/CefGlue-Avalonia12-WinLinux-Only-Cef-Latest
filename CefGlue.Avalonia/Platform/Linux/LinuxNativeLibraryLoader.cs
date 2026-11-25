using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Xilium.CefGlue.Avalonia.Platform.Linux
{
    /// <summary>
    /// Preloads libHarfBuzzSharp.so with RTLD_GLOBAL on Linux before libcef.so / libgtk-3.so
    /// load system libharfbuzz.so.0, avoiding ELF symbol-interposition SIGSEGV crashes
    /// (e.g. when running directly or under an IDE debugger without LD_PRELOAD).
    /// </summary>
    internal static class LinuxNativeLibraryLoader
    {
        private const int RTLD_NOW = 0x0002;
        private const int RTLD_GLOBAL = 0x0100;

        private static bool _harfBuzzPreloaded;

        [DllImport("libc.so.6", EntryPoint = "dlopen", CharSet = CharSet.Ansi)]
        private static extern IntPtr DlOpen(string fileName, int flags);

        public static void PreloadHarfBuzz()
        {
            if (_harfBuzzPreloaded || CefRuntime.Platform != CefRuntimePlatform.Linux)
            {
                return;
            }

            _harfBuzzPreloaded = true;

            var rid = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                ? "linux-arm64"
                : "linux-x64";

            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", "libHarfBuzzSharp.so"),
                Path.Combine(AppContext.BaseDirectory, "libHarfBuzzSharp.so"),
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "runtimes", rid, "native", "libHarfBuzzSharp.so")
            };

            foreach (var path in candidates)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    var handle = DlOpen(path, RTLD_NOW | RTLD_GLOBAL);
                    if (handle != IntPtr.Zero)
                    {
                        return;
                    }
                }
            }
        }
    }
}
