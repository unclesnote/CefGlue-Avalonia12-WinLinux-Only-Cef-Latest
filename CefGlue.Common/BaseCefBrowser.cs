using System;
using System.Threading.Tasks;
using Xilium.CefGlue.Common.Events;
using Xilium.CefGlue.Common.Handlers;
using Xilium.CefGlue.Common.Helpers.Logger;
using Xilium.CefGlue.Common.Platform;

namespace Xilium.CefGlue.Common
{
    /// <summary>
    /// Browser interface shared among the several implementations (Avalonia, WPF, ...)
    /// </summary>
    public abstract partial class BaseCefBrowser : IDisposable
    {
        protected readonly ILogger _logger;

        private readonly CommonBrowserAdapter _adapter;

        #region Disposable

        public BaseCefBrowser(Func<CefRequestContext> cefRequestContextFactory = null, BrowserProcessHandler browserProcessHandler = null)
        {
            if (!CefRuntimeLoader.IsLoaded)
            {
                CefRuntimeLoader.Load(browserProcessHandler);
            }

#if HAS_NLOG
            _logger = new Logger(nameof(BaseCefBrowser));
#else
            _logger = new NullLogger(nameof(BaseCefBrowser));
#endif

            if (CefRuntimeLoader.IsOSREnabled)
            {
                 var offscreen = new CommonOffscreenBrowserAdapter(this, nameof(BaseCefBrowser), CreateOffScreenControlHost(), CreatePopupHost(), _logger, cefRequestContextFactory?.Invoke());
                 offscreen.Paint += OffscreenHostOnPaint;

                 _adapter = offscreen;
            } 
            else
            {
                _adapter = new CommonBrowserAdapter(this, nameof(BaseCefBrowser), CreateControl(), _logger, cefRequestContextFactory?.Invoke());
            }

            _adapter.RuntimeStyle = DefaultRuntimeStyle;
        }

        ~BaseCefBrowser()
        {
            if (CefRuntimeLoader.IsOSREnabled && _adapter is IOffscreenCefBrowserHost offscreenHost)
            {
                offscreenHost.Paint -= OffscreenHostOnPaint;
            }

            Dispose(false);
        }

        private void OffscreenHostOnPaint(object sender, PaintEventArgs e) =>
            OnPaint(e.IsPopup, e.Buffer, e.Width, e.Height, e.DirtyRects);

        protected virtual void OnPaint(bool isPopup, IntPtr buffer, int width, int height, CefRectangle[] dirtyRects)
        {
            // can be overridden by offscreen browser implementations to react on paint events.
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _adapter?.Dispose(disposing);
        }

#endregion

        /// <summary>
        /// Creates the instance of the popup control that will host the browser popups.
        /// </summary>
        /// <returns></returns>
        internal abstract IOffScreenPopupHost CreatePopupHost();

        /// <summary>
        /// Creates the instance of the control that will host the browser.
        /// </summary>
        /// <returns></returns>
        internal abstract IControl CreateControl();

        /// <summary>
        /// Creates the instance of the control that will host the browser in the offscreen mode.
        /// </summary>
        /// <returns></returns>
        internal abstract IOffScreenControlHost CreateOffScreenControlHost();

        /// <summary>
        /// Creates a new handler for managing off-screen keyboard interactions.
        /// </summary>
        /// <returns>An object that implements the <see cref="IOffScreenKeyboardHandler"/> interface for handling off-screen
        /// keyboard input.</returns>
        public abstract IOffScreenKeyboardHandler CreateOffScreenKeyboardHandler(object control);

        /// <summary>
        /// Event fired when the browser is initialized.
        /// </summary>
        public event Action BrowserInitialized { add => _adapter.Initialized += value; remove => _adapter.Initialized -= value; }
        
        /// <summary>
        /// Event fired when the browser starts loading a frame.
        /// </summary>
        public event LoadStartEventHandler LoadStart { add => _adapter.LoadStart += value; remove => _adapter.LoadStart -= value; }

        /// <summary>
        /// Event fired when the browser ends loading a frame.
        /// </summary>
        public event LoadEndEventHandler LoadEnd { add => _adapter.LoadEnd += value; remove => _adapter.LoadEnd -= value; }

        /// <summary>
        /// Event fired when the loading state of a frame changes.
        /// </summary>
        public event LoadingStateChangeEventHandler LoadingStateChange { add => _adapter.LoadingStateChange += value; remove => _adapter.LoadingStateChange -= value; }

