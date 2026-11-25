# CefGlue (Avalonia 12 · Windows & Linux x64)

A fork of [`Deon-Berlin/CefGlue`](https://github.com/Deon-Berlin/CefGlue) focused strictly on **Avalonia 12** on **Windows and Linux x64**.

## Direction & Upstream Dependency

- **Target scope**: .NET 10 + Avalonia 12 on **Windows x64** and **Linux x64** only. macOS, WPF, and ARM64 are out of scope (upstream code is left untouched in the tree to avoid merge conflicts).
- **Parent dependency**: CEF upgrades and the `cef.runtime.win-x64` / `cef.runtime.linux-x64` NuGet packages come from the parent fork [`Deon-Berlin/CefGlue`](https://github.com/Deon-Berlin/CefGlue). Future CEF updates will be tracked by cherry-picking from the parent fork.

## Differences from Parent (`Deon-Berlin/CefGlue`)

Baseline: `Deon-Berlin/CefGlue` `main` (`4a09ce6`).

### Framework & Scope

| Area | Parent (`Deon-Berlin/CefGlue`) | This Fork |
| --- | --- | --- |
| Avalonia | 11.3.17 | **12.2.0** (Avalonia 12 API migration + Wayland/X11 fixes from `OutSystems/CefGlue#249`: pointer-capture `PointerExited` guard and null-safe `VisibilityChanged` on detach) |
| Dependencies | `ReactiveUI.Avalonia`, `System.Reactive.Linq`, `Avalonia.Diagnostics`, unused `System.Runtime.Loader` | Removed; replaced with `System.Threading.Timer` + `Dispatcher.UIThread.Post` and `Avalonia.Reactive.AnonymousObserver` (`AvaloniaUI.DiagnosticsSupport` in demo only) |
| Runtime packages in `CefGlue.Next.Common` | Windows, macOS, Linux (x64 & ARM64 — 6 packages) | **Windows & Linux x64 only** (`CefGlue.WinLinuxOnly.targets` removes `cef.runtime.osx-*` and `*-arm64`) |
| WPF | Supported | Not targeted |

### Linux Behaviour (Linux-only; Windows unchanged)

All items below apply **only on Linux**. **Windows** keeps upstream behaviour unchanged (`MultiThreadedMessageLoop = true`, native Windows IME, no polling timer, no signal/HarfBuzz workarounds).

| Area | Windows | Linux (This Fork) |
| --- | --- | --- |
| CEF message loop | `MultiThreadedMessageLoop = true` (separate CEF thread, event-driven) | **Avalonia UI thread** (`MultiThreadedMessageLoop = false`, `ExternalMessagePump = false`), 60 Hz default timer (`CEFGLUE_MESSAGE_PUMP_INTERVAL_MS` overrides) |
| IME (e.g. Korean) | Native Windows IME (no extra handling needed) | **`LinuxImeEnvironment`** detects IBus / Fcitx / Fcitx5 and populates missing `GTK_IM_MODULE`, `XMODIFIERS`, and `IBUS_ENABLE_SYNC_MODE=1` via managed + native `libc setenv()` before CEF initializes (IBus verified end-to-end; Fcitx/Fcitx5 configured, live session unverified) |
| Signal handlers | Standard Windows SEH | **`LinuxSignalHandlerScope`** saves and restores POSIX signal handlers across `CefRuntime.Initialize()` (no-op on Windows) |
| HarfBuzz symbol clash | Not applicable | **`LinuxNativeLibraryLoader`** preloads `libHarfBuzzSharp.so` with `RTLD_NOW` and `RTLD_GLOBAL` |
| CEF resources (`icudtl.dat`, `*.pak`, `locales/`) | Already flat next to `libcef.dll` | **`FlattenLinuxCefResources`** in `CefGlue.Common.targets` copies `Resources/` next to `libcef.so` on Linux builds |

### Bug Fixes & Tests

| Area | Parent (`Deon-Berlin/CefGlue`) | This Fork |
| --- | --- | --- |
| Native object unregistration | Stale/orphaned bound queries on `UnregisterJavascriptObject` | Fixed (`OutSystems/CefGlue` `#253`, `#255`) in `JavascriptToNativeDispatcherRenderSide` + `NativeObjectUnregistrationTests` |
| Timezone & invalid URL tests | Local `DateTime` comparisons and `http://0.0.0.0` | UTC-aligned comparisons and `http://invalidurl` (`OutSystems/CefGlue#249`) |
| Linux tests | None | `LinuxImeEnvironmentTests` (10 unit tests), `LinuxSignalHandlerTests`, `[Explicit]` live X11 + IBus Hangul E2E test (`LinuxIBusEndToEndTests`) |

### Packaging & Tooling

| Area | Parent (`Deon-Berlin/CefGlue`) | This Fork |
| --- | --- | --- |
| Local NuGet build | CI publish to NuGet.org | `./publish.sh` packs all 5 `CefGlue.Next.*` packages into `Nuget/output/<YYYYMMDD>_<HHMM>/` |
| Demo & IDE workflow | None | `./run_demo.sh` (Win/Linux x64) and `.vscode/` (`tasks.json`, `launch.json`, `settings.json`) |
| `.gitignore` | Standard | Also ignores `GPUCache/` and `DawnCache/` |

## Fork Commands

```bash
# Build all 5 NuGet packages into Nuget/output/<YYYYMMDD>_<HHMM>/
./publish.sh

# Clean-build and run the Avalonia demo (Windows/Linux x64)
./run_demo.sh

# Run tests on Linux x64
dotnet test CefGlue.Tests/CefGlue.Tests.csproj -c Release -r linux-x64

# Run the [Explicit] live X11 + IBus Hangul E2E test on Linux
dotnet test CefGlue.Tests/CefGlue.Tests.csproj -c Release -r linux-x64 -- NUnit.Where="method == EnglishAndKoreanIBusInputWorkInBrowser"
```
