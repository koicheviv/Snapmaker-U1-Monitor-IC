# Snapmaker U1 Monitor IC – Android App

An independent Android app for monitoring a **Snapmaker U1** over your local network (LAN) and automating a few useful phone-screen actions.

> This is an independent community project, not an official Snapmaker application.

## 📥 Download

**[Download Snapmaker U1 Monitor IC v1.0.0](https://github.com/koicheviv/Snapmaker-U1-Monitor-IC/releases/tag/v1.0.0)**

Open the release page and expand **Assets** to find the available APK files.

[View all releases](https://github.com/koicheviv/Snapmaker-U1-Monitor-IC/releases)

## Features

- Connects to the Snapmaker U1 over the local network (LAN).
- Monitors printer status and print activity.
- Detects when a new print starts.
- Automatically wakes the phone screen when a new print is detected.
- Keeps the screen awake while a print is running.
- Automatically locks the phone screen 15 minutes after printing finishes.
- Uses Android Accessibility Service to lock the screen on supported Android versions.

## Android permissions and why the app needs them

The app requests only the Android permissions needed for printer monitoring and its screen-management features.

### Network and screen-wake permissions

- **INTERNET** – allows the app to communicate with the Snapmaker U1 over the network.
- **ACCESS_NETWORK_STATE** – lets the app check network connectivity.
- **ACCESS_WIFI_STATE** – supports the Wi-Fi monitoring connection.
- **WAKE_LOCK** – keeps the CPU awake during monitoring and briefly wakes the display when a new print is detected.

### Settings that must be enabled by the user

Some features require the user to enable a special Android setting manually:

- **Accessibility Service** – used only to request the system's screen-lock action. It does **not** retrieve or read window contents (`canRetrieveWindowContent=false`).
- **Device Administrator** – used as the screen-lock method on older Android versions (Android 8.1 / API 27 and earlier). It requests only the **Force lock** policy. On Android 9 / API 28 and later, the app uses the Accessibility screen-lock action instead.

### Notifications

On Android 13 and later, Android requires the **POST_NOTIFICATIONS** runtime permission before the app can show print-start notifications. The app asks for this permission when notification support is initialized.

Notifications are disabled on LG devices because the existing screen-wake behavior works there.

The Accessibility Service and Device Administrator options are controlled by the user in Android Settings and can be disabled there. The app does **not** need access to contacts, SMS, the camera, or the microphone.

## Device compatibility and testing

I've tested the application on four devices:

- Xiaomi phone running Android 12
- Xiaomi phone running Android 14
- Xiaomi 11T Pro
- LG G2 running Android 5.0

The automatic screen-lock feature has worked on all four devices during my tests.

## Feedback and suggestions

I'd like to hear what other Snapmaker U1 owners think about the project:

- Would you find an Android monitoring app like this useful?
- What features would you like to see added?
- Have you encountered any problems, or do you have suggestions for improvements?

This is an independent community project, not an official Snapmaker application.

**GitHub repository:**  
https://github.com/koicheviv/Snapmaker-U1-Monitor-IC

Any feedback, suggestions, and ideas are welcome!

Thanks!
