using System;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace CefGlue.Tests.Platform
{
    public class LinuxSignalHandlerTests : TestBase
    {
        [Test]
        [ExcludeOnWindows("Linux-only test.")]
        public void CoreClrSignalHandlersSurviveCefInitialize()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }

            string nullText = null;
            Assert.Throws<NullReferenceException>(() => _ = nullText.Length);

            var sigCgtLine = File.ReadLines("/proc/self/status")
                .FirstOrDefault(line => line.StartsWith("SigCgt:", StringComparison.Ordinal));
            Assert.IsNotNull(sigCgtLine, "Expected SigCgt entry in /proc/self/status.");

            var hexValue = sigCgtLine.Substring("SigCgt:".Length).Trim();
            Assert.IsTrue(
                ulong.TryParse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var sigCgt),
                $"Failed to parse SigCgt value '{hexValue}'.");

            // Linux signal bitmask is 0-indexed (bit N-1 corresponds to signal N):
            // SIGTRAP = 5 (bit 4), SIGSEGV = 11 (bit 10).
            Assert.IsTrue((sigCgt & (1UL << 4)) != 0, $"Expected SIGTRAP handler bit to remain set in SigCgt (0x{sigCgt:x16}).");
            Assert.IsTrue((sigCgt & (1UL << 10)) != 0, $"Expected SIGSEGV handler bit to remain set in SigCgt (0x{sigCgt:x16}).");
        }
    }
}
