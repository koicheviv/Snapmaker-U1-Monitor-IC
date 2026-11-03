using System;
using Android.App;
using Android.App.Admin;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Provider;
using Android.Views;

namespace SnapmakerU1Monitor;

public class AndroidScreenController
{
    private const long ScreenWakeTimeoutMilliseconds = 7000;
    private const string AccessibilityLockServiceClass =
        "com.companyname.snapmakeru1monitor.ScreenLockAccessibilityService";

    private readonly Activity _activity;
    private readonly Context _appContext;

    private readonly PowerManager.WakeLock? _cpuWakeLock;
    private readonly PowerManager.WakeLock? _screenWakeLock;
    private readonly WifiManager.WifiLock? _wifiLock;

    private readonly DevicePolicyManager? _devicePolicyManager;
    private readonly ComponentName _adminComponent;

    public AndroidScreenController(Activity activity)
    {
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
        _appContext = activity.ApplicationContext ?? activity;

        PowerManager? powerManager =
            (PowerManager?)_appContext.GetSystemService(Context.PowerService);

        _cpuWakeLock = powerManager?.NewWakeLock(
            WakeLockFlags.Partial,
            "SnapmakerU1Monitor:Cpu");

        _screenWakeLock = powerManager?.NewWakeLock(
            WakeLockFlags.ScreenBright | WakeLockFlags.AcquireCausesWakeup,
            "SnapmakerU1Monitor:Screen");

        _screenWakeLock?.SetReferenceCounted(false);

        WifiManager? wifiManager =
            (WifiManager?)_appContext.GetSystemService(Context.WifiService);

        _wifiLock = wifiManager?.CreateWifiLock(
            Android.Net.WifiMode.Full,
            "SnapmakerU1Monitor:Wifi");

        _wifiLock?.SetReferenceCounted(false);

        _devicePolicyManager =
            (DevicePolicyManager?)_appContext.GetSystemService(Context.DevicePolicyService);

        _adminComponent = new ComponentName(
            _appContext,
            Java.Lang.Class.FromType(typeof(ScreenAdminReceiver)));
    }

    // GLOBAL_ACTION_LOCK_SCREEN was added in Android 9 / API 28.
    // Older devices such as the LG G2 continue to use Device Admin.
    public bool UsesAccessibilityLock =>
        (int)Build.VERSION.SdkInt >= 28;

