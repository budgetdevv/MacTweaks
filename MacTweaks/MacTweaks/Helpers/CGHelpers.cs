using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using AppKit;
using CoreFoundation;
using CoreGraphics;
using ObjCRuntime;

namespace MacTweaks.Helpers
{
    public static class CGHelpers
    {
        private static readonly nfloat ScreenHeight = NSScreen.MainScreen.Frame.Height;
        
        public static CGPoint InvertY(this CGPoint point)
        {
            // https://sl.bing.net/cQoOrHsLws0
            
            var screenHeight = ScreenHeight;
            
            // Adjust for MacOS's coordinate system
            var adjustedY = screenHeight - point.Y;

            return new CGPoint(point.X, adjustedY);
        }

        public static nfloat GetCenterX(this CGRect rect)
        {
            return rect.X + rect.Width / 2;
        }
        
        public static nfloat GetCenterY(this CGRect rect)
        {
            return rect.Y + rect.Height / 2;
        }
        
        public static CGPoint GetCentrePoint(this CGRect rect)
        {
            return new CGPoint(GetCenterX(rect), GetCenterY(rect));
        }
        
        public static bool CGEventTapIsDisabled(this CGEventType type)
        {
            return type >= CGEventType.TapDisabledByTimeout;
        }

        public static CGEventFlags GetKeyModifiersOnly(this CGEventFlags flags)
        {
            // Mask off anything 255 and below ( Apparently some bits are set 255 and below )
            
            const CGEventFlags IGNORE_255_AND_BELOW_MASK = (CGEventFlags) ~((ulong) 255);
            
            const CGEventFlags IGNORE_NON_COALESCED_MASK = ~CGEventFlags.NonCoalesced;

            const CGEventFlags IGN0RE_MASK = IGNORE_255_AND_BELOW_MASK & IGNORE_NON_COALESCED_MASK;

            return flags & IGN0RE_MASK;
        }

        public static class CGEventTapManager
        {
            public delegate CGEvent CGEventTapCallbackDelegate(IntPtr proxy, CGEventType type, CGEvent @event);

            public struct CGEventTapCallback
            {
                private readonly List<CGEventTapCallbackDelegate> Callbacks;

                public bool IsEmpty => Callbacks.Count == 0;
                
                public event CGEventTapCallbackDelegate Event
                {
                    add => Callbacks.Add(value);
                    remove => Callbacks.Remove(value);
                }
                
                public CGEventTapCallback()
                {
                    Callbacks = new List<CGEventTapCallbackDelegate>();
                }
                
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public IntPtr Invoke(IntPtr proxy, CGEventType type, IntPtr eventHandle)
                {
                    // https://developer.apple.com/documentation/coregraphics/cgeventtapcallback?language=objc
                    
                    // The original eventHandle and the returned will be released by
                    // the library itself. So we shouldn't release them on our own.
                    
                    // We use owns: true for conversions to managed to avoid incrementing
                    // CFRetainCount. For the first conversion ( Which is the original eventHandle ),
                    // the library will only release it after our Invoke() function is called,
                    // so there is no need to increment CFRetainCount. For subsequent conversions
                    // that happen when the returned pointer is changed, we also do not increment
                    // CFRetainCount. This is because any new CGEvents are assumed to be created
                    // by the event callback. We do release those CGEvents, however.
                    
                    var isOriginalEvent = true;
                    
                    // owns: true doesn't increment CFRetainCount
                    var previousEvent = Runtime.GetINativeObject<CGEvent>(eventHandle, true)!;
                    
                    foreach (var callback in Callbacks)
                    {
                        var currentEvent = callback(proxy, type, previousEvent);

                        var areSame = previousEvent == currentEvent;
                        
                        if (areSame)
                        {
                            continue;
                        }
                        
                        if (!isOriginalEvent)
                        {
                            previousEvent.Dispose();
                        }

                        else // The original and returned CGEVent gets cleaned up
                        {
                            isOriginalEvent = false;
                        }
                        
                        // Don't move this up - We need to make sure previousEvent
                        // is released if it is not the original one
                        if (currentEvent != null)
                        {
                            previousEvent = currentEvent;
                            continue;
                        }

                        eventHandle = IntPtr.Zero;
                        goto Ret;
                    }
                    
                    // Previous is actually current
                    eventHandle = previousEvent.Handle;
                    
                    Ret:
                    return eventHandle;
                }
            }
            
