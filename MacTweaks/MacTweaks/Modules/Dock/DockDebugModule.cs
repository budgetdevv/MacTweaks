using System;
using CoreFoundation;
using CoreGraphics;
using MacTweaks.Helpers;
using ObjCRuntime;

namespace MacTweaks.Modules.Dock
{
    #if DEBUG
    public class DockDebugModule: IModule
    {
        private readonly AppDelegate AppDelegate;

        public DockDebugModule(AppDelegate appDelegate)
        {
            AppDelegate = appDelegate;
        }
        
        public void Start()
        {
            CGHelpers.CGEventTapManager.OnRightMouseDown.Event += OnRightMouseDown;
        }
        
        private IntPtr OnRightMouseDown(IntPtr tapProxyEvent, CGEventType eventType, IntPtr handle, CGEvent @event)
        {
            var mouseLocation = @event.Location;
            
            AccessibilityHelpers.AXGetElementAtPosition((float) mouseLocation.X, (float) mouseLocation.Y, out var data);
                    
            Console.WriteLine($"{data.AXTitle} | {data.AXSubrole} | {data.AXIsApplicationRunning} | {data.PID} | {data.Rect} [ x:{mouseLocation.X} y:{mouseLocation.Y} ]");

            return handle;
        }
        
        public void Stop()
        {
            CGHelpers.CGEventTapManager.OnRightMouseDown.Event -= OnRightMouseDown;
        }
    }
    #endif
}