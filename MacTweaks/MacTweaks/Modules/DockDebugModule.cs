using System;
using Foundation;
using MacTweaks.Helpers;
using AppKit;
using CoreGraphics;

namespace MacTweaks.Modules
{
    public class DockDebugModule: IModule
    {
        private NSObject OnRightMouseDownHandle;
        
        public void Start()
        {
            OnRightMouseDownHandle = NSEvent.AddGlobalMonitorForEventsMatchingMask(NSEventMask.RightMouseDown, OnRightMouseDown);
        }
        
        private static void OnRightMouseDown(NSEvent @event)
        {
            var mouseLocation = @event.LocationInWindow.ToMacOSCoordinates();

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