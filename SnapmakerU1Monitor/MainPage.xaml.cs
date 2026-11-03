using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using MauiPreferences = Microsoft.Maui.Storage.Preferences;
using MauiKeyboard = Microsoft.Maui.Keyboard;

namespace SnapmakerU1Monitor;

public partial class MainPage : ContentPage
{
    private readonly PrinterService _printer = new();

    private readonly HttpClient _thumbnailClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    private const string PrinterIpKey = "printer_ip";
    private const string PrinterPortKey = "printer_port";
    private const int DefaultPrinterPort = 7125;

    private CancellationTokenSource? _updateCts;
    private Task? _updateTask;

    private bool _startupStarted;
    private bool _isBusy;
    private bool? _landscapeLayoutApplied;

    private string _thumbnailFilename = "";
    private ImageSource? _thumbnailSource;
    private DateTime _lastThumbnailAttempt = DateTime.MinValue;

    private string _lastLoggedState = "";
    private string _lastConnectionProblem = "";

    private int _updateCounter;

#if ANDROID
    // These fields are only used by Android screen automation.
    private bool _initialPrinterStateHandled;
    private bool _printWasActive;
    private DateTime? _screenLockAtUtc;

    private AndroidScreenController? _screenController;
    private PrintNotificationManager? _printNotificationManager;
    private bool _screenLockPromptShown;
#endif

    public MainPage()
    {
        InitializeComponent();
        ApplyLanguage();
        SizeChanged += OnPageSizeChanged;

        string savedIp =
            MauiPreferences.Get(PrinterIpKey, "");

        int savedPort =
            MauiPreferences.Get(
                PrinterPortKey,
                DefaultPrinterPort);

        if (!string.IsNullOrWhiteSpace(savedIp))
        {
            _printer.BaseUrl =
                $"http://{savedIp}:{savedPort}";
        }

        Diagnostics.Log("APPLICATION STARTED");

        Diagnostics.Log(
            "PRINTER ADDRESS: " +
            (string.IsNullOrWhiteSpace(_printer.BaseUrl)
                ? "NOT CONFIGURED"
                : _printer.BaseUrl));

        Diagnostics.Log(
            "Log file: " + Diagnostics.GetLogFilePath());
    }

