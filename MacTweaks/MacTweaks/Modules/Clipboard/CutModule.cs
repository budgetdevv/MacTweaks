using System;
using AppKit;
using CoreGraphics;
using MacTweaks.Helpers;

namespace MacTweaks.Modules.Clipboard
{
    public class CutModule: IModule
    {
        private nint LastCommandXChangeCount;
        
        private static readonly NSPasteboard GeneralPasteboard = NSPasteboard.GeneralPasteboard;
        
        public void Start()
        {
            CGHelpers.CGEventTapManager.OnKeyDown.Event += OnCommandX;

            LastCommandXChangeCount = -1;
        }

        private static readonly NSWorkspace SharedWorkspace = NSWorkspace.SharedWorkspace;
        
        private CGEvent OnCommandX(IntPtr proxy, CGEventType type, CGEvent @event)
        {
            var flags = @event.Flags;
                
            if (flags.GetKeyModifiersOnly() == CGEventFlags.Command)
            {
                var keyCode = AccessibilityHelpers.CGEventField.KeyboardEventKeycode;
                    
                var keyValue = unchecked((NSKey) AccessibilityHelpers.CGEventGetIntegerValueField(@event.Handle, keyCode));
                    
                var pasteboard = GeneralPasteboard;

                if (SharedWorkspace.FrontmostApplication.LocalizedName == ConstantHelpers.FINDER_APP_NAME)
                {
                    var currentChangeCount = pasteboard.ChangeCount;

                    var finderSelectedItemsCount = AccessibilityHelpers.FinderGetSelectedItemsCount();

                    var finderHaveSelectedItems = finderSelectedItemsCount != 0;
                    
                    if (keyValue == NSKey.X)
                    {
                        if (finderHaveSelectedItems)
                        {
                            var commandC = new CGEvent(CGHelpers.HIDEventSource, (ushort) NSKey.C, true);
                            
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
                    
                    // We check for !finderHaveSelectedItems, since command + alt + V doesn't
                    // work for pasting when we are trying to rename a finder element ( Folder, files etc )
                    // When renaming element(s), they are considered "selected"
                    else if (keyValue == NSKey.V && currentChangeCount == LastCommandXChangeCount && !finderHaveSelectedItems)
                    {
                        // Holding down option causes it to move instead of paste.
                        // Modify the event to do so
                        @event.Flags = flags | CGEventFlags.Alternate;
                    }
                }
            }
                
            return @event;
        }

        public void Stop()
        {
            CGHelpers.CGEventTapManager.OnKeyDown.Event -= OnCommandX;
        }
    }
}