using System;
using AppKit;
using Foundation;
using MacTweaks.Helpers;
using MacTweaks.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace MacTweaks
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        private ServiceProvider Services;
        
        public override void DidFinishLaunching(NSNotification notification)
        {
            AccessibilityHelpers.RequestForAccessibilityIfNotGranted();

            var collection = new ServiceCollection();

            collection.AddSingleton<IModule, DockModule>();

            var services = Services = collection.BuildServiceProvider();

            foreach (var service in services.GetServices<IModule>())
            {
                service.Start();
            }
        }

        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
        }
    }
}