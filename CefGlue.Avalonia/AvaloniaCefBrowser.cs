using System;
using Avalonia.Controls;
using Xilium.CefGlue.Avalonia.Platform;
using Xilium.CefGlue.Avalonia.Platform.Linux;
using Xilium.CefGlue.Avalonia.Platform.Linux.Ime;
using Xilium.CefGlue.Common;
using Xilium.CefGlue.Common.Platform;

namespace Xilium.CefGlue.Avalonia
{
    /// <summary>
    /// The Avalonia CEF browser.
    /// </summary>
    public class AvaloniaCefBrowser(Func<CefRequestContext> cefRequestContextFactory = null)
        : BaseCefBrowser(cefRequestContextFactory)
    {
        static AvaloniaCefBrowser()
        {
            if ((CefRuntime.Platform == CefRuntimePlatform.MacOS || CefRuntime.Platform == CefRuntimePlatform.Linux) && !CefRuntimeLoader.IsLoaded)
            {
                if (CefRuntime.Platform == CefRuntimePlatform.Linux)
                {
                    LinuxNativeLibraryLoader.PreloadHarfBuzz();
                    LinuxImeEnvironment.Apply();
                }

                var browserProcessHandler = new AvaloniaBrowserProcessHandler();
                CefRuntimeLoader.Load(browserProcessHandler);
                if (CefRuntime.Platform == CefRuntimePlatform.Linux)
                {
                    browserProcessHandler.StartPump();
                }
            }
        }

        internal override IControl CreateControl()
        {
            return new AvaloniaControl(this, VisualChildren);
        }

        internal override IOffScreenControlHost CreateOffScreenControlHost()
        {
            return new AvaloniaOffScreenControlHost(this, VisualChildren, CreateOffScreenKeyboardHandler(this));
        }

        public override IOffScreenKeyboardHandler CreateOffScreenKeyboardHandler(object control)
        {
            return new AvaloniaOffScreenKeyboardHandler(control as Control);
        }

        internal override IOffScreenPopupHost CreatePopupHost()
        {
            var popup = new ExtendedAvaloniaPopup
            {
                PlacementTarget = this
            };
            return new AvaloniaPopup(popup, popup.VisualChildren, CreateOffScreenKeyboardHandler(popup));
        }
    }
}