            // ReSharper disable MemberCanBePrivate.Global
            public static CGEventTapCallback
                OnNull = new CGEventTapCallback(),
                OnLeftMouseDown = new CGEventTapCallback(),
                OnLeftMouseUp = new CGEventTapCallback(),
                OnRightMouseDown = new CGEventTapCallback(),
                OnRightMouseUp = new CGEventTapCallback(),
                OnMouseMoved = new CGEventTapCallback(),
                OnLeftMouseDragged = new CGEventTapCallback(),
                OnRightMouseDragged = new CGEventTapCallback(),
                OnKeyDown = new CGEventTapCallback(),
                OnKeyUp = new CGEventTapCallback(),
                OnFlagsChanged = new CGEventTapCallback(),
                OnScrollWheel = new CGEventTapCallback(),
                OnTabletPointer = new CGEventTapCallback(),
                OnTabletProximity = new CGEventTapCallback(),
                OnOtherMouseDown = new CGEventTapCallback(),
                OnOtherMouseUp = new CGEventTapCallback(),
                OnOtherMouseDragged = new CGEventTapCallback();
            // ReSharper restore MemberCanBePrivate.Global

            private static bool Initialized = false;
            
            public static void Initialize()
            {
                if (Initialized || !CGEventTapCache.EventTap.IsValid)
                {
                    throw new Exception("Something went wrong");
                }

                Initialized = true;
            }
            
            private static class CGEventTapCache
            {
                public static readonly CFMachPort EventTap;
            
                public static readonly bool
                    OnNullEnabled,
                    OnLeftMouseDownEnabled, 
                    OnLeftMouseUpEnabled, 
                    OnRightMouseDownEnabled, 
                    OnRightMouseUpEnabled, 
                    OnMouseMovedEnabled, 
                    OnLeftMouseDraggedEnabled, 
                    OnRightMouseDraggedEnabled, 
                    OnKeyDownEnabled, 
                    OnKeyUpEnabled, 
                    OnFlagsChangedEnabled, 
                    OnScrollWheelEnabled, 
                    OnTabletPointerEnabled, 
                    OnTabletProximityEnabled, 
                    OnOtherMouseDownEnabled, 
                    OnOtherMouseUpEnabled, 
                    OnOtherMouseDraggedEnabled;
                
                // Ensures that the garbage collector won't collect it.
                private static readonly CGEvent.CGEventTapCallback Callback;

                private static readonly CGEventMask Mask;

                private static readonly bool UseSwitch, BypassCheck;

                private static readonly ThreadHelpers.MainLoopTimer EventTapStatusPoller;
                