    public bool IsAccessibilityLockEnabled()
    {
        try
        {
            if (!UsesAccessibilityLock)
                return false;

            string? enabledServices = Settings.Secure.GetString(
                _appContext.ContentResolver,
                Settings.Secure.EnabledAccessibilityServices);

            if (string.IsNullOrWhiteSpace(enabledServices))
                return false;

            var component = new ComponentName(
                _appContext.PackageName,
                AccessibilityLockServiceClass);

            string expected = component.FlattenToString();

            foreach (string entry in enabledServices.Split(':'))
            {
                if (string.Equals(
                    entry,
                    expected,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Diagnostics.LogException("IsAccessibilityLockEnabled", ex);
            return false;
        }
    }

    public void RequestAccessibilityLockActivation()
    {
        try
        {
            _activity.StartActivity(
                new Intent(Settings.ActionAccessibilitySettings));
        }
        catch (Exception ex)
        {
            Diagnostics.LogException(
                "RequestAccessibilityLockActivation",
                ex);
        }
    }

    public void StartMonitoring()
    {
        try
        {
            if (_cpuWakeLock != null && !_cpuWakeLock.IsHeld)
            {
                _cpuWakeLock.Acquire();
            }

            if (_wifiLock != null && !_wifiLock.IsHeld)
            {
                _wifiLock.Acquire();
            }

            Diagnostics.Log("ANDROID WAKE LOCKS ACQUIRED");
        }
        catch (Exception ex)
        {
            Diagnostics.LogException("StartMonitoring", ex);
        }
    }

    public void StopMonitoring()
    {
        try
        {
            if (_cpuWakeLock != null && _cpuWakeLock.IsHeld)
            {
                _cpuWakeLock.Release();
            }

            if (_wifiLock != null && _wifiLock.IsHeld)
            {
                _wifiLock.Release();
            }

            Diagnostics.Log("ANDROID WAKE LOCKS RELEASED");
        }
        catch (Exception ex)
        {
            Diagnostics.LogException("StopMonitoring", ex);
        }
    }

    public bool IsAdminEnabled()
    {
        return _devicePolicyManager != null &&
               _devicePolicyManager.IsAdminActive(_adminComponent);
    }

    public void RequestAdminActivation()
    {
        Intent intent = new Intent(DevicePolicyManager.ActionAddDeviceAdmin);

        intent.PutExtra(
            DevicePolicyManager.ExtraDeviceAdmin,
            _adminComponent);

        intent.PutExtra(
            DevicePolicyManager.ExtraAddExplanation,
            "Snapmaker U1 Monitor ще изключва екрана 15 минути след приключване на печата и ще го включва при започване на нов печат.");

        _activity.StartActivity(intent);
    }

    public void SetKeepScreenOn(bool keepOn)
    {
        try
        {
            Android.Views.Window? window = _activity.Window;
            if (window == null)
            {
                return;
            }

            if (keepOn)
            {
                window.AddFlags(WindowManagerFlags.KeepScreenOn);
            }
            else
            {
                window.ClearFlags(
                    WindowManagerFlags.KeepScreenOn |
                    WindowManagerFlags.ShowWhenLocked |
                    WindowManagerFlags.DismissKeyguard |
                    WindowManagerFlags.TurnScreenOn);
            }

            // These Activity APIs were added in Android 8.1 (API 27).
            // The version guard is recognized by the .NET platform analyzer.
            if (OperatingSystem.IsAndroidVersionAtLeast(27))
            {
                _activity.SetShowWhenLocked(keepOn);
                _activity.SetTurnScreenOn(keepOn);
            }
        }
        catch (Exception ex)
        {
            Diagnostics.LogException("SetKeepScreenOn", ex);
        }
    }

    public void WakeScreen()
    {
        try
        {
            Android.Views.Window? window = _activity.Window;
            if (window == null)
            {
                Diagnostics.Log("SCREEN WAKE SKIPPED: WINDOW IS NULL");
                return;
            }

            if (_activity.IsFinishing)
            {
                Diagnostics.Log("SCREEN WAKE SKIPPED: ACTIVITY IS FINISHING");
                return;
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(27))
            {
                // Android 8.1+: use the Activity APIs recommended by Android.
                // This can show the Activity over the lock screen, but does not
                // bypass a secure PIN, pattern, password or biometric lock.
                _activity.SetShowWhenLocked(true);
                _activity.SetTurnScreenOn(true);
                window.AddFlags(WindowManagerFlags.KeepScreenOn);
            }
            else
            {
                // Compatibility path for older Android versions such as LG G2.
                window.AddFlags(
                    WindowManagerFlags.KeepScreenOn |
                    WindowManagerFlags.ShowWhenLocked |
                    WindowManagerFlags.DismissKeyguard |
                    WindowManagerFlags.TurnScreenOn);
            }

            // Retain a short wake lock as an OEM fallback.
            if (_screenWakeLock != null)
            {
                if (_screenWakeLock.IsHeld)
                {
                    _screenWakeLock.Release();
                }

                _screenWakeLock.Acquire(ScreenWakeTimeoutMilliseconds);
            }

            Diagnostics.Log(
                "SCREEN WAKE REQUESTED; API=" +
                Build.VERSION.SdkInt);
        }
        catch (Exception ex)
        {
            Diagnostics.LogException("WakeScreen", ex);
        }
    }

    public bool LockScreen()
    {
        try
        {
            if (UsesAccessibilityLock)
            {
                if (!IsAccessibilityLockEnabled())
                {
                    Diagnostics.Log(
                        "SCREEN LOCK FAILED: ACCESSIBILITY SERVICE NOT ENABLED");
                    return false;
                }

                bool locked = ScreenLockAccessibilityService.TryLockScreen();
                Diagnostics.Log(
                    locked
                        ? "SCREEN LOCK REQUESTED VIA ACCESSIBILITY GLOBAL ACTION"
                        : "SCREEN LOCK FAILED: ACCESSIBILITY SERVICE NOT CONNECTED");

                return locked;
            }

            // Compatibility path for Android 8.1 and older (including LG G2).
            if (!IsAdminEnabled())
            {
                Diagnostics.Log("SCREEN LOCK FAILED: ADMIN NOT ENABLED");
                return false;
            }

            if (_devicePolicyManager == null)
            {
                Diagnostics.Log("SCREEN LOCK FAILED: DEVICE POLICY MANAGER IS NULL");
                return false;
            }

            _devicePolicyManager.LockNow();
            Diagnostics.Log("SCREEN LOCKED VIA DEVICE ADMIN (LEGACY ANDROID)");
            return true;
        }
        catch (Exception ex)
        {
            Diagnostics.LogException("LockScreen", ex);
            return false;
        }
    }
}
