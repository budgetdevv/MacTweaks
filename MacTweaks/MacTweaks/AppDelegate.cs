﻿using System;
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
            var exitMenuItem = new NSMenuItem($"Quit {ConstantHelpers.APP_NAME}",
                (handler, args) =>
                {
                    Environment.Exit(0);
                });
            notifyMenu.AddItem(exitMenuItem);

            // Display tray icon in upper-right-hand corner of the screen
            var statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(30);
            statusItem.Menu = notifyMenu;
            statusItem.Image = NSImage.FromStream(System.IO.File.OpenRead("/Users/trumpmcdonaldz/Pictures/DonaldNaSmirk.jpeg"));
            statusItem.HighlightMode = true;

            // Remove from dock
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

        private void MakeAccessibilityCheckerWindow()
        {
            // Create the window
            var window = new NSWindow(new CoreGraphics.CGRect(200, 200, 400, 200), NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Miniaturizable, NSBackingStore.Buffered, false);
            window.Title = ConstantHelpers.APP_NAME;

            // Create the label
            var label = new NSTextField(new CoreGraphics.CGRect(50, 100, 300, 50));
            label.StringValue = "Click on the button when you've granted the app accessibility access";
            label.Alignment = NSTextAlignment.Center;
            label.Editable = false;
            label.Bordered = false;
            label.DrawsBackground = false;

            // Create the button
            var button = new NSButton(new CoreGraphics.CGRect(100, 50, 200, 30));
            button.Title = "Check for accessibility access";
            button.BezelStyle = NSBezelStyle.Rounded;
            
            button.Activated += (sender, args) =>
            {
                if (AccessibilityHelpers.RequestForAccessibilityIfNotGranted())
                {
                    window.Close();
                    Start();
                }
            };

            var contentView = window.ContentView;
            
            // Add the label and the button to the window's content view
            contentView.AddSubview(label);
            contentView.AddSubview(button);
            
            window.MakeKeyAndOrderFront(this);

            NSRunningApplication.CurrentApplication.Activate(default);
        }
        
        public override void DidFinishLaunching(NSNotification notification)
        {
            if (AccessibilityHelpers.RequestForAccessibilityIfNotGranted())
            {
                Start();
            }

            else
            {
                MakeAccessibilityCheckerWindow();
            }
        }

        private void Start() 
        {
            ConstructMenuBarIcon();
            
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