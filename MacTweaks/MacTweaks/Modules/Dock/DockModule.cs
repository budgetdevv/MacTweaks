using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using AppKit;
using Foundation;
using MacTweaks.Helpers;
using ObjCRuntime;

namespace MacTweaks.Modules.Dock
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
        
        public delegate void MouseEvent(NSEvent @event);

        public event MouseEvent OnBottomLeftHotCornerLeftClick, OnBottomRightHotCornerLeftClick;

        private static readonly NSWorkspace SharedWorkspace = NSWorkspace.SharedWorkspace;
        
        public void Start()
        {
            OnRightMouseDownHandle = NSEvent.AddGlobalMonitorForEventsMatchingMask(NSEventMask.LeftMouseDown, OnDockLeftClick);

            DockTileThreshold = CalculateDockHeight();
            
            DockTile = NSApplication.SharedApplication.DockTile;

            OnBottomRightHotCornerLeftClick += (@event) =>
            {
                var sharedWorkspace = SharedWorkspace;

                //TODO: Make a dictionary cache for running applications
                foreach (var app in sharedWorkspace.RunningApplications)
                {
                    if (app.LocalizedName != "Finder")
                    {
                        continue;
                    }
                    
                    app.Activate(default);
                
                    sharedWorkspace.HideOtherApplications();

                    AccessibilityHelpers.MinimizeAllWindowsForApplication(app.ProcessIdentifier);
                    
                    break;
                }
            };
        }

        private static readonly NSScreen MainScreen = NSScreen.MainScreen;

        private static readonly nfloat CenterX = MainScreen.Frame.GetCenterX();

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

            if (mouseLocation.Y > DockTileThreshold)
            {
                return;
            }
            
            var mouseLocationMacOS = mouseLocation.ToMacOSCoordinates();
                
            var exists = AccessibilityHelpers.AXGetElementAtPosition((float) mouseLocationMacOS.X, (float) mouseLocationMacOS.Y, out var clickedElement);

            if (exists)
            {
                if (clickedElement.ApplicationIsRunning || clickedElement.AXSubrole == "AXApplicationDockItem")
                {
                    var title = clickedElement.AXTitle;
                    
                    var sharedWorkspace = NSWorkspace.SharedWorkspace;
                    
                    var activeApp = sharedWorkspace.FrontmostApplication;

                    var titleSpan = title.AsSpan();
                    
                    if (!activeApp.GetDockName().SequenceEqual(titleSpan))
                    {
                        goto HideApp;
                    }
                    
                    // Check if the active application have all windows minimized.
                    // If so, we shouldn't attempt to hide the application
                    if (AccessibilityHelpers.ApplicationAllWindowsAreMinimized(activeApp.ProcessIdentifier))
                    {
                        return;
                    }
                    
                    HideApp:
                    foreach (var app in sharedWorkspace.RunningApplications)
                    {
                        if (!app.GetDockName().SequenceEqual(titleSpan))
                        {
                            continue;
                        }

                        if (app.Active) // TODO: Replace this weird hack with new mouse detection mechanism
                        {
                            Task.Delay(100).ContinueWith(x =>
                            {
                                sharedWorkspace.InvokeOnMainThread(() =>
                                {
                                    if (title != "Finder")
                                    {
                                        app.Hide();
                                    }

                                    else
                                    {
                                        // This is necessary. If we use hide for Finder, and all other apps are hidden,
                                        // it will force another app to become active
                                        // ( Thus they will appear, and it is weird )
                                        AccessibilityHelpers.MinimizeAllWindowsForApplication(app.ProcessIdentifier);
                                    }
                                });
                            });
                        }
                        
                        // Clicking on dock icon re-activates app anyway
                        
                        break;
                    }
                }
            }

            else
            {
                // Hot corners
                
                var centerX = CenterX;

                if (mouseLocation.X > centerX)
                {
                    OnBottomRightHotCornerLeftClick?.Invoke(@event);
                }

                else
                {
                    OnBottomLeftHotCornerLeftClick?.Invoke(@event);
                }
            }
        }

        public void Stop()
        {
            OnRightMouseDownHandle.Dispose();
        }
    }
}