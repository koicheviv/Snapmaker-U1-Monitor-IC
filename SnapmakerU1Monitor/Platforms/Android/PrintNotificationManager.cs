#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Maui.ApplicationModel;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;

[assembly: Android.App.UsesPermission(Android.Manifest.Permission.PostNotifications)]

namespace SnapmakerU1Monitor;

/// <summary>
/// Sends a local Android notification when a new printer job starts.
/// LG devices are intentionally excluded because the LG G2 already wakes
/// successfully using the existing screen controller.
/// </summary>
public sealed class PrintNotificationManager
{
    private const string ChannelId = "snapmaker_print_events";
    private const string ChannelName = "Printer notifications";
    private const int NotificationId = 1001;

    private bool _enabled;

    public async Task<bool> InitializeAsync()
    {
        try
        {
            if (IsLgDevice())
            {
                _enabled = false;
                Diagnostics.Log("PRINT NOTIFICATIONS SKIPPED - LG DEVICE");
                return false;
            }

            Context context = Android.App.Application.Context;

            NotificationManager? manager =
                context.GetSystemService(Context.NotificationService)
                as NotificationManager;

            if (manager is null)
            {
                _enabled = false;
                Diagnostics.Log("PRINT NOTIFICATIONS UNAVAILABLE - NO NOTIFICATION MANAGER");
                return false;
            }

            // Notification channels exist from Android 8.0 (API 26).
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                var channel = new NotificationChannel(
                    ChannelId,
                    ChannelName,
                    NotificationImportance.High)
                {
                    Description = "Alerts when a new Snapmaker U1 print starts."
                };

                channel.EnableVibration(true);
                manager.CreateNotificationChannel(channel);
            }

            // Android 13 (API 33) and later require runtime notification permission.
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                PermissionStatus permission =
                    await MauiPermissions.CheckStatusAsync<MauiPermissions.PostNotifications>();

                if (permission != PermissionStatus.Granted)
                {
                    permission =
                        await MauiPermissions.RequestAsync<MauiPermissions.PostNotifications>();
                }

                _enabled = permission == PermissionStatus.Granted;

                Diagnostics.Log(
                    "PRINT NOTIFICATION PERMISSION: " + permission);
            }
            else
            {
                // Before Android 13, notifications are enabled by default
                // unless the user disabled them in system settings.
                _enabled = true;
                Diagnostics.Log(
                    "PRINT NOTIFICATION PERMISSION: NOT REQUIRED ON THIS ANDROID VERSION");
            }

            return _enabled;
        }
        catch (Exception ex)
        {
            _enabled = false;
            Diagnostics.LogException("PrintNotificationManager.InitializeAsync", ex);
            return false;
        }
    }

    public void ShowPrintStarted(string? filename)
    {
        try
        {
            if (!_enabled || IsLgDevice())
                return;

            Context context = Android.App.Application.Context;

            NotificationManager? manager =
                context.GetSystemService(Context.NotificationService)
                as NotificationManager;

            if (manager is null)
                return;

            string contentText = string.IsNullOrWhiteSpace(filename)
                ? "A new print has started."
                : $"Print started: {filename}";

            Notification.Builder? builder;

            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                builder = new Notification.Builder(context, ChannelId);
            }
            else
            {
                builder = new Notification.Builder(context);

                // Priority and defaults are used only on Android versions
                // that predate notification channels.
                builder.SetPriority((int)NotificationPriority.High);
                builder.SetDefaults(
                    NotificationDefaults.Sound | NotificationDefaults.Vibrate);
            }

            if (builder is null)
                return;

            builder.SetSmallIcon(Android.Resource.Drawable.IcDialogInfo);
            builder.SetContentTitle("Snapmaker U1 Monitor IC");
            builder.SetContentText(contentText);
            builder.SetStyle(
                new Notification.BigTextStyle().BigText(contentText));
            builder.SetAutoCancel(true);
            builder.SetCategory(Notification.CategoryStatus);
            builder.SetVisibility(NotificationVisibility.Public);

            Intent? launchIntent =
                context.PackageManager?.GetLaunchIntentForPackage(
                    context.PackageName ?? string.Empty);

            if (launchIntent is not null)
            {
                launchIntent.SetFlags(
                    ActivityFlags.ClearTop | ActivityFlags.SingleTop);

                PendingIntentFlags flags = PendingIntentFlags.UpdateCurrent;

                if (OperatingSystem.IsAndroidVersionAtLeast(23))
                {
                    flags |= PendingIntentFlags.Immutable;
                }

                PendingIntent? pendingIntent = PendingIntent.GetActivity(
                    context,
                    NotificationId,
                    launchIntent,
                    flags);

                if (pendingIntent is not null)
                {
                    builder.SetContentIntent(pendingIntent);
                }
            }

            Notification? notification = builder.Build();

            if (notification is not null)
            {
                manager.Notify(NotificationId, notification);
                Diagnostics.Log("PRINT START NOTIFICATION SENT: " + contentText);
            }
        }
        catch (Exception ex)
        {
            Diagnostics.LogException("PrintNotificationManager.ShowPrintStarted", ex);
        }
    }

    private static bool IsLgDevice()
    {
        string manufacturer = Build.Manufacturer ?? string.Empty;
        string brand = Build.Brand ?? string.Empty;

        return manufacturer.Contains("LGE", StringComparison.OrdinalIgnoreCase)
               || manufacturer.Equals("LG", StringComparison.OrdinalIgnoreCase)
               || brand.Equals("LG", StringComparison.OrdinalIgnoreCase)
               || brand.Equals("LGE", StringComparison.OrdinalIgnoreCase);
    }
}
#endif
