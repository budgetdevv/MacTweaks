using System;
using System.Linq;
using AppKit;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using MacTweaks.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace MacTweaks.Modules.Dock
{
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
        
        private IntPtr OnRightMouseDown(IntPtr tapProxyEvent, CGEventType eventType, IntPtr eventRef, IntPtr userInfo)
        {
            var @event = new CGEvent(eventRef);

            var mouseLocation = @event.Location;
            
            AccessibilityHelpers.AXGetElementAtPosition((float) mouseLocation.X, (float) mouseLocation.Y, out var data);
                    
            Console.WriteLine($"{data.AXTitle} | {data.AXSubrole} | {data.AXIsApplicationRunning} | {data.PID} | {data.Rect} [ x:{mouseLocation.X} y:{mouseLocation.Y} ]");

            return eventRef;
        }
        
        public void Stop()
        {
            OnRightMouseDownHandle.Dispose();
        }
    }
}