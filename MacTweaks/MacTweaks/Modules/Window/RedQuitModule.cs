using System;
using AppKit;
using CoreFoundation;
using CoreGraphics;
using MacTweaks.Helpers;

namespace MacTweaks.Modules.Window
{
    public class RedQuitModule: IModule
    {
        private CGEvent.CGEventTapCallback OnLeftClickCallback;

        private CFMachPort OnLeftClickHandle;
        
        public void Start()
        {
            var onLeftClickCallback = OnLeftClickCallback = OnLeftClick;
            
            var onLeftClickHandle = OnLeftClickHandle = CGEvent.CreateTap(
                CGEventTapLocation.Session, 
                CGEventTapPlacement.HeadInsert,
                CGEventTapOptions.Default, 
                CGEventMask.LeftMouseDown, 
                onLeftClickCallback,
                IntPtr.Zero);
            
            CFRunLoop.Main.AddSource(onLeftClickHandle.CreateRunLoopSource(), CFRunLoop.ModeCommon);
            
            CGEvent.TapEnable(onLeftClickHandle);
        }

        private static readonly NSWorkspace SharedWorkspace = NSWorkspace.SharedWorkspace;
        
        private IntPtr OnLeftClick(IntPtr proxy, CGEventType type, IntPtr handle, IntPtr userInfo)
        {
            if (!type.CGEventTapIsDisabled())
            {
                var @event = new CGEvent(handle);

                var mouseLocation = @event.Location;
            
                var exists = AccessibilityHelpers.AXGetElementAtPosition((float) mouseLocation.X, (float) mouseLocation.Y, out var clickedElement);
                
                if (exists && clickedElement.AXSubrole == "AXCloseButton")
                {
                    var pid = clickedElement.PID;

                    foreach (var app in SharedWorkspace.RunningApplications)
                    {
                        if (app.ProcessIdentifier != pid)
                        {
                            continue;
                        }

                        // We don't want to terminate Finder...it will cause desktop to go KABOOM!
                        // app.ActivationPolicy == NSApplicationActivationPolicy.Regular is the lazy way to do it for now...
                        // TODO: Add whitelist functionality for this module
                        if (app.LocalizedName != ConstantHelpers.FINDER_APP_NAME 
                            && app.ActivationPolicy == NSApplicationActivationPolicy.Regular
                            // TODO: Optimize this by getting window count from C-side. This will help avoid 2 allocations.
                            && (!AccessibilityHelpers.GetWindowListForApplication(pid, out var windows) || windows.Length <= 1))
                        {
                            app.Terminate();

                            return IntPtr.Zero;
                        }
                        
                        break;
                    }
                }
            }

            else
            {
                CGEvent.TapEnable(OnLeftClickHandle);
            }
            
            return handle;
        }

        public void Stop()
        {
            OnLeftClickHandle.Dispose();
        }
    }
}