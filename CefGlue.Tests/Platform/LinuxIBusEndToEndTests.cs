using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using NUnit.Framework;

namespace CefGlue.Tests.Platform
{
    public class LinuxIBusEndToEndTests : TestBase
    {
        [DllImport("libX11.so.6")]
        private static extern IntPtr XOpenDisplay(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern int XCloseDisplay(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern int XFlush(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern uint XKeysymToKeycode(IntPtr display, ulong keysym);

        [DllImport("libXtst.so.6")]
        private static extern int XTestFakeMotionEvent(IntPtr display, int screenNumber, int x, int y, ulong delay);

        [DllImport("libXtst.so.6")]
        private static extern int XTestFakeButtonEvent(IntPtr display, uint button, int isPress, ulong delay);

        [DllImport("libXtst.so.6")]
        private static extern int XTestFakeKeyEvent(IntPtr display, uint keycode, int isPress, ulong delay);

        private const ulong XK_Shift_L = 0xffe1;
        private const ulong XK_space = 0x0020;

        protected override async Task ExtraSetup()
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (TopLevel.GetTopLevel(Browser) is Window win)
                {
                    win.Width = 600;
                    win.Height = 400;
                    win.Position = new PixelPoint(150, 150);
                    win.Activate();
                }
            });

            await Browser.LoadContent("<html><body><input id='t' type='text' style='width:500px;height:120px;font-size:28px' autofocus /></body></html>");
            await base.ExtraSetup();
        }

        [Test]
        [Explicit("Needs a live X11 session with IBus Hangul and Shift+Space as the Hangul toggle.")]
        [Category("Interactive")]
        public async Task EnglishAndKoreanIBusInputWorkInBrowser()
        {
            if (!OperatingSystem.IsLinux() || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
            {
                Assert.Ignore("Requires a live Linux X11 + IBus session.");
            }

            await Task.Delay(500);

            var topLeft = await Dispatcher.UIThread.InvokeAsync(() => Browser.PointToScreen(new Point(120, 60)));
            var display = XOpenDisplay(IntPtr.Zero);
            Assert.AreNotEqual(IntPtr.Zero, display, "Failed to open X11 display.");

            try
            {
                // Click inside the input element
                XTestFakeMotionEvent(display, -1, topLeft.X, topLeft.Y, 0);
                XTestFakeButtonEvent(display, 1, 1, 0);
                XTestFakeButtonEvent(display, 1, 0, 0);
                XFlush(display);
                await Task.Delay(300);

                // First mode (whichever mode IBus started in: Latin -> "gksrmf ", or Hangul -> "한글 ")
                foreach (var sym in new ulong[] { 'g', 'k', 's', 'r', 'm', 'f', XK_space })
                {
                    SendKey(display, sym);
                    await Task.Delay(50);
                }

                await Task.Delay(300);
                var firstValue = await EvaluateJavascript<string>("return document.getElementById('t').value;");
                TestContext.Progress.WriteLine($"First input value: '{firstValue}'");

                // Toggle IBus Hangul/Latin mode via Shift+Space and clear input
                await SendModifiedKeyAsync(display, XK_Shift_L, XK_space);
                await Task.Delay(300);
                await EvaluateJavascript<string>("var el = document.getElementById('t'); el.value = ''; el.focus(); return '';");
                var focusState = await EvaluateJavascript<string>("return document.hasFocus() + ':' + (document.activeElement ? document.activeElement.id : 'none');");
                TestContext.Progress.WriteLine($"Focus after toggle: '{focusState}'");

                try
                {
                    foreach (var sym in new ulong[] { 'g', 'k', 's', 'r', 'm', 'f', XK_space })
                    {
                        SendKey(display, sym);
                        await Task.Delay(50);
                    }

                    await Task.Delay(300);
                    var secondValue = await EvaluateJavascript<string>("return document.getElementById('t').value;");
                    TestContext.Progress.WriteLine($"Second input value: '{secondValue}'");

                    var values = new[] { firstValue, secondValue };
                    Assert.That(values, Contains.Item("gksrmf "), $"Expected English 'gksrmf ' in one mode, got: [{string.Join(", ", values)}]");
                    Assert.That(values, Contains.Item("한글 "), $"Expected Korean '한글 ' via IBus in the toggled mode, got: [{string.Join(", ", values)}]");
                }
                finally
                {
                    // Restore original IBus input mode
                    await SendModifiedKeyAsync(display, XK_Shift_L, XK_space);
                    await Task.Delay(150);
                }
            }
            finally
            {
                XCloseDisplay(display);
            }
        }

        private static void SendKey(IntPtr display, ulong keysym)
        {
            var kc = XKeysymToKeycode(display, keysym);
            Assert.AreNotEqual(0u, kc, $"No keycode for keysym 0x{keysym:x}");
            XTestFakeKeyEvent(display, kc, 1, 0);
            XTestFakeKeyEvent(display, kc, 0, 0);
            XFlush(display);
        }

        private static async Task SendModifiedKeyAsync(IntPtr display, ulong modifierKeysym, ulong keysym)
        {
            var modKc = XKeysymToKeycode(display, modifierKeysym);
            var kc = XKeysymToKeycode(display, keysym);
            XTestFakeKeyEvent(display, modKc, 1, 0);
            XFlush(display);
            await Task.Delay(30);
            XTestFakeKeyEvent(display, kc, 1, 0);
            XFlush(display);
            await Task.Delay(30);
            XTestFakeKeyEvent(display, kc, 0, 0);
            XFlush(display);
            await Task.Delay(30);
            XTestFakeKeyEvent(display, modKc, 0, 0);
            XFlush(display);
        }
    }
}
