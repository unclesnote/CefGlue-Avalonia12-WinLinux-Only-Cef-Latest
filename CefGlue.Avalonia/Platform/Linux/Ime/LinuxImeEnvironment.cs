using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Xilium.CefGlue.Avalonia.Platform.Linux.Ime
{
    /// <summary>
    /// Fills in the environment variables Chromium's GTK IM context needs to use the desktop's
    /// input method (IBus or Fcitx/Fcitx5). Must run before CEF initializes; the IME itself is
    /// handled by Chromium's GtkUi and the GTK IM module.
    /// </summary>
    internal static class LinuxImeEnvironment
    {
        private const string XModifiersImPrefix = "@im=";

        // Names an IME framework uses in GTK_IM_MODULE / XMODIFIERS (@im=name) / QT_IM_MODULE,
        // mapped to the GTK IM module Chromium's GtkUi should load.
        // IBus is verified end to end; Fcitx/Fcitx5 is detected and configured the same way but has
        // not been verified in a live session (fcitx5-gtk may need a sync-mode setting like IBus).
        private static readonly Dictionary<string, string> GtkImModules = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ibus"] = "ibus",
            ["fcitx"] = "fcitx",
            ["fcitx5"] = "fcitx",
        };

        [DllImport("libc.so.6", EntryPoint = "setenv")]
        private static extern int NativeSetEnv(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string value,
            int overwrite);

        /// <returns>The detected GTK IM module ("ibus" or "fcitx"), or null when no supported IME is active.</returns>
        public static string Apply(
            Func<string, string> getEnv = null,
            Action<string, string> setEnv = null)
        {
            getEnv ??= Environment.GetEnvironmentVariable;
            setEnv ??= SetProcessEnvironmentVariable;

            var gtkImModule = Detect(getEnv);
            if (gtkImModule == null)
            {
                return null;
            }

            SetIfMissing(getEnv, setEnv, "GTK_IM_MODULE", gtkImModule);
            SetIfMissing(getEnv, setEnv, "XMODIFIERS", XModifiersImPrefix + gtkImModule);

            if (gtkImModule == "ibus")
            {
                // Chromium's GtkUi in CEF pumps X11EventSource via MessagePumpGlib rather than gtk_main(),
                // so im-ibus.so's default async mode drops unhandled keys (English, Space, Enter, etc.)
                // re-injected via gdk_event_put(). Enabling IBus sync mode makes gtk_im_context_filter_keypress()
                // return handled/unhandled synchronously so both Hangul composition and normal keys work.
                SetIfMissing(getEnv, setEnv, "IBUS_ENABLE_SYNC_MODE", "1");
            }

            return gtkImModule;
        }

        private static string Detect(Func<string, string> getEnv)
        {
            // An explicit GTK_IM_MODULE is the user's choice for GTK apps (Chromium included); honour it,
            // and leave unsupported modules such as "xim" untouched.
            var gtkIm = getEnv("GTK_IM_MODULE");
            if (!string.IsNullOrWhiteSpace(gtkIm))
            {
                return Lookup(gtkIm);
            }

            return Lookup(GetXModifiersIm(getEnv("XMODIFIERS"))) ?? Lookup(getEnv("QT_IM_MODULE"));
        }

        private static string Lookup(string name) =>
            name != null && GtkImModules.TryGetValue(name.Trim(), out var gtkImModule) ? gtkImModule : null;

        // "@im=ibus" -> "ibus"
        private static string GetXModifiersIm(string xmodifiers)
        {
            var start = xmodifiers?.IndexOf(XModifiersImPrefix, StringComparison.OrdinalIgnoreCase) ?? -1;
            if (start < 0)
            {
                return null;
            }

            start += XModifiersImPrefix.Length;
            var end = xmodifiers.IndexOfAny(new[] { '@', ' ' }, start);
            return end < 0 ? xmodifiers.Substring(start) : xmodifiers.Substring(start, end - start);
        }

        private static void SetIfMissing(Func<string, string> getEnv, Action<string, string> setEnv, string name, string value)
        {
            if (string.IsNullOrWhiteSpace(getEnv(name)))
            {
                setEnv(name, value);
            }
        }

        private static void SetProcessEnvironmentVariable(string name, string value)
        {
            Environment.SetEnvironmentVariable(name, value);

            // On Linux, .NET's Environment.SetEnvironmentVariable only updates the managed
            // cache and does not call libc setenv(), so native GTK/IBus/Fcitx modules calling
            // getenv()/g_getenv() would not observe the change unless we also call setenv().
            NativeSetEnv(name, value, 1);
        }
    }
}
