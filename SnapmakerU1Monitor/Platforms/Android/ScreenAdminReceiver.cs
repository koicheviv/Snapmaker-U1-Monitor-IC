
using Android.App;
using Android.App.Admin;
using Android.Content;

namespace SnapmakerU1Monitor;

[BroadcastReceiver(
    Name = "com.companyname.snapmakeru1monitor.ScreenAdminReceiver",
    Enabled = true,
    Exported = true,
    Permission = "android.permission.BIND_DEVICE_ADMIN",
    Label = "Snapmaker U1 Monitor")]

[IntentFilter(new[]
{
    DeviceAdminReceiver.ActionDeviceAdminEnabled
})]

[MetaData(
    DeviceAdminReceiver.DeviceAdminMetaData,
    Resource = "@xml/device_admin")]

public class ScreenAdminReceiver : DeviceAdminReceiver
{
}
