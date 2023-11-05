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
        
        private static IntPtr OnLeftClick(IntPtr proxy, CGEventType type, IntPtr handle, CGEvent @event)
        {
            var mouseLocation = @event.Location;
            
            var exists = AccessibilityHelpers.AXGetElementAtPosition((float) mouseLocation.X, (float) mouseLocation.Y, out var clickedElement);

            if (!exists || clickedElement.AXSubrole != "AXCloseButton")
            {
                return handle;
            }

            var clickedElementPID = clickedElement.PID;

            foreach (var app in SharedWorkspace.RunningApplications)
            {
                var currentPID = app.ProcessIdentifier;

                if (currentPID != clickedElementPID)
                {
                    continue;
                }

                // We don't want to terminate Finder...it will cause desktop to go KABOOM!
                // app.ActivationPolicy == NSApplicationActivationPolicy.Regular is the lazy way to do it for now...
                // TODO: Add whitelist functionality for this module
                if (app.LocalizedName != ConstantHelpers.FINDER_APP_NAME
                    && app.ActivationPolicy == NSApplicationActivationPolicy.Regular
                    && AccessibilityHelpers.GetWindowCountForApplication(currentPID) <= 1)
                {
                    app.Terminate();

                    return IntPtr.Zero;
                }

                break;
            }

            return handle;
        }

        public void Stop()
        {
            CGHelpers.CGEventTapManager.OnLeftMouseDown.Event -= OnLeftClick;
        }
    }
}