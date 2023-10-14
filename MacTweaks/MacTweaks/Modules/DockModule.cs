using System;
using System.Threading.Tasks;
using System.Web;
using Foundation;
using MacTweaks.Helpers;
using AppKit;
using CoreGraphics;

namespace MacTweaks.Modules
{
    public static class DockHelpers
    {
        public static ReadOnlySpan<char> GetDockName(this NSRunningApplication app)
        {
            // Example: file:///Users/trumpmcdonaldz/anaconda3/Anaconda-Navigator.app/

            const string SUFFIX = ".app/";

            var url = HttpUtility.UrlDecode(app.BundleUrl.ToString());

            // Should be constant folded...This truncates the suffix
            var span = url.AsSpan(0, url.Length - SUFFIX.Length);

            var index = span.LastIndexOf('/');

            if (index != -1)
            {
                span =  span.Slice(index + 1);
            }

            return span;
        }
    }
    
    public class DockModule: IModule
    {
        private NSObject OnRightMouseDownHandle;
        
        private nfloat DockTileThreshold;

        private NSDockTile DockTile;
        
        public void Start()
        {
            OnRightMouseDownHandle = NSEvent.AddGlobalMonitorForEventsMatchingMask(NSEventMask.LeftMouseDown, OnDockLeftClick);

            DockTileThreshold = CalculateDockHeight();
            
            DockTile = NSApplication.SharedApplication.DockTile;
        }

        private static readonly NSScreen MainScreen = NSScreen.MainScreen;

        private static readonly NSStatusBar SystemStatusBar = NSStatusBar.SystemStatusBar;
        
        public static nfloat CalculateDockHeight()
        {
            // TODO: Implement dock resize detection logic.
            // Probably could store last update timestamp, and update every few seconds
            // https://stackoverflow.com/questions/35826550/how-to-get-position-width-and-height-of-mac-os-x-dock-cocoa-carbon-c-qt
            // We use this info to avoid pinvoke calls when the coordinate is outside of dock's range.
            
            var mainScreen = MainScreen;

            var totalHeight = mainScreen.Frame.Height;

            var visibleHeight = mainScreen.VisibleFrame.Height;

            var dockHeight = totalHeight - (visibleHeight + SystemStatusBar.Thickness);

            return dockHeight;
        }
        
        private void OnDockLeftClick(NSEvent @event)
        {
            var mouseLocation = @event.LocationInWindow;

            if (mouseLocation.Y <= DockTileThreshold)
            {
                mouseLocation = mouseLocation.ToMacOSCoordinates();
                
                var exists = AccessibilityHelpers.AXGetElementAtPosition((float) mouseLocation.X, (float) mouseLocation.Y, out var clickedElement);

                var title = clickedElement.AXTitle;
                
                if (exists || clickedElement.ApplicationIsRunning || clickedElement.AXSubrole == "AXApplicationDockItem")
                {
                    var sharedWorkspace = NSWorkspace.SharedWorkspace;
                    
                    foreach (var app in sharedWorkspace.RunningApplications)
                    {
                        var dockName = app.GetDockName().ToString();
                        
                        if (dockName != title)
                        {
                            continue;
                        }

                        if (!app.Hidden) // TODO: Replace this weird hack with new mouse detection mechanism
                        {
                            Task.Delay(100).ContinueWith(x =>
                            {
                                sharedWorkspace.InvokeOnMainThread(() => app.Hide());
                            });
                        }
                        
                        // Clicking on dock icon re-activates app anyway
                        
                        break;
                    }
                }
            }
        }

        public void Stop()
        {
            OnRightMouseDownHandle.Dispose();
        }
    }
}