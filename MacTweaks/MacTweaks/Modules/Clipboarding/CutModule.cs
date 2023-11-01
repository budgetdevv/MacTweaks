using System;
using AppKit;
using CoreFoundation;
using CoreGraphics;
using MacTweaks.Helpers;
using ObjCRuntime;

namespace MacTweaks.Modules.Clipboarding
{
    public class CutModule: IModule
    {
        private CGEvent.CGEventTapCallback OnCommandXCallback;

        private CFMachPort OnCommandXHandle;

        private nint LastCommandXChangeCount;
        
        private static readonly NSPasteboard GeneralPasteboard = NSPasteboard.GeneralPasteboard;
        
        public void Start()
        {
            var onCommandXCallback = OnCommandXCallback = OnCommandX;
            
            var onCommandXHandle = OnCommandXHandle = CGEvent.CreateTap(
                CGEventTapLocation.HID, 
                CGEventTapPlacement.HeadInsert,
                CGEventTapOptions.Default, 
                CGEventMask.KeyDown, 
                onCommandXCallback,
                IntPtr.Zero);
            
            CFRunLoop.Main.AddSource(onCommandXHandle.CreateRunLoopSource(), CFRunLoop.ModeCommon);
            
            CGEvent.TapEnable(onCommandXHandle);

            LastCommandXChangeCount = -1;
        }

        private static readonly NSWorkspace SharedWorkspace = NSWorkspace.SharedWorkspace;
        
        private IntPtr OnCommandX(IntPtr proxy, CGEventType type, IntPtr handle, IntPtr userInfo)
        {
            if (!type.CGEventTapIsDisabled())
            {
                var @event = Runtime.GetINativeObject<CGEvent>(handle, false);

                var flags = @event.Flags;
                
                if (flags.GetKeyModifiersOnly() == CGEventFlags.Command)
                {
                    var keyCode = AccessibilityHelpers.CGEventField.KeyboardEventKeycode;
                    
                    var keyValue = unchecked((NSKey) AccessibilityHelpers.CGEventGetIntegerValueField(handle, keyCode));
                    
                    var pasteboard = GeneralPasteboard;

                    if (SharedWorkspace.FrontmostApplication.LocalizedName == ConstantHelpers.FINDER_APP_NAME)
                    {
                        var currentChangeCount = pasteboard.ChangeCount;
                        
                        if (keyValue == NSKey.X)
                        {
                            if (AccessibilityHelpers.FinderGetSelectedItemsCount() != 0)
                            {
                                var eventSource = new CGEventSource(CGEventSourceStateID.HidSystem);

                                var commandC = new CGEvent(eventSource, (ushort) NSKey.C, true);
                            
                                CGEvent.Post(commandC, CGEventTapLocation.HID);

                                commandC.EventType = CGEventType.KeyUp;
                            
                                CGEvent.Post(commandC, CGEventTapLocation.HID);
                            
                                // AccessibilityHelpers.CGEventSetIntegerValueField(handle, keyCode, unchecked((long) NSKey.C));
                            
                                LastCommandXChangeCount = currentChangeCount + 1;
                            
                                // We still want Command X to fall through, in the event where we are trying to
                                // cut an application / folder's name ( Renaming selects them ).
                            }

                            else
                            {
                                LastCommandXChangeCount = -1;
                            }
                        }
                    
                        else if (keyValue == NSKey.V && currentChangeCount == LastCommandXChangeCount)
                        {
                            // Holding down option causes it to move instead of paste.
                            // Modify the event to do so
                            @event.Flags = flags | CGEventFlags.Alternate;
                        }
                    }
                }
            }

            else
            {
                CGEvent.TapEnable(OnCommandXHandle);
            }
            
            return handle;
        }

        public void Stop()
        {
            OnCommandXHandle.Dispose();
        }
    }
}