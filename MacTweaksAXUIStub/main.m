#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>
#import <Cocoa/Cocoa.h>

typedef struct
{
    NSString* AXTitle;
    NSString* AXSubrole;
    NSRect Rect;
    NSNumber* AXIsApplicationRunning;
} AXUIElement;

bool AXGetElementAtPosition(AXUIElementRef sysWide, float x, float y, AXUIElement* outputPtr)
{
    AXUIElement output;

    // This element will contain whatever we are hovering over.
    AXUIElementRef element = NULL;

    // Check to see what it is
    int err = AXUIElementCopyElementAtPosition(sysWide, x, y, &element);

    AXValueRef value;
    NSRect rect;

    // Check to see if this found something, if not, return undefined.
    if (err == kAXErrorSuccess && AXUIElementCopyAttributeValue(element, kAXSubroleAttribute, (CFTypeRef*) &output.AXSubrole) == 0)
    {
        // Get the size of the element
        AXUIElementCopyAttributeValue(element, kAXSizeAttribute, (CFTypeRef*) &value);
        AXValueGetValue(value, kAXValueCGSizeType, (void*) &rect.size);

        // Get the position of the element
        AXUIElementCopyAttributeValue(element, kAXPositionAttribute, (CFTypeRef*) &value);
        AXValueGetValue(value, kAXValueCGPointType, (void*) &rect.origin);

        // Get the title of the element
        AXUIElementCopyAttributeValue(element, kAXTitleAttribute, (CFTypeRef*) &output.AXTitle);

        // Get the running status of the element
        AXUIElementCopyAttributeValue(element, kAXIsApplicationRunningAttribute, (CFTypeRef*) &output.AXIsApplicationRunning);

        output.Rect = rect;

        CFRelease(value);

        *outputPtr = output;

        return true;
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

bool ApplicationAllWindowsAreMinimizedDirect(AXUIElementRef app)
{
    CFArrayRef windowsList;

    if (app != NULL && GetWindowListForApplication(app, &windowsList))
    {
        CFBooleanRef value;

        bool areMinimized;

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

                areMinimized = CFBooleanGetValue(value);

                if (areMinimized)
                {
                    continue;
                }

                break;
            }
        }

        CFRelease(value);
        CFRelease(windowsList);

        return areMinimized;
    }

    return false;
}

bool ApplicationAllWindowsAreMinimized(int pid)
{
    AXUIElementRef app = AXUIElementCreateApplication(pid);

    bool areMinimized = ApplicationAllWindowsAreMinimizedDirect(app);

    CFRelease(app);

    return areMinimized;
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