        /// <summary>
        /// Event fired when the an error occurs while loading a frame.
        /// </summary>
        public event LoadErrorEventHandler LoadError { add => _adapter.LoadError += value; remove => _adapter.LoadError -= value; }

        /// <summary>
        /// Event fired when the address changes.
        /// </summary>
        public event AddressChangedEventHandler AddressChanged { add => _adapter.AddressChanged += value; remove => _adapter.AddressChanged -= value; }

        /// <summary>
        /// Event fired when a console message arrives.
        /// </summary>
        public event ConsoleMessageEventHandler ConsoleMessage { add => _adapter.ConsoleMessage += value; remove => _adapter.ConsoleMessage -= value; }

        /// <summary>
        /// Event fired when the browser receives a status message. |message| contains the text that will be displayed in the status message.
        /// </summary>
        public event StatusMessageEventHandler StatusMessage { add => _adapter.StatusMessage += value; remove => _adapter.StatusMessage -= value; }

        /// <summary>
        /// Event fired when the title changes.
        /// </summary>
        public event TitleChangedEventHandler TitleChanged { add => _adapter.TitleChanged += value; remove => _adapter.TitleChanged -= value; }

        /// <summary>
        /// Event fired when the javascript context of the specified frame is created and ready to execute.
        /// </summary>
        public event JavascriptContextLifetimeEventHandler JavascriptContextCreated { add => _adapter.JavascriptContextCreated += value; remove => _adapter.JavascriptContextCreated -= value; }

        /// <summary>
        /// Event fired when the javascript context of the specified frame is destroyed.
        /// </summary>
        public event JavascriptContextLifetimeEventHandler JavascriptContextReleased { add => _adapter.JavascriptContextReleased += value; remove => _adapter.JavascriptContextReleased -= value; }

        /// <summary>
        /// Event fired when a javascript uncaugh exception is thrown.
        /// </summary>
        public event JavascriptUncaughtExceptionEventHandler JavascriptUncaughException { add => _adapter.JavascriptUncaughtException += value; remove => _adapter.JavascriptUncaughtException -= value; }

        /// <summary>
        /// Event fired when an internal browser exception is unhandled.
        /// </summary>
        public event AsyncUnhandledExceptionEventHandler UnhandledException { add => _adapter.UnhandledException += value; remove => _adapter.UnhandledException -= value; }
        
        /// <summary>
        /// Return the handler for context menus. If no handler is provided the default implementation will be used.
        /// </summary>
        public ContextMenuHandler ContextMenuHandler { get => _adapter.ContextMenuHandler; set => _adapter.ContextMenuHandler = value; }

        /// <summary>
        /// Return the handler for dialogs. If no handler is provided the default implementation will be used.
        /// </summary>
        public DialogHandler DialogHandler { get => _adapter.DialogHandler; set => _adapter.DialogHandler = value; }

        /// <summary>
        /// Return the handler for download events. If no handler is returned downloads will not be allowed.
        /// </summary>
        public DownloadHandler DownloadHandler { get => _adapter.DownloadHandler; set => _adapter.DownloadHandler = value; }

        /// <summary>
        /// Return the handler for drag events.
        /// </summary>
        public DragHandler DragHandler { get => _adapter.DragHandler; set => _adapter.DragHandler = value; }

        /// <summary>
        /// Return the handler for find result events.
        /// </summary>
        public FindHandler FindHandler { get => _adapter.FindHandler; set => _adapter.FindHandler = value; }

        /// <summary>
        /// Return the handler for focus events.
        /// </summary>
        public FocusHandler FocusHandler { get => _adapter.FocusHandler; set => _adapter.FocusHandler = value; }

        /// <summary>
        /// Return the handler for keyboard events.
        /// </summary>
        public KeyboardHandler KeyboardHandler { get => _adapter.KeyboardHandler; set => _adapter.KeyboardHandler = value; }

        /// <summary>
        /// Return the handler for browser request events.
        /// </summary>
        public RequestHandler RequestHandler { get => _adapter.RequestHandler; set => _adapter.RequestHandler = value; }

        /// <summary>
        /// Return the handler for browser life span events.
        /// </summary>
        public LifeSpanHandler LifeSpanHandler { get => _adapter.LifeSpanHandler; set => _adapter.LifeSpanHandler = value; }