    // =====================================
    // RESPONSIVE PORTRAIT / LANDSCAPE LAYOUT
    // =====================================

    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        ApplyResponsiveLayout();
    }

    private void ApplyResponsiveLayout()
    {
        if (Width <= 0 || Height <= 0)
            return;

        bool isLandscape = Width > Height;

        if (_landscapeLayoutApplied == isLandscape)
            return;

        _landscapeLayoutApplied = isLandscape;

        ResponsiveGrid.RowDefinitions.Clear();
        ResponsiveGrid.ColumnDefinitions.Clear();

        if (isLandscape)
        {
            // Wide layout: status/job | progress | time/finish.
            ResponsiveGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) });
            ResponsiveGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            ResponsiveGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) });

            ResponsiveGrid.RowDefinitions.Add(
                new RowDefinition { Height = GridLength.Auto });
            ResponsiveGrid.RowDefinitions.Add(
                new RowDefinition { Height = GridLength.Auto });

            Grid.SetRow(StatusLabel, 0);
            Grid.SetColumn(StatusLabel, 0);

            Grid.SetRow(JobGrid, 1);
            Grid.SetColumn(JobGrid, 0);

            Grid.SetRow(PrintProgress, 0);
            Grid.SetColumn(PrintProgress, 1);

            Grid.SetRow(ProgressLabel, 1);
            Grid.SetColumn(ProgressLabel, 1);

            Grid.SetRow(TimeGrid, 0);
            Grid.SetColumn(TimeGrid, 2);

            Grid.SetRow(FinishGrid, 1);
            Grid.SetColumn(FinishGrid, 2);

            JobGrid.ColumnDefinitions[0].Width = new GridLength(76);
            JobThumbnail.WidthRequest = 68;
            JobThumbnail.HeightRequest = 68;
            JobLabel.FontSize = 14;

            StatusLabel.FontSize = 32;
            ProgressLabel.FontSize = 22;
            ElapsedTitleLabel.FontSize = 9;
            RemainingTitleLabel.FontSize = 9;
            ElapsedLabel.FontSize = 16;
            RemainingLabel.FontSize = 16;
            FinishTitleLabel.FontSize = 10;
            FinishTimeLabel.FontSize = 22;

            ResponsiveGrid.RowSpacing = 10;
            ResponsiveGrid.ColumnSpacing = 12;
            ResponsiveGrid.VerticalOptions = LayoutOptions.Center;
        }
        else
        {
            // Portrait layout: the familiar single-column arrangement.
            ResponsiveGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = GridLength.Star });

            for (int i = 0; i < 6; i++)
            {
                ResponsiveGrid.RowDefinitions.Add(
                    new RowDefinition { Height = GridLength.Auto });
            }

            Grid.SetRow(StatusLabel, 0);
            Grid.SetColumn(StatusLabel, 0);

            Grid.SetRow(JobGrid, 1);
            Grid.SetColumn(JobGrid, 0);

            Grid.SetRow(PrintProgress, 2);
            Grid.SetColumn(PrintProgress, 0);

            Grid.SetRow(ProgressLabel, 3);
            Grid.SetColumn(ProgressLabel, 0);

            Grid.SetRow(TimeGrid, 4);
            Grid.SetColumn(TimeGrid, 0);

            Grid.SetRow(FinishGrid, 5);
            Grid.SetColumn(FinishGrid, 0);

            JobGrid.ColumnDefinitions[0].Width = new GridLength(100);
            JobThumbnail.WidthRequest = 90;
            JobThumbnail.HeightRequest = 90;
            JobLabel.FontSize = 17;

            StatusLabel.FontSize = 44;
            ProgressLabel.FontSize = 26;
            ElapsedTitleLabel.FontSize = 11;
            RemainingTitleLabel.FontSize = 11;
            ElapsedLabel.FontSize = 20;
            RemainingLabel.FontSize = 20;
            FinishTitleLabel.FontSize = 12;
            FinishTimeLabel.FontSize = 27;

            ResponsiveGrid.RowSpacing = 14;
            ResponsiveGrid.ColumnSpacing = 14;
            ResponsiveGrid.VerticalOptions = LayoutOptions.Start;
        }

        Diagnostics.Log(
            isLandscape
                ? "LAYOUT CHANGED: LANDSCAPE"
                : "LAYOUT CHANGED: PORTRAIT");
    }

    // =====================================
    // STARTUP
    // =====================================

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Diagnostics.Log("PAGE APPEARING");

#if ANDROID
        InitializeScreenController();
#endif

        if (!_startupStarted)
        {
            _startupStarted = true;
            _ = InitializeApplicationAsync();
        }
    }

    protected override void OnDisappearing()
    {
        Diagnostics.Log("PAGE DISAPPEARING");

        // Не спираме следенето при изгасен дисплей.

        base.OnDisappearing();
    }

    private async Task InitializeApplicationAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_printer.BaseUrl))
            {
                ShowSetupRequired();

                bool configured =
                    await PromptForPrinterIpAsync();

                if (!configured)
                {
                    ShowSetupRequired();
                    return;
                }
            }

#if ANDROID
            await EnsurePrintNotificationPermissionAsync();
            await EnsureScreenLockAccessAsync();
#endif

            StartUpdateLoop();
        }
        catch (Exception ex)
        {
            Diagnostics.LogException(
                "InitializeApplicationAsync",
                ex);

            ShowApplicationError(ex.Message);
        }
    }

    private void StartUpdateLoop()
    {
        if (_updateTask != null && !_updateTask.IsCompleted)
            return;

        _updateCts?.Dispose();

        _updateCts = new CancellationTokenSource();

#if ANDROID
        // Keep the CPU and Wi-Fi awake only while the printer update loop runs.
        _screenController?.StartMonitoring();
#endif

        _updateTask =
            RunUpdateLoopAsync(_updateCts.Token);
    }

