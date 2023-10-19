using System;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;
using AppKit;
using CoreFoundation;
using Foundation;
using MacTweaks.Helpers;

namespace MacTweaks.Modules.Keystrokes
{
    public class CommandDeleteModule: IModule
    {
        private CGEvent.CGEventTapCallback OnCommandDeleteCallback;

        private CFMachPort OnCommandDeleteHandle;
        
        public void Start()
        {
            var onCommandDeleteCallback = OnCommandDeleteCallback = OnCommandDelete;
            
            var onCommandDeleteHandle = OnCommandDeleteHandle = CGEvent.CreateTap(
                CGEventTapLocation.Session, 
                CGEventTapPlacement.HeadInsert,
                CGEventTapOptions.Default, 
                CGEventMask.KeyDown, 
                onCommandDeleteCallback,
                IntPtr.Zero);
            
            CFRunLoop.Main.AddSource(onCommandDeleteHandle.CreateRunLoopSource(), CFRunLoop.ModeCommon);
            
            CGEvent.TapEnable(onCommandDeleteHandle);
        }

        private static readonly NSWorkspace SharedWorkspace = NSWorkspace.SharedWorkspace;

        // private class MountedVolumesEqualityComparer: IEqualityComparer<string>
        // {
        //     public bool Equals(string x, string y)
        //     {
        //         // This is mostly to handle MountedLocalVolumePaths comparisons against AccessibilityHelpers.FinderGetSelectedFilePaths(),
        //         // since the latter includes a trailing slash
        //         
        //         if (x != y)
        //         {
        //             var length = Math.Min(x.Length, y.Length);
        //
        //             return x.AsSpan(0, length).SequenceEqual(y.AsSpan(0, length));
        //         }
        //
        //         return true;
        //     }
        //
        //     public int GetHashCode(string obj)
        //     {
        //         
        //     }
        // }
        //
        // private static readonly MountedVolumesEqualityComparer MountedVolumesStringEqualityComparer = new MountedVolumesEqualityComparer();
        
        private IntPtr OnCommandDelete(IntPtr proxy, CGEventType type, IntPtr handle, IntPtr userInfo)
        {
            if (!type.CGEventTapIsDisabled())
            {
                var @event = new CGEvent(handle);

                if (@event.Flags.HasFlag(CGEventFlags.Command))
                {
                    var keyCode = (NSKey) AccessibilityHelpers.CGEventGetIntegerValueField(handle, AccessibilityHelpers.CGEventField.KeyboardEventKeycode);

                    if (keyCode == NSKey.Delete)
                    {
                        var workspace = SharedWorkspace;

                        var selectedPaths = AccessibilityHelpers.FinderGetSelectedFilePaths();

                        var mountedVolumes = workspace.MountedLocalVolumePaths.ToHashSet();
                        
                        foreach (var path in selectedPaths)
                        {
                            var span = path.AsSpan();

                            if (span.Length > 1)
                            {
                                // Get rid of trailing slash
                                span = span.Slice(0, span.Length - 1);
                            }

                            var actualPath = span.ToString();
                            
                            if (mountedVolumes.Contains(actualPath))
                            {
                                // NSWorkspace.UnmountAndEjectDevice() doesn't support network volumes
                                
                                if (AccessibilityHelpers.UnmountVolume(actualPath))
                                {
                                    continue;
                                }
                                
                                // Volume is in use. Display a warning dialog.
                                var alert = new NSAlert
                                {
                                    AlertStyle = NSAlertStyle.Warning,
                                    InformativeText = $"The volume {actualPath} is in use and cannot be ejected.",
                                    MessageText = "Warning"
                                };
                                alert.RunSheetModal(null);
                            }
                        }
                    
                        return IntPtr.Zero;
                    }
                }
            }

            else
            {
                CGEvent.TapEnable(OnCommandDeleteHandle);
            }
            
            return handle;
        }

        public void Stop()
        {
            OnCommandDeleteHandle.Dispose();
        }
    }
}