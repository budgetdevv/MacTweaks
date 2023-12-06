using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AppKit;
using Foundation;
using MacTweaks.Helpers;

namespace MacTweaks.Modules.LockdownBrowser
{
    public class QuitOnLockDownBrowserLaunch: IModule
    {
        private const int EXIT_DELAY_IN_SECONDS = 3;

        private static readonly TimeSpan ExitDelay = TimeSpan.FromSeconds(EXIT_DELAY_IN_SECONDS);
        
        // Make sure GC holds onto a reference
        private NSObject DidActivateApplicationNotification;

        public void Start()
        {
            DidActivateApplicationNotification = NSWorkspace.Notifications.ObserveDidActivateApplication(OnApplicationActivated);
        }

        private static void OnApplicationActivated(object sender, NSWorkspaceApplicationEventArgs eventArgs)
        {
            if (eventArgs.Application.BundleIdentifier != ConstantHelpers.LOCKDOWN_BROWSER_BUNDLE_ID)
            {
                return;
            }

            ExitWithDialog();
        }

        // Don't pollute hot path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ExitWithDialog()
        {
            var dialog = new NSAlert()
            {
                MessageText = $"{ConstantHelpers.APP_NAME} will exit in {EXIT_DELAY_IN_SECONDS} seconds, due to lockdown browser being launched.",
                AlertStyle = NSAlertStyle.Informational
            };
            dialog.AddButton("OK");
            
            dialog.BeginSheet(NSApplication.SharedApplication.MainWindow);

            Task.Delay(ExitDelay).ContinueWith(_ => Environment.Exit(0));
        }

        public void Stop()
        {
            DidActivateApplicationNotification.Dispose();
        }
    }
}