                [SuppressMessage("ReSharper", "AssignmentInConditionalExpression")]
                static CGEventTapCache()
                {
                    var mask = (CGEventMask) 0;
                    
                    if (OnNullEnabled = !OnNull.IsEmpty) 
                    { 
                        mask |= CGEventMask.Null; 
                    }

                    if (OnLeftMouseDownEnabled = !OnLeftMouseDown.IsEmpty) 
                    { 
                        mask |= CGEventMask.LeftMouseDown; 
                    }

                    if (OnLeftMouseUpEnabled = !OnLeftMouseUp.IsEmpty) 
                    { 
                        mask |= CGEventMask.LeftMouseUp; 
                    }

                    if (OnRightMouseDownEnabled = !OnRightMouseDown.IsEmpty) 
                    { 
                        mask |= CGEventMask.RightMouseDown; 
                    }

                    if (OnRightMouseUpEnabled = !OnRightMouseUp.IsEmpty) 
                    { 
                        mask |= CGEventMask.RightMouseUp; 
                    }

                    if (OnMouseMovedEnabled = !OnMouseMoved.IsEmpty) 
                    { 
                        mask |= CGEventMask.MouseMoved; 
                    }

                    if (OnLeftMouseDraggedEnabled = !OnLeftMouseDragged.IsEmpty) 
                    { 
                        mask |= CGEventMask.LeftMouseDragged; 
                    }

                    if (OnRightMouseDraggedEnabled = !OnRightMouseDragged.IsEmpty) 
                    { 
                        mask |= CGEventMask.RightMouseDragged; 
                    }

                    if (OnKeyDownEnabled = !OnKeyDown.IsEmpty) 
                    { 
                        mask |= CGEventMask.KeyDown; 
                    }

                    if (OnKeyUpEnabled = !OnKeyUp.IsEmpty) 
                    { 
                        mask |= CGEventMask.KeyUp; 
                    }

                    if (OnFlagsChangedEnabled = !OnFlagsChanged.IsEmpty) 
                    { 
                        mask |= CGEventMask.FlagsChanged; 
                    }

                    if (OnScrollWheelEnabled = !OnScrollWheel.IsEmpty) 
                    { 
                        mask |= CGEventMask.ScrollWheel; 
                    }

                    if (OnTabletPointerEnabled = !OnTabletPointer.IsEmpty) 
                    { 
                        mask |= CGEventMask.TabletPointer; 
                    }

                    if (OnTabletProximityEnabled = !OnTabletProximity.IsEmpty) 
                    { 
                        mask |= CGEventMask.TabletProximity; 
                    }

                    if (OnOtherMouseDownEnabled = !OnOtherMouseDown.IsEmpty) 
                    { 
                        mask |= CGEventMask.OtherMouseDown; 
                    }

                    if (OnOtherMouseUpEnabled = !OnOtherMouseUp.IsEmpty) 
                    { 
                        mask |= CGEventMask.OtherMouseUp; 
                    }

                    if (OnOtherMouseDraggedEnabled = !OnOtherMouseDragged.IsEmpty) 
                    { 
                        mask |= CGEventMask.OtherMouseDragged; 
                    }
                    
                    var eventTap = EventTap = CGEvent.CreateTap(
                        CGEventTapLocation.HID,
                        CGEventTapPlacement.HeadInsert,
                        CGEventTapOptions.Default,
                        Mask = mask,
                        Callback = OnCallback,
                        IntPtr.Zero)!;
                    
                    CFRunLoop.Main.AddSource(eventTap.CreateRunLoopSource(), CFRunLoop.ModeCommon);
            
                    CGEvent.TapEnable(eventTap);

                    var count = BitOperations.PopCount((ulong) mask);
                    
                    UseSwitch = count > 2;
                    BypassCheck = count <= 1;

                    EventTapStatusPoller = new ThreadHelpers.MainLoopTimer(TimeSpan.FromSeconds(5), _ =>
                    {
                        if (AccessibilityHelpers.RequestForAccessibilityIfNotGranted())
                        {
                            return;
                        }

                        HandleAccessibilityRevokedAccess();
                    });
                }

