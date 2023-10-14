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

        private static ServiceCollection GetServiceCollection()
        {
            var collection = new ServiceCollection();
            
            #if DEBUG
            collection.AddSingleton<IModule, DockDebugModule>();
            #endif
            
            collection.AddSingleton<IModule, DockModule>();

            return collection;
        }
        
        public override void DidFinishLaunching(NSNotification notification)
        {
            AccessibilityHelpers.RequestForAccessibilityIfNotGranted();
            
            var services = Services = GetServiceCollection().BuildServiceProvider();

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