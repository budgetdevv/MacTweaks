using System;
using System.Web;
using AppKit;
using CoreGraphics;
using Foundation;
using MacTweaks.Helpers;

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
        //Do NOT cache NSScreen.MainScreen or NSStatusBar.SystemStatusBar, since data might become stale
        
        public nfloat DockHeight, DockHeightThreshold, MenuBarHeight;

        public NSDockTile DockTile;

        public nfloat CenterX;
        
        public delegate void MouseEvent(CGEvent @event);

        public event MouseEvent OnBottomLeftHotCornerLeftClick, OnBottomRightHotCornerLeftClick;

        private static readonly NSWorkspace SharedWorkspace = NSWorkspace.SharedWorkspace;

        private void CalculateScreenMetadata(NSScreen mainScreen)
        {
            // Unfortunately, NSStatusBar.SystemStatusBar.Thickness is unreliable. See: https://github.com/feedback-assistant/reports/issues/140
            AccessibilityHelpers.GetMenuBarSize(NSRunningApplication.CurrentApplication.ProcessIdentifier, out var menuBarSize);

            var menuBarHeight = MenuBarHeight = menuBarSize.Height;
            
            // Calculate dock height
            // https://stackoverflow.com/questions/35826550/how-to-get-position-width-and-height-of-mac-os-x-dock-cocoa-carbon-c-qt
            // We use this info to avoid pinvoke calls when the coordinate is outside of dock's range.

            var totalHeight = mainScreen.Frame.Height;

            var visibleHeight = mainScreen.VisibleFrame.Height;

            var dockHeightThreshold = DockHeightThreshold = visibleHeight + menuBarHeight;
            
            DockHeight = totalHeight - dockHeightThreshold;
            
            CenterX = NSScreen.MainScreen.Frame.GetCenterX();
        }
        
        public void Start()
        {
            var mainScreen = NSScreen.MainScreen;
            
            CalculateScreenMetadata(mainScreen);
            
            DockTile = NSApplication.SharedApplication.DockTile;
            
            CGHelpers.CGEventTapManager.OnLeftMouseDown.Event += OnDockLeftClick;

            NSNotificationCenter.DefaultCenter.AddObserver(NSApplication.DidChangeScreenParametersNotification, DidChangeScreenParameters);
            
            OnBottomRightHotCornerLeftClick += (@event) =>
            {
                var sharedWorkspace = SharedWorkspace;

                //TODO: Make a dictionary cache for running applications
                foreach (var app in sharedWorkspace.RunningApplications)
                { 
                    if (app.LocalizedName != ConstantHelpers.FINDER_APP_NAME)
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

        private void DidChangeScreenParameters(NSNotification notification)
        {
            var mainScreen = NSScreen.MainScreen;
            
            CalculateScreenMetadata(mainScreen);

            // var x = mainScreen.Frame;
            // x.Height = DockHeight;
            // x.Location = new CGPoint(0, 0);
            //
            // var window = new NSWindow(x, NSWindowStyle.Closable, NSBackingStore.Buffered, false);
            //
            // window.MakeKeyAndOrderFront(default);
        }
        
        private IntPtr OnDockLeftClick(IntPtr proxy, CGEventType type, IntPtr handle, CGEvent @event)
        {
            var mouseLocation = @event.Location;

            if (mouseLocation.Y <= DockHeightThreshold)
            {
                return handle;
            }

            var sharedWorkspace = SharedWorkspace;
            
            var activeApp = sharedWorkspace.FrontmostApplication;
                
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (activeApp == null)
            {
                // Let's just put it this way...It is never supposed to be null,
                // but it is right after you enter password for admin dialog ( For sudo or system changes etc )
                // Just let the mouse event flow through...
                    
                //TODO: FIND OUT WHY!!!

                goto FlowThrough;
            }
                
            var exists = AccessibilityHelpers.AXGetElementAtPosition((float) mouseLocation.X, (float) mouseLocation.Y, out var clickedElement);

            string title;
                
            if (exists && (title = clickedElement.AXTitle) != null)
            {
                var subrole = clickedElement.AXSubrole;
                    
                if (clickedElement.ApplicationIsRunning && subrole == "AXApplicationDockItem")
                {
                    var titleSpan = title.AsSpan();
                    
                    if (!activeApp.GetDockName().SequenceEqual(titleSpan))
                    {
                        goto HideApp;
                    }
                    
                    // Check if the active application have all windows minimized.
                    // If so, we shouldn't attempt to hide the application
                    // AccessibilityHelpers.ApplicationAllWindowsAreMinimized will return false if there are no active
                    // windows. This resolves the bug of finder not launching when there are no active windows.
                    
                    if (!AccessibilityHelpers.ApplicationAllWindowsAreMinimized(activeApp.ProcessIdentifier, out var areMinimized) || areMinimized)
                    {
                        return handle;
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
                            if (title != ConstantHelpers.FINDER_APP_NAME)
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

                            return IntPtr.Zero;
                        }

                        // Clicking on dock icon re-activates app anyway
                        break;
                    }
                }

                else if (subrole == "AXTrashDockItem" && AccessibilityHelpers.TryToggleTrashWindow())
                {
                    return IntPtr.Zero;
                }
            }

            else
            {
                if (!AccessibilityHelpers.ApplicationFocusedWindowIsFullScreen(activeApp.ProcessIdentifier))
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
                
                // Don't do anything in fullscreen mode
            }

            FlowThrough:
            return handle;
        }

        public void Stop()
        {
            CGHelpers.CGEventTapManager.OnLeftMouseDown.Event -= OnDockLeftClick; 
        }
    }
}