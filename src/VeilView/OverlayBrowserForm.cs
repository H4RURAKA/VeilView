using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace VeilView;

internal sealed class OverlayBrowserForm : Form
{
    private static readonly OpacityPreset[] OpacityPresets =
    {
        new(1.00, 0),
        new(0.70, 30),
        new(0.30, 70)
    };

    private readonly AppOptions _options;
    private readonly AppSettings _settings;

    private readonly TabControl _tabs = new();
    private readonly TextBox _urlBox = new();
    private readonly Button _modeButton = new();
    private readonly Button _goButton = new();
    private readonly Button _backButton = new();
    private readonly Button _forwardButton = new();
    private readonly Button _reloadButton = new();
    private readonly Button _newTabButton = new();
    private readonly Button _closeTabButton = new();
    private readonly Button _opacity0Button = new();
    private readonly Button _opacity30Button = new();
    private readonly Button _opacity70Button = new();
    private readonly Button _topMostButton = new();
    private readonly Label _statusLabel = new();
    private readonly ToolTip _toolTip = new();

    private bool _keyboardPreserveEnabled = true;
    private bool _urlChangeComesFromBrowser;
    private bool _closingTab;
    private IntPtr _lastForegroundWindow = IntPtr.Zero;

    public OverlayBrowserForm(AppOptions options, AppSettings settings)
    {
        _options = options;
        _settings = settings;

        Text = "VeilView";
        StartPosition = FormStartPosition.Manual;
        Location = new Point(_options.X, _options.Y);
        Size = new Size(_options.Width, _options.Height);
        MinimumSize = new Size(500, 320);
        TopMost = _options.TopMost;
        Opacity = OpacityFromTransparency(_options.TransparencyPercent);
        ShowInTaskbar = true;
        FormBorderStyle = FormBorderStyle.Sizable;

        BuildUi();

        Load += OnLoaded;
        Shown += (_, _) =>
        {
            CaptureCurrentForegroundWindow();
            ApplyWindowMode(noActivate: true);
        };
        FormClosing += (_, _) => SaveCurrentSettings();
    }

    protected override bool ShowWithoutActivation => _keyboardPreserveEnabled;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;

            if (TopMost)
            {
                cp.ExStyle |= NativeMethods.WS_EX_TOPMOST;
            }

            if (_keyboardPreserveEnabled)
            {
                cp.ExStyle |= NativeMethods.WS_EX_NOACTIVATE;
            }