                [SkipLocalsInit]
                private static IntPtr OnCallback(IntPtr proxy, CGEventType type, IntPtr eventHandle, IntPtr userInfo)
                {
                    if (CGEventTapIsDisabled(type))
                    {
                        goto HandleDisabled;
                    }

                    Unsafe.SkipInit(out CGEventTapCallback eventCallback);
                    
                    if (UseSwitch)
                    {
                        eventCallback = type switch
                        {
                            CGEventType.Null => OnNull,
                            CGEventType.LeftMouseDown => OnLeftMouseDown,
                            CGEventType.LeftMouseUp => OnLeftMouseUp,
                            CGEventType.RightMouseDown => OnRightMouseDown,
                            CGEventType.RightMouseUp => OnRightMouseUp,
                            CGEventType.MouseMoved => OnMouseMoved,
                            CGEventType.LeftMouseDragged => OnLeftMouseDragged,
                            CGEventType.RightMouseDragged => OnRightMouseDragged,
                            CGEventType.KeyDown => OnKeyDown,
                            CGEventType.KeyUp => OnKeyUp,
                            CGEventType.FlagsChanged => OnFlagsChanged,
                            CGEventType.ScrollWheel => OnScrollWheel,
                            CGEventType.TabletPointer => OnTabletPointer,
                            CGEventType.TabletProximity => OnTabletProximity,
                            CGEventType.OtherMouseDown => OnOtherMouseDown,
                            CGEventType.OtherMouseUp => OnOtherMouseUp,
                            CGEventType.OtherMouseDragged => OnOtherMouseDragged
                        };
                    }

                    else
                    {
                        var bypassCheck = BypassCheck;
                        
                        if (OnNullEnabled && (type == CGEventType.Null || bypassCheck)) 
                        { 
                            eventCallback = OnNull; 
                        }

                        if (OnLeftMouseDownEnabled && (type == CGEventType.LeftMouseDown || bypassCheck)) 
                        { 
                            eventCallback = OnLeftMouseDown; 
                        }

                        if (OnLeftMouseUpEnabled && (type == CGEventType.LeftMouseUp || bypassCheck)) 
                        { 
                            eventCallback = OnLeftMouseUp; 
                        }

                        if (OnRightMouseDownEnabled && (type == CGEventType.RightMouseDown || bypassCheck)) 
                        { 
                            eventCallback = OnRightMouseDown; 
                        }

                        if (OnRightMouseUpEnabled && (type == CGEventType.RightMouseUp || bypassCheck)) 
                        { 
                            eventCallback = OnRightMouseUp; 
                        }

                        if (OnMouseMovedEnabled && (type == CGEventType.MouseMoved || bypassCheck)) 
                        { 
                            eventCallback = OnMouseMoved; 
                        }

                        if (OnLeftMouseDraggedEnabled && (type == CGEventType.LeftMouseDragged || bypassCheck)) 
                        { 
                            eventCallback = OnLeftMouseDragged; 
                        }

                        if (OnRightMouseDraggedEnabled && (type == CGEventType.RightMouseDragged || bypassCheck)) 
                        { 
                            eventCallback = OnRightMouseDragged; 
                        }

                        if (OnKeyDownEnabled && (type == CGEventType.KeyDown || bypassCheck)) 
                        { 
                            eventCallback = OnKeyDown; 
                        }

                        if (OnKeyUpEnabled && (type == CGEventType.KeyUp || bypassCheck)) 
                        { 
                            eventCallback = OnKeyUp; 
                        }

                        if (OnFlagsChangedEnabled && (type == CGEventType.FlagsChanged || bypassCheck)) 
                        { 
                            eventCallback = OnFlagsChanged; 
                        }

                        if (OnScrollWheelEnabled && (type == CGEventType.ScrollWheel || bypassCheck)) 
                        { 
                            eventCallback = OnScrollWheel; 
                        }

                        if (OnTabletPointerEnabled && (type == CGEventType.TabletPointer || bypassCheck)) 
                        { 
                            eventCallback = OnTabletPointer; 
                        }

                        if (OnTabletProximityEnabled && (type == CGEventType.TabletProximity || bypassCheck)) 
                        { 
                            eventCallback = OnTabletProximity; 
                        }

                        if (OnOtherMouseDownEnabled && (type == CGEventType.OtherMouseDown || bypassCheck)) 
                        { 
                            eventCallback = OnOtherMouseDown; 
                        }

                        if (OnOtherMouseUpEnabled && (type == CGEventType.OtherMouseUp || bypassCheck)) 
                        { 
                            eventCallback = OnOtherMouseUp; 
                        }

                        if (OnOtherMouseDraggedEnabled && (type == CGEventType.OtherMouseDragged || bypassCheck)) 
                        { 
                            eventCallback = OnOtherMouseDragged; 
                        }
                    }

                    return eventCallback.Invoke(proxy, type, eventHandle);
                    
                    HandleDisabled:
                    return HandleDisabled();

                    // Don't pollute hot path
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    IntPtr HandleDisabled()
                    {
                        if (AccessibilityHelpers.RequestForAccessibilityIfNotGranted())
                        {
                            CGEvent.TapEnable(EventTap);
                        }

                        else
                        {
                            HandleAccessibilityRevokedAccess();
                        }
                        
                        return eventHandle;
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                private static void HandleAccessibilityRevokedAccess()
                {
                    AppHelpers.RelinquishSudoAccess();
                    AppHelpers.TryRelaunchApp();
                    Environment.Exit(0);
                }
            }
        }
    }
}