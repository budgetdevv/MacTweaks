using System;
using AppKit;
using CoreGraphics;
using MacTweaks.Helpers;

namespace MacTweaks.Modules.Window
{
    public class RedQuitModule : IModule
    {
        private static readonly NSWorkspace SharedWorkspace = NSWorkspace.SharedWorkspace;
        
        public void Start()
        {
            CGHelpers.CGEventTapManager.OnLeftMouseDown.Event += OnLeftClick;
        }
        
        private static CGEvent OnLeftClick(IntPtr proxy, CGEventType type, CGEvent @event)
        {
            var mouseLocation = @event.Location;
            
            var exists = AccessibilityHelpers.AXGetElementAtPosition((float) mouseLocation.X, (float) mouseLocation.Y, out var clickedElement);

            if (!exists || clickedElement.AXSubrole != "AXCloseButton")
            {
                return @event;
            }

            var clickedElementPID = clickedElement.PID;

            var config = AppHelpers.Config;

            foreach (var app in SharedWorkspace.RunningApplications)
            {
                var currentPID = app.ProcessIdentifier;

                if (currentPID != clickedElementPID)
                {
                    continue;
                }
                
                if (!config.RedQuitAppIsWhitelisted(app) && AccessibilityHelpers.GetWindowCountForApplication(currentPID) <= 1)
                {
                    if ((@event.Flags & CGEventFlags.Shift) != CGEventFlags.Shift)
                    {
                        app.Terminate();
                    }

                    else
                    {
                        // It seems impossible to get accessibility element of a window that is not responding. 
                        // TODO: Find out if there's a way to remedy this.
                        app.ForceTerminate();
                    }

                    return null;
                }

                break;
            }

            return @event;
        }

        public void Stop()
        {
            CGHelpers.CGEventTapManager.OnLeftMouseDown.Event -= OnLeftClick;
        }
    }
}