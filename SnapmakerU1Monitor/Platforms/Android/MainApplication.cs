
using Android.App;
using Android.Runtime;
using System;
using System.Threading.Tasks;

namespace SnapmakerU1Monitor
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();

            AndroidEnvironment.UnhandledExceptionRaiser +=
                OnAndroidUnhandledException;

            AppDomain.CurrentDomain.UnhandledException +=
                OnUnhandledException;

            TaskScheduler.UnobservedTaskException +=
                OnUnobservedTaskException;

            Diagnostics.Log("ANDROID APPLICATION CREATED");
        }

        private void OnAndroidUnhandledException(
            object? sender,
            RaiseThrowableEventArgs e)
        {
            Diagnostics.Log(
                "ANDROID UNHANDLED EXCEPTION: " + e.Exception);

            // Не маркираме грешката като обработена.
            // Android запазва нормалното поведение при срив.
        }

        private void OnUnhandledException(
            object? sender,
            UnhandledExceptionEventArgs e)
        {
            Diagnostics.Log(
                "UNHANDLED EXCEPTION: " + e.ExceptionObject);

            Diagnostics.Log(
                "IsTerminating: " + e.IsTerminating);
        }

        private void OnUnobservedTaskException(
            object? sender,
            UnobservedTaskExceptionEventArgs e)
        {
            Diagnostics.LogException(
                "UNOBSERVED TASK EXCEPTION",
                e.Exception);

            e.SetObserved();
        }

        protected override MauiApp CreateMauiApp()
            => MauiProgram.CreateMauiApp();
    }
}