        /// <summary>
        /// Return the handler for browser display state events.
        /// </summary>
        public DisplayHandler DisplayHandler { get => _adapter.DisplayHandler; set => _adapter.DisplayHandler = value; }

        /// <summary>
        /// Return the handler for off-screen rendering events.
        /// </summary>
        public RenderHandler RenderHandler { get => _adapter.RenderHandler; set => _adapter.RenderHandler = value; }

        /// <summary>
        /// Return the handler for JavaScript dialogs. If no handler is provided the default implementation will be used.
        /// </summary>
        public JSDialogHandler JSDialogHandler { get => _adapter.JSDialogHandler; set => _adapter.JSDialogHandler = value; }

        /// <summary>
        /// Gets or set the url.
        /// </summary>
        public partial string Address { get; set; }

        /// <summary>
        /// Returns true when the underlying browser has been initialized.
        /// </summary>
        public bool IsBrowserInitialized => _adapter.IsInitialized;

        /// <summary>
        /// Returns true when the javascript engine of the frame.
        /// </summary>
        public bool IsJavascriptEngineInitialized => _adapter.IsJavascriptEngineInitialized;

        /// <summary>
        /// Returns true if the browser is currently loading.
        /// </summary>
        public bool IsLoading => _adapter.IsLoading;

        /// <summary>
        /// Returns the current page title.
        /// </summary>
        public string Title => _adapter.Title;

        /// <summary>
        /// Gets or sets the initial browser settings.
        /// Changing this property after browser becomes initialized does not have any effects.
        /// </summary>
        public CefBrowserSettings Settings => _adapter.Settings;

        /// <summary>
        /// Gets or sets whether the default download bubble (the download UI displayed by
        /// the Chrome runtime style) is shown when a download starts.
        /// Only applies to browsers using the Chrome runtime style (Alloy style browsers have
        /// no download UI). When set to false the download bubble popup is hidden and downloads
        /// continue silently with their suggested file name, unless a
        /// <see cref="DownloadHandler"/> handles the download itself. Note the download
        /// toolbar button and its start/complete animations cannot be disabled via
        /// preferences; use the Alloy runtime style to remove all Chrome UI.
        /// Must be set before the browser becomes initialized.
        /// </summary>
        public bool ShowDownloadBubble { get => _adapter.ShowDownloadBubble; set => _adapter.ShowDownloadBubble = value; }

        /// <summary>
        /// Gets or sets the application-wide default <see cref="CefRuntimeStyle"/> assigned to newly
        /// created <see cref="BaseCefBrowser"/> instances when they are constructed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Why this option is exposed:</b> Starting in CEF 128+, <see cref="CefRuntimeStyle.Default"/>
        /// resolves to the Chrome runtime style, which wraps each browser in Chromium's full Chrome UI
        /// layer and <c>SessionService</c>. Keeping this default preserves existing upstream behavior
        /// for applications that rely on Chrome runtime features, while letting applications opt in
        /// globally to <see cref="CefRuntimeStyle.Alloy"/> without modifying upstream defaults.
        /// </para>
        /// <para>
        /// <b>Issue background:</b> When embedding multiple browsers backed by persistent
        /// <see cref="CefRequestContext"/> profiles (<c>CachePath</c> set) with
        /// <see cref="CefRequestContextSettings.PersistSessionCookies"/> enabled, CEF's
        /// <c>ChromeBrowserContext</c> configures <c>prefs::kRestoreOnStartup = SessionStartupPref::kPrefValueLast</c>
        /// on the profile (see CEF issue #3767). Under the Chrome runtime style, opening a saved
        /// profile (for example a second site profile or after an unclean exit) can cause Chromium's
        /// <c>SessionService</c>/<c>SessionRestore</c> to spawn an unintended standalone top-level
        /// Chrome browser window (logged as <c>"Creating a chrome browser without a client"</c>)
        /// alongside the embedded control. Setting <see cref="CefRuntimeStyle.Alloy"/> embeds a
        /// lightweight content-layer browser (<c>AlloyBrowserHostImpl</c>) without a Chrome
        /// <c>Browser</c> UI host or <c>SessionService</c>/<c>SessionRestore</c>, eliminating the
        /// extra standalone window and Chrome UI overlays while preserving session cookie persistence.
        /// </para>
        /// <para>
        /// Individual instances can still override <see cref="RuntimeStyle"/> before browser creation.
        /// Windowless (OSR) browsers always use Alloy style in CEF regardless of this property, and
        /// DevTools popups always request <see cref="CefRuntimeStyle.Chrome"/> as required by CEF.
        /// </para>
        /// </remarks>
        /// <example>
        /// Configure once at application startup before creating any browser controls:
        /// <code>
        /// AvaloniaCefBrowser.DefaultRuntimeStyle = CefRuntimeStyle.Alloy;
        /// // or equivalently:
        /// BaseCefBrowser.DefaultRuntimeStyle = CefRuntimeStyle.Alloy;
        /// </code>
        /// </example>
        public static CefRuntimeStyle DefaultRuntimeStyle { get; set; } = CefRuntimeStyle.Default;