            return cp;
        }
    }

    private BrowserTab? CurrentTab => _tabs.SelectedTab?.Tag as BrowserTab;

    private WebView2? CurrentBrowser => CurrentTab?.Browser;


    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!_keyboardPreserveEnabled)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.T:
                    _ = CreateNewTabAsync("about:blank", select: true, navigate: true);
                    return true;
                case Keys.Control | Keys.W:
                    CloseCurrentTab();
                    return true;
                case Keys.Control | Keys.L:
                    _urlBox.Focus();
                    _urlBox.SelectAll();
                    return true;
                case Keys.Control | Keys.R:
                    CurrentBrowser?.CoreWebView2?.Reload();
                    return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void WndProc(ref Message m)
    {
        if (_keyboardPreserveEnabled && m.Msg == NativeMethods.WM_MOUSEACTIVATE)
        {
            m.Result = new IntPtr(NativeMethods.MA_NOACTIVATE);
            return;
        }

        base.WndProc(ref m);
    }

    private void BuildUi()
    {
        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 42,
            Padding = new Padding(6, 6, 6, 5),
            BackColor = Color.FromArgb(245, 245, 245)
        };

        ConfigureButton(_backButton, "‹", 32, DockStyle.Left, (_, _) =>
        {
            if (CurrentBrowser?.CoreWebView2?.CanGoBack == true) CurrentBrowser.CoreWebView2.GoBack();
        }, "뒤로");

        ConfigureButton(_forwardButton, "›", 32, DockStyle.Left, (_, _) =>
        {
            if (CurrentBrowser?.CoreWebView2?.CanGoForward == true) CurrentBrowser.CoreWebView2.GoForward();
        }, "앞으로");

        ConfigureButton(_reloadButton, "⟳", 36, DockStyle.Left, (_, _) => CurrentBrowser?.CoreWebView2?.Reload(), "새로고침");
        ConfigureButton(_newTabButton, "+", 32, DockStyle.Left, async (_, _) => await CreateNewTabAsync("about:blank", select: true, navigate: true), "새 탭");
        ConfigureButton(_closeTabButton, "×", 32, DockStyle.Left, (_, _) => CloseCurrentTab(), "현재 탭 닫기");

        ConfigureButton(_modeButton, "주소 입력", 92, DockStyle.Right, (_, _) => SetKeyboardPreserve(!_keyboardPreserveEnabled), "주소창이나 웹페이지에 직접 입력할 때 사용");
        ConfigureButton(_goButton, "이동", 48, DockStyle.Right, (_, _) => NavigateFromUrlBox(), "주소 이동");
        ConfigureButton(_opacity70Button, "70%", 52, DockStyle.Right, (_, _) => SetTransparencyPreset(70), "투명도 70%");
        ConfigureButton(_opacity30Button, "30%", 52, DockStyle.Right, (_, _) => SetTransparencyPreset(30), "투명도 30%");
        ConfigureButton(_opacity0Button, "0%", 52, DockStyle.Right, (_, _) => SetTransparencyPreset(0), "투명도 0%");
        ConfigureButton(_topMostButton, TopMost ? "항상 위" : "일반", 66, DockStyle.Right, (_, _) => ToggleTopMost(), "항상 위 토글");

        _urlBox.Dock = DockStyle.Fill;
        _urlBox.ReadOnly = true;
        _urlBox.BorderStyle = BorderStyle.FixedSingle;
        _urlBox.Text = _options.Url;
        _urlBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                NavigateFromUrlBox();
            }
        };

        _statusLabel.Dock = DockStyle.Bottom;
        _statusLabel.Height = 22;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Padding = new Padding(8, 0, 0, 0);
        _statusLabel.ForeColor = Color.DimGray;

        _tabs.Dock = DockStyle.Fill;
        _tabs.Padding = new Point(12, 4);
        _tabs.SelectedIndexChanged += (_, _) =>
        {
            if (!_closingTab)
            {
                SyncUiFromCurrentTab();
            }
        };

        toolbar.Controls.Add(_urlBox);
        toolbar.Controls.Add(_goButton);
        toolbar.Controls.Add(_modeButton);
        toolbar.Controls.Add(_opacity70Button);
        toolbar.Controls.Add(_opacity30Button);
        toolbar.Controls.Add(_opacity0Button);
        toolbar.Controls.Add(_topMostButton);
        toolbar.Controls.Add(_closeTabButton);
        toolbar.Controls.Add(_newTabButton);
        toolbar.Controls.Add(_reloadButton);
        toolbar.Controls.Add(_forwardButton);
        toolbar.Controls.Add(_backButton);

        Controls.Add(_tabs);
        Controls.Add(_statusLabel);
        Controls.Add(toolbar);

        UpdateModeUi();
        UpdateOpacityButtons();
        UpdateNavButtons();
    }

    private void ConfigureButton(Button button, string text, int width, DockStyle dock, EventHandler click, string? tooltip = null)
    {
        button.Text = text;
        button.Width = width;
        button.Dock = dock;
        button.Margin = Padding.Empty;
        button.Click += click;

        if (!string.IsNullOrWhiteSpace(tooltip))
        {
            _toolTip.SetToolTip(button, tooltip);
        }
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        try
        {
            Directory.CreateDirectory(AppSettings.WebViewUserDataFolder);

            var runtimeVersion = CoreWebView2Environment.GetAvailableBrowserVersionString();
            if (string.IsNullOrWhiteSpace(runtimeVersion))
            {
                throw new InvalidOperationException("Microsoft Edge WebView2 Runtime이 감지되지 않았습니다.");
            }

            var requestedTabs = (_options.UrlWasSpecified || _options.StartTabs.Length > 0)
                ? new[] { _options.Url }
                    .Concat(_options.StartTabs)
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Take(12)
                    .ToArray()
                : Array.Empty<string>();

            var restoredTabs = requestedTabs.Length > 0
                ? requestedTabs
                : (_settings.LastTabs ?? Array.Empty<string>())
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(12)
                    .ToArray();

            if (restoredTabs.Length == 0)
            {
                await CreateNewTabAsync(NormalizeUrl(_options.Url), select: true, navigate: true);
            }
            else
            {
                for (var i = 0; i < restoredTabs.Length; i++)
                {
                    await CreateNewTabAsync(restoredTabs[i], select: false, navigate: true);
                }

                _tabs.SelectedIndex = requestedTabs.Length > 0
                    ? 0
                    : Math.Clamp(_settings.ActiveTabIndex, 0, _tabs.TabPages.Count - 1);
                SyncUiFromCurrentTab();
            }

            ApplyWindowMode(noActivate: true);
        }
        catch (DllNotFoundException ex)
        {
            MessageBox.Show(
                this,
                "WebView2Loader.dll을 불러오지 못했습니다. 단일 실행 파일 빌드는 IncludeNativeLibrariesForSelfExtract=true 옵션이 필요합니다.\n\n" +
                "먼저 BUILD_SINGLE_EXE.cmd로 다시 빌드한 뒤 dist\\VeilView.exe만 이동해 보세요.\n" +
                "그래도 실패하면 BUILD_PORTABLE_FOLDER.cmd로 폴더 번들을 사용하세요.\n\n" +
                "실행 위치: " + AppContext.BaseDirectory + "\n\n" +
                "오류: " + ex.Message,
                "VeilView",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                "WebView2 초기화에 실패했습니다. WebView2 Runtime 설치 상태 또는 빌드 산출물을 확인하세요.\n\n" +
                "단일 exe 빌드: BUILD_SINGLE_EXE.cmd\n" +
                "폴더 번들 빌드: BUILD_PORTABLE_FOLDER.cmd\n\n" +
                "실행 위치: " + AppContext.BaseDirectory + "\n\n" +
                "오류: " + ex.GetBaseException().Message,
                "VeilView",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async Task<BrowserTab> CreateNewTabAsync(string? initialUrl, bool select, bool navigate)
    {
        var tabPage = new TabPage("새 탭");
        var browser = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = Color.White,
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = AppSettings.WebViewUserDataFolder
            }
        };

        var tab = new BrowserTab(tabPage, browser);
        tabPage.Tag = tab;
        tabPage.Controls.Add(browser);
        _tabs.TabPages.Add(tabPage);

        if (select)
        {
            _tabs.SelectedTab = tabPage;
        }

        await browser.EnsureCoreWebView2Async();
        ConfigureWebView(tab);

        if (navigate)
        {
            var target = string.IsNullOrWhiteSpace(initialUrl) ? "about:blank" : NormalizeUrl(initialUrl);
            tab.Url = target;
            browser.CoreWebView2.Navigate(target);
        }

        SyncUiFromCurrentTab();
        return tab;
    }

    private void ConfigureWebView(BrowserTab tab)
    {
        var webView = tab.Browser.CoreWebView2;
        if (webView is null) return;

        webView.Settings.AreDefaultContextMenusEnabled = true;
        webView.Settings.AreDevToolsEnabled = false;
        webView.Settings.AreBrowserAcceleratorKeysEnabled = false;
        webView.Settings.IsStatusBarEnabled = false;
        webView.Settings.IsZoomControlEnabled = true;
        webView.Settings.IsGeneralAutofillEnabled = false;
        webView.Settings.IsPasswordAutosaveEnabled = false;

        webView.SourceChanged += (_, _) =>
        {
            SyncUrlFromBrowser(tab, webView.Source);
            UpdateTabTitle(tab);
        };

        webView.NavigationCompleted += (_, _) =>
        {
            SyncUrlFromBrowser(tab, webView.Source);
            UpdateTabTitle(tab);
            if (ReferenceEquals(CurrentTab, tab))
            {
                UpdateNavButtons();
            }
        };

        webView.HistoryChanged += (_, _) =>
        {
            if (ReferenceEquals(CurrentTab, tab))
            {
                UpdateNavButtons();
            }
        };

        webView.DocumentTitleChanged += (_, _) => UpdateTabTitle(tab);
        webView.NewWindowRequested += (_, e) => HandleNewWindowRequested(e);

        UpdateTabTitle(tab);
        UpdateNavButtons();
    }

    private async void HandleNewWindowRequested(CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        CoreWebView2Deferral? deferral = null;

        try
        {
            deferral = e.GetDeferral();
            var newTab = await CreateNewTabAsync("about:blank", select: true, navigate: false);

            if (newTab.Browser.CoreWebView2 is null)
            {
                throw new InvalidOperationException("새 탭 WebView2 초기화에 실패했습니다.");
            }

            e.NewWindow = newTab.Browser.CoreWebView2;
            if (!string.IsNullOrWhiteSpace(e.Uri))
            {
                newTab.Url = e.Uri;
                UpdateTabTitle(newTab);
            }
        }
        catch
        {
            var fallbackUri = string.IsNullOrWhiteSpace(e.Uri) ? "about:blank" : e.Uri;
            BeginInvoke(new Action(async () => await CreateNewTabAsync(fallbackUri, select: true, navigate: true)));
        }
        finally
        {
            deferral?.Complete();
        }
    }

    private void CloseCurrentTab()
    {
        if (CurrentTab is null) return;

        if (_tabs.TabPages.Count <= 1)
        {
            CurrentBrowser?.CoreWebView2?.Navigate("about:blank");
            if (CurrentTab is not null)
            {
                CurrentTab.Url = "about:blank";
                CurrentTab.Page.Text = "새 탭";
            }
            SyncUiFromCurrentTab();
            return;
        }

        _closingTab = true;
        var page = CurrentTab.Page;
        var index = _tabs.SelectedIndex;
        _tabs.TabPages.Remove(page);
        page.Dispose();
        _closingTab = false;

        if (_tabs.TabPages.Count > 0)
        {
            _tabs.SelectedIndex = Math.Min(index, _tabs.TabPages.Count - 1);
        }

        SyncUiFromCurrentTab();
    }

    private void UpdateTabTitle(BrowserTab tab)
    {
        if (tab.Browser.CoreWebView2 is null) return;

        var title = tab.Browser.CoreWebView2.DocumentTitle;
        if (string.IsNullOrWhiteSpace(title))
        {
            title = GetDisplayUrl(tab.Browser.CoreWebView2.Source);
        }

        tab.Page.Text = ShortenTitle(title, 18);
    }

    private void SyncUiFromCurrentTab()
    {
        if (CurrentTab is not null && CurrentBrowser?.CoreWebView2 is not null)
        {
            SyncUrlFromBrowser(CurrentTab, CurrentBrowser.CoreWebView2.Source);
        }
        else
        {
            _urlChangeComesFromBrowser = true;
            _urlBox.Text = "about:blank";
            _urlChangeComesFromBrowser = false;
        }

        UpdateNavButtons();
        UpdateModeUi();
    }

    private void UpdateNavButtons()
    {
        var webView = CurrentBrowser?.CoreWebView2;

        _backButton.Enabled = webView?.CanGoBack == true;
        _forwardButton.Enabled = webView?.CanGoForward == true;
        _reloadButton.Enabled = webView is not null;
        _goButton.Enabled = CurrentBrowser is not null;
        _closeTabButton.Enabled = _tabs.TabPages.Count > 1;
    }

    private void SyncUrlFromBrowser(BrowserTab tab, string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        tab.Url = url;
        if (!ReferenceEquals(CurrentTab, tab)) return;

        _urlChangeComesFromBrowser = true;
        _urlBox.Text = url;
        _urlChangeComesFromBrowser = false;
        _settings.LastUrl = url;
    }

    private void NavigateFromUrlBox()
    {
        if (_urlChangeComesFromBrowser) return;

        var target = NormalizeUrl(_urlBox.Text);
        _settings.LastUrl = target;

        var browser = CurrentBrowser;
        if (browser?.CoreWebView2 is null)
        {
            _ = CreateNewTabAsync(target, select: true, navigate: true);
        }
        else
        {
            if (CurrentTab is not null)
            {
                CurrentTab.Url = target;
            }
            browser.CoreWebView2.Navigate(target);
        }

        SetKeyboardPreserve(enabled: true);
    }

    private void SetKeyboardPreserve(bool enabled)
    {
        if (!enabled)
        {
            CaptureCurrentForegroundWindow();
        }

        _keyboardPreserveEnabled = enabled;
        _urlBox.ReadOnly = enabled;

        UpdateModeUi();
        ApplyWindowMode(noActivate: enabled);

        if (!enabled)
        {
            Activate();
            _urlBox.Focus();
            _urlBox.SelectAll();
        }
        else
        {
            RestoreLastForegroundWindow();
        }
    }

    private void UpdateModeUi()
    {
        _modeButton.Text = _keyboardPreserveEnabled ? "주소 입력" : "보존 복귀";
        _topMostButton.Text = TopMost ? "항상 위" : "일반";
        UpdateOpacityButtons();

        _statusLabel.Text = _keyboardPreserveEnabled
            ? "키보드 보존: 마우스 조작은 VeilView에서 처리하고, 키보드는 기존 활성 창에 남깁니다. 새탭에서 열기는 VeilView 탭으로 열립니다."
            : "주소 입력: VeilView가 잠시 키보드를 받습니다. Enter 또는 [이동] 후 키보드 보존으로 돌아갑니다.";
    }

    private void CaptureCurrentForegroundWindow()
    {
        if (!IsHandleCreated) return;

        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground != IntPtr.Zero && foreground != Handle && NativeMethods.IsWindow(foreground))
        {
            _lastForegroundWindow = foreground;
        }
    }

    private void RestoreLastForegroundWindow()
    {
        if (_lastForegroundWindow != IntPtr.Zero && NativeMethods.IsWindow(_lastForegroundWindow))
        {
            NativeMethods.SetForegroundWindow(_lastForegroundWindow);
        }
    }

    private void ApplyWindowMode(bool noActivate)
    {
        if (!IsHandleCreated) return;

        var current = NativeMethods.GetWindowLongPtrSafe(Handle, NativeMethods.GWL_EXSTYLE).ToInt64();
        var next = current;

        if (TopMost)
        {
            next |= NativeMethods.WS_EX_TOPMOST;
        }
        else
        {
            next &= ~NativeMethods.WS_EX_TOPMOST;
        }

        if (_keyboardPreserveEnabled)
        {
            next |= NativeMethods.WS_EX_NOACTIVATE;
        }
        else
        {
            next &= ~NativeMethods.WS_EX_NOACTIVATE;
        }

        if (next != current)
        {
            NativeMethods.SetWindowLongPtrSafe(Handle, NativeMethods.GWL_EXSTYLE, new IntPtr(next));
        }

        var flags = NativeMethods.SWP_NOMOVE
                    | NativeMethods.SWP_NOSIZE
                    | NativeMethods.SWP_NOOWNERZORDER
                    | NativeMethods.SWP_FRAMECHANGED;

        if (noActivate)
        {
            flags |= NativeMethods.SWP_NOACTIVATE;
        }

        var zOrder = TopMost ? NativeMethods.HWND_TOPMOST : NativeMethods.HWND_NOTOPMOST;
        NativeMethods.SetWindowPos(Handle, zOrder, 0, 0, 0, 0, flags);
    }

    private void ToggleTopMost()
    {
        TopMost = !TopMost;
        _settings.TopMost = TopMost;
        UpdateModeUi();
        ApplyWindowMode(noActivate: _keyboardPreserveEnabled);
    }

    private void SetTransparencyPreset(int transparencyPercent)
    {
        var preset = OpacityPresets.FirstOrDefault(p => p.TransparencyPercent == transparencyPercent);
        if (preset == default)
        {
            preset = OpacityPresets[0];
        }

        Opacity = preset.Opacity;
        _settings.TransparencyPercent = preset.TransparencyPercent;
        UpdateOpacityButtons();
    }

    private void UpdateOpacityButtons()
    {
        var percent = GetTransparencyPercent(Opacity);
        _opacity0Button.Text = percent == 0 ? "0% ✓" : "0%";
        _opacity30Button.Text = percent == 30 ? "30% ✓" : "30%";
        _opacity70Button.Text = percent == 70 ? "70% ✓" : "70%";
    }

    private void SaveCurrentSettings()
    {
        _settings.X = Location.X;
        _settings.Y = Location.Y;
        _settings.Width = Math.Max(MinimumSize.Width, Width);
        _settings.Height = Math.Max(MinimumSize.Height, Height);
        _settings.TransparencyPercent = GetTransparencyPercent(Opacity);
        _settings.TopMost = TopMost;

        if (!string.IsNullOrWhiteSpace(_urlBox.Text))
        {
            _settings.LastUrl = _urlBox.Text;
        }

        _settings.LastTabs = _tabs.TabPages
            .Cast<TabPage>()
            .Select(page => page.Tag as BrowserTab)
            .Where(tab => tab is not null)
            .Select(tab => tab!.Url)
            .Where(url => !string.IsNullOrWhiteSpace(url)
                          && !url.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
            .Take(12)
            .ToArray();
        _settings.ActiveTabIndex = Math.Max(0, _tabs.SelectedIndex);

        try
        {
            _settings.Save();
        }
        catch
        {
            // Settings persistence is non-critical. Do not block shutdown.
        }
    }

    private static string NormalizeUrl(string rawInput)
    {
        var input = rawInput.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            return "about:blank";
        }

        if (input.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
        {
            return "about:blank";
        }

        if (Uri.TryCreate(input, UriKind.Absolute, out var absoluteUri)
            && (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps || absoluteUri.Scheme == Uri.UriSchemeFile))
        {
            return absoluteUri.ToString();
        }

        if (input.Contains('.') && !input.Contains(' '))
        {
            return "https://" + input;
        }

        return "https://www.google.com/search?q=" + Uri.EscapeDataString(input);
    }

    private static string GetDisplayUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl)) return "새 탭";
        if (rawUrl.Equals("about:blank", StringComparison.OrdinalIgnoreCase)) return "새 탭";

        if (Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
        {
            return string.IsNullOrWhiteSpace(uri.Host) ? rawUrl : uri.Host;
        }

        return rawUrl;
    }

    private static string ShortenTitle(string title, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(title)) return "새 탭";
        title = title.Trim();
        return title.Length <= maxLength ? title : title[..Math.Max(1, maxLength - 1)] + "…";
    }

    private static int GetOpacityPresetIndex(double opacity)
    {
        var bestIndex = 0;
        var bestDistance = double.MaxValue;

        for (var i = 0; i < OpacityPresets.Length; i++)
        {
            var distance = Math.Abs(OpacityPresets[i].Opacity - opacity);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static double SnapOpacity(double opacity) => OpacityPresets[GetOpacityPresetIndex(opacity)].Opacity;

    private static double OpacityFromTransparency(int transparencyPercent)
    {
        var preset = OpacityPresets.FirstOrDefault(p => p.TransparencyPercent == transparencyPercent);
        return preset?.Opacity ?? OpacityPresets[0].Opacity;
    }

    private static int GetTransparencyPercent(double opacity) => OpacityPresets[GetOpacityPresetIndex(opacity)].TransparencyPercent;

    private sealed class BrowserTab
    {
        public BrowserTab(TabPage page, WebView2 browser)
        {
            Page = page;
            Browser = browser;
        }

        public TabPage Page { get; }
        public WebView2 Browser { get; }
        public string Url { get; set; } = "about:blank";
    }

    private sealed record OpacityPreset(double Opacity, int TransparencyPercent);
}
