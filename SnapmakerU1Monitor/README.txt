Snapmaker U1 Monitor IC — Branding integration

Files in this package:
  Resources/AppIcon/appicon.png
  Resources/Splash/splash.png

1) Copy/extract the Resources folder into the MAUI project root, allowing the
   two PNG files to be added. This does not modify any C# or XAML code.

2) Open SnapmakerU1Monitor.csproj. REPLACE the existing MauiIcon entry with:

   <MauiIcon Include="Resources\AppIcon\appicon.png" BaseSize="512,512" />

   REPLACE the existing MauiSplashScreen entry with:

   <MauiSplashScreen Include="Resources\Splash\splash.png"
                     BaseSize="216,384" />

   Do not add duplicate MauiIcon or MauiSplashScreen entries. Remove the old
   ForegroundFile attribute from MauiIcon because this is a single flattened
   PNG icon, not a two-layer icon.

3) Keep the old SVG files as backups if desired, but make sure the project file
   points only to the new PNG files for the app icon and splash screen.

4) In Visual Studio choose Build > Clean Solution, then Build > Build Solution.
   Create/sign the Android APK using the same command you have been using.

Notes:
- The icon is a 1024x1024 PNG.
- The splash artwork is a 432x768 PNG. BaseSize 216x384 treats it as a 2x
  bitmap baseline for MAUI scaling; the original PNG remains unchanged.
- No printer-monitoring, screen wake/lock, localization, or connection code is
  changed by this branding package.
- The printer illustration is a stylized rendering based on the Snapmaker U1
  reference image supplied in the conversation, not an official product photo.
- Keep the existing app package identity and signing configuration unchanged.