        /// <summary>
        /// Gets or sets the <see cref="CefRuntimeStyle"/> applied when creating this browser in
        /// windowed mode. Initialized from <see cref="DefaultRuntimeStyle"/> at construction time
        /// (which defaults to <see cref="CefRuntimeStyle.Default"/> for backwards compatibility).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Why this option is exposed:</b> In CEF 128+, <see cref="CefRuntimeStyle.Default"/>
        /// selects the Chrome runtime style, whereas some embedded webview scenarios require the
        /// Chromium content-layer <see cref="CefRuntimeStyle.Alloy"/> behavior used in older CEF
        /// releases. Exposing this per-instance property (together with <see cref="DefaultRuntimeStyle"/>)
        /// preserves existing behavior by default while allowing selective or global opt-in.
        /// </para>
        /// <para>
        /// <b>Issue background:</b> If you attach a persistent <see cref="CefRequestContext"/> with
        /// <see cref="CefRequestContextSettings.PersistSessionCookies"/> set to <see langword="true"/>,
        /// CEF enables <c>prefs::kRestoreOnStartup = kPrefValueLast</c> on that profile (CEF issue
        /// #3767). With the Chrome runtime style, Chromium's <c>SessionService</c>/<c>SessionRestore</c>
        /// may open a separate unmanaged Chrome window (<c>"Creating a chrome browser without a client"</c>)
        /// when that profile is loaded. Setting <see cref="RuntimeStyle"/> to
        /// <see cref="CefRuntimeStyle.Alloy"/> creates an <c>AlloyBrowserHostImpl</c> instead, which
        /// prevents standalone session-restore windows and Chrome UI overlays (such as download bubbles)
        /// while keeping session cookie persistence intact through <c>ChromeBrowserContext</c>.
        /// </para>
        /// <para>
        /// Must be set before the native browser is created (before initial layout/attachment to the
        /// visual tree). Windowless (OSR) browsers always use Alloy style in CEF, and DevTools
        /// always uses <see cref="CefRuntimeStyle.Chrome"/>.
        /// </para>
        /// </remarks>
        /// <example>
        /// Per-instance configuration in C#:
        /// <code>
        /// var browser = new AvaloniaCefBrowser(GetRequestContext)
        /// {
        ///     RuntimeStyle = CefRuntimeStyle.Alloy
        /// };
        /// </code>
        /// Or in AXAML:
        /// <code>
        /// &lt;cef:AvaloniaCefBrowser RuntimeStyle="Alloy" /&gt;
        /// </code>
        /// </example>
        public CefRuntimeStyle RuntimeStyle { get => _adapter.RuntimeStyle; set => _adapter.RuntimeStyle = value; }

        /// <summary>
        /// Get or set the current zoom level. The default zoom level is 0.0.
        /// </summary>
        public double ZoomLevel { get => _adapter.ZoomLevel; set => _adapter.ZoomLevel = value; }

        /// <summary>
        /// Get <see cref="CefRequestContext"/>
        /// </summary>
        public CefRequestContext RequestContext { get => _adapter.RequestContext; }

        /// <summary>
        /// The undelying cef browser instance. Can be used for advanced functionality.
        /// </summary>
        protected CefBrowser UnderlyingBrowser => _adapter.Browser;

        /// <summary>
        /// Returns true if the browser can navigate backwards.
        /// </summary>
        /// <returns></returns>
        public bool CanGoBack => _adapter.CanGoBack();

        /// <summary>
        /// Navigate backwards.
        /// </summary>
        public void GoBack()
        {
            _adapter.GoBack();
        }

        /// <summary>
        /// Returns true if the browser can navigate forward.
        /// </summary>
        /// <returns></returns>
        public bool CanGoForward => _adapter.CanGoForward();

