#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>
#import <Cocoa/Cocoa.h>

typedef struct
{
    NSString* AXTitle;
    NSString* AXSubrole;
    NSRect Rect;
    NSNumber* AXIsApplicationRunning;
    pid_t PID;

} AXUIElement;

bool AXGetElementAtPosition(AXUIElementRef sysWide, float x, float y, AXUIElement* outputPtr)
{
    AXUIElement output;

    // This handle will contain whatever we are hovering over.
    AXUIElementRef handle;

    // Check to see what it is
    bool success = AXUIElementCopyElementAtPosition(sysWide, x, y, &handle) == 0;

    AXValueRef value;
    NSRect rect;

    // Check to see if this found something, if not, return undefined.
    if (success)
    {
        AXUIElementCopyAttributeValue(handle, kAXSubroleAttribute, (CFTypeRef*) &output.AXSubrole);

        // Get the size of the handle
        AXUIElementCopyAttributeValue(handle, kAXSizeAttribute, (CFTypeRef*) &value);
        AXValueGetValue(value, kAXValueCGSizeType, (void*) &rect.size);

        // Get the position of the handle
        AXUIElementCopyAttributeValue(handle, kAXPositionAttribute, (CFTypeRef*) &value);
        AXValueGetValue(value, kAXValueCGPointType, (void*) &rect.origin);

        // Get the title of the handle
        AXUIElementCopyAttributeValue(handle, kAXTitleAttribute, (CFTypeRef*) &output.AXTitle);

        // Get the running status of the handle
        AXUIElementCopyAttributeValue(handle, kAXIsApplicationRunningAttribute, (CFTypeRef*) &output.AXIsApplicationRunning);

        // Get PID of the handle
        AXUIElementGetPid(handle, &output.PID);

        output.Rect = rect;

        *outputPtr = output;

        return success;
    }

    return false;
}

AXUIElementRef AXUIGetApplicationAccessibilityElement(int pid)
{
    return AXUIElementCreateApplication(pid);
}

bool GetWindowListForApplication(AXUIElementRef app, CFArrayRef* windowsList)
{
    return AXUIElementCopyAttributeValue(app, kAXWindowsAttribute, (CFTypeRef*) windowsList) == 0;
}

bool MinimizeAllWindowsForApplicationDirect(AXUIElementRef app)
{
    // https://stackoverflow.com/questions/4231110/how-can-i-move-resize-windows-programmatically-from-another-application

    CFArrayRef windowsList;

    if (app != NULL && GetWindowListForApplication(app, &windowsList))
    {
        CFBooleanRef value = kCFBooleanTrue;

        for (int i = 0; i < CFArrayGetCount(windowsList); i++)
        {
            AXUIElementRef window = CFArrayGetValueAtIndex(windowsList, i);

            AXUIElementSetAttributeValue(window, kAXMinimizedAttribute, value);
        }

        CFRelease(windowsList);

        return true;
    }

    return false;
}

bool MinimizeAllWindowsForApplication(int pid)
{
    AXUIElementRef app = AXUIElementCreateApplication(pid);

    bool success = MinimizeAllWindowsForApplicationDirect(app);

    CFRelease(app);

    return success;
}

bool ApplicationAllWindowsAreMinimizedDirect(AXUIElementRef app, bool* areMinimized)
{
    CFArrayRef windowsList;

    bool areMinimizedLocal = true;

    if (app != NULL && GetWindowListForApplication(app, &windowsList))
    {
        CFBooleanRef value;

        for (int i = 0; i < CFArrayGetCount(windowsList); i++)
        {
            AXUIElementRef window = CFArrayGetValueAtIndex(windowsList, i);

            AXUIElementCopyAttributeValue(window, kAXMinimizedAttribute, (CFTypeRef*) &value);

            NSString* title;

            AXUIElementCopyAttributeValue(window, kAXTitleAttribute, (CFTypeRef*) &title);

            //TODO: Optimize detecting "ghost" window
            if (title != NULL) // Finder has a "ghost" window for some reason, causing false negatives
            {
                CFRelease(title);

                areMinimizedLocal = CFBooleanGetValue(value);

                if (areMinimizedLocal)
                {
                    continue;
                }

                break;
            }
        }

        *areMinimized = areMinimizedLocal;

        CFRelease(value);
        CFRelease(windowsList);

        return true;
    }

    return false;
}

bool ApplicationAllWindowsAreMinimized(int pid, bool* areMinimized)
{
    AXUIElementRef app = AXUIElementCreateApplication(pid);

    bool success = ApplicationAllWindowsAreMinimizedDirect(app, areMinimized);

    CFRelease(app);

    return success;
}

#define kAXFullScreenAttribute CFSTR("AXFullScreen")