#if ANDROID

    private void InitializeScreenController()
    {
        try
        {
            var activity = Platform.CurrentActivity;

            if (activity == null)
                return;

            if (_screenController == null)
            {
                _screenController =
                    new AndroidScreenController(activity);

                _screenController.StartMonitoring();

                Diagnostics.Log(
                    "SCREEN CONTROLLER INITIALIZED");
            }
        }
        catch (Exception ex)
        {
            Diagnostics.LogException(
                "InitializeScreenController",
                ex);
        }
    }

    private async Task EnsurePrintNotificationPermissionAsync()
    {
        try
        {
            _printNotificationManager ??=
                new PrintNotificationManager();

            bool enabled =
                await _printNotificationManager.InitializeAsync();

            Diagnostics.Log(
                "PRINT NOTIFICATIONS ENABLED: " + enabled);
        }
        catch (Exception ex)
        {
            Diagnostics.LogException(
                "EnsurePrintNotificationPermissionAsync",
                ex);
        }
    }

    private async Task EnsureScreenLockAccessAsync()
    {
        try
        {
            if (_screenController == null || _screenLockPromptShown)
                return;

            // Android 9 (API 28) and later use the system accessibility
            // global lock action. Unlike DevicePolicyManager.LockNow(),
            // it does not explicitly demand strong authentication after
            // every lock request. The actual biometric behavior still
            // needs to be verified on each OEM build.
            if (_screenController.UsesAccessibilityLock)
            {
                if (_screenController.IsAccessibilityLockEnabled())
                {
                    Diagnostics.Log("ACCESSIBILITY SCREEN LOCK SERVICE ENABLED");
                    return;
                }

                _screenLockPromptShown = true;

                bool enableAccessibilityLock = await DisplayAlertAsync(
                    "Заключване на екрана",
                    "За да може приложението да заключва екрана след 15 минути и да запази стандартното отключване с PIN, отпечатък или лице, Android изисква да включиш услугата за заключване на Snapmaker U1 Monitor IC в Accessibility settings. Услугата изпраща само системната команда за заключване и не заявява достъп до съдържанието на екрана. Да отворя ли настройките?",
                    "Отвори настройки",
                    "По-късно");

                if (enableAccessibilityLock)
                {
                    _screenController.RequestAccessibilityLockActivation();
                    Diagnostics.Log("REQUESTING ACCESSIBILITY SCREEN LOCK SERVICE");
                }

                return;
            }

            // Android 8.1 and older keep the existing Device Admin path
            // for compatibility with the LG G2.
            if (_screenController.IsAdminEnabled())
                return;

            _screenLockPromptShown = true;

            bool enableDeviceAdmin = await DisplayAlertAsync(
                T("screen_management"),
                T("screen_management_message"),
                T("continue"),
                T("later"));

            if (enableDeviceAdmin)
            {
                _screenController.RequestAdminActivation();
                Diagnostics.Log("REQUESTING DEVICE ADMIN");
            }
        }
        catch (Exception ex)
        {
            Diagnostics.LogException(
                "EnsureScreenLockAccessAsync",
                ex);
        }
    }

