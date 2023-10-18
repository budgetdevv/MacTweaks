using System;
using System.Linq;
using AppKit;
using CoreFoundation;
using Foundation;
using MacTweaks.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace MacTweaks.Modules.Dock
{
    public class DockDebugModule: IModule
    {
        private NSObject OnRightMouseDownHandle;

        private readonly AppDelegate AppDelegate;

        public DockDebugModule(AppDelegate appDelegate)
        {
            AppDelegate = appDelegate;
        }
        
        public void Start()
        {
            OnRightMouseDownHandle = NSEvent.AddGlobalMonitorForEventsMatchingMask(NSEventMask.RightMouseDown, OnRightMouseDown);
        }
        
        private void OnRightMouseDown(NSEvent @event)
        {
            var dm = AppDelegate.Services.GetServices<IModule>().First(x => x.GetType() == typeof(DockModule));

            // CFRunLoop.Main.ContainsSource();
            
            var mouseLocation = @event.LocationInWindow.InvertY();

            AccessibilityHelpers.AXGetElementAtPosition((float) mouseLocation.X, (float) mouseLocation.Y, out var data);

            var location = data.Rect.Location;
                    
            Console.WriteLine($"{data.AXTitle} | {data.AXSubrole} | {data.AXIsApplicationRunning} | {location.X} | {location.Y}");
        }
        
        public void Stop()
        {
            OnRightMouseDownHandle.Dispose();
        }
    }
}