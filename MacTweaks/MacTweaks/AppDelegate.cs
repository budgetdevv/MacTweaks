using System;
using AppKit;
using Foundation;
using MacTweaks.Helpers;

namespace MacTweaks
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        public AppDelegate()
        {
            /*caret*/
        }
        
        public override void DidFinishLaunching(NSNotification notification)
        {
            Console.WriteLine(AccessibilityHelpers.RequestForAccessibilityIfNotGranted());
            
            NSEvent.AddGlobalMonitorForEventsMatchingMask(
                NSEventMask.RightMouseDown, @event =>
                {
                    var mouseLocation = @event.LocationInWindow.ToMacOSCoordinates();

                    AccessibilityHelpers.AXGetElementAtPosition((float) mouseLocation.X, (float) mouseLocation.Y, out var data);

                    var location = data.Rect.Location;
                    
                    Console.WriteLine($"{data.AXTitle} | {data.AXSubrole} | {data.AXIsApplicationRunning} | {location.X} | {location.Y}");
                });
        }

        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
        }
    }
}