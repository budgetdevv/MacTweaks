// #define DEBUG_ElevatedPrivileges

using System;
using System.IO;
using AppKit;
using CoreGraphics;
using Foundation;
using MacTweaks.Helpers;
using MacTweaks.Modules;
using MacTweaks.Modules.Clipboard;
using MacTweaks.Modules.Credentials;
using MacTweaks.Modules.Dock;
using MacTweaks.Modules.Energy;
using MacTweaks.Modules.Keystrokes;
using MacTweaks.Modules.Mouse;
using MacTweaks.Modules.Window;
using Microsoft.Extensions.DependencyInjection;

namespace MacTweaks
{
    // ReSharper disable once ClassNeverInstantiated.Global
    [Register("AppDelegate")]
    public class AppDelegate: NSApplicationDelegate
    {
        public readonly ServiceProvider Services;

        private NSStatusItem MenuBarStatusItem; // Prevent the menubar icon from being GC-ed
        
        private ServiceCollection GetServiceCollection()
        {
            var collection = new ServiceCollection();
            
            collection.AddSingleton<AppDelegate>(this);
            
            #if DEBUG
            collection.AddSingleton<IModule, DockDebugModule>();
            #endif
            
            collection.AddSingleton<IModule, DockModule>();
            
            collection.AddSingleton<IModule, CommandQModule>();
            
            collection.AddSingleton<IModule, RedQuitModule>();
            
            collection.AddSingleton<IModule, CommandDeleteModule>();
            
            collection.AddSingleton<IModule, CutModule>();
            
            collection.AddSingleton<IModule, LowPowerMode>();
            
            collection.AddSingleton<IModule, BypassAskForPasswordModule>();
            
            collection.AddSingleton<IModule, NavigationModule>();

            return collection;
        }
        
        public AppDelegate()
        {
            Services = GetServiceCollection().BuildServiceProvider();
        }

        private const string RequestForPermissionsScriptText = @"tell application ""System Events""
                                                                 	-- Request for System Events access is implicit in this script because we're using 'System Events'
                                                                 	
                                                                 	-- Request for Desktop access
                                                                 	set desktopPath to path to desktop as string
                                                                 	get alias desktopPath
                                                                 	
                                                                 	-- Request for Finder access
                                                                 	tell application ""Finder""
                                                                 		get home
                                                                 	end tell
                                                                 	
                                                                 	-- Request for Volumes access
                                                                 	set volumesPath to ""/Volumes/"" as POSIX file as alias
                                                                 	get volumesPath
                                                                 end tell";

        private static readonly NSAppleScript RequestForPermissionsScript = new NSAppleScript(RequestForPermissionsScriptText);
        
        private static bool RequestForPermissions()
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return RequestForPermissionsScript.ExecuteAndReturnError(out _) != null && AccessibilityHelpers.RequestForAccessibilityIfNotGranted();
        }
        
        public override void DidFinishLaunching(NSNotification notification)
        {
            if (RequestForPermissions())
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
            if (!Directory.Exists(ConstantHelpers.MAC_TWEAKS_LOGS_PATH))
            {
                Directory.CreateDirectory(ConstantHelpers.MAC_TWEAKS_LOGS_PATH);
            }
            
            #if RELEASE || DEBUG_ElevatedPrivileges
            if (!AppHelpers.IsSudoUser)
            {
                if (AppHelpers.TryRelaunchApp(true))
                {
                    Environment.Exit(0);
                }

                else
                {
                    // Create the window
                    var window = new NSWindow(new CGRect(200, 200, 400, 200), NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Miniaturizable, NSBackingStore.Buffered, false);
                    window.Title = ConstantHelpers.APP_NAME;

                    // Create the label
                    var label = new NSTextField(new CGRect(50, 100, 300, 50));
                    label.StringValue = "Sudo elevation failure. Try again?";
                    label.Alignment = NSTextAlignment.Center;
                    label.Editable = false;
                    label.Bordered = false;
                    label.DrawsBackground = false;
                    
                    var contentView = window.ContentView!;

                    contentView.AddSubview(label);

                    // Create the button
                    var button = new NSButton(new CGRect(75, 50, 125, 30));
                    button.Title = "Yes";
                    button.BezelStyle = NSBezelStyle.Rounded;
            
                    button.Activated += (sender, args) =>
                    {
                        Start();
                    };
                    
                    contentView.AddSubview(button);
                    
                    button = new NSButton(new CGRect(200, 50, 125, 30));
                    button.Title = "No";
                    button.BezelStyle = NSBezelStyle.Rounded;
            
                    // ReSharper disable once UnusedParameter.Local
                    button.Activated += (sender, args) =>
                    {
                        Environment.Exit(0);
                    };
                    
                    contentView.AddSubview(button);
            
                    window.MakeKeyAndOrderFront(this);

                    NSRunningApplication.CurrentApplication.Activate(default);
                }
    
                return;
            }
            #endif
            
            // Remove from dock
            NSApplication.SharedApplication.ActivationPolicy = NSApplicationActivationPolicy.Accessory;
            
            ConstructMenuBarIcon();
            
            foreach (var service in Services.GetServices<IModule>())
            {
                service.Start();
            }
            
            CGHelpers.CGEventTapManager.Initialize();
        }