bool ApplicationFocusedWindowIsFullScreenDirect(AXUIElementRef app)
{
    AXUIElementRef window;

    bool success = AXUIElementCopyAttributeValue(app, kAXFocusedWindowAttribute, (CFTypeRef*) &window) == 0;

    if (success)
    {
        CFBooleanRef value;

        bool isFullScreen;

        success = AXUIElementCopyAttributeValue(window, kAXFullScreenAttribute, (CFTypeRef*) &value) == 0;

        if (success)
        {
            isFullScreen = CFBooleanGetValue(value);

            CFRelease(value);
        }

        else
        {
            isFullScreen = false;
        }

        CFRelease(window);

        return isFullScreen;
    }

    return false;
}

bool ApplicationFocusedWindowIsFullScreen(int pid)
{
    AXUIElementRef app = AXUIElementCreateApplication(pid);

    bool isFullScreen = ApplicationFocusedWindowIsFullScreenDirect(app);

    CFRelease(app);

    return isFullScreen;
}

int64_t CGEventGetIntegerValueFieldWrapper(CGEventRef event, CGEventField field)
{
    return CGEventGetIntegerValueField(event, field);
}

bool CloseWindowDirect(AXUIElementRef window)
{
    AXUIElementRef closeButton;

    if (AXUIElementCopyAttributeValue(window, kAXCloseButtonAttribute, (CFTypeRef*) &closeButton) == 0)
    {
        bool success = AXUIElementPerformAction(closeButton, kAXPressAction) == 0;

        CFRelease(closeButton);

        return success;
    }

    return false;
}

bool ApplicationCloseFocusedWindowDirect(AXUIElementRef app)
{
    AXUIElementRef window;

    bool success = AXUIElementCopyAttributeValue(app, kAXFocusedWindowAttribute, (CFTypeRef*) &window) == 0;

    if (success)
    {
        success = CloseWindowDirect(window);

        CFRelease(window);

        return success;
    }

    return false;
}

bool ApplicationCloseFocusedWindow(int pid)
{
    AXUIElementRef app = AXUIElementCreateApplication(pid);

    bool success = ApplicationCloseFocusedWindowDirect(app);

    CFRelease(app);

    return success;
}

typedef struct
{
    AXUIElement Base;
    AXUIElementRef Handle;
} AXUIElementRaw;

bool AXGetElementAtPositionRaw(AXUIElementRef sysWide, float x, float y, AXUIElementRaw* outputPtr)
{
    AXUIElementRaw output;
    AXUIElement base;

    // This handle will contain whatever we are hovering over.
    AXUIElementRef handle;

    // Check to see what it is
    bool success = AXUIElementCopyElementAtPosition(sysWide, x, y, &handle) == 0;

    AXValueRef value;
    NSRect rect;

    // Check to see if this found something, if not, return undefined.
    if (success)
    {
        output.Handle = handle;

        AXUIElementCopyAttributeValue(handle, kAXSubroleAttribute, (CFTypeRef*) &base.AXSubrole);

        // Get the size of the handle
        AXUIElementCopyAttributeValue(handle, kAXSizeAttribute, (CFTypeRef*) &value);
        AXValueGetValue(value, kAXValueCGSizeType, (void*) &rect.size);

        // Get the position of the handle
        AXUIElementCopyAttributeValue(handle, kAXPositionAttribute, (CFTypeRef*) &value);
        AXValueGetValue(value, kAXValueCGPointType, (void*) &rect.origin);

        // Get the title of the handle
        AXUIElementCopyAttributeValue(handle, kAXTitleAttribute, (CFTypeRef*) &base.AXTitle);

        // Get the running status of the handle
        AXUIElementCopyAttributeValue(handle, kAXIsApplicationRunningAttribute, (CFTypeRef*) &base.AXIsApplicationRunning);

        // Get PID of the handle
        AXUIElementGetPid(handle, &base.PID);

        base.Rect = rect;

        output.Base = base;

        *outputPtr = output;

        return success;
    }

    return false;
}

// TODO: Make APIs for closing all windows of given application

//bool ApplicationCloseAllWindowsDirect(AXUIElementRef app)
//{
//    AXUIElementRef app = AXUIElementCreateApplication(pid);
//
//    bool isFullScreen = ApplicationFocusedWindowIsFullScreenDirect(app);
//
//    CFRelease(app);
//
//    return isFullScreen;
//}
//
//bool ApplicationCloseAllWindows(int pid)
//{
//    AXUIElementRef app = AXUIElementCreateApplication(pid);
//
//    bool isFullScreen = ApplicationFocusedWindowIsFullScreenDirect(app);
//
//    CFRelease(app);
//
//    return isFullScreen;
//}