        /// <summary>
        /// Navigate forwards.
        /// </summary>
        public void GoForward()
        {
            _adapter.GoForward();
        }

        /// <summary>
        /// Reload the current page.
        /// </summary>
        public void Reload(bool ignoreCache = false)
        {
            _adapter.Reload(ignoreCache);
        }

        /// <summary>
        /// Send a key event to the browser.
        /// </summary>
        public void SendKeyEvent(CefKeyEvent keyEvent)
        {
            _adapter.SendKeyEvent(keyEvent);
        }

        /// <summary>
        /// Executes the specified javascript snippet.
        /// </summary>
        /// <param name="code">The javascript snippet.</param>
        /// <param name="url">Url where the script in question can be found.</param>
        /// <param name="line">The base line number to use for error, if any.</param>
        public void ExecuteJavaScript(string code, string url = null, int line = 1)
        {
            _adapter.ExecuteJavaScript(code, url ?? "about:blank", line);
        }

        /// <summary>
        /// Evaluates the specified javascript snippet.
        /// </summary>
        /// <typeparam name="T">The type of the resulting object.</typeparam>
        /// <param name="code">The javascript snippet.</param>
        /// <param name="url">Url where the script in question can be found.</param>
        /// <param name="line">The base line number to use for error, if any.</param>
        /// <param name="frameName">The name of the frame where the script will be executed.</param>
        /// <param name="timeout">Timeout after which the evaluation is cancelled.</param>
        /// <returns>The result of the evaluation.</returns>
        public Task<T> EvaluateJavaScript<T>(string code, string frameName = null, string url = null, int line = 1, TimeSpan? timeout = null)
        {
            return _adapter.EvaluateJavaScript<T>(code, url ?? "about:blank", line, frameName, timeout);
        }

        /// <summary>
        /// Evaluates the specified javascript snippet.
        /// </summary>
        /// <typeparam name="T">The type of the resulting object.</typeparam>
        /// <param name="code">The javascript snippet.</param>
        /// <param name="url">Url where the script in question can be found.</param>
        /// <param name="line">The base line number to use for error, if any.</param>
        /// <param name="frame">The frame where the script will be executed.</param>
        /// <param name="timeout">Timeout after which the evaluation is cancelled.</param>
        /// <returns>The result of the evaluation.</returns>
        public Task<T> EvaluateJavaScript<T>(string code, CefFrame frame, string url = null, int line = 1, TimeSpan? timeout = null)
        {
            return _adapter.EvaluateJavaScript<T>(code, url ?? "about:blank", line, frame, timeout);
        }

        /// <summary>
        /// Opens the Developer tools.
        /// </summary>
        public void ShowDeveloperTools()
        {
            _adapter.ShowDeveloperTools();
        }

        /// <summary>
        /// Closes the Developer tools (opened previously).
        /// </summary>
        public void CloseDeveloperTools()
        {
            _adapter.CloseDeveloperTools();
        }

        /// <summary>
        /// Registers a Javascript object in this browser instance.
        /// </summary>
        /// <param name="targetObject">The object to be made accessible to Javascript</param>
        /// <param name="name">The name of the object. (e.g. "potatoes", if you want the object to be accessible as window.potatoes).</param>
        /// <param name="methodHandler">Optional handler to be executed before the target object method is called. You may use this for instance to log calls.</param>
        public void RegisterJavascriptObject(object targetObject, string name, MethodCallHandler methodHandler = null)
        {
            _adapter.RegisterJavascriptObject(targetObject, name, methodHandler);
        }

        /// <summary>
        /// Unregisters a Javascript object in this browser instance.
        /// </summary>
        /// <param name="name">The name of the object used to register.</param>
        public void UnregisterJavascriptObject(string name)
        {
            _adapter.UnregisterJavascriptObject(name);
        }

        /// <summary>
        /// Returns true if an object has already beend regsisterd with the specified name.
        /// </summary>
        /// <param name="name">Name of the object to check.</param>
        /// <returns></returns>
        public bool IsJavascriptObjectRegistered(string name)
        {
            return _adapter.IsJavascriptObjectRegistered(name);
        }

        public void Zoom(CefZoomCommand command)
        {
            _adapter.Zoom(command);
        }

        /// <summary>
        /// Creates the browser (if not created yet) with the specified dimensions.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns>True if browser was created, false otherwise.</returns>
        protected bool CreateBrowser(int width, int height)
        {
            return _adapter.CreateBrowser(width, height);
        }
    }
}
