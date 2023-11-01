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
        
        private CFMachPort OnRightMouseDownHandle;

        private CGEvent.CGEventTapCallback Callback;

        public DockDebugModule(AppDelegate appDelegate)
        {
            AppDelegate = appDelegate;
        }
        
        public void Start()
        {
            Callback = OnRightMouseDown;
            
            var eventTap = OnRightMouseDownHandle = CGEvent.CreateTap(
                CGEventTapLocation.HID,
                CGEventTapPlacement.HeadInsert,
                CGEventTapOptions.Default,
                CGEventMask.RightMouseDown,
                Callback,
                IntPtr.Zero);
            
            CFRunLoop.Main.AddSource(eventTap.CreateRunLoopSource(), CFRunLoop.ModeCommon);
            
            CGEvent.TapEnable(eventTap);
        }
        
        private IntPtr OnRightMouseDown(IntPtr tapProxyEvent, CGEventType eventType, IntPtr handle, IntPtr userInfo)
        {
            var @event = Runtime.GetINativeObject<CGEvent>(handle, false);

            var mouseLocation = @event.Location;
            
            AccessibilityHelpers.AXGetElementAtPosition((float) mouseLocation.X, (float) mouseLocation.Y, out var data);
                    
            Console.WriteLine($"{data.AXTitle} | {data.AXSubrole} | {data.AXIsApplicationRunning} | {data.PID} | {data.Rect} [ x:{mouseLocation.X} y:{mouseLocation.Y} ]");

            return handle;
        }
        
        public void Stop()
        {
            OnRightMouseDownHandle.Dispose();
        }
    }
    #endif
}