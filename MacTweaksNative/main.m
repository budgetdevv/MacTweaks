#import <AppKit/AppKit.h>
#import <Cocoa/Cocoa.h>
#import <Foundation/Foundation.h>
#include <IOKit/IOKitLib.h>
#include <IOKit/graphics/IOGraphicsLib.h>
#include "brightness.h"

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

bool GetWindowListForApplicationDirect(AXUIElementRef app, CFArrayRef* windowsList)
{
    return AXUIElementCopyAttributeValue(app, kAXWindowsAttribute, (CFTypeRef*) windowsList) == 0;
}

bool GetWindowListForApplication(pid_t pid, CFArrayRef* windowsList)
{
    AXUIElementRef app = AXUIElementCreateApplication(pid);

    bool success = GetWindowListForApplicationDirect(app, windowsList);

    CFRelease(app);

    return success;
}

bool MinimizeAllWindowsForApplicationDirect(AXUIElementRef app)
{
    // https://stackoverflow.com/questions/4231110/how-can-i-move-resize-windows-programmatically-from-another-application

    CFArrayRef windowsList;

    if (app != NULL && GetWindowListForApplicationDirect(app, &windowsList))
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

    if (app != NULL && GetWindowListForApplicationDirect(app, &windowsList))
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

void CGEventSetIntegerValueFieldWrapper(CGEventRef event, CGEventField field, int64_t value)
{
    CGEventSetIntegerValueField(event, field, value);
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

bool WindowToggleMinimize(AXUIElementRef window)
{
    CFBooleanRef value;

    AXUIElementCopyAttributeValue(window, kAXMinimizedAttribute, (CFTypeRef*) &value);

    bool isMinimized = !CFBooleanGetValue(value);

    value = isMinimized ? kCFBooleanTrue : kCFBooleanFalse;

    AXUIElementSetAttributeValue(window, kAXMinimizedAttribute, value);

    return isMinimized;
}

bool GetAllDisplaysBrightness(float* result, int* count)
{
    CGDirectDisplayID display[kMaxDisplays];

    CGDisplayCount numDisplays;

    CGDisplayErr error = CGGetOnlineDisplayList(kMaxDisplays, display, &numDisplays);

    if (error == kCGErrorSuccess)
    {
        int successCount = 0;

        for (CGDisplayCount i = 0; i < numDisplays; ++i)
        {
            CGDirectDisplayID currentDisplay = display[i];
            CGDisplayModeRef mode = CGDisplayCopyDisplayMode(currentDisplay);

            if (mode == NULL)
            {
                continue;
            }

            successCount++;

            CGDisplayModeRelease(mode);

            io_service_t service = CGDisplayGetIOServicePort(currentDisplay);

            GetBrightness(currentDisplay, service, result);

            result++;
        }

        if (successCount != 0)
        {
            return true;
        }
    }

    return false;
}

bool SetAllDisplaysBrightness(float brightnessLevel, int* count)
{
    CGDirectDisplayID display[kMaxDisplays];

    CGDisplayCount numDisplays;

    CGDisplayErr error = CGGetOnlineDisplayList(kMaxDisplays, display, &numDisplays);

    if (error == kCGErrorSuccess)
    {
        int successCount = 0;

        for (CGDisplayCount i = 0; i < numDisplays; ++i)
        {
            CGDirectDisplayID currentDisplay = display[i];
            CGDisplayModeRef mode = CGDisplayCopyDisplayMode(currentDisplay);

            if (mode == NULL)
            {
                continue;
            }

            successCount++;

            CGDisplayModeRelease(mode);

            io_service_t service = CGDisplayGetIOServicePort(currentDisplay);

            SetBrightness(currentDisplay, service, brightnessLevel);
        }

        if (successCount != 0)
        {
            return true;
        }
    }

    return false;
}

bool GetDisplayBrightness(uint32_t display_id, float* result)
{
    CGDirectDisplayID display[kMaxDisplays];

    CGDisplayCount numDisplays;

    CGDisplayErr error = CGGetOnlineDisplayList(kMaxDisplays, display, &numDisplays);

    if (error == kCGErrorSuccess)
    {
        CGDisplayModeRef mode = CGDisplayCopyDisplayMode(display_id);

        if (mode == NULL)
        {
            printf("Fuck");
            goto Fail;
        }

        CGDisplayModeRelease(mode);

        io_service_t service = CGDisplayGetIOServicePort(display_id);

        GetBrightness(display_id, service, result);

        return true;
    }

    Fail:
    return false;
}

bool GetMainDisplayBrightness(float* result)
{
    return GetDisplayBrightness(CGMainDisplayID(), result);
}

bool SetDisplayBrightness(uint32_t display_id, float brightnessLevel)
{
    CGDirectDisplayID display[kMaxDisplays];

    CGDisplayCount numDisplays;

    CGDisplayErr error = CGGetOnlineDisplayList(kMaxDisplays, display, &numDisplays);

    if (error == kCGErrorSuccess)
    {
        CGDisplayModeRef mode = CGDisplayCopyDisplayMode(display_id);

        if (mode == NULL)
        {
            goto Fail;
        }

        CGDisplayModeRelease(mode);

        io_service_t service = CGDisplayGetIOServicePort(display_id);

        SetBrightness(display_id, service, brightnessLevel);

        return true;
    }

    Fail:
    return false;
}

bool SetMainDisplayBrightness(float brightnessLevel)
{
    return SetDisplayBrightness(CGMainDisplayID(), brightnessLevel);
}

bool GetMenuBarSize(pid_t pid, CGSize* size)
{
    AXUIElementRef app = AXUIElementCreateApplication(pid);

    bool success = app != NULL;

    if (success)
    {
        CFArrayRef children;
        // Get the children of the application element
        success = AXUIElementCopyAttributeValues(app, kAXChildrenAttribute, 0, 100, &children) == kAXErrorSuccess;

        if (success)
        {
            // Loop through the children elements
            for (CFIndex i = 0; i < CFArrayGetCount(children); i++)
            {
                AXUIElementRef child = CFArrayGetValueAtIndex(children, i);
                CFTypeRef role;

                // Get the role of the child element
                success = AXUIElementCopyAttributeValue(child, kAXRoleAttribute, &role) == kAXErrorSuccess;

                if (success)
                {
                    bool isMenuBar = CFEqual(role, kAXMenuBarRole);

                    CFRelease(role);

                    // Check if the role is menu bar
                    if (isMenuBar)
                    {
                        CFTypeRef sizeRef;

                        success = AXUIElementCopyAttributeValue(child, kAXSizeAttribute, &sizeRef) == kAXErrorSuccess;

                        if (success)
                        {
                            AXValueGetValue(sizeRef, kAXValueCGSizeType, size);

                            CFRelease(sizeRef);

                            break;
                        }

                    }
                }
            }
            // Release the children array
            CFRelease(children);
        }
        // Release the application element
        CFRelease(app);

        return success;
    }
}



