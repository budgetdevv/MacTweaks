using System;
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

                        if (selectedPaths != null)
                        {
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

                                else
                                {
                                    NSFileManager.DefaultManager.TrashItem(new NSUrl(path, true), out _, out _);
                                }
                            }
                    
                            return IntPtr.Zero;
                        }
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