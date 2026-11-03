using System;
using System.Diagnostics.CodeAnalysis;
using Android.AccessibilityServices;
using Android.App;
using Android.Content;
using Android.Views.Accessibility;

namespace SnapmakerU1Monitor;

[Service(
    Name = "com.companyname.snapmakeru1monitor.ScreenLockAccessibilityService",
    Permission = "android.permission.BIND_ACCESSIBILITY_SERVICE",
    Exported = true,
    Label = "Snapmaker U1 Monitor IC")]
public sealed class ScreenLockAccessibilityService : AccessibilityService
{
    private static readonly object Sync = new();
    private static WeakReference<ScreenLockAccessibilityService>? _current;

    // Android invokes this callback when the user enables the service.
    // The base AccessibilityService declaration is protected.
    protected override void OnServiceConnected()
    {
        base.OnServiceConnected();

        lock (Sync)
        {
            _current = new WeakReference<ScreenLockAccessibilityService>(this);
        }

        Diagnostics.Log("ACCESSIBILITY SCREEN LOCK SERVICE CONNECTED");
    }

    // Keep the service entry points and callbacks available to Android's
    // runtime linker because Android instantiates this class from the manifest.
    [DynamicDependency(
        DynamicallyAccessedMemberTypes.All,
        typeof(ScreenLockAccessibilityService))]
    public static bool TryLockScreen()
    {
        ScreenLockAccessibilityService? service = null;

        lock (Sync)
        {
            if (_current == null || !_current.TryGetTarget(out service))
                return false;
        }

        try
        {
            // GLOBAL_ACTION_LOCK_SCREEN is available only on Android 9 / API 28+.
            if (!OperatingSystem.IsAndroidVersionAtLeast(28))
                return false;

            // Use Android's system lock action rather than DevicePolicyManager.LockNow().
            // Whether biometrics remain available is device/OEM dependent and must be tested.
            return service.PerformGlobalAction(GlobalAction.LockScreen);
        }
        catch (Exception ex)
        {
            Diagnostics.LogException("AccessibilityService.TryLockScreen", ex);
            return false;
        }
    }

    public override void OnAccessibilityEvent(AccessibilityEvent? e)
    {
        // Intentionally empty. The service does not inspect screen content,
        // text, clicks, or user input. It only exposes the system lock action.
    }

    public override void OnInterrupt()
    {
        // No feedback or event processing is performed by this service.
    }

    public override bool OnUnbind(Intent? intent)
    {
        ClearCurrentInstance();
        return base.OnUnbind(intent);
    }

    public override void OnDestroy()
    {
        ClearCurrentInstance();
        base.OnDestroy();
    }

    private void ClearCurrentInstance()
    {
        lock (Sync)
        {
            if (_current != null &&
                _current.TryGetTarget(out ScreenLockAccessibilityService? current) &&
                ReferenceEquals(current, this))
            {
                _current = null;
            }
        }
    }
}