        private void ConstructMenuBarIcon()
        {
            // TODO: Improve this mess
            
            // Construct menu that will be displayed when tray icon is clicked
            var menuBarIconMenu = new NSMenu();
            
            var menuItem = new NSMenuItem($"Compact App Memory",
                (handler, args) =>
                {
                    AppHelpers.TryRelaunchApp(asSudo: AppHelpers.IsSudoUser);
                    Environment.Exit(0);
                });
            
            menuBarIconMenu.AddItem(menuItem);
            
            menuItem = new NSMenuItem($"Quit {ConstantHelpers.APP_NAME}",
                (handler, args) =>
                {
                    Environment.Exit(0);
                });
            
            menuBarIconMenu.AddItem(menuItem);

            const int ICON_SIZE = 25;

            // Display tray icon in upper-right-hand corner of the screen
            var statusItem = MenuBarStatusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(ICON_SIZE);
            statusItem.Menu = menuBarIconMenu;

            var statusButton = statusItem.Button;
            
            var image = NSImage.FromStream(File.OpenRead(ConstantHelpers.APP_ICON_PATH));
            statusButton.Image = ResizeImage(image, new CGSize(ICON_SIZE, ICON_SIZE));
            statusButton.Highlighted = false;
            
            return;

            NSImage ResizeImage(NSImage sourceImage, CGSize newSize)
            {
                var newImage = new NSImage(newSize);
                newImage.LockFocus();
                
                sourceImage.Size = newSize;
                
                NSGraphicsContext.CurrentContext.ImageInterpolation = NSImageInterpolation.High;
                
                sourceImage.Draw(new CGRect(0, 0, newSize.Width, newSize.Height),
                    new CGRect(0, 0, sourceImage.Size.Width, sourceImage.Size.Height),
                    NSCompositingOperation.SourceOver,
                    1);
                
                newImage.UnlockFocus();

                return newImage;
            }
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
            var window = new NSWindow(new CGRect(200, 200, 400, 200), NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Miniaturizable, NSBackingStore.Buffered, false);
            window.Title = ConstantHelpers.APP_NAME;

            // Create the label
            var label = new NSTextField(new CGRect(50, 100, 300, 50));
            label.StringValue = "Click on the button when you've granted the app accessibility access";
            label.Alignment = NSTextAlignment.Center;
            label.Editable = false;
            label.Bordered = false;
            label.DrawsBackground = false;

            // Create the button
            var button = new NSButton(new CGRect(100, 50, 200, 30));
            button.Title = "Check for accessibility access";
            button.BezelStyle = NSBezelStyle.Rounded;
            
            button.Activated += (sender, args) =>
            {
                if (RequestForPermissions())
                {
                    window.Close();
                    Start();
                }
            };

            var contentView = window.ContentView!;
            
            // Add the label and the button to the window's content view
            contentView.AddSubview(label);
            contentView.AddSubview(button);
            
            window.MakeKeyAndOrderFront(this);

            NSRunningApplication.CurrentApplication.Activate(default);
        }
        
        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
        }
    }
}