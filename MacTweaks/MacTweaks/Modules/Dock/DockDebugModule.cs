using System;
using CoreGraphics;
using MacTweaks.Helpers;

namespace MacTweaks.Modules.Dock
{
    #if DEBUG
    public class DockDebugModule: IModule
    {
        public void Start()
        {
            CGHelpers.CGEventTapManager.OnRightMouseDown.Event += OnRightMouseDown;
        }
        
        private static CGEvent OnRightMouseDown(IntPtr tapProxyEvent, CGEventType eventType, CGEvent @event)
        {
            var mouseLocation = @event.Location;
            
            AccessibilityHelpers.AXGetElementAtPosition((float) mouseLocation.X, (float) mouseLocation.Y, out var data);
                    
            Console.WriteLine($"{data.AXTitle} | {data.AXSubrole} | {data.AXIsApplicationRunning} | {data.PID} | {data.Rect} [ x:{mouseLocation.X} y:{mouseLocation.Y} ]");

            return @event;
        }
        
        public void Stop()
        {
            CGHelpers.CGEventTapManager.OnRightMouseDown.Event -= OnRightMouseDown;
        }
    }
    #endif
}