#endif

    // =====================================
    // PRINTER SETTINGS
    // =====================================

    private async Task<bool> PromptForPrinterIpAsync(bool forceEnglish = false)
    {
        string savedIp =
            MauiPreferences.Get(PrinterIpKey, "");

        int savedPort =
            MauiPreferences.Get(
                PrinterPortKey,
                DefaultPrinterPort);

        string? ip = await DisplayPromptAsync(
            forceEnglish ? "Snapmaker U1 Monitor IC" : T("app_title"),
            forceEnglish
                ? "Enter the IP address of your Snapmaker U1.\n\nDefault Moonraker port: 7125.\nYou can change it later in Advanced Settings."
                : T("ip_prompt"),
            accept: forceEnglish ? "Save" : T("save"),
            cancel: forceEnglish ? "Cancel" : T("cancel"),
            placeholder: "192.168.100.13",
            maxLength: 253,
            initialValue: savedIp);

        if (ip == null)
            return false;

        ip = ip.Trim();

        if (ip.Contains("://") ||
            ip.Contains('/') ||
            ip.Contains(' ') ||
            Uri.CheckHostName(ip) == UriHostNameType.Unknown)
        {
            await DisplayAlertAsync(
                forceEnglish ? "Invalid address" : T("invalid_address_title"),
                forceEnglish
                    ? "Enter a valid IP address or printer hostname."
                    : T("invalid_address_message"),
                "OK");

            return false;
        }

        SavePrinterSettings(ip, savedPort);

        return true;
    }

    private void SavePrinterSettings(string ip, int port)
    {
        MauiPreferences.Set(PrinterIpKey, ip);
        MauiPreferences.Set(PrinterPortKey, port);

        _printer.BaseUrl = $"http://{ip}:{port}";

        _thumbnailFilename = "";
        _thumbnailSource = null;
        _lastThumbnailAttempt = DateTime.MinValue;

        _lastConnectionProblem = "";

        Diagnostics.Log(
            "PRINTER SETTINGS SAVED: " + _printer.BaseUrl);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = AppLocalization.Status("CONNECTING");
            StatusLabel.TextColor = Colors.Orange;

            JobLabel.Text = T("connecting");

            JobLabel.TextColor = Colors.White;
        });
    }

    private async Task ChangePrinterIpAsync()
    {
        // Settings is intentionally always English, regardless of app language.
        bool configured =
            await PromptForPrinterIpAsync(forceEnglish: true);

        if (configured)
            StartUpdateLoop();
    }

    private async Task ChangePrinterPortAsync()
    {
        int currentPort =
            MauiPreferences.Get(
                PrinterPortKey,
                DefaultPrinterPort);

        string? portText = await DisplayPromptAsync(
            "Advanced Settings",
            "Moonraker port:",
            accept: "Save",
            cancel: "Cancel",
            placeholder: "7125",
            maxLength: 5,
            keyboard: MauiKeyboard.Numeric,
            initialValue: currentPort.ToString());

        if (portText == null)
            return;

        if (!int.TryParse(portText, out int port) ||
            port < 1 ||
            port > 65535)
        {
            await DisplayAlertAsync(
                "Invalid port",
                "The port must be a number between 1 and 65535.",
                "OK");

            return;
        }

        string ip = MauiPreferences.Get(PrinterIpKey, "");

        if (string.IsNullOrWhiteSpace(ip))
        {
            await DisplayAlertAsync(
                "Missing IP address",
                "Enter the printer IP address first.",
                "OK");

            return;
        }

        SavePrinterSettings(ip, port);

        StartUpdateLoop();
    }

    private string T(string key) => AppLocalization.Get(key);

    private void ApplyLanguage()
    {
        Title = T("app_title");
        HeaderLabel.Text = T("app_title");
        // Keep the entry point to settings recognizable in every language.
        SettingsButton.Text = "SETTINGS";
        ElapsedTitleLabel.Text = T("elapsed_time");
        RemainingTitleLabel.Text = T("remaining_time");
        FinishTitleLabel.Text = T("expected_finish");
        FooterLabel.Text = T("footer");

        if (JobLabel.Text == "No active print" ||
            JobLabel.Text == "Няма активен печат")
        {
            JobLabel.Text = T("no_active_print");
        }

        if (RemainingLabel.Text == "Calculating..." ||
            RemainingLabel.Text == "Изчисляване...")
        {
            RemainingLabel.Text = T("calculating");
        }

        // Обновяваме етикетите, без да прекъсваме мониторинга.
        if (StatusLabel.Text == "IDLE" ||
            StatusLabel.Text == "STANDBY" ||
            StatusLabel.Text == "ГОТОВ")
        {
            StatusLabel.Text = AppLocalization.Status("STANDBY");
        }
    }

    private async Task ChangeLanguageAsync()
    {
        // The language selector itself always remains in English.
        const string english = "English";
        const string bulgarian = "Bulgarian";

        string? choice = await DisplayActionSheetAsync(
            "Language",
            "Close",
            null,
            english,
            bulgarian);

        if (choice == english)
        {
            AppLocalization.SetLanguage("en");
        }
        else if (choice == bulgarian)
        {
            AppLocalization.SetLanguage("bg");
        }
        else
        {
            return;
        }

        ApplyLanguage();

        await DisplayAlertAsync(
            "Language",
            "Language preference saved.",
            "OK");
    }

    private async void OnSettingsClicked(
        object? sender,
        EventArgs e)
    {
        try
        {
            string savedIp =
                MauiPreferences.Get(PrinterIpKey, "");

            if (string.IsNullOrWhiteSpace(savedIp))
            {
                await ChangePrinterIpAsync();
                return;
            }

            const string changeIpOption = "Change printer IP";
            const string advancedPortOption = "Moonraker port (Advanced)";
            const string languageOption = "Language";

            string? action = await DisplayActionSheetAsync(
                "SETTINGS",
                "Close",
                null,
                changeIpOption,
                advancedPortOption,
                languageOption);

            if (action == changeIpOption)
            {
                await ChangePrinterIpAsync();
            }
            else if (action == advancedPortOption)
            {
                await ChangePrinterPortAsync();
            }
            else if (action == languageOption)
            {
                await ChangeLanguageAsync();
            }
        }
        catch (Exception ex)
        {
            Diagnostics.LogException(
                "OnSettingsClicked",
                ex);

            await DisplayAlertAsync(
                "Settings error",
                ex.Message,
                "OK");
        }
    }

    // =====================================
    // CONNECTION MESSAGES
    // =====================================

    private void ShowSetupRequired()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = AppLocalization.Status("SETUP");
            StatusLabel.TextColor = Colors.Orange;
            JobLabel.Text = T("setup_required_message");
            JobLabel.TextColor = Colors.White;
            RemainingLabel.Text = "--:--";
            FinishTimeLabel.Text = "--:--";
        });
    }

    private void ShowConnectionProblem(string details)
    {
        string message =
            T("offline_heading") + "\n\n" +
            T("offline_check_list") + "\n" +
            "• " + T("offline_wifi") + "\n" +
            "• " + T("offline_ip") + "\n" +
            "• " + T("offline_power") + "\n" +
            "• " + T("offline_port") + "\n\n" +
            T("offline_settings") + "\n\n" +
            T("details") + ": " + details;

        if (_lastConnectionProblem != message)
        {
            Diagnostics.Log(
                "PRINTER CONNECTION PROBLEM: " + message);

            _lastConnectionProblem = message;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = AppLocalization.Status("OFFLINE");
            StatusLabel.TextColor = Color.FromArgb("#FF3333");

            JobLabel.Text = message;
            JobLabel.TextColor = Color.FromArgb("#FF9999");

            PrintProgress.Progress = 0;
            ProgressLabel.Text = "--";
            ElapsedLabel.Text = "--:--:--";
            RemainingLabel.Text = "--:--";
            FinishTimeLabel.Text = "--:--";
        });
    }

    private void ShowApplicationError(string details)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = AppLocalization.Status("APP ERROR");
            StatusLabel.TextColor = Colors.Red;

            JobLabel.Text =
                T("app_error_message") + "\n\n" + details;

            JobLabel.TextColor = Colors.Red;
        });
    }

    // =====================================
    // UPDATE LOOP
    // =====================================

    private async Task RunUpdateLoopAsync(
        CancellationToken token)
    {
        Diagnostics.Log("UPDATE LOOP STARTED");

        try
        {
            while (!token.IsCancellationRequested)
            {
                await UpdatePrinterStatusAsync(token);

                await Task.Delay(3000, token);
            }
        }
        catch (OperationCanceledException)
            when (token.IsCancellationRequested)
        {
            Diagnostics.Log("UPDATE LOOP CANCELLED");
        }
        catch (Exception ex)
        {
            Diagnostics.LogException(
                "RunUpdateLoopAsync",
                ex);
        }
        finally
        {
#if ANDROID
            // Release both wake locks whenever monitoring stops or fails.
            _screenController?.StopMonitoring();
#endif
            Diagnostics.Log("UPDATE LOOP STOPPED");
        }
    }

    // =====================================
    // THUMBNAIL
    // =====================================

    private async Task<ImageSource?> LoadThumbnailAsync(
        string filename,
        CancellationToken token)
    {
        try
        {
            string baseUrl =
                _printer.BaseUrl.TrimEnd('/');

            string metadataUrl =
                $"{baseUrl}/server/files/metadata?filename={Uri.EscapeDataString(filename)}";

            string metadataJson =
                await _thumbnailClient.GetStringAsync(
                    metadataUrl,
                    token);

            using JsonDocument metadata =
                JsonDocument.Parse(metadataJson);

            JsonElement thumbnails =
                metadata.RootElement
                    .GetProperty("result")
                    .GetProperty("thumbnails");

            JsonElement selectedThumbnail = default;
            int bestSize = 0;

            foreach (JsonElement thumbnail in
                     thumbnails.EnumerateArray())
            {
                int width =
                    thumbnail.GetProperty("width").GetInt32();

                int height =
                    thumbnail.GetProperty("height").GetInt32();

                int size = width * height;

                if (width == 300 && height == 300)
                {
                    selectedThumbnail = thumbnail;
                    bestSize = size;
                    break;
                }

                if (size > bestSize)
                {
                    selectedThumbnail = thumbnail;
                    bestSize = size;
                }
            }

            if (bestSize == 0)
                return null;

            string relativePath =
                selectedThumbnail
                    .GetProperty("relative_path")
                    .GetString() ?? "";

            if (string.IsNullOrWhiteSpace(relativePath))
                return null;

            string encodedPath = string.Join(
                "/",
                relativePath.Split('/')
                    .Select(Uri.EscapeDataString));

            string thumbnailUrl =
                $"{baseUrl}/server/files/gcodes/{encodedPath}";

            byte[] imageBytes =
                await _thumbnailClient.GetByteArrayAsync(
                    thumbnailUrl,
                    token);

            token.ThrowIfCancellationRequested();

            Diagnostics.Log(
                $"THUMBNAIL LOADED: {filename}, {imageBytes.Length} bytes");

            return ImageSource.FromStream(
                () => new MemoryStream(imageBytes));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Diagnostics.LogException(
                "LoadThumbnailAsync",
                ex);

            return null;
        }
    }

    // =====================================
    // PRINTER STATUS
    // =====================================

    private async Task UpdatePrinterStatusAsync(
        CancellationToken token)
    {
        if (_isBusy)
            return;

        _isBusy = true;

        try
        {
            using JsonDocument doc =
                await _printer.GetPrinterStatusAsync(token);

            token.ThrowIfCancellationRequested();

            if (!string.IsNullOrEmpty(_lastConnectionProblem))
            {
                Diagnostics.Log(
                    "PRINTER CONNECTION RESTORED");

                _lastConnectionProblem = "";
            }

            JsonElement status =
                doc.RootElement
                    .GetProperty("result")
                    .GetProperty("status");

            JsonElement printStats =
                status.GetProperty("print_stats");

            string state =
                printStats.GetProperty("state")
                    .GetString() ?? "idle";

            string filename =
                printStats.GetProperty("filename")
                    .GetString() ?? "";

            string printMessage = "";

            if (printStats.TryGetProperty(
                "message",
                out JsonElement printMessageElement))
            {
                printMessage =
                    printMessageElement.GetString() ?? "";
            }

            double progress =
                status.GetProperty("display_status")
                    .GetProperty("progress")
                    .GetDouble();

            double elapsedSeconds =
                printStats.TryGetProperty(
                    "print_duration",
                    out JsonElement duration)
                    ? duration.GetDouble()
                    : 0;

            // Klipper webhooks.
            string webhooksState = "";
            string webhooksMessage = "";

            if (status.TryGetProperty(
                "webhooks",
                out JsonElement webhooks))
            {
                if (webhooks.TryGetProperty(
                    "state",
                    out JsonElement webhookState))
                {
                    webhooksState =
                        webhookState.GetString() ?? "";
                }

                if (webhooks.TryGetProperty(
                    "state_message",
                    out JsonElement webhookMessage))
                {
                    webhooksMessage =
                        webhookMessage.GetString() ?? "";
                }
            }

            bool klipperError =
                webhooksState.Equals(
                    "error",
                    StringComparison.OrdinalIgnoreCase);

            bool klipperShutdown =
                webhooksState.Equals(
                    "shutdown",
                    StringComparison.OrdinalIgnoreCase);

            bool printError =
                state.Equals(
                    "error",
                    StringComparison.OrdinalIgnoreCase);

            bool hasPrinterError =
                klipperError || klipperShutdown || printError;

            bool printerReady =
                string.IsNullOrWhiteSpace(webhooksState) ||
                webhooksState.Equals(
                    "ready",
                    StringComparison.OrdinalIgnoreCase);

            // Избираме показвания статус.
            string displayState;
            string errorMessage = "";

            if (klipperError || printError)
            {
                displayState = "ERROR";

                errorMessage =
                    !string.IsNullOrWhiteSpace(webhooksMessage)
                        ? webhooksMessage
                        : printMessage;

                if (string.IsNullOrWhiteSpace(errorMessage))
                    errorMessage = T("printer_reported_error");
            }
            else if (klipperShutdown)
            {
                displayState = "SHUTDOWN";

                errorMessage =
                    !string.IsNullOrWhiteSpace(webhooksMessage)
                        ? webhooksMessage
                        : T("klipper_shutdown");
            }
            else if (!printerReady)
            {
                displayState =
                    string.IsNullOrWhiteSpace(webhooksState)
                        ? "UNKNOWN"
                        : webhooksState.ToUpperInvariant();

                errorMessage = webhooksMessage;
            }
            else
            {
                displayState = state.ToUpperInvariant();
            }

            // =====================================
            // ELAPSED / REMAINING TIME
            // =====================================

            string elapsedText =
                TimeSpan.FromSeconds(
                    Math.Max(0, elapsedSeconds))
                    .ToString(@"hh\:mm\:ss");

            string remainingText = "--:--";
            string finishTimeText = "--:--";

            if ((state == "printing" || state == "paused") &&
                progress > 0.01 &&
                progress <= 1.0)
            {
                double remainingSeconds =
                    elapsedSeconds *
                    (1.0 - progress) / progress;

                remainingText =
                    TimeSpan.FromSeconds(
                        Math.Max(0, remainingSeconds))
                        .ToString(@"hh\:mm\:ss");

                finishTimeText =
                    state == "printing"
                        ? DateTime.Now
                            .AddSeconds(remainingSeconds)
                            .ToString("HH:mm")
                        : T("paused");
            }
            else if (state == "complete")
            {
                remainingText = "00:00:00";
                finishTimeText = T("finished");
            }
            else if (state == "cancelled")
            {
                finishTimeText = T("cancelled");
            }
            else if (hasPrinterError)
            {
                finishTimeText = T("error");
            }

            // =====================================
            // SCREEN AUTOMATION
            // =====================================

#if ANDROID
            bool printIsActive =
                printerReady &&
                (state == "printing" || state == "paused");

            bool wakeScreen = false;
            bool keepScreenOn = false;
            bool lockScreen = false;

            if (!_initialPrinterStateHandled)
            {
                _initialPrinterStateHandled = true;

                if (printIsActive)
                {
                    _printWasActive = true;
                    keepScreenOn = true;
                }
                else if (state == "complete" ||
                         state == "cancelled" ||
                         state == "error" ||
                         klipperError ||
                         klipperShutdown)
                {
                    _screenLockAtUtc =
                        DateTime.UtcNow.AddMinutes(15);

                    keepScreenOn = true;

                    Diagnostics.Log(
                        "INITIAL TERMINAL STATE - TIMER STARTED");
                }
            }
            else if (printIsActive)
            {
                if (!_printWasActive)
                {
                    wakeScreen = true;

                    Diagnostics.Log(
                        "NEW PRINT DETECTED");
                }

                _printWasActive = true;
                _screenLockAtUtc = null;
                keepScreenOn = true;
            }
            else if (_printWasActive)
            {
                _printWasActive = false;

                _screenLockAtUtc =
                    DateTime.UtcNow.AddMinutes(15);

                keepScreenOn = true;

                Diagnostics.Log(
                    "PRINT ENDED - 15 MINUTE TIMER STARTED");
            }
            else if (_screenLockAtUtc.HasValue)
            {
                if (DateTime.UtcNow >= _screenLockAtUtc.Value)
                {
                    lockScreen = true;
                }
                else
                {
                    keepScreenOn = true;
                }
            }

            if (_screenController == null)
                InitializeScreenController();

            if (wakeScreen)
            {
                await MainThread.InvokeOnMainThreadAsync(
                    () => _screenController?.WakeScreen());

                // LG G2 already wakes correctly. Other Android phones
                // receive a local notification instead of relying only
                // on the screen-wake request.
                _printNotificationManager?.ShowPrintStarted(filename);
            }
            else if (keepScreenOn)
            {
                await MainThread.InvokeOnMainThreadAsync(
                    () => _screenController?.SetKeepScreenOn(true));
            }

            if (lockScreen)
            {
                await MainThread.InvokeOnMainThreadAsync(
                    () => _screenController?.SetKeepScreenOn(false));

                bool locked =
                    await MainThread.InvokeOnMainThreadAsync(
                        () => _screenController?.LockScreen() ?? false);

                if (locked)
                {
                    _screenLockAtUtc = null;

                    Diagnostics.Log(
                        "SCREEN LOCKED AFTER 15 MINUTES");
                }
                else
                {
                    _screenLockAtUtc =
                        DateTime.UtcNow.AddMinutes(1);

                    Diagnostics.Log(
                        "SCREEN LOCK FAILED - RETRY IN 1 MINUTE");
                }
            }

#endif

            // =====================================
            // THUMBNAIL
            // =====================================

            if (filename != _thumbnailFilename)
            {
                _thumbnailFilename = filename;
                _thumbnailSource = null;
                _lastThumbnailAttempt = DateTime.MinValue;
            }

            if (!string.IsNullOrWhiteSpace(filename) &&
                _thumbnailSource == null &&
                DateTime.UtcNow - _lastThumbnailAttempt
                    >= TimeSpan.FromSeconds(30))
            {
                _lastThumbnailAttempt = DateTime.UtcNow;

                _thumbnailSource =
                    await LoadThumbnailAsync(filename, token);
            }

            token.ThrowIfCancellationRequested();

            // =====================================
            // DIAGNOSTICS
            // =====================================

            string logSignature =
                $"{displayState}|{webhooksState}|{errorMessage}";

            if (logSignature != _lastLoggedState)
            {
                Diagnostics.Log(
                    $"PRINTER STATE: {displayState}; " +
                    $"KLIPPER: {webhooksState}; " +
                    $"PRINT: {state}; " +
                    $"PROGRESS: {progress:P0}");

                if (hasPrinterError)
                {
                    Diagnostics.Log(
                        "PRINTER ERROR MESSAGE: " +
                        errorMessage);
                }

                _lastLoggedState = logSignature;
            }

            _updateCounter++;

            if (_updateCounter >= 20)
            {
                _updateCounter = 0;

                long memoryBytes =
                    GC.GetTotalMemory(false);

                Diagnostics.Log(
                    $"MEMORY: {memoryBytes / 1024 / 1024} MB");
            }

            // =====================================
            // UI UPDATE
            // =====================================

            ImageSource? currentThumbnail =
                _thumbnailSource;

            string jobDisplayText;

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                string printerErrorText =
                    $"{T("printer_error_prefix")}\n{errorMessage}";

                jobDisplayText = string.IsNullOrWhiteSpace(filename)
                    ? printerErrorText
                    : $"{filename}\n{printerErrorText}";
            }
            else
            {
                jobDisplayText = string.IsNullOrWhiteSpace(filename)
                    ? T("no_active_print")
                    : filename;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                StatusLabel.Text = AppLocalization.Status(displayState);

                StatusLabel.TextColor = displayState switch
                {
                    "PRINTING" => Color.FromArgb("#00BFFF"),
                    "PAUSED" => Color.FromArgb("#FFB000"),
                    "COMPLETE" => Color.FromArgb("#00D66B"),
                    "CANCELLED" => Color.FromArgb("#FFB000"),
                    "ERROR" => Color.FromArgb("#FF3333"),
                    "SHUTDOWN" => Color.FromArgb("#FF3333"),
                    "STARTUP" => Color.FromArgb("#FFB000"),
                    "OFFLINE" => Color.FromArgb("#FF3333"),
                    _ => Color.FromArgb("#00D66B")
                };

                JobLabel.Text = jobDisplayText;

                JobLabel.TextColor =
                    hasPrinterError ||
                    displayState == "SHUTDOWN"
                        ? Color.FromArgb("#FF6666")
                        : Colors.White;

                JobThumbnail.Source = currentThumbnail;

                PrintProgress.Progress =
                    Math.Clamp(progress, 0, 1);

                ProgressLabel.Text =
                    $"{progress * 100:0}%";

                ElapsedLabel.Text = elapsedText;
                RemainingLabel.Text = remainingText;
                FinishTimeLabel.Text = finishTimeText;
            });
        }
        catch (OperationCanceledException)
            when (!token.IsCancellationRequested)
        {
            ShowConnectionProblem(
                T("connection_timeout"));
        }
        catch (OperationCanceledException)
        {
            // Нормално спиране.
        }
        catch (HttpRequestException ex)
        {
            ShowConnectionProblem(ex.Message);
        }
        catch (JsonException ex)
        {
            Diagnostics.LogException(
                "Invalid printer response",
                ex);

            ShowConnectionProblem(
                T("invalid_response") + " " + ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            ShowConnectionProblem(ex.Message);
        }
        catch (Exception ex)
        {
            Diagnostics.LogException(
                "UpdatePrinterStatusAsync",
                ex);

            ShowApplicationError(ex.Message);
        }
        finally
        {
            _isBusy = false;
        }
    }
}
