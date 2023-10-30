using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using CoreGraphics;
using Foundation;
using MacTweaks.Helpers;
using MacTweaks.Modules;
using MacTweaks.Modules.Clipboarding;
using MacTweaks.Modules.Dock;
using MacTweaks.Modules.Energy;
using MacTweaks.Modules.Keystrokes;
using MacTweaks.Modules.Window;
using Microsoft.Extensions.DependencyInjection;

namespace MacTweaks
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        public readonly ServiceProvider Services;

        private NSStatusItem MenuBarStatusItem; // Prevent the menubar icon from being GC-ed
        
        public AppDelegate()
        {
            Services = GetServiceCollection().BuildServiceProvider();
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
            if (!AccessibilityHelpers.IsRoot())
            {
                var macTweaks = NSRunningApplication.CurrentApplication;
                
                var command = $"sudo sh -c 'nohup \"{macTweaks.BundleUrl.Path}/Contents/MacOS/{macTweaks.GetDockName().ToString()}\" > /dev/null 2>&1 &'";
                
                var psi = new ProcessStartInfo("/bin/zsh", $"-c \"{command}\"")
                {
                    // To hide the window, set UseShellExecute to false and RedirectStandardOutput to true
                    UseShellExecute = false,
                    RedirectStandardOutput = true, 
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
            
                var process = new Process();
                process.StartInfo = psi;
                process.Start();

                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Environment.Exit(0);
                }

                else
                {
                    //TODO: Handle authentication failure
                    Environment.Exit(0);
                }
    
                return;
            }
            
            // Remove from dock
            NSApplication.SharedApplication.ActivationPolicy = NSApplicationActivationPolicy.Accessory;
            
            ConstructMenuBarIcon();
            
            foreach (var service in Services.GetServices<IModule>())
            {
                service.Start();
            }
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
            
            collection.AddSingleton<IModule, RedQuitModule>();
            
            collection.AddSingleton<IModule, CommandDeleteModule>();
            
            collection.AddSingleton<IModule, CutModule>();
            
            collection.AddSingleton<IModule, LowPowerMode>();

            return collection;
        }

        private void ConstructMenuBarIcon()
        {
            // TODO: Improve this mess
            
            // Construct menu that will be displayed when tray icon is clicked
            var menuBarIconMenu = new NSMenu();
            var exitMenuItem = new NSMenuItem($"Quit {ConstantHelpers.APP_NAME}",
                (handler, args) =>
                {
                    Environment.Exit(0);
                });
            menuBarIconMenu.AddItem(exitMenuItem);

            // Display tray icon in upper-right-hand corner of the screen
            var statusItem = MenuBarStatusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(30);
            statusItem.Menu = menuBarIconMenu;
            statusItem.Image = NSImage.FromStream(System.IO.File.OpenRead("/Users/trumpmcdonaldz/Pictures/DonaldNaSmirk.jpeg"));
            statusItem.HighlightMode = true;
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
        
        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
        }
    }
}