using System;
using Android.AccessibilityServices;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views.Accessibility;

namespace SnapmakerU1Monitor;

[Service(
    Name = "com.companyname.snapmakeru1monitor.ScreenLockAccessibilityService",
    Permission = "android.permission.BIND_ACCESSIBILITY_SERVICE",
    Exported = true,
    Label = "Snapmaker U1 Monitor IC")]
[IntentFilter(new[] { AccessibilityService.ServiceInterface })]
[MetaData(
    AccessibilityService.ServiceMetaData,
    Resource = "@xml/snapmaker_screen_lock_accessibility_service")]
[Preserve(AllMembers = true)]
public sealed class ScreenLockAccessibilityService : AccessibilityService
{
    private static readonly object Sync = new();
    private static WeakReference<ScreenLockAccessibilityService>? _current;

    public override void OnServiceConnected()
    {
        base.OnServiceConnected();

        lock (Sync)
        {
            _current = new WeakReference<ScreenLockAccessibilityService>(this);
        }

        Diagnostics.Log("ACCESSIBILITY SCREEN LOCK SERVICE CONNECTED");
    }

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
            // This is Android's system global lock action. It follows the
            // normal keyguard lock path, rather than the DevicePolicyManager
            // force-lock path that explicitly requires strong authentication.
            return service.PerformGlobalAction(GlobalAction.LockScreen);
        }
        catch (Exception ex)
        {
            Diagnostics.LogException("AccessibilityService.TryLockScreen", ex);
            return false;
        }
    }

    public override void OnAccessibilityEvent(AccessibilityEvent e)
    {
        // Intentionally empty. The service does not inspect screen content,
        // text, clicks, or user input. It only exposes the system lock action.
    }

    public override void OnInterrupt()
    {
        // No feedback or event processing is performed by this service.
    }

    public override bool OnUnbind(Intent intent)
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
