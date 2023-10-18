using System;
using System.Linq;
using System.Threading;
using AppKit;
using Foundation;
using MacTweaks.Helpers;
using MacTweaks.Modules;
using MacTweaks.Modules.Dock;
using MacTweaks.Modules.Keystrokes;
using Microsoft.Extensions.DependencyInjection;

namespace MacTweaks
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        public readonly ServiceProvider Services;

        public AppDelegate()
        {
            Services = GetServiceCollection().BuildServiceProvider();
        }
        
        private ServiceCollection GetServiceCollection()
        {
            var collection = new ServiceCollection();
            
            collection.AddSingleton<AppDelegate>(this);
            
            #if DEBUG
            collection.AddSingleton<IModule, DockDebugModule>();
            #endif
            
            collection.AddSingleton<IModule, DockModule>();

            collection.AddSingleton<IModule, CommandQModule>();

            return collection;
        }

        private static void ConstructMenuBarIcon()
        {
            // TODO: Improve this mess
            
            // Construct menu that will be displayed when tray icon is clicked
            var notifyMenu = new NSMenu();
            var exitMenuItem = new NSMenuItem("Quit My Application", 
                (a,b) => { System.Environment.Exit(0); }); // Just add 'Quit' command
            notifyMenu.AddItem(exitMenuItem);

            // Display tray icon in upper-right-hand corner of the screen
            var sItem = NSStatusBar.SystemStatusBar.CreateStatusItem(30);
            sItem.Menu = notifyMenu;
            sItem.Image = NSImage.FromStream(System.IO.File.OpenRead("/Users/trumpmcdonaldz/Pictures/DonaldNaSmirk.jpeg"));
            sItem.HighlightMode = true;

            // Remove the system tray icon from upper-right hand corner of the screen
            // (works without adjusting the LSUIElement setting in Info.plist)
            NSApplication.SharedApplication.ActivationPolicy = NSApplicationActivationPolicy.Accessory;
        }

        public void ConstructMenu()
        {
            var menu = NSApplication.SharedApplication.MainMenu;
            var optionsMenu = new NSMenuItem("Options");
            
            menu.AddItem(optionsMenu);

            var optionsSubMenu = optionsMenu.Submenu = new NSMenu("Options");
            optionsSubMenu.AddItem(new NSMenuItem("Quit", "q", (sender, e) => NSApplication.SharedApplication.Terminate(this)));
        }
        
        public override void DidFinishLaunching(NSNotification notification)
        {
            ConstructMenuBarIcon();
            
            NSWorkspace.SharedWorkspace.RunningApplications.First(x => x.LocalizedName == "MacTweaks").Activate(default);
            
            AccessibilityHelpers.RequestForAccessibilityIfNotGranted();

            foreach (var service in Services.GetServices<IModule>())
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