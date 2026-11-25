using System.Collections.Generic;
using NUnit.Framework;
using Xilium.CefGlue.Avalonia.Platform.Linux.Ime;

namespace CefGlue.Tests.Platform
{
    public class LinuxImeEnvironmentTests
    {
        private static string Configure(Dictionary<string, string> env) =>
            LinuxImeEnvironment.Apply(
                k => env.TryGetValue(k, out var v) ? v : null,
                (k, v) => env[k] = v);

        [Test]
        public void ConfiguresIBusWhenDetectedFromXModifiers()
        {
            var env = new Dictionary<string, string> { ["XMODIFIERS"] = "@im=ibus" };

            Assert.AreEqual("ibus", Configure(env));
            Assert.AreEqual("ibus", env["GTK_IM_MODULE"]);
            Assert.AreEqual("@im=ibus", env["XMODIFIERS"]);
            Assert.AreEqual("1", env["IBUS_ENABLE_SYNC_MODE"]);
        }

        [Test]
        public void ConfiguresIBusWhenDetectedFromQtImModule()
        {
            var env = new Dictionary<string, string> { ["QT_IM_MODULE"] = "ibus" };

            Assert.AreEqual("ibus", Configure(env));
            Assert.AreEqual("ibus", env["GTK_IM_MODULE"]);
            Assert.AreEqual("@im=ibus", env["XMODIFIERS"]);
            Assert.AreEqual("1", env["IBUS_ENABLE_SYNC_MODE"]);
        }

        [Test]
        public void ConfiguresFcitxWhenDetectedFromXModifiers()
        {
            var env = new Dictionary<string, string> { ["XMODIFIERS"] = "@im=fcitx" };

            Assert.AreEqual("fcitx", Configure(env));
            Assert.AreEqual("fcitx", env["GTK_IM_MODULE"]);
            Assert.IsFalse(env.ContainsKey("IBUS_ENABLE_SYNC_MODE"));
        }

        [Test]
        public void ConfiguresFcitxWhenDetectedFromFcitx5QtImModule()
        {
            var env = new Dictionary<string, string> { ["QT_IM_MODULE"] = "fcitx5" };

            Assert.AreEqual("fcitx", Configure(env));
            Assert.AreEqual("fcitx", env["GTK_IM_MODULE"]);
            Assert.AreEqual("@im=fcitx", env["XMODIFIERS"]);
            Assert.IsFalse(env.ContainsKey("IBUS_ENABLE_SYNC_MODE"));
        }

        [Test]
        public void XModifiersTakesPrecedenceOverQtImModule()
        {
            var env = new Dictionary<string, string>
            {
                ["XMODIFIERS"] = "@im=fcitx",
                ["QT_IM_MODULE"] = "ibus"
            };

            Assert.AreEqual("fcitx", Configure(env));
            Assert.AreEqual("fcitx", env["GTK_IM_MODULE"]);
        }

        [Test]
        public void ExplicitGtkImModuleTakesPrecedence()
        {
            var env = new Dictionary<string, string>
            {
                ["GTK_IM_MODULE"] = "fcitx",
                ["XMODIFIERS"] = "@im=ibus"
            };

            Assert.AreEqual("fcitx", Configure(env));
            Assert.AreEqual("fcitx", env["GTK_IM_MODULE"]);
            Assert.AreEqual("@im=ibus", env["XMODIFIERS"]);
            Assert.IsFalse(env.ContainsKey("IBUS_ENABLE_SYNC_MODE"));
        }

        [Test]
        public void LeavesUnsupportedExplicitGtkImModuleUntouched()
        {
            var env = new Dictionary<string, string>
            {
                ["GTK_IM_MODULE"] = "xim",
                ["XMODIFIERS"] = "@im=ibus"
            };

            Assert.IsNull(Configure(env));
            Assert.AreEqual("xim", env["GTK_IM_MODULE"]);
            Assert.IsFalse(env.ContainsKey("IBUS_ENABLE_SYNC_MODE"));
        }

        [Test]
        public void EnablesIBusSyncModeWhenGtkImModuleIsAlreadyIBus()
        {
            var env = new Dictionary<string, string> { ["GTK_IM_MODULE"] = "ibus" };

            Assert.AreEqual("ibus", Configure(env));
            Assert.AreEqual("1", env["IBUS_ENABLE_SYNC_MODE"]);
        }

        [Test]
        public void PreservesExplicitIBusSyncMode()
        {
            var env = new Dictionary<string, string>
            {
                ["GTK_IM_MODULE"] = "ibus",
                ["IBUS_ENABLE_SYNC_MODE"] = "0"
            };

            Assert.AreEqual("ibus", Configure(env));
            Assert.AreEqual("0", env["IBUS_ENABLE_SYNC_MODE"]);
        }

        [Test]
        public void DoesNothingWhenNoImeIsActive()
        {
            var env = new Dictionary<string, string>();

            Assert.IsNull(Configure(env));
            Assert.IsEmpty(env);
        }
    }
}
