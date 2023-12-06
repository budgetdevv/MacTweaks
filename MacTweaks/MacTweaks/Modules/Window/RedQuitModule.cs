using System;
using System.Collections.Generic;
using AppKit;
using CoreGraphics;
using MacTweaks.Helpers;

namespace MacTweaks.Modules.Window
{
    public class RedQuitModule : IModule
    {
        private static readonly NSWorkspace SharedWorkspace = NSWorkspace.SharedWorkspace;

        private HashSet<string> Whitelist;
        
        public void Start()
        {
            CGHelpers.CGEventTapManager.OnLeftMouseDown.Event += OnLeftClick;

            Whitelist = AppHelpers.Config.RedQuitWhitelist;
        }
        
        private CGEvent OnLeftClick(IntPtr proxy, CGEventType type, CGEvent @event)
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

                if ((@event.Flags & CGEventFlags.Shift) != CGEventFlags.Shift)
                {
                    if (Whitelist.Contains(app.BundleIdentifier) || AccessibilityHelpers.GetWindowCountForApplication(currentPID) > 1)
                    {
                        goto FlowThrough;
                    }
                    
                    app.Terminate();
                }

                else
                {
                    app.ForceTerminate();
                }

                return null;
            }

            FlowThrough:
            return @event;
        }

        public void Stop()
        {
            CGHelpers.CGEventTapManager.OnLeftMouseDown.Event -= OnLeftClick;
        }
